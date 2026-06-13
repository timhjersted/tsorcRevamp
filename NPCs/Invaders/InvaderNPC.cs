using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.AI;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>
    /// Abstract base for human-sized invader enemies rendered via the player draw pipeline.
    ///
    /// WEAPON NOTES
    /// ─────────────────────────────────────────────────────────────────
    /// • The puppet Player drives arm animation (inventory[0] holds weapon, noUseGraphic=true).
    ///   The actual weapon sprite is drawn manually so we control its exact screen position.
    /// • Hand position is calculated from the current swing/stab arc so the sprite
    ///   tracks the animated arm naturally.
    ///
    /// PROJECTILE NOTES
    /// ─────────────────────────────────────────────────────────────────
    /// • Any projectile fired BY an invader must be hostile=true, friendly=false.
    ///   Player-friendly projectiles will damage the invader itself and not the player.
    ///   Create a Projectiles/Enemy/ copy for every weapon the invader uses at range.
    /// </summary>
    public abstract class InvaderNPC : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Invaders/InvaderPlaceholder";

        // ── Invasion banner ───────────────────────────────────────────────────────
        /// <summary>
        /// The name shown in the "INVADED BY ____" banner when this invader spawns.
        /// Override in each concrete class.  Rendered in uppercase automatically.
        /// </summary>
        protected virtual string InvaderTitle => "UNKNOWN INVADER";

        // ── Layer coordination ────────────────────────────────────────────────────
        /// <summary>
        /// Set to <c>this</c> for the exact duration of a <see cref="Main.PlayerRenderer.DrawPlayer"/>
        /// call so that <see cref="InvaderWeaponDrawLayer"/> knows which invader to draw for.
        /// Null at all other times — the layer is a no-op for normal players.
        /// </summary>
        internal static InvaderNPC DrawingPuppetFor;

        /// <summary>Draw color captured in PreDraw, read by <see cref="InvaderWeaponDrawLayer"/>.</summary>
        internal Color _layerDrawColor;

        // ── Puppet ────────────────────────────────────────────────────────────────
        private Player _puppet;
        private Item _meleeItemCache;
        private Item _rangedItemCache;
        private int  _cachedMeleeType  = -1;
        private int  _cachedRangedType = -1;

        // ── Loadout ───────────────────────────────────────────────────────────────
        protected abstract int HeadArmorItemType  { get; }
        protected abstract int BodyArmorItemType  { get; }
        protected abstract int LegsArmorItemType  { get; }
        protected abstract int MeleeWeaponItemType  { get; }   // -1 = none
        protected abstract int RangedWeaponItemType { get; }   // -1 = none
        protected virtual  int MagicWeaponItemType  => -1;
        /// <summary>Item type of the polearm/spear used for the SpearAttack phase.  -1 = no spear attack.</summary>
        protected virtual  int SpearWeaponItemType  => -1;
        protected abstract int MeleeDamage  { get; }
        protected abstract int RangedDamage { get; }
        protected virtual  int SpearDamage  => 0;
        protected virtual  int MagicDamage  => 0;

        // ── Tuning ────────────────────────────────────────────────────────────────
        protected virtual float TopSpeed       => 2.5f;
        protected virtual float Acceleration   => 0.09f;
        protected virtual float BrakingPower   => 0.22f;
        protected virtual float RunDistance    => 420f;
        protected virtual float RunSpeedMult   => 1.35f;
        protected virtual int   TeleportTelegraphTicks => 140;
        protected virtual int   TeleportDustTypeId     => DustID.Smoke;
        protected virtual Color TeleportDustTint       => Color.White;
        protected virtual float TeleportDustScale      => 0.8f;
        protected virtual int   TeleportDustCount      => 20;

        protected virtual float MeleeRange     => 80f;
        protected virtual float StabRange      => 160f;   // distance at which stab is preferred
        protected virtual float RangedRange    => 520f;
        protected virtual float MinRangedRange => 200f;   // won't use ranged if closer than this

        // Telegraph minimum: 30 ticks = 0.5 s.  Long enough for the player to read the
        // wind-up arc (sword raises, holds at apex, then swings).  Heavier attacks override
        // upward; lighter attacks should not go below this floor.
        protected virtual int MeleeTelegraphTicks  => 35;
        // MeleeAttackTicks matches WeaponAnimMax so the full swing arc completes
        // within the attack phase rather than bleeding into recovery.
        protected virtual int MeleeAttackTicks     => WeaponAnimMax; // 22 ticks = one full swing
        protected virtual int MeleeRecoveryTicks   => 25;

        protected virtual int StabTelegraphTicks   => 38;
        protected virtual int StabAttackTicks      => 8;
        protected virtual int StabRecoveryTicks    => 32;

        protected virtual int RangedTelegraphTicks => 38;
        protected virtual int RangedAttackTicks    => 10;
        protected virtual int RangedRecoveryTicks  => 55;

        /// How many ticks must pass after a ranged burst before ranged is allowed again.
        /// Gives the invader time to close distance and fight in melee first.
        protected virtual int RangedCooldownAfterUse => 300;
        /// How many ticks must pass after a stab before another stab is allowed.
        /// Prevents instant re-stab when the player dodgerolls through.
        protected virtual int StabCooldownAfterUse   => 120;
        /// Velocity multiplier applied during the stab lunge (StabAttack phase).
        /// Base × TopSpeed px/frame.  Lower values = shorter lunge; higher = more aggressive dash.
        protected virtual float StabLungeSpeedMult => 2.0f;
        /// Maximum number of ranged shots per engagement burst (rolls 1–N each time).
        protected virtual int MaxRangedBurst         => 3;
        protected virtual int SingleRangedBurstChance => 0;

        protected virtual bool CanStab => false;

        // ── Ranged animation style ────────────────────────────────────────────────
        /// <summary>
        /// Controls body-frame poses and weapon-rotation arc during the Ranged phases.
        /// Override per subclass to match the weapon being used.
        ///   Throw    — overhand throw (shurikens, knives): arm lifts up then swings forward.
        ///   Crossbow — horizontal aim: arm extends forward at Use3, quick click/fire (useAnimation ≈ 22).
        ///   Bow      — diagonal draw: arm rises from Use3 → Use1 over the telegraph, snaps forward at release (useAnimation ≈ 60).
        /// </summary>
        protected enum RangedStyle { Throw, Crossbow, Bow }
        /// <summary>Animation style for the primary ranged weapon.  Override per subclass.</summary>
        protected virtual RangedStyle RangedAnimStyle => RangedStyle.Throw;

        // ── Secondary ranged weapon (optional) ───────────────────────────────────
        // When set, each burst randomly picks primary OR secondary based on range bands and chance.
        // Typical use: close/medium throw (primary) + long-range crossbow or bow (secondary).
        protected virtual int         SecondaryRangedWeaponItemType  => -1;    // -1 = no secondary
        protected virtual int         SecondaryRangedDamage          => 0;
        protected virtual RangedStyle SecondaryRangedAnimStyle       => RangedStyle.Crossbow;
        protected virtual float       SecondaryRangedRange           => 700f;
        protected virtual float       SecondaryRangedMinRange        => 300f;
        protected virtual int         SecondaryRangedTelegraphTicks  => 25;
        protected virtual int         SecondaryRangedAttackTicks     => 10;
        protected virtual int         SecondaryRangedRecoveryTicks   => 50;
        protected virtual int         SecondaryRangedCooldownAfterUse => 300;
        /// <summary>Max shots in a secondary burst (rolls 1–N).  Default 1 — suits crossbow/sniper.</summary>
        protected virtual int         SecondaryMaxRangedBurst        => 1;
        /// <summary>Chance (0–100) of using secondary when both primary and secondary are in range.</summary>
        protected virtual int         SecondaryRangedChance          => 50;
        /// <summary>Chance (0–100) of planting feet during secondary ranged attack.</summary>
        protected virtual int         SecondaryStandingRangedChance  => 70;
        protected virtual Color       SecondaryRangedFlashColor      => Color.White;

        // ── Secondary ranged burst patterns (optional) ────────────────────────────
        // When non-null, replaces the simple SecondaryMaxRangedBurst random-shot system.
        // Each entry is an array of inter-shot pause durations (ticks).
        //   total shots in that pattern = pauses.Length + 1
        //   e.g. new int[]{30,60} → 3 shots: fire, wait 30, fire, wait 60, fire.
        //        new int[]{}      → single shot.
        // Leave as null (default) to keep the classic random-burst system.
        protected virtual int[][]  SecondaryRangedBurstPatterns        => null;
        /// <summary>Extra telegraph ticks added to the base secondary telegraph for each pattern.  Indexed by pattern.</summary>
        protected virtual int[]    SecondaryRangedBurstTelegraphExtras  => null;
        /// <summary>Flash color used for each burst pattern (overrides SecondaryRangedFlashColor).  Indexed by pattern.</summary>
        protected virtual Color[]  SecondaryRangedBurstFlashColors      => null;
        /// <summary>Relative selection weights for each burst pattern.  null = uniform random.</summary>
        protected virtual int[]    SecondaryRangedBurstChances          => null;

        // ── Spear / polearm ───────────────────────────────────────────────────────
        protected virtual float SpearRange          => 200f;   // max distance for spear poke
        protected virtual int   SpearTelegraphTicks => 35;
        protected virtual int   SpearAttackTicks    => 12;
        protected virtual int   SpearRecoveryTicks  => 30;
        protected virtual int   SpearCooldownAfterUse => 90;
        /// <summary>
        /// Forward velocity during SpearAttack, as a multiplier of TopSpeed.
        /// 0 = fully stationary poke; 0.5–1.0 = short hop; > 1.0 = short lunge.
        /// </summary>
        protected virtual float SpearPushSpeedMult  => 0f;

        // ── Magic / spellcast ─────────────────────────────────────────────────────
        protected virtual float MagicRange          => 700f;
        protected virtual float MinMagicRange       => 0f;
        protected virtual int   MagicTelegraphTicks => 50;
        protected virtual int   MagicAttackTicks    => 15;
        protected virtual int   MagicRecoveryTicks  => 70;
        protected virtual int   MagicCooldownAfterUse => 300;
        /// <summary>Flash color for the spear-poke and magic-cast telegraphs.</summary>
        protected virtual Color SpearTelegraphFlashColor => Color.LightYellow;
        protected virtual Color MagicTelegraphFlashColor => Color.MediumPurple;

        // ── Estus healing ─────────────────────────────────────────────────────────
        /// <summary>How many estus drinks the invader starts with.</summary>
        protected virtual int   EstusChargesMax         => 10;
        /// <summary>Fraction of max HP restored per drink.</summary>
        protected virtual float EstusHealFraction       => 0.20f;
        /// <summary>Ticks of cooldown between any two heals (prevents spam).</summary>
        protected virtual int   HealCooldownTicks       => 300;
        /// <summary>Ticks the drinking animation lasts after HP is restored.</summary>
        protected virtual int   HealAnimationTicks      => 70;
        /// <summary>
        /// Accumulated recent damage (as a fraction of max HP) that triggers an emergency heal.
        /// E.g. 0.15 = trigger heal if more than 15 % max HP taken in the recent burst window.
        /// </summary>
        protected virtual float RecentDamageThreshold   => 0.18f;
        /// <summary>How fast recent-damage memory decays per tick (fraction of max HP).</summary>
        protected virtual float RecentDamageDecayRate   => 0.0025f;
        /// <summary>Minimum pixel distance from the player before the invader stops fleeing and drinks.</summary>
        protected virtual float FleeToHealDistance      => 40 * 16f;  // 40 tiles
        /// <summary>Max ticks spent fleeing before drinking anyway (in case player is chasing).</summary>
        protected virtual int   FleeToHealMaxTicks      => 150;

        // ── Navigation (SF4 A* pathfinding; jump tuning below) ───────────────────
        protected virtual float InvaderJumpPower      => 10f;
        protected virtual float InvaderJumpBoost      => 6f;
        protected virtual bool  InvaderCanDoubleJump  => false;
        protected virtual float InvaderDoubleJumpPower => 6f;

        /// <summary>
        /// Run the ground-movement AI for this invader.  Defaults to the project's classic
        /// FighterAI (supports teleport / dodgeroll / pounce).  Subclasses can override to
        /// swap in a different driver — e.g. <c>SmartFighter4AI.Run</c> — without touching
        /// the base flow.  Called once per tick from <see cref="AI"/> while grounded.
        /// </summary>
        protected virtual void RunMovementAI(float speedMult)
        {
            tsorcRevampAIs.FighterAI(NPC,
                topSpeed:     TopSpeed * speedMult,
                acceleration: Acceleration,
                brakingPower: BrakingPower,
                canTeleport:  true,
                doorBreakingDamage: 4,
                canDodgeroll: Phase != AttackPhase.FleeToHeal && Phase != AttackPhase.Healing,
                canPounce:    Phase == AttackPhase.Idle);
        }

        // ── State machine ─────────────────────────────────────────────────────────
        protected enum AttackPhase
        {
            Idle,
            MeleeTelegraph, MeleeAttack, MeleeRecovery,
            StabTelegraph,  StabAttack,  StabRecovery,
            RangedTelegraph, RangedAttack, RangedRecovery,
            SpearTelegraph,  SpearAttack,  SpearRecovery,
            MagicTelegraph,  MagicAttack,  MagicRecovery,
            /// <summary>
            /// Inter-shot pause during a crossbow burst pattern.
            /// The invader holds horizontal aim and waits for the timer; the next shot fires
            /// automatically when it expires (no new telegraph phase).
            /// </summary>
            CrossbowBurstPause,
            /// <summary>
            /// Slow walk toward player after a recovery phase; no attacks allowed.
            /// Gives variety: the invader advances calmly before re-engaging.
            /// </summary>
            CasualStroll,
            /// <summary>
            /// Sprint away from the player until far enough to drink safely, then enter Healing.
            /// Interrupted if the invader can't reach safe distance within <see cref="FleeToHealMaxTicks"/>.
            /// </summary>
            FleeToHeal,
            /// <summary>
            /// Stand still and play the estus-drinking animation.  HP is restored at the start
            /// of this phase.  Transitions back to Idle when the animation finishes.
            /// </summary>
            Healing,
            /// <summary>Initial telegraph for a multi-step melee combo (step 0).  Fires the
            /// combo's initial flash, locks direction for the entire combo duration.</summary>
            MeleeComboTelegraph,
            /// <summary>Active hitbox frame of the current combo step.  Applies per-step
            /// forward push and damage scaling.</summary>
            MeleeComboAttack,
            /// <summary>Inter-step pause within a combo.  Next step's attack fires when timer expires —
            /// no per-step telegraph or flash.</summary>
            MeleeComboPause,
            /// <summary>Recovery after the last step of a combo.  Applies per-combo cooldown.</summary>
            MeleeComboRecovery,
        }
        protected AttackPhase Phase = AttackPhase.Idle;
        protected int PhaseTimer;

        // Counts down; ranged is blocked while > 0
        protected int _rangedCooldown;
        // Counts down; stab is blocked while > 0 (prevents instant re-stab after dodgeroll)
        protected int _stabCooldown;
        // Counts down; spear poke is blocked while > 0
        protected int _spearCooldown;
        // Counts down; magic cast is blocked while > 0
        protected int _magicCooldown;
        // Counts down; secondary ranged is blocked while > 0
        private int _secondaryRangedCooldown;
        // Counts down; any heal is blocked while > 0
        private int _healCooldown;

        // ── Active ranged burst context ───────────────────────────────────────────
        // Set at the start of each burst (primary OR secondary) so all phases in the
        // same burst use consistent timing, display, and animation — without having to
        // re-query virtual properties on every tick.
        private bool        _usingSecondaryRanged;
        private RangedStyle _activeRangedStyle;
        private int         _activeRangedItemType;
        private Color       _activeRangedFlashColor;
        private int         _activeRangedTelegraphTicks;
        private int         _activeRangedAttackTicks;
        private int         _activeRangedRecoveryTicks;

        /// <summary>
        /// True while the current ranged burst is using the secondary weapon.
        /// Read this in <see cref="DoRangedAttack"/> to decide which projectile to fire.
        /// </summary>
        protected bool IsSecondaryRangedActive => _usingSecondaryRanged;

        // ── Healing state ─────────────────────────────────────────────────────────
        // -1 = uninitialized (set to EstusChargesMax on first AI tick)
        protected int  _estusCharges      = -1;
        // Rolling damage accumulator (fraction of max HP); decays each tick.
        private float  _recentDamage;
        // One-shot flags: true once healed at that threshold; reset when HP rises back above it.
        private bool   _halfHpHealed;
        private bool   _quarterHpHealed;

        // ── Behaviour variety ─────────────────────────────────────────────────────
        /// <summary>Chance (0–100) of entering a casual stroll after any recovery phase.</summary>
        protected virtual int   CasualStrollChance    => 30;
        protected virtual int   CasualStrollMinTicks  => 60;   // 1 second
        protected virtual int   CasualStrollMaxTicks  => 120;  // 2 seconds
        protected virtual float CasualStrollSpeedMult => 0.35f;
        /// <summary>Chance (0–100) of planting feet and firing ranged vs. slow-approach firing.</summary>
        protected virtual int   StandingRangedChance  => 33;

        // ── Telegraph flash colors ────────────────────────────────────────────────
        /// <summary>Color of the ring-flash VFX spawned ~25 frames before a melee or stab attack.</summary>
        protected virtual Color MeleeTelegraphFlashColor  => Color.White;
        /// <summary>Color of the ring-flash VFX spawned ~25 frames before a ranged attack.</summary>
        protected virtual Color RangedTelegraphFlashColor => Color.White;

        private bool _standingShot;        // decided at start of each ranged burst
        private int  _rangedShotsRemaining; // shots left in current burst (classic mode only)
        private bool _weaponVisible;        // true only during telegraphs and attack frames

        // Crossbow burst-pattern state (set at burst start; null = classic multi-shot mode).
        // Each element is the pause duration (ticks) before the NEXT shot.
        // Total shots in the pattern = _interShotPauses.Length + 1.
        private int[] _interShotPauses;
        private int   _interShotPauseIndex;

        // ── Weapon visual ─────────────────────────────────────────────────────────
        private int   _heldItemType;
        private float _weaponRotation;    // direction-neutral draw angle
        private int   _weaponAnim;        // counts down WeaponAnimMax→0 during swing
        private const int   WeaponAnimMax  = 22;
        /// <summary>
        /// Resting hold angle when not attacking (≈ −17° — arm slightly raised, weapon pointing
        /// diagonally forward). The weapon smoothly eases to this during walk / jump / idle.
        /// </summary>
        private const float HoldRotation  = -0.30f;

        private bool IsWeaponVisiblePhase =>
            Phase == AttackPhase.MeleeTelegraph || Phase == AttackPhase.MeleeAttack ||
            Phase == AttackPhase.StabTelegraph  || Phase == AttackPhase.StabAttack  ||
            Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack ||
            Phase == AttackPhase.CrossbowBurstPause ||
            Phase == AttackPhase.SpearTelegraph  || Phase == AttackPhase.SpearAttack  ||
            Phase == AttackPhase.MagicTelegraph  || Phase == AttackPhase.MagicAttack  ||
            Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack ||
            Phase == AttackPhase.MeleeComboPause ||
            (_flight != null && _flight.IsDiving && MeleeWeaponItemType >= 0);

        private bool IsMeleeComboPhase =>
            Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack ||
            Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery;

        private bool IsWeaponPosePhase => IsWeaponVisiblePhase;

        private static bool IsClimbableRopeTile(Tile tile)
        {
            return tile.HasTile &&
                   (tile.TileType == TileID.Rope ||
                    tile.TileType == TileID.Chain ||
                    tile.TileType == TileID.VineRope ||
                    tile.TileType == TileID.SilkRope ||
                    tile.TileType == TileID.WebRope);
        }

        private bool IsTouchingClimbableRope()
        {
            int left = (int)(NPC.position.X / 16f);
            int right = (int)((NPC.position.X + NPC.width - 1f) / 16f);
            int top = (int)(NPC.position.Y / 16f);
            int bottom = (int)((NPC.position.Y + NPC.height - 1f) / 16f);

            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    if (IsClimbableRopeTile(Framing.GetTileSafely(x, y)))
                        return true;
                }
            }

            return false;
        }

        // ── Direction hold (anti-bounce) ──────────────────────────────────────────
        /// <summary>
        /// Ticks remaining during which the current facing direction is locked.
        /// Prevents rapid flip-flopping when the player dodgerolls past the invader —
        /// FighterAI would otherwise flip direction every single tick.
        /// </summary>
        private int _directionHoldTicks;

        // ── Wall-blocked navigation ───────────────────────────────────────────────
        /// <summary>Ticks elapsed while stuck (no LOS, near-zero velocity, free phase).
        /// Drives stand-still scan → teleport fallback when blocked by impassable terrain.</summary>
        private int _wallBlockedTimer;
        /// <summary>Set by OnHitByX when struck with no LOS during a wall-scan (> 60 ticks blocked);
        /// triggers a short retreat followed by an emergency teleport.</summary>
        private bool _hitThroughWallFlag;
        /// <summary>When > 0 the invader retreats away from the player; teleport fires on expiry.</summary>
        private int _retreatBeforeTeleportTimer;

        // ── Telegraph flash ───────────────────────────────────────────────────────
        /// <summary>True once the flash VFX for the current telegraph phase has been spawned.</summary>
        private bool _flashFired;

        // ── Melee combo state ────────────────────────────────────────────────────
        /// <summary>Pool of combos for the current melee archetype.  Cached at first use.</summary>
        private MeleeCombo[] _meleeComboPool;
        /// <summary>Pool of combos for the current ranged archetype.  Cached at first use.</summary>
        private RangedCombo[] _rangedComboPool;
        /// <summary>Per-combo cooldown counters (sized to pool length on first use).</summary>
        private int[] _meleeComboCooldowns;
        private int[] _rangedComboCooldowns;
        /// <summary>The combo currently executing (only valid in MeleeCombo* phases).</summary>
        private MeleeCombo _activeMeleeCombo;
        /// <summary>Index of the active combo within its pool.  -1 = no combo active.</summary>
        private int _activeMeleeComboIndex = -1;
        /// <summary>Which step (0-indexed) of the active combo we're currently executing.</summary>
        private int _meleeComboStepIndex;
        /// <summary>Direction locked for the entire active combo.  Player can dodgeroll through
        /// and end up safely behind the swing arc — FighterAI's direction flips are overridden
        /// across all four MeleeCombo* phases.</summary>
        private int _comboLockedDir;

        // ── Ranged combo state ────────────────────────────────────────────────────
        /// <summary>The ranged combo currently executing.</summary>
        private RangedCombo _activeRangedCombo;
        /// <summary>Index of the active ranged combo within its pool.  -1 = not using combo system.</summary>
        private int _activeRangedComboIndex = -1;
        /// <summary>Which shot (0-indexed) we're firing.</summary>
        private int _rangedComboShotIndex;

        // ── Archetype detection (lazy-cached) ─────────────────────────────────────
        private WeaponArchetype? _autoMeleeArchetype;
        private WeaponArchetype? _autoRangedArchetype;

        /// <summary>
        /// Weapon archetype that determines the melee combo pool for this invader.
        /// Auto-detected from the item's useStyle/damage class; override to force a specific archetype.
        /// </summary>
        protected virtual WeaponArchetype MeleeArchetype
        {
            get
            {
                if (_autoMeleeArchetype.HasValue) return _autoMeleeArchetype.Value;
                if (MeleeWeaponItemType <= 0) { _autoMeleeArchetype = WeaponArchetype.None; return WeaponArchetype.None; }
                var probe = new Item();
                probe.SetDefaults(MeleeWeaponItemType);
                var a = WeaponArchetypeTables.DetectMelee(probe);
                _autoMeleeArchetype = a;
                return a;
            }
        }

        /// <summary>
        /// Weapon archetype that determines the ranged combo pool for this invader.
        /// Auto-detected from the primary ranged item; override to force a specific archetype.
        /// </summary>
        protected virtual WeaponArchetype RangedArchetype
        {
            get
            {
                if (_autoRangedArchetype.HasValue) return _autoRangedArchetype.Value;
                if (RangedWeaponItemType <= 0) { _autoRangedArchetype = WeaponArchetype.None; return WeaponArchetype.None; }
                var probe = new Item();
                probe.SetDefaults(RangedWeaponItemType);
                var a = WeaponArchetypeTables.DetectRanged(probe);
                _autoRangedArchetype = a;
                return a;
            }
        }

        /// <summary>Chance (0-100) of preferring a combo over the legacy slash/stab/spear path
        /// when both are available.  Set to 0 to disable melee combos entirely.</summary>
        protected virtual int MeleeComboChance => 65;
        /// <summary>Chance (0-100) of preferring a ranged combo over the legacy random-burst path.
        /// Set to 0 to disable ranged combos entirely.</summary>
        protected virtual int RangedComboChance => 50;

        /// <summary>Multiplier applied to a melee combo's first-step TelegraphTicks at runtime.
        /// Default 1.35 = ~35% longer telegraph windows than the raw data table values, giving
        /// the player more time to read the incoming combo.  Lower for faster, more aggressive
        /// invaders.</summary>
        protected virtual float ComboTelegraphMultiplier => 1.35f;

        /// <summary>Hard floor on a combo's first-step telegraph duration (ticks).  Even after
        /// applying <see cref="ComboTelegraphMultiplier"/>, no combo can start with less than
        /// this many ticks of wind-up — guarantees the player sees the sword-raise / hold-at-apex
        /// arc clearly before damage is delivered.  30 ticks = 0.5 s minimum.</summary>
        protected virtual int MinComboTelegraphTicks => 30;

        /// <summary>Max distance (px) at which a melee combo can begin.  Beyond this, the
        /// invader falls through to ranged/legacy attack paths.  Default = StabRange + 80
        /// covers thrusts + most dash lunges.</summary>
        protected virtual float ComboMaxStartRange => StabRange + 80f;

        // ── Wings / flight ────────────────────────────────────────────────────────
        /// <summary>Master toggle: when true, this invader can take off, hover, dive, and land.
        /// Subclasses can wire this to a static config flag or a ModConfig field.</summary>
        protected virtual bool HasWings => false;

        /// <summary>Vanilla wing item type whose sprite + wingSlot is used for the puppet draw.
        /// Default is AngelWings; subclasses override for thematic fit.</summary>
        protected virtual int WingsAccessoryItemType => ItemID.AngelWings;

        /// <summary>Flight tuning.  Override to customize hover altitude / dive speed / cooldowns.</summary>
        protected virtual EnemyFlightConfig FlightConfig => EnemyFlightConfig.Default;

        /// <summary>Chance per second of triggering a random idle takeoff burst (0-100).
        /// Set to 0 to only fly when tactically necessary (player above, blocked, low HP).</summary>
        protected virtual int RandomTakeoffChance => 8;

        /// <summary>Height differential (player above invader) above which the invader
        /// will preferentially take off rather than try to navigate up on foot.</summary>
        protected virtual float FlightHeightTrigger => 120f;

        /// <summary>HP fraction at or below which the invader gains an aerial-phase preference
        /// (random takeoff chance is amplified, dive attacks more likely).</summary>
        protected virtual float FlightHpEscalationFrac => 0.50f;

        private EnemyFlightController _flight;
        private Item _wingsItemCache;
        private int  _cachedWingsType = -1;
        /// <summary>Cooldown between consecutive aerial-dive hits.  Ticks down each AI frame.</summary>
        private int  _aerialHitCooldown;

        // ── Stab / spear lunge direction locks ───────────────────────────────────
        /// <summary>NPC.direction captured when StabAttack begins. Using this instead of the
        /// live NPC.direction prevents rubber-banding when the player dodgerolls through during
        /// the lunge and FighterAI flips direction toward the new player position.</summary>
        private int _stabLungeDir;
        /// <summary>Same lock for SpearAttack — captures facing at the start of the poke so the
        /// weapon sprite doesn't snap to a new direction if the player moves slightly.</summary>
        private int _spearLungeDir;

        // ── Frame animation ───────────────────────────────────────────────────────
        private float _frameCounter;
        private const int FrameHeight = 56;

        // ─────────────────────────────────────────────────────────────────────────
        // AI
        // ─────────────────────────────────────────────────────────────────────────

        public override void AI()
        {
            // ── First-spawn invasion banner ────────────────────────────────────────
            // localAI[0] is not synced across the network, so every client (and singleplayer)
            // initialises it at 0 independently.  On the very first tick we fire the banner
            // exactly once per NPC instance on each client — no packet required.
            if (Main.netMode != NetmodeID.Server && NPC.localAI[0] == 0f)
            {
                NPC.localAI[0] = 1f;
                tsorcRevamp.LocationBannerText  = "INVADED BY " + InvaderTitle.ToUpperInvariant();
                tsorcRevamp.LocationBannerTimer = tsorcRevamp.LOCATION_BANNER_TOTAL;
            }

            // ── Lazy estus init ────────────────────────────────────────────────────
            if (_estusCharges < 0)
                _estusCharges = EstusChargesMax;

            if (_rangedCooldown          > 0) _rangedCooldown--;
            if (_secondaryRangedCooldown > 0) _secondaryRangedCooldown--;
            if (_stabCooldown            > 0) _stabCooldown--;
            if (_spearCooldown           > 0) _spearCooldown--;
            if (_magicCooldown           > 0) _magicCooldown--;
            if (_healCooldown            > 0) _healCooldown--;
            if (_meleeComboCooldowns != null)
                for (int i = 0; i < _meleeComboCooldowns.Length; i++)
                    if (_meleeComboCooldowns[i] > 0) _meleeComboCooldowns[i]--;
            if (_rangedComboCooldowns != null)
                for (int i = 0; i < _rangedComboCooldowns.Length; i++)
                    if (_rangedComboCooldowns[i] > 0) _rangedComboCooldowns[i]--;
            if (_aerialHitCooldown > 0) _aerialHitCooldown--;

            // Decay recent-damage memory so old hits don't keep triggering heals forever.
            _recentDamage = Math.Max(0f, _recentDamage - RecentDamageDecayRate);

            // Reset HP-threshold flags once the invader's HP rises back above each level
            // (heal restored it, or it just started above those values).
            float hpFrac = (float)NPC.life / NPC.lifeMax;
            if (_halfHpHealed    && hpFrac > 0.55f) _halfHpHealed    = false;
            if (_quarterHpHealed && hpFrac > 0.30f) _quarterHpHealed = false;

            if (_directionHoldTicks > 0)
                _directionHoldTicks--;

            // Push navigation settings onto the GlobalNPC instance so FighterAI uses them.
            // Tier 2 enables waypoint routing: when stuck, it scans for a horizontal opening,
            // an elevated ledge to jump to, or a platform edge to drop off.
            var gnpc = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            gnpc.CanUseRopes     = true; // Invaders default to rope-climbing (only takes effect on SF4-mover invaders)
            gnpc.MaxJumpPower    = InvaderJumpPower;
            gnpc.MaxJumpBoost    = InvaderJumpBoost;
            gnpc.CanDoubleJump   = InvaderCanDoubleJump;
            gnpc.DoubleJumpPower = InvaderDoubleJumpPower;
            gnpc.TeleportTelegraphTime = TeleportTelegraphTicks;
            gnpc.TeleportDustType      = TeleportDustTypeId;
            gnpc.TeleportDustColor     = TeleportDustTint;
            gnpc.TeleportDustScale     = TeleportDustScale;
            gnpc.TeleportDustCount     = TeleportDustCount;

            Player target = Main.player[NPC.target];
            float distToTarget = NPC.Distance(target.Center);

            // ── Flight tick (before FighterAI) ────────────────────────────────────
            // When airborne, the flight controller fully owns velocity / noGravity / direction.
            // We skip FighterAI, rope climbing, and wall-blocked detection so they don't fight
            // the flight controller's intent.
            if (HasWings)
            {
                _flight ??= new EnemyFlightController(FlightConfig);
                _flight.Tick(NPC, target);
                if (_flight.IsAirborne)
                {
                    // Run attack AI (for aerial combos) but skip ground movement entirely.
                    if (gnpc.DodgeTimer <= 0)
                        InvaderAttackAI();
                    TickWeaponAnim();
                    return;
                }
            }

            float speedMult = (Phase == AttackPhase.CasualStroll || Phase == AttackPhase.Healing)
                ? CasualStrollSpeedMult : 1f;
            if (Phase == AttackPhase.Idle && distToTarget > RunDistance)
                speedMult *= RunSpeedMult;

            // Capture direction before the movement AI might change it.
            int dirBefore = NPC.direction;

            RunMovementAI(speedMult);

            // Anti-bounce: if FighterAI just reversed direction but the hold timer is still
            // running (e.g. the player dodgerolled past), revert to the previous direction so
            // the invader doesn't flip back and forth every tick.
            if (NPC.direction != dirBefore && _directionHoldTicks > 0)
                NPC.direction = dirBefore;
            else if (NPC.direction != dirBefore)
                _directionHoldTicks = 30; // lock new direction for ~½ s before allowing another flip

            // ── Flee-to-heal movement override ────────────────────────────────────
            // FighterAI always moves TOWARD the player.  When fleeing we invert velocity.X
            // so the invader sprints away instead.  Y velocity (jumping) from FighterAI is
            // preserved so it can still clear obstacles while fleeing.
            if (Phase == AttackPhase.FleeToHeal)
            {
                Player fleeFrom      = Main.player[NPC.target];
                float  awayDir       = Math.Sign(NPC.Center.X - fleeFrom.Center.X);
                if (awayDir == 0) awayDir = -NPC.direction;
                NPC.velocity.X       = (float)awayDir * TopSpeed * 2.0f;
                NPC.direction        = (int)awayDir;
                _directionHoldTicks  = 5; // allow quick correction while fleeing
            }

            // ── Dodge-roll movement guard ──────────────────────────────────────────
            // When the GlobalNPC dodge system fires it sets velocity.X = 5 * direction and
            // grants invulnerability for DodgeTimer ticks.  If we then run InvaderAttackAI
            // it would call SlowDown() or set a stab-lunge velocity, overriding the dash.
            // Instead we skip attack AI entirely for those ticks so the dodge movement lands.
            bool onRope = IsTouchingClimbableRope();
            NPC.noGravity = onRope;
            if (onRope)
            {
                float ropeClimbSpeed = MathHelper.Clamp(TopSpeed * 0.75f, 1.0f, 2.2f);
                float yDelta = target.Center.Y - NPC.Center.Y;
                if (Math.Abs(yDelta) > 12f)
                {
                    float desiredVy = Math.Sign(yDelta) * ropeClimbSpeed;
                    NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, desiredVy, 0.18f);
                    NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y, -ropeClimbSpeed, ropeClimbSpeed);
                }
                else
                {
                    NPC.velocity.Y *= 0.60f;
                }
            }

            // ── Wall-blocked detection and resolution ──────────────────────────────
            // Track ticks where the invader has no LOS and can't move — it is stuck against
            // terrain.  Instead of jumping fruitlessly we:
            //   (a) stand still so FighterAI's Tier-2 scanner can find a route (~1 s)
            //   (b) teleport after 5 s if nothing is found
            //   (c) retreat ~10 tiles then teleport immediately if struck through the wall
            {
                bool wallLOS   = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
                bool freePhase = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll;

                // Accumulate when stuck (no LOS, on ground, not moving); drain when LOS restored.
                if (freePhase && !wallLOS && NPC.velocity.Y == 0f && Math.Abs(NPC.velocity.X) < 0.5f)
                    _wallBlockedTimer++;
                else if (wallLOS)
                    _wallBlockedTimer = Math.Max(0, _wallBlockedTimer - 3);

                // > 1 s stuck: nearly stop (cuts jump-bounce noise) before the teleport fallback below.
                if (_wallBlockedTimer > 60 && !wallLOS)
                {
                    NPC.velocity.X *= 0.15f;
                }

                // > 5 s with no progress: teleport.
                if (_wallBlockedTimer > 300)
                {
                    _wallBlockedTimer = 0;
                    if (gnpc.TeleportCountdown == 0 && gnpc.TeleportAppearanceTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                        tsorcRevampAIs.QueueTeleport(NPC, 50, true, 90);
                    EnterPhase(AttackPhase.Idle, 0);
                }

                // Struck through wall while blocked → short retreat, then emergency teleport.
                if (_hitThroughWallFlag)
                {
                    _hitThroughWallFlag         = false;
                    _wallBlockedTimer           = 0;
                    _retreatBeforeTeleportTimer = 40; // ~40 ticks at 2× speed ≈ 10 tiles
                }
                if (_retreatBeforeTeleportTimer > 0)
                {
                    _retreatBeforeTeleportTimer--;
                    float awayX = Math.Sign(NPC.Center.X - target.Center.X);
                    if (awayX == 0) awayX = -NPC.direction;
                    NPC.velocity.X      = (float)awayX * TopSpeed * 2f;
                    NPC.direction       = (int)awayX;
                    _directionHoldTicks = 5;
                    if (_retreatBeforeTeleportTimer == 0 && gnpc.TeleportCountdown == 0 && gnpc.TeleportAppearanceTimer == 0
                        && Main.netMode != NetmodeID.MultiplayerClient)
                        tsorcRevampAIs.QueueTeleport(NPC, 50, true, 60);
                }
            }

            if (gnpc.DodgeTimer <= 0)
                InvaderAttackAI();

            TickWeaponAnim();
        }

        /// <summary>
        /// Returns true when the invader should interrupt whatever it is doing to flee and heal.
        /// Checked at the top of InvaderAttackAI every tick.
        /// </summary>
        private bool ShouldHeal()
        {
            if (_estusCharges <= 0 || _healCooldown > 0) return false;
            // Don't interrupt an already-running heal sequence.
            if (Phase == AttackPhase.FleeToHeal || Phase == AttackPhase.Healing) return false;

            float hp = (float)NPC.life / NPC.lifeMax;

            // Threshold 1: first time HP falls at or below 50 %.
            if (!_halfHpHealed && hp <= 0.50f) return true;
            // Threshold 2: first time HP falls at or below 25 %.
            if (!_quarterHpHealed && hp <= 0.25f) return true;
            // Threshold 3: burst damage — took a lot of damage in a short window.
            if (_recentDamage >= RecentDamageThreshold) return true;

            return false;
        }

        private void InvaderAttackAI()
        {
            Player target = Main.player[NPC.target];
            float  dist   = NPC.Distance(target.Center);
            bool   hasLOS = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            // ── Healing intercept (highest priority) ──────────────────────────────
            // Check before the main switch so any phase can be interrupted to flee and heal.
            if (ShouldHeal())
            {
                EnterPhase(AttackPhase.FleeToHeal, FleeToHealMaxTicks);
                return;
            }

            // Weapon only exists visually during telegraphs and attack frames.
            _weaponVisible = IsWeaponVisiblePhase;

            switch (Phase)
            {
                case AttackPhase.Idle:
                    if (_heldItemType <= 0)
                        SetDisplayWeapon(MeleeWeaponItemType >= 0 ? MeleeWeaponItemType : RangedWeaponItemType, swing: false);

                    if (!hasLOS)
                        break;

                    // ── Wings: airborne behavior + takeoff triggers ───────────────
                    if (HasWings && _flight != null)
                    {
                        if (_flight.IsAirborne)
                        {
                            // While airborne, choose aerial actions instead of ground combos.
                            // The flight controller's Hover mode auto-refreshes itself; we
                            // periodically interrupt with a strafe or dive for variety/damage.
                            if (_flight.Mode == FlightMode.Hover && Main.rand.Next(90) == 0)
                            {
                                if (Main.rand.Next(2) == 0)
                                {
                                    _flight.RequestDive(target.Center);
                                    DoAerialDiveTelegraph();
                                }
                                else
                                {
                                    float side = Math.Sign(NPC.Center.X - target.Center.X);
                                    if (side == 0) side = -NPC.direction;
                                    _flight.RequestStrafe(target.Center + new Vector2(-side * 240f, 0f));
                                }
                            }
                            else if (_flight.IsDiving && _aerialHitCooldown <= 0)
                            {
                                // Dive contact: drop a hitbox at NPC center.
                                DoAerialDiveHit();
                                _aerialHitCooldown = 8; // brief inter-hit cooldown during dive
                            }
                            else if (_flight.Mode == FlightMode.Hover
                                  && Main.rand.Next(150) == 0
                                  && RangedWeaponItemType >= 0
                                  && _rangedCooldown <= 0
                                  && hasLOS)
                            {
                                // Aerial ranged: route through the full RangedTelegraph phase
                                // so the player sees the weapon visibly held in hand (stars
                                // raised, crossbow extended) and the aim animation completes
                                // before firing — same wind-up convention as ground shots.
                                // ForceStanding = true because the flight controller fully owns
                                // velocity while airborne; the telegraph's velocity damping is
                                // gated on !IsAirborne and would otherwise no-op anyway.
                                SetupRangedBurst(useSecondary: false, shotsOverride: 1, forceStanding: true);
                                EnterPhase(AttackPhase.RangedTelegraph, _activeRangedTelegraphTicks);
                            }
                            break;
                        }
                        else
                        {
                            // Grounded: roll for tactical takeoff.
                            bool playerAbove = target.Center.Y < NPC.Center.Y - FlightHeightTrigger;
                            bool stuck       = _wallBlockedTimer > 60;
                            float hpFrac     = (float)NPC.life / NPC.lifeMax;
                            int   randChance = hpFrac <= FlightHpEscalationFrac
                                             ? RandomTakeoffChance * 3
                                             : RandomTakeoffChance;
                            bool randomBurst = randChance > 0
                                             && Main.GameUpdateCount % 60 == 0
                                             && Main.rand.Next(100) < randChance;

                            if (playerAbove || stuck || randomBurst)
                            {
                                if (_flight.RequestTakeoff())
                                    break;
                            }
                        }
                    }

                    // ── Melee combo intercept ─────────────────────────────────────
                    // Roll first: combo system competes with the legacy slash/stab path.
                    // Combos require grounded, height-accessible target, archetype set, AND
                    // player within effective combo reach so the swings actually connect.
                    if (MeleeArchetype != WeaponArchetype.None
                        && MeleeWeaponItemType >= 0
                        && NPC.velocity.Y == 0f
                        && NPC.Center.Y - target.Center.Y < 48f
                        && dist <= ComboMaxStartRange
                        && Main.rand.Next(100) < MeleeComboChance)
                    {
                        if (TryStartMeleeCombo(dist))
                            break;
                    }

                    // Gate melee/stab on vertical accessibility: if the player is standing on a
                    // ledge above the invader, skip attack phases and let FighterAI navigate up
                    // first.  Without this the invader telegraphs but can never connect.
                    // Also gate on grounded (velocity.Y == 0) — mid-air attacks cause the invader
                    // to mid-air flip direction after overshooting and attack in the wrong direction.
                    float heightDiff = NPC.Center.Y - target.Center.Y; // positive = player above

                    bool wantSlash  = MeleeWeaponItemType  >= 0 && dist <= MeleeRange  && heightDiff < 36f
                                      && NPC.velocity.Y == 0f;
                    bool wantStab   = CanStab               && dist <= StabRange && dist > MeleeRange
                                      && _stabCooldown <= 0 && heightDiff < 48f && NPC.velocity.Y == 0f;
                    // Spear poke: medium-to-long melee reach, grounded, no lunge required.
                    bool wantSpear  = SpearWeaponItemType >= 0 && dist <= SpearRange
                                      && _spearCooldown <= 0 && heightDiff < 48f && NPC.velocity.Y == 0f;
                    bool wantPrimary   = RangedWeaponItemType >= 0 && dist <= RangedRange
                                         && dist >= MinRangedRange && _rangedCooldown <= 0;
                    bool wantSecondary = SecondaryRangedWeaponItemType >= 0 && dist <= SecondaryRangedRange
                                         && dist >= SecondaryRangedMinRange && _secondaryRangedCooldown <= 0;
                    // Magic: fires from any elevation (no velocity.Y check), blocked at very close range.
                    bool wantMagic     = MagicWeaponItemType >= 0 && dist <= MagicRange
                                         && dist >= MinMagicRange && _magicCooldown <= 0;

                    if (wantSlash)
                        EnterPhase(AttackPhase.MeleeTelegraph, MeleeTelegraphTicks);
                    else if (wantStab)
                        EnterPhase(AttackPhase.StabTelegraph, StabTelegraphTicks);
                    else if (wantSpear)
                    {
                        _spearLungeDir = NPC.direction;
                        EnterPhase(AttackPhase.SpearTelegraph, SpearTelegraphTicks);
                    }
                    else if (wantPrimary || wantSecondary)
                    {
                        // Pick secondary when both are available (SecondaryRangedChance roll),
                        // or always if only secondary is in range.
                        bool useSecondary = wantSecondary &&
                            (!wantPrimary || Main.rand.Next(100) < SecondaryRangedChance);
                        SetupRangedBurst(useSecondary, shotsOverride: -1, forceStanding: false);
                        EnterPhase(AttackPhase.RangedTelegraph, _activeRangedTelegraphTicks);
                    }
                    else if (wantMagic)
                        EnterPhase(AttackPhase.MagicTelegraph, MagicTelegraphTicks);
                    break;

                // ── Melee slash ───────────────────────────────────────────────
                case AttackPhase.MeleeTelegraph:
                    SlowDown();
                    SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                    CheckAndFireFlash(MeleeTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(MeleeWeaponItemType, swing: true);
                        DoMeleeAttack();
                        EnterPhase(AttackPhase.MeleeAttack, MeleeAttackTicks);
                    }
                    break;

                case AttackPhase.MeleeAttack:
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.MeleeRecovery, MeleeRecoveryTicks);
                    break;

                case AttackPhase.MeleeRecovery:
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Stab / lunge ──────────────────────────────────────────────
                case AttackPhase.StabTelegraph:
                    SlowDown();
                    SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                    SpawnTelegraphDust();
                    CheckAndFireFlash(MeleeTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        _stabLungeDir = NPC.direction; // lock direction so lunge can't rubber-band
                        DoStabAttack();
                        EnterPhase(AttackPhase.StabAttack, StabAttackTicks);
                    }
                    break;

                case AttackPhase.StabAttack:
                    // Use the direction locked at lunge start — not the live NPC.direction which
                    // FighterAI may flip if the player dodgerolls through to the other side.
                    // Lunge speed = TopSpeed × StabLungeSpeedMult.  Default 2.0 is firm but not
                    // overshooting; override per-subclass for more/less aggressive dashes.
                    NPC.velocity.X      = _stabLungeDir * (TopSpeed * StabLungeSpeedMult);
                    NPC.direction       = _stabLungeDir;
                    NPC.spriteDirection = _stabLungeDir;
                    _directionHoldTicks = Math.Max(_directionHoldTicks, 5);
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.StabRecovery, StabRecoveryTicks);
                    break;

                case AttackPhase.StabRecovery:
                    if (--PhaseTimer <= 0)
                    {
                        _stabCooldown = StabCooldownAfterUse; // prevent instant re-stab after dodgeroll
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Ranged ────────────────────────────────────────────────────
                // Telegraph: raise arm and aim.  Always decelerate so the invader
                // clearly "prepares to throw".  Standing shots brake harder to a full
                // stop; moving shots just slow — this still reads as deliberate aiming.
                case AttackPhase.RangedTelegraph:
                    // Skip ground velocity damping while airborne — flight controller owns velocity.
                    if (_flight == null || !_flight.IsAirborne)
                    {
                        if (_standingShot)
                            NPC.velocity.X *= 0.10f; // planted shot — brake to nearly stopped
                        else
                            SlowDown();               // mobile shot — decelerate but still drifting
                    }
                    SetDisplayWeapon(_activeRangedItemType, swing: false);
                    CheckAndFireFlash(_activeRangedFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(_activeRangedItemType, swing: true);
                        DoRangedAttack();
                        EnterPhase(AttackPhase.RangedAttack, _activeRangedAttackTicks);
                    }
                    break;

                case AttackPhase.RangedAttack:
                    if (_flight == null || !_flight.IsAirborne)
                    {
                        if (_standingShot)
                            NPC.velocity.X *= 0.25f; // planted shot — bleed off any residual momentum
                        else
                            SlowDown();               // mobile shot — continue decelerating through follow-through
                    }
                    if (--PhaseTimer <= 0)
                    {
                        if (_interShotPauses != null)
                        {
                            // ── Crossbow burst-pattern mode ───────────────────────────────────
                            // Each entry in _interShotPauses is a pause before the next shot.
                            // Exhausting all pauses means the final shot just fired → recovery.
                            if (_interShotPauseIndex < _interShotPauses.Length)
                            {
                                // Hold aim, wait the specified duration, then fire next shot.
                                EnterPhase(AttackPhase.CrossbowBurstPause,
                                           _interShotPauses[_interShotPauseIndex++]);
                            }
                            else
                            {
                                // All pattern shots fired — burst complete.
                                SetDisplayWeapon(MeleeWeaponItemType >= 0 ? MeleeWeaponItemType : _activeRangedItemType, swing: false);
                                EnterPhase(AttackPhase.RangedRecovery, _activeRangedRecoveryTicks);
                            }
                        }
                        else
                        {
                            // ── Classic multi-shot mode (throwing stars, primary ranged) ───────
                            _rangedShotsRemaining--;
                            if (_rangedShotsRemaining > 0)
                            {
                                // More shots left in the burst — each gets a full telegraph wind-up.
                                _weaponAnim = 0; // reset swing counter so telegraph starts clean
                                EnterPhase(AttackPhase.RangedTelegraph, _activeRangedTelegraphTicks);
                            }
                            else
                            {
                                // Burst finished — switch back to melee sprite.
                                SetDisplayWeapon(MeleeWeaponItemType >= 0 ? MeleeWeaponItemType : _activeRangedItemType, swing: false);
                                EnterPhase(AttackPhase.RangedRecovery, _activeRangedRecoveryTicks);
                            }
                        }
                    }
                    break;

                case AttackPhase.RangedRecovery:
                    if (--PhaseTimer <= 0)
                    {
                        // Apply the cooldown to the weapon that was just used.
                        if (_usingSecondaryRanged)
                            _secondaryRangedCooldown = SecondaryRangedCooldownAfterUse;
                        else
                            _rangedCooldown = RangedCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Crossbow burst pause ──────────────────────────────────────
                // Hold horizontal aim between shots in a pattern burst.
                // When the timer expires the next shot fires immediately — no re-telegraph.
                // This gives a deliberate "controlled volley" feel without extra wind-up.
                case AttackPhase.CrossbowBurstPause:
                    if (_flight == null || !_flight.IsAirborne)
                    {
                        if (_standingShot)
                            NPC.velocity.X *= 0.10f; // stay planted during the pause
                        else
                            SlowDown();
                    }
                    SetDisplayWeapon(_activeRangedItemType, swing: false);
                    if (--PhaseTimer <= 0)
                    {
                        // Fire next shot immediately — the arm is already at horizontal aim.
                        SetDisplayWeapon(_activeRangedItemType, swing: true);
                        DoRangedAttack();
                        EnterPhase(AttackPhase.RangedAttack, _activeRangedAttackTicks);
                    }
                    break;

                // ── Spear / polearm poke ──────────────────────────────────────
                // A stationary or short-push reach attack for long polearms.
                // SpearPushSpeedMult = 0 → pure stationary poke (no movement);
                // > 0 → small forward hop scaled by TopSpeed (less than stab lunge).
                case AttackPhase.SpearTelegraph:
                    SlowDown();
                    SetDisplayWeapon(SpearWeaponItemType, swing: false);
                    SpawnTelegraphDust();
                    CheckAndFireFlash(SpearTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(SpearWeaponItemType, swing: true);
                        DoSpearAttack();
                        EnterPhase(AttackPhase.SpearAttack, SpearAttackTicks);
                    }
                    break;

                case AttackPhase.SpearAttack:
                    // Lock to direction captured at telegraph start — prevents sprite snap if
                    // player moves to the other side while the arm is extending.
                    NPC.direction       = _spearLungeDir;
                    NPC.spriteDirection = _spearLungeDir;
                    _directionHoldTicks = Math.Max(_directionHoldTicks, 5);
                    if (SpearPushSpeedMult > 0f)
                        NPC.velocity.X = _spearLungeDir * (TopSpeed * SpearPushSpeedMult);
                    else
                        NPC.velocity.X *= 0.25f; // stationary poke — bleed off momentum
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.SpearRecovery, SpearRecoveryTicks);
                    break;

                case AttackPhase.SpearRecovery:
                    if (--PhaseTimer <= 0)
                    {
                        _spearCooldown = SpearCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Magic / spellcast ─────────────────────────────────────────
                // Charge-up then fire a spell projectile.  The invader brakes to a
                // halt during the telegraph so it reads as a deliberate cast.
                case AttackPhase.MagicTelegraph:
                    SlowDown();
                    SetDisplayWeapon(MagicWeaponItemType, swing: false);
                    CheckAndFireFlash(MagicTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(MagicWeaponItemType, swing: true);
                        DoMagicAttack();
                        EnterPhase(AttackPhase.MagicAttack, MagicAttackTicks);
                    }
                    break;

                case AttackPhase.MagicAttack:
                    NPC.velocity.X *= 0.5f; // momentum bleeds off during cast follow-through
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.MagicRecovery, MagicRecoveryTicks);
                    break;

                case AttackPhase.MagicRecovery:
                    if (--PhaseTimer <= 0)
                    {
                        _magicCooldown = MagicCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Casual stroll ─────────────────────────────────────────────
                // Slow walk toward player with no attacks — gives post-attack breathing room
                // and variation before the next engagement.
                case AttackPhase.CasualStroll:
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.Idle, 0);
                    break;

                // ── Flee to heal ──────────────────────────────────────────────
                // Sprint away until at safe distance (or timer expires), then drink.
                case AttackPhase.FleeToHeal:
                    // Velocity is already set in AI() above; just check distance / timer.
                    if (dist >= FleeToHealDistance || --PhaseTimer <= 0)
                    {
                        // Apply the heal NOW (at the moment of drinking).
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int healAmount = (int)(NPC.lifeMax * EstusHealFraction);
                            NPC.life = Math.Min(NPC.lifeMax, NPC.life + healAmount);
                            NPC.HealEffect(healAmount);
                        }
                        _estusCharges--;
                        _healCooldown  = HealCooldownTicks;
                        _recentDamage  = 0f;

                        // Mark whichever threshold(s) we're healing past so they don't re-fire
                        // immediately.  They'll reset once HP climbs back above the level.
                        float hp = (float)NPC.life / NPC.lifeMax;
                        if (hp > 0.50f) _halfHpHealed    = true;
                        if (hp > 0.25f) _quarterHpHealed = true;

                        EnterPhase(AttackPhase.Healing, HealAnimationTicks);
                    }
                    break;

                // ── Healing ───────────────────────────────────────────────────
                // Stand still and play the drinking animation for HealAnimationTicks.
                case AttackPhase.Healing:
                    NPC.velocity.X *= 0.6f; // brake to a halt
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Melee combo: telegraph (step 0) ───────────────────────────
                case AttackPhase.MeleeComboTelegraph:
                    LockComboDirection();
                    SlowDown();
                    SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                    CheckAndFireFlash(_activeMeleeCombo.InitialFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(MeleeWeaponItemType, swing: true);
                        DoComboMeleeHit(_activeMeleeCombo.Steps[_meleeComboStepIndex]);
                        EnterPhase(AttackPhase.MeleeComboAttack, _activeMeleeCombo.Steps[_meleeComboStepIndex].AttackTicks);
                    }
                    break;

                // ── Melee combo: active hitbox frame ──────────────────────────
                case AttackPhase.MeleeComboAttack:
                {
                    LockComboDirection();
                    var step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                    if (step.ForwardPushMult > 0f)
                        NPC.velocity.X = _comboLockedDir * (TopSpeed * step.ForwardPushMult);
                    else
                        NPC.velocity.X *= 0.65f;
                    if (--PhaseTimer <= 0)
                    {
                        int nextIdx = _meleeComboStepIndex + 1;
                        if (nextIdx < _activeMeleeCombo.Steps.Length)
                        {
                            int pause = Math.Max(1, step.PostStepPause);
                            EnterPhase(AttackPhase.MeleeComboPause, pause);
                        }
                        else
                        {
                            EnterPhase(AttackPhase.MeleeComboRecovery, MeleeRecoveryTicks);
                        }
                    }
                    break;
                }

                // ── Melee combo: inter-step pause ─────────────────────────────
                // Direction UNLOCKED during the pause: if the player dodgerolled through,
                // the invader can re-face them so the next step swings the correct way.
                // The next step's lock direction is captured just before its attack fires.
                case AttackPhase.MeleeComboPause:
                    SlowDown();
                    // Re-face the player during the pause window.
                    {
                        int faceDir = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction       = faceDir;
                        NPC.spriteDirection = faceDir;
                    }
                    if (--PhaseTimer <= 0)
                    {
                        _meleeComboStepIndex++;
                        // Recapture the lock direction for the upcoming step.
                        _comboLockedDir = NPC.direction;
                        var nextStep = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                        SetDisplayWeapon(MeleeWeaponItemType, swing: true);
                        DoComboMeleeHit(nextStep);
                        _weaponAnim = WeaponAnimMax;
                        EnterPhase(AttackPhase.MeleeComboAttack, nextStep.AttackTicks);
                    }
                    break;

                // ── Melee combo: final recovery ───────────────────────────────
                // Direction unlocked during recovery — invader can re-orient toward player.
                case AttackPhase.MeleeComboRecovery:
                    if (--PhaseTimer <= 0)
                    {
                        if (_meleeComboCooldowns != null
                            && _activeMeleeComboIndex >= 0
                            && _activeMeleeComboIndex < _meleeComboCooldowns.Length)
                        {
                            _meleeComboCooldowns[_activeMeleeComboIndex] = _activeMeleeCombo.CooldownAfterUse;
                        }
                        _activeMeleeComboIndex = -1;
                        EnterCasualOrIdle();
                    }
                    break;
            }
        }

        /// <summary>
        /// Force direction to the value captured at combo start.  Called every tick
        /// across all four MeleeCombo* phases so a player who dodgerolls through ends up
        /// behind the swing arc — FighterAI's natural facing-flip is overridden.
        /// </summary>
        private void LockComboDirection()
        {
            NPC.direction       = _comboLockedDir;
            NPC.spriteDirection = _comboLockedDir;
            _directionHoldTicks = Math.Max(_directionHoldTicks, 5);
        }

        /// <summary>
        /// Configure all ranged-burst state for the current weapon trigger.  Used by both the
        /// ground Idle path and the airborne hover-shot path so they share one canonical setup.
        ///   <paramref name="useSecondary"/>: pick secondary (crossbow) vs primary (stars/bow).
        ///   <paramref name="shotsOverride"/>: -1 = roll randomly via MaxRangedBurst; else use this exact count.
        ///   <paramref name="forceStanding"/>: bypass StandingRangedChance roll (used by aerial — controller owns velocity).
        /// </summary>
        private void SetupRangedBurst(bool useSecondary, int shotsOverride, bool forceStanding)
        {
            _usingSecondaryRanged = useSecondary;

            _activeRangedItemType       = useSecondary ? SecondaryRangedWeaponItemType : RangedWeaponItemType;
            _activeRangedStyle          = useSecondary ? SecondaryRangedAnimStyle      : RangedAnimStyle;
            _activeRangedFlashColor     = useSecondary ? SecondaryRangedFlashColor     : RangedTelegraphFlashColor;
            _activeRangedTelegraphTicks = useSecondary ? SecondaryRangedTelegraphTicks : RangedTelegraphTicks;
            _activeRangedAttackTicks    = useSecondary ? SecondaryRangedAttackTicks    : RangedAttackTicks;
            _activeRangedRecoveryTicks  = useSecondary ? SecondaryRangedRecoveryTicks  : RangedRecoveryTicks;

            if (forceStanding)
            {
                _standingShot = true;
            }
            else
            {
                int standingChance = useSecondary ? SecondaryStandingRangedChance : StandingRangedChance;
                _standingShot = Main.rand.Next(100) < standingChance;
            }

            // Shot count: explicit override (aerial = 1) takes precedence; otherwise roll.
            if (shotsOverride > 0)
            {
                _rangedShotsRemaining = shotsOverride;
            }
            else
            {
                int burstMax = useSecondary ? SecondaryMaxRangedBurst : MaxRangedBurst;
                _rangedShotsRemaining = !useSecondary && SingleRangedBurstChance > 0
                                        && Main.rand.Next(100) < SingleRangedBurstChance
                    ? 1
                    : Math.Max(1, Main.rand.Next(1, burstMax + 1));
            }

            // ── Crossbow burst-pattern selection (secondary ranged only) ──────────────
            // When SecondaryRangedBurstPatterns is defined the classic _rangedShotsRemaining
            // counter is replaced by a pause-array that drives the shot sequence.
            _interShotPauses     = null;
            _interShotPauseIndex = 0;
            if (useSecondary
                && shotsOverride < 0   // override skips patterns (aerial single-shot)
                && SecondaryRangedBurstPatterns != null
                && SecondaryRangedBurstPatterns.Length > 0)
            {
                int patIdx = PickPatternByChance(SecondaryRangedBurstChances,
                                                 SecondaryRangedBurstPatterns.Length);
                _interShotPauses = SecondaryRangedBurstPatterns[patIdx];

                if (SecondaryRangedBurstTelegraphExtras != null
                    && patIdx < SecondaryRangedBurstTelegraphExtras.Length)
                    _activeRangedTelegraphTicks += SecondaryRangedBurstTelegraphExtras[patIdx];

                if (SecondaryRangedBurstFlashColors != null
                    && patIdx < SecondaryRangedBurstFlashColors.Length)
                    _activeRangedFlashColor = SecondaryRangedBurstFlashColors[patIdx];
            }
        }

        /// <summary>
        /// Lazy-init the melee combo pool and per-combo cooldown array for the active archetype.
        /// </summary>
        private void EnsureMeleeComboPool()
        {
            if (_meleeComboPool != null) return;
            _meleeComboPool = WeaponArchetypeTables.GetMeleeCombos(MeleeArchetype);
            if (_meleeComboPool != null)
                _meleeComboCooldowns = new int[_meleeComboPool.Length];
        }

        private void EnsureRangedComboPool()
        {
            if (_rangedComboPool != null) return;
            _rangedComboPool = WeaponArchetypeTables.GetRangedCombos(RangedArchetype);
            if (_rangedComboPool != null)
                _rangedComboCooldowns = new int[_rangedComboPool.Length];
        }

        /// <summary>
        /// Try to start a melee combo from the active archetype's pool.  Selection is
        /// range-aware (close/mid/far bands) and HP-aware (heavy combos weighted up as HP drops).
        /// Returns true if a combo started; false if no eligible combo (cooldowns/weights).
        /// </summary>
        private bool TryStartMeleeCombo(float dist)
        {
            EnsureMeleeComboPool();
            if (_meleeComboPool == null || _meleeComboPool.Length == 0) return false;

            // Filter pool to combos whose cooldown is ready; rebuild weights array.
            float hpFrac = (float)NPC.life / NPC.lifeMax;
            float closeMax = MeleeRange + 30f;
            float midMax   = StabRange + 60f;

            // Walk the picker but with cooldown filter: temporarily zero the BaseWeight
            // of any combo on cooldown.  We use a stack array of effective weights.
            int total = 0;
            int[] effective = new int[_meleeComboPool.Length];
            ComboRangeBand band = dist <= closeMax ? ComboRangeBand.Close
                               : dist <= midMax   ? ComboRangeBand.Mid
                               : ComboRangeBand.Far;
            float heavyMult = hpFrac <= 0.33f ? 2.5f : hpFrac <= 0.66f ? 1.5f : 1.0f;

            for (int i = 0; i < _meleeComboPool.Length; i++)
            {
                if (_meleeComboCooldowns[i] > 0) { effective[i] = 0; continue; }
                float w = _meleeComboPool[i].BaseWeight;
                w *= (_meleeComboPool[i].Preferred == band
                      || _meleeComboPool[i].Preferred == ComboRangeBand.Any) ? 2.0f : 0.4f;
                if (_meleeComboPool[i].HeavyCommit) w *= heavyMult;
                effective[i] = (int)w;
                total += effective[i];
            }
            if (total <= 0) return false;

            int roll = Main.rand.Next(total);
            int cumulative = 0;
            int chosen = -1;
            for (int i = 0; i < _meleeComboPool.Length; i++)
            {
                cumulative += effective[i];
                if (effective[i] > 0 && roll < cumulative) { chosen = i; break; }
            }
            if (chosen < 0) return false;

            _activeMeleeComboIndex = chosen;
            _activeMeleeCombo      = _meleeComboPool[chosen];
            _meleeComboStepIndex   = 0;
            _comboLockedDir        = NPC.direction;
            // Minimum 30 ticks (0.5 s) so the wind-up arc has visible time to play.
            // Lighter combos that have raw values below 22 (×1.35 → ~30) get floored here.
            int telegraphTicks = Math.Max(MinComboTelegraphTicks,
                                          (int)(_activeMeleeCombo.Steps[0].TelegraphTicks * ComboTelegraphMultiplier));
            EnterPhase(AttackPhase.MeleeComboTelegraph, telegraphTicks);
            return true;
        }

        /// <summary>
        /// Spawn the damage hitbox for the current combo step.  Default applies the step's
        /// DamageMult and ReachMult on top of the invader's MeleeRange × 0.7 base reach.
        /// Subclasses can override for per-step VFX/sounds without losing the scaling.
        /// </summary>
        protected virtual void DoComboMeleeHit(MeleeComboStep step)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float reach = MeleeRange * 0.7f * step.ReachMult;
            int   dmg   = (int)(MeleeDamage * step.DamageMult);
            // Route through the same swing-arc helper as TryMeleeHit so overlap / alignment
            // hits behave identically across step combos and one-shot swings.
            SpawnMeleeHitbox(_comboLockedDir, reach, dmg, knockback: 3f);
        }

        // ── Aerial actions (default implementations) ──────────────────────────────
        /// <summary>Fire a telegraph flash + dust burst when a dive begins.  Subclass override
        /// for thematic dive markers.  Also equips the melee weapon for the dive visual.</summary>
        protected virtual void DoAerialDiveTelegraph()
        {
            if (MeleeWeaponItemType >= 0)
                SetDisplayWeapon(MeleeWeaponItemType, swing: false);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            SpawnTelegraphFlash(Color.OrangeRed);
        }

        /// <summary>Spawn the dive's damage hitbox.  Called repeatedly while in DiveAttack mode
        /// to give the dive a sustained contact-damage feel.</summary>
        protected virtual void DoAerialDiveHit()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            // Dive: 120 px wide box centered on the NPC so a player directly under the dive lands a hit.
            int boxW = 120;
            int boxH = 80;
            Vector2 topLeft = NPC.Center - new Vector2(boxW / 2f, boxH / 2f);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), topLeft, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.InvaderMeleeHitbox>(),
                (int)(MeleeDamage * 1.2f), 4f, Main.myPlayer, boxW, boxH);
        }

        /// <summary>Fire a single aerial ranged shot.  Default delegates to DoRangedAttack
        /// so subclass spawns its existing projectile + sound.  Override for true aerial-only
        /// projectiles (e.g. bombs dropped straight down).</summary>
        protected virtual void DoAerialRangedShot(Player target)
        {
            DoRangedAttack();
        }

        /// <summary>
        /// After any recovery phase, roll for a casual stroll.  If the roll fails
        /// (or the subclass sets CasualStrollChance to 0), go straight back to Idle.
        /// </summary>
        private void EnterCasualOrIdle()
        {
            if (NPC.HasValidTarget && NPC.Distance(Main.player[NPC.target].Center) <= StabRange + 40f)
            {
                EnterPhase(AttackPhase.Idle, 0);
                return;
            }

            if (CasualStrollChance > 0 && Main.rand.Next(100) < CasualStrollChance)
                EnterPhase(AttackPhase.CasualStroll,
                    Main.rand.Next(CasualStrollMinTicks, CasualStrollMaxTicks + 1));
            else
                EnterPhase(AttackPhase.Idle, 0);
        }

        /// <summary>
        /// Picks an index into a pattern array using weighted chances, or uniformly at random
        /// when <paramref name="chances"/> is null or empty.
        /// </summary>
        /// <param name="chances">Relative weights for each index.  Does not need to sum to 100.</param>
        /// <param name="count">Number of valid patterns to pick from.</param>
        private static int PickPatternByChance(int[] chances, int count)
        {
            if (count <= 1) return 0;
            if (chances == null || chances.Length == 0)
                return Main.rand.Next(count);

            int limit = Math.Min(chances.Length, count);
            int total = 0;
            for (int i = 0; i < limit; i++) total += chances[i];
            if (total <= 0) return Main.rand.Next(count);

            int roll = Main.rand.Next(total);
            int cumulative = 0;
            for (int i = 0; i < limit; i++)
            {
                cumulative += chances[i];
                if (roll < cumulative) return i;
            }
            return 0;
        }

        // ── Attack implementations (subclass) ────────────────────────────────────
        protected abstract void DoMeleeAttack();
        protected abstract void DoRangedAttack();
        protected virtual  void DoStabAttack()  { }
        /// <summary>
        /// Spawn the spear hitbox or projectile.  Called at the moment SpearAttack begins.
        /// Use <see cref="SpearWeaponItemType"/>, <see cref="SpearDamage"/>, and <see cref="SpearRange"/>.
        /// </summary>
        protected virtual  void DoSpearAttack() { }
        /// <summary>
        /// Fire the magic projectile.  Called at the moment MagicAttack begins.
        /// Use <see cref="MagicWeaponItemType"/> and <see cref="MagicDamage"/>.
        /// </summary>
        protected virtual  void DoMagicAttack() { }

        /// <summary>
        /// Minimum tick count a telegraph phase is allowed to last. <see cref="CheckAndFireFlash"/>
        /// fires the warning flash when <c>PhaseTimer ≤ 30</c>, so any telegraph shorter than this
        /// would fire the flash AT (or very near) the attack moment, defeating the purpose of the
        /// warning. Clamping here lets subclasses set whatever telegraph value they want without
        /// having to remember the floor — the player always gets a real 30-tick warning.
        /// </summary>
        private const int MinTelegraphTicks = 30;

        protected void EnterPhase(AttackPhase phase, int duration)
        {
            bool isTelegraph = phase == AttackPhase.MeleeTelegraph
                            || phase == AttackPhase.StabTelegraph
                            || phase == AttackPhase.RangedTelegraph
                            || phase == AttackPhase.SpearTelegraph
                            || phase == AttackPhase.MagicTelegraph
                            || phase == AttackPhase.MeleeComboTelegraph;
            Phase      = phase;
            // Guarantee at least 30 ticks of telegraph so the flash always leads the attack by 30.
            PhaseTimer = isTelegraph ? Math.Max(duration, MinTelegraphTicks) : duration;
            // Each new telegraph phase gets its own flash.
            if (isTelegraph) _flashFired = false;
        }

        private void SlowDown() => NPC.velocity.X *= 0.80f;

        // ── Damage tracking for emergency heal ────────────────────────────────────
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            _recentDamage += (float)damageDone / NPC.lifeMax;
            // Struck through a wall while stuck: flag for retreat + emergency teleport.
            if (_wallBlockedTimer > 60 && !Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1))
                _hitThroughWallFlag = true;
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            _recentDamage += (float)damageDone / NPC.lifeMax;
            if (_wallBlockedTimer > 60 && !Collision.CanHitLine(NPC.Center, 1, 1, projectile.Center, 1, 1))
                _hitThroughWallFlag = true;
        }

        // ── Melee hitbox helper ───────────────────────────────────────────────────
        // The hitbox CENTER is placed at the midpoint of the swing zone (NPC center → reach tip)
        // and the hitbox is widened to span the full swing. This guarantees a player gets hit when:
        //   • perfectly stacked on the invader  (inner edge of hitbox covers NPC center)
        //   • at full swing reach                (outer edge extends past `reach`)
        //   • slightly above / below the invader (taller 60 px hitbox covers vertical overlap)
        // The previous tip-only placement (40×40 hitbox at NPC.Center + direction*reach)
        // missed any player closer than ~half-reach to the NPC, including overlap.
        protected void TryMeleeHit(float reach = -1f)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            float r = reach < 0 ? MeleeRange * 0.7f : reach;
            SpawnMeleeHitbox(NPC.direction, r, MeleeDamage, knockback: 3f);
        }

        /// <summary>
        /// Spawn a swing-arc hitbox covering NPC center → outward `reach`. Width scales with
        /// reach so long-reach stab attacks still get full coverage from the NPC out to the tip.
        /// </summary>
        private void SpawnMeleeHitbox(int swingDir, float reach, int damage, float knockback)
        {
            // Width = at least 100 (covers stacked overlap) or reach × 1.4 (covers long stabs).
            // Height = 60 so a player jumping just above or crouching just below still registers.
            int boxW = (int)System.MathF.Max(100f, reach * 1.4f);
            int boxH = 60;
            Vector2 center   = NPC.Center + new Vector2(swingDir * reach * 0.5f, -8f);
            Vector2 topLeft  = center - new Vector2(boxW / 2f, boxH / 2f);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), topLeft, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.InvaderMeleeHitbox>(),
                damage, knockback, Main.myPlayer, boxW, boxH);
        }

        // ── Telegraph dust ────────────────────────────────────────────────────────
        private void SpawnTelegraphDust()
        {
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustDirect(NPC.Center - new Vector2(8f), 16, 16, 89,
                    Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.5f),
                    0, default, 1.2f);
                d.noGravity = true;
            }
        }

        // ── Telegraph flash VFX ───────────────────────────────────────────────────
        /// <summary>
        /// Fires the ring-flash VFX once the attack is within 30 frames.
        /// Call each tick during a telegraph phase; self-gates via <see cref="_flashFired"/>.
        /// Short telegraphs (< 30 ticks) fire immediately on the first call.
        /// </summary>
        private void CheckAndFireFlash(Color color)
        {
            if (_flashFired || PhaseTimer > 30) return;
            SpawnTelegraphFlash(color);
            _flashFired = true;
        }

        private void SpawnTelegraphFlash(Color color)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Projectile.NewProjectileDirect(
                NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(),
                0, 0, Main.myPlayer,
                UsefulFunctions.ColorToFloat(color));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Weapon visual
        // ─────────────────────────────────────────────────────────────────────────

        protected void SetDisplayWeapon(int itemType, bool swing)
        {
            _heldItemType = itemType;
            if (swing)
                _weaponAnim = WeaponAnimMax;
        }

        private void TickWeaponAnim()
        {
            if (_weaponAnim > 0)
                _weaponAnim--;

            float t = WeaponAnimMax > 0 ? 1f - (float)_weaponAnim / WeaponAnimMax : 1f;

            if (Phase == AttackPhase.StabTelegraph)
            {
                // Telegraph: dip sword downward — at +π/2 the sprite (naturally diagonal at 0)
                // appears 45° below horizontal, giving a clear "cocked for thrust" read.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.18f);
            }
            else if (Phase == AttackPhase.StabAttack)
            {
                // Thrust: snap to horizontal — at +π/4 the sprite appears perfectly flat,
                // pointing straight at the player (the "90° / flat" the user asked for).
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.42f);
            }
            else if (Phase == AttackPhase.RangedTelegraph)
            {
                float rangedT = RangedTelegraphTicks > 0
                    ? 1f - (float)PhaseTimer / RangedTelegraphTicks
                    : 1f;
                switch (_activeRangedStyle)
                {
                    case RangedStyle.Crossbow:
                        // Arm extends forward to a horizontal aim and holds — no arc.
                        _weaponRotation = MathHelper.Lerp(_weaponRotation, 0.05f, 0.22f);
                        break;
                    case RangedStyle.Bow:
                        // Arm gradually rises from hold → overhead as the bow draws back.
                        _weaponRotation = MathHelper.Lerp(-0.30f, -1.10f, rangedT);
                        break;
                    default: // Throw
                        // Hold the hand high while lining up the throw.
                        _weaponRotation = MathHelper.Lerp(-0.75f, -0.10f, rangedT);
                        break;
                }
            }
            else if (Phase == AttackPhase.RangedAttack)
            {
                float rangedT = RangedAttackTicks > 0
                    ? 1f - (float)PhaseTimer / RangedAttackTicks
                    : 1f;
                switch (_activeRangedStyle)
                {
                    case RangedStyle.Crossbow:
                        // Small forward snap / click — arm barely moves, just a quick jolt.
                        _weaponRotation = MathHelper.Lerp(0.05f, 0.18f, rangedT);
                        break;
                    case RangedStyle.Bow:
                        // Snap the arm forward from overhead as the arrow releases.
                        _weaponRotation = MathHelper.Lerp(-1.10f, 0.35f, rangedT);
                        break;
                    default: // Throw
                        // Swing the arm forward as the projectile leaves the hand.
                        _weaponRotation = MathHelper.Lerp(-0.10f, 0.45f, rangedT);
                        break;
                }
            }
            else if (Phase == AttackPhase.CrossbowBurstPause)
            {
                // Hold horizontal aim between shots — same target angle as Crossbow telegraph.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, 0.05f, 0.22f);
            }
            else if (Phase == AttackPhase.SpearTelegraph)
            {
                // Dip the spear down slightly — same "cocked for thrust" read as StabTelegraph.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.18f);
            }
            else if (Phase == AttackPhase.SpearAttack)
            {
                // Snap to horizontal for the poke — slower ease than stab since it's a stationary reach.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.30f);
            }
            else if (Phase == AttackPhase.MagicTelegraph)
            {
                float magicT = MagicTelegraphTicks > 0
                    ? 1f - (float)PhaseTimer / MagicTelegraphTicks
                    : 1f;
                // Arm rises overhead during the charge (Use1 / raised overhead pose).
                _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.40f, 0.12f);
            }
            else if (Phase == AttackPhase.MagicAttack)
            {
                float magicT = MagicAttackTicks > 0
                    ? 1f - (float)PhaseTimer / MagicAttackTicks
                    : 1f;
                // Thrust forward as the spell fires.
                _weaponRotation = MathHelper.Lerp(-1.40f, 0.20f, magicT);
            }
            else if (Phase == AttackPhase.MeleeTelegraph)
            {
                // Wind-up: weapon rises from hold angle to the top of the arc quickly
                _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.3f, 0.30f);
            }
            else if (Phase == AttackPhase.MeleeAttack)
            {
                // Downswing: full broadsword arc, raised behind head → slash down-forward
                // t runs 0→1 over WeaponAnimMax ticks (reset when swing begins)
                _weaponRotation = MathHelper.Lerp(-1.3f, 1.0f, t);
            }
            else if (Phase == AttackPhase.MeleeComboTelegraph
                  || Phase == AttackPhase.MeleeComboAttack
                  || Phase == AttackPhase.MeleeComboPause)
            {
                // Drive rotation from the current step's ComboMotion.
                var step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                bool inTel = Phase == AttackPhase.MeleeComboTelegraph;
                bool inPause = Phase == AttackPhase.MeleeComboPause;

                switch (step.Motion)
                {
                    case ComboMotion.OverheadArc:
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.3f, 0.30f);
                        else if (inPause) _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.0f, 0.20f);
                        else              _weaponRotation = MathHelper.Lerp(-1.3f, 1.0f, t);
                        break;
                    case ComboMotion.UnderhandArc:
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, 1.0f, 0.30f);
                        else if (inPause) _weaponRotation = MathHelper.Lerp(_weaponRotation, 0.7f, 0.20f);
                        else              _weaponRotation = MathHelper.Lerp(1.0f, -1.3f, t);
                        break;
                    case ComboMotion.HorizontalSweep:
                        // Flat side-to-side: arm extends, weapon held near horizontal
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, -0.4f, 0.25f);
                        else if (inPause) _weaponRotation = MathHelper.Lerp(_weaponRotation, 0.3f, 0.18f);
                        else              _weaponRotation = MathHelper.Lerp(-0.4f, 0.6f, t);
                        break;
                    case ComboMotion.VerticalChop:
                        // Straight overhead → straight down (hammer)
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.55f, 0.32f);
                        else if (inPause) _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.2f, 0.18f);
                        else              _weaponRotation = MathHelper.Lerp(-1.55f, 1.4f, t);
                        break;
                    case ComboMotion.Thrust:
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.20f);
                        else if (inPause) _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2 * 0.8f, 0.18f);
                        else              _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.42f);
                        break;
                    case ComboMotion.JoustDash:
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.18f);
                        else              _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.45f);
                        break;
                    case ComboMotion.Spin:
                        // Continuous rotation; 1 full revolution per WeaponAnimMax-ish ticks
                        _weaponRotation += 0.28f;
                        if (_weaponRotation > MathHelper.TwoPi) _weaponRotation -= MathHelper.TwoPi;
                        break;
                    case ComboMotion.IaidoDraw:
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, 1.2f, 0.15f); // weapon held low/behind
                        else              _weaponRotation = MathHelper.Lerp(1.2f, -0.5f, t);                // fast snap forward
                        break;
                    case ComboMotion.GroundSlam:
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.55f, 0.25f);
                        else              _weaponRotation = MathHelper.Lerp(-1.55f, 1.5f, t);
                        break;
                }
            }
            else if (_flight != null && _flight.IsDiving && MeleeWeaponItemType >= 0)
            {
                // Dive thrust pose: weapon held forward+down toward the player, locked.
                // PiOver4 (~45°) reads as a downward-forward stab when paired with the
                // dive's downward velocity and the puppet's Use3 forward-arm pose.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.35f);
            }
            else
            {
                // Idle / walking / jumping / recovery:
                // Ease the weapon back to the natural hold angle so it always looks carried.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, HoldRotation, 0.10f);
            }
        }

        /// <summary>
        /// Returns the world-space position of the front hand by reading the current body-frame
        /// row and applying the known arm-tip offsets from the vanilla player sprite sheet.
        ///
        /// These offsets were taken directly from <c>MeleeAnimation.cs</c> (BroadswordRework)
        /// where they were calibrated against the vanilla player sprite, so they exactly match
        /// where the rendered arm ends for each Use row.  Using a direct lookup here is more
        /// reliable than <c>Player.GetFrontHandPosition</c> on a puppet, because that API
        /// depends on composite arm state that the puppet draw path doesn't fully initialise.
        /// </summary>
        private Vector2 GetHandPosition()
        {
            if (_puppet == null)
                return NPC.Center;

            // Row of the body frame in the player sprite sheet (Use1=1 … Use4=4, walk/idle=other).
            int bodyRow = _puppet.bodyFrame.Y / FrameHeight;

            // Arm-tip offsets relative to NPC.Center, in direction-neutral space.
            // X is given for facing-right; multiply by NPC.direction for the actual side.
            Vector2 offset = bodyRow switch
            {
                1 => new Vector2(-8f, -9f),  // Use1 — arm fully raised, weapon overhead
                2 => new Vector2( 4f, -8f),  // Use2 — arm raised (ranged telegraph / ranged throw)
                3 => new Vector2( 4f,  2f),  // Use3 — arm level / forward (stab attack)
                4 => new Vector2( 4f,  7f),  // Use4 — arm dipped (stab telegraph)
                _ => new Vector2( 4f,  2f),  // fallback — level arm
            };

            return NPC.Center + new Vector2(offset.X * NPC.direction, offset.Y);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Puppet
        // ─────────────────────────────────────────────────────────────────────────

        private Item GetCachedWeaponItem(int itemType)
        {
            if (itemType == MeleeWeaponItemType)
            {
                if (_cachedMeleeType != itemType)
                {
                    _meleeItemCache = new Item();
                    _meleeItemCache.SetDefaults(itemType);
                    _meleeItemCache.noUseGraphic = true; // arm animates; sprite is drawn manually
                    _cachedMeleeType = itemType;
                }
                return _meleeItemCache;
            }
            if (itemType == RangedWeaponItemType)
            {
                if (_cachedRangedType != itemType)
                {
                    _rangedItemCache = new Item();
                    _rangedItemCache.SetDefaults(itemType);
                    _rangedItemCache.noUseGraphic = true;
                    _cachedRangedType = itemType;
                }
                return _rangedItemCache;
            }
            return new Item(); // air
        }

        private void InitPuppet()
        {
            _puppet = new Player();
            _puppet.active  = true;
            _puppet.whoAmI  = 0;
            _puppet.gravDir = 1f;
            _puppet.Male    = true;

            _puppet.armor[0] = new Item();  _puppet.armor[0].SetDefaults(HeadArmorItemType);
            _puppet.armor[1] = new Item();  _puppet.armor[1].SetDefaults(BodyArmorItemType);
            _puppet.armor[2] = new Item();  _puppet.armor[2].SetDefaults(LegsArmorItemType);

            // These equip-slot indices are what the renderer reads to pick armor textures
            _puppet.head = _puppet.armor[0].headSlot;
            _puppet.body = _puppet.armor[1].bodySlot;
            _puppet.legs = _puppet.armor[2].legSlot;

            // Wings — populated only when HasWings, so wing layer renders behind the body
            if (HasWings && WingsAccessoryItemType > 0)
            {
                _wingsItemCache = new Item();
                _wingsItemCache.SetDefaults(WingsAccessoryItemType);
                _cachedWingsType = WingsAccessoryItemType;
                _puppet.wings = _wingsItemCache.wingSlot;
                _puppet.wingTimeMax = 200; // arbitrary > 0 so wing layer treats it as a real wing
            }

            // Pre-set default weapon so it shows from the first frame
            _heldItemType = MeleeWeaponItemType >= 0 ? MeleeWeaponItemType : RangedWeaponItemType;
        }

        private void SyncPuppet()
        {
            _puppet.position  = NPC.position;
            _puppet.velocity  = NPC.velocity;
            _puppet.direction = NPC.direction;
            _puppet.width     = NPC.width;
            _puppet.height    = NPC.height;
            _puppet.gravDir   = 1f;

            // Wing flap state: drive vanilla wing draw layer's animation.
            // controlJump=true + wingTime>0 makes the wing layer pick the flap frames;
            // false + wingTime=0 makes it pick the glide/closed frame.
            if (HasWings && _flight != null)
            {
                bool airborne = _flight.IsAirborne;
                _puppet.wings = _wingsItemCache?.wingSlot ?? 0;
                _puppet.wingTime = airborne ? _puppet.wingTimeMax : 0;
                _puppet.controlJump = _flight.WingsActiveThisTick;
                // Flag the puppet as in-air so the wing layer doesn't suppress the draw.
                _puppet.wingFrame = (int)(_flight.WingAnimPhase * 4f) % 4;
            }
            else
            {
                _puppet.wings = 0;
                _puppet.wingTime = 0;
                _puppet.controlJump = false;
            }

            // Only real combat poses keep the weapon in hand; recovery, idle, healing,
            // and casual movement fall back to the natural arm/leg draw.
            bool inAttackPhase = IsWeaponPosePhase;
            _puppet.itemAnimationMax = WeaponAnimMax;
            _puppet.selectedItem     = 0;

            if (inAttackPhase)
            {
                // Put weapon in hand so the arm extends to the correct Use1–Use4 pose.
                // noUseGraphic=true prevents DrawPlayer from rendering the sprite itself —
                // InvaderWeaponDrawLayer handles that at the correct layer depth.
                _puppet.inventory[0]  = _heldItemType > 0 ? GetCachedWeaponItem(_heldItemType) : new Item();
                // Keep itemAnimation >= 1 so the arm stays in extended-hold pose between swings.
                _puppet.itemAnimation = Math.Max(_weaponAnim, 1);
                _puppet.itemRotation  = NPC.direction * _weaponRotation;
            }
            else
            {
                // Idle / casual stroll: no weapon in hand — arm hangs naturally in the
                // walk/idle pose without extending as if gripping something.
                _puppet.inventory[0]  = new Item();
                _puppet.itemAnimation = 0;
                _puppet.itemRotation  = 0f;
            }

            SyncFrames();
        }

        private void SyncFrames()
        {
            bool onGround = NPC.velocity.Y == 0f;
            bool moving   = Math.Abs(NPC.velocity.X) > 0.15f;

            // Always advance walk counter when moving on ground — legs animate
            // even during attacks so the invader doesn't glide with frozen feet.
            // Guard against Main.gamePaused so the animation freezes with every other NPC.
            if (onGround && moving && !Main.gamePaused)
            {
                _frameCounter += Math.Abs(NPC.velocity.X) * 0.55f;
                if (_frameCounter >= 14f) _frameCounter = 0f;
            }

            // ── Body frame ────────────────────────────────────────────────────────
            // Attack phases drive the upper body (Use1–Use4).  The arm pose tracks
            // the weapon angle via the pitch formula so the shoulder stays consistent
            // with the visual weapon rotation throughout the full swing arc.
            //   Use1 (row 1) = arm fully raised (weapon behind/above head, -1.3 rad)
            //   Use2 (row 2) = arm raised
            //   Use3 (row 3) = arm level / forward
            //   Use4 (row 4) = arm lowered  (weapon pointing down-forward, +1.0 rad)
            // Pitch formula: (1 - sin(weaponAngle)) / 2  →  1 = up, 0 = down.

            int bodyRow;
            bool isMeleeSwing = Phase == AttackPhase.MeleeTelegraph || Phase == AttackPhase.MeleeAttack;

            if (Phase == AttackPhase.Healing)
            {
                // Arm raised to drink — Use2 matches the "arm held up" pose
                bodyRow = 2;
            }
            else if (isMeleeSwing)
            {
                float pitch = (1f - (float)Math.Sin(_weaponRotation)) / 2f;
                if      (pitch > 0.95f) bodyRow = 1; // Use1 — arm fully up
                else if (pitch > 0.70f) bodyRow = 2; // Use2
                else if (pitch > 0.30f) bodyRow = 3; // Use3
                else                   bodyRow = 4; // Use4 — arm fully down
            }
            else if (Phase == AttackPhase.StabTelegraph)
            {
                bodyRow = 4; // Use4 — arm dipped down to match sword dip telegraph
            }
            else if (Phase == AttackPhase.StabAttack)
            {
                bodyRow = 3; // Use3 — arm level/forward for the horizontal thrust
            }
            else if (Phase == AttackPhase.RangedTelegraph)
            {
                // Body row depends on animation style so the arm matches the weapon arc.
                bodyRow = _activeRangedStyle switch
                {
                    RangedStyle.Crossbow => 3, // Use3 — arm level/forward for horizontal aim
                    RangedStyle.Bow      =>    // Bow draws: arm rises from Use3 → Use2 → Use1 over the telegraph
                        PhaseTimer > _activeRangedTelegraphTicks * 0.60f ? 3 :
                        PhaseTimer > _activeRangedTelegraphTicks * 0.25f ? 2 : 1,
                    _                    => 2, // Throw default — raised arm
                };
            }
            else if (Phase == AttackPhase.RangedAttack)
            {
                bodyRow = _activeRangedStyle switch
                {
                    RangedStyle.Crossbow => 3, // Use3 — arm stays level, small click jolt
                    RangedStyle.Bow      => 3, // Use3 — arm snaps forward at release
                    _                    => 3, // Throw — arm forward at release
                };
            }
            else if (Phase == AttackPhase.CrossbowBurstPause)
            {
                bodyRow = 3; // Use3 — arm level/forward, holding aim during inter-shot pause
            }
            else if (Phase == AttackPhase.SpearTelegraph)
            {
                bodyRow = 4; // Use4 — arm dipped, spear angled down in "ready to poke" read
            }
            else if (Phase == AttackPhase.SpearAttack)
            {
                bodyRow = 3; // Use3 — arm level/forward for the reach poke
            }
            else if (Phase == AttackPhase.MagicTelegraph)
            {
                // Arm rises overhead as the charge builds.
                bodyRow = PhaseTimer > MagicTelegraphTicks * 0.50f ? 2 : 1;
            }
            else if (Phase == AttackPhase.MagicAttack)
            {
                bodyRow = 3; // Use3 — arm thrusts forward as the spell fires
            }
            else if (Phase == AttackPhase.MeleeComboTelegraph
                  || Phase == AttackPhase.MeleeComboAttack
                  || Phase == AttackPhase.MeleeComboPause)
            {
                // Body row depends on the current step's motion to keep the arm pose
                // aligned with the weapon-rotation lerp from TickWeaponAnim.
                bool inTel = Phase == AttackPhase.MeleeComboTelegraph;
                var motion = _activeMeleeCombo.Steps[_meleeComboStepIndex].Motion;
                switch (motion)
                {
                    case ComboMotion.OverheadArc:
                    case ComboMotion.VerticalChop:
                    case ComboMotion.GroundSlam:
                        // Pitch-based row matches existing slash arc body selection
                        if (inTel) bodyRow = 1;
                        else
                        {
                            float pitch = (1f - (float)Math.Sin(_weaponRotation)) / 2f;
                            if      (pitch > 0.95f) bodyRow = 1;
                            else if (pitch > 0.70f) bodyRow = 2;
                            else if (pitch > 0.30f) bodyRow = 3;
                            else                   bodyRow = 4;
                        }
                        break;
                    case ComboMotion.UnderhandArc:
                        bodyRow = inTel ? 4 : (_weaponRotation < 0f ? 1 : 3);
                        break;
                    case ComboMotion.HorizontalSweep:
                        bodyRow = 3; // arm level/forward
                        break;
                    case ComboMotion.Thrust:
                    case ComboMotion.JoustDash:
                        bodyRow = inTel ? 4 : 3;
                        break;
                    case ComboMotion.Spin:
                        // Rotate body row to suggest motion; cycle Use1→Use3 by rotation phase
                        bodyRow = 1 + ((int)(_weaponRotation / MathHelper.PiOver2) & 3);
                        if (bodyRow > 4) bodyRow = 4;
                        break;
                    case ComboMotion.IaidoDraw:
                        bodyRow = inTel ? 4 : 3;
                        break;
                    default:
                        bodyRow = 3;
                        break;
                }
            }
            else if (_flight != null && _flight.IsDiving && MeleeWeaponItemType >= 0)
            {
                // Dive: arm forward, weapon thrusting toward the dive target.
                bodyRow = 3; // Use3 — arm level/forward
            }
            else if (!onGround)
            {
                bodyRow = 5; // Jump
            }
            else if (moving)
            {
                bodyRow = 6 + (int)_frameCounter; // Walk1–Walk14
            }
            else
            {
                bodyRow = 0; // Idle
            }

            // ── Leg frame ─────────────────────────────────────────────────────────
            // Legs ALWAYS follow movement — never locked to the attack state.
            // The body (Use) frames carry the attack animation; the legs stay natural.
            int legRow;
            if (!onGround)
                legRow = 5; // Jump
            else if (moving)
                legRow = 6 + (int)_frameCounter; // Walk1–Walk14 (shared counter with body)
            else
                legRow = 0; // Idle

            _puppet.bodyFrame = new Rectangle(0, FrameHeight * bodyRow, 40, FrameHeight);
            _puppet.legFrame  = new Rectangle(0, FrameHeight * legRow,  40, FrameHeight);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Draw
        // ─────────────────────────────────────────────────────────────────────────

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (_puppet == null)
                InitPuppet();

            SyncPuppet();

            // Store draw color so InvaderWeaponDrawLayer can read it during the pipeline below.
            _layerDrawColor = drawColor;

            // InvaderWeaponDrawLayer is registered at AfterParent(HeldItem) in tModLoader's pipeline.
            // By setting DrawingPuppetFor = this for exactly the duration of DrawPlayer, that layer
            // wakes up and calls DrawWeaponToLayer — inserting the weapon sprite after body/legs
            // but before the front arm, which gives the correct "hand gripping the sword" look.
            DrawingPuppetFor = this;
            Main.PlayerRenderer.DrawPlayer(Main.Camera, _puppet, NPC.position, 0f, Vector2.Zero);
            DrawingPuppetFor = null;

            return false;
        }

        /// <summary>
        /// Called by <see cref="InvaderWeaponDrawLayer"/> during the <c>DrawPlayer</c> pipeline.
        /// Adds the weapon sprite to <paramref name="drawInfo"/>'s draw-data cache at the correct
        /// layer depth — after body/legs but before the front arm — so the hand appears to grip it.
        /// </summary>
        internal void DrawWeaponToLayer(ref PlayerDrawSet drawInfo)
        {
            // Healing: draw the estus flask instead of the combat weapon.
            if (Phase == AttackPhase.Healing)
            {
                DrawEstusFlaskToLayer(ref drawInfo);
                return;
            }

            if (Phase == AttackPhase.FleeToHeal || !_weaponVisible || _heldItemType <= 0)
                return;

            var texAsset = TextureAssets.Item[_heldItemType];
            if (texAsset?.Value == null)
                return;

            Texture2D tex     = texAsset.Value;
            float     scale   = _heldItemType == RangedWeaponItemType ? 0.42f : 0.62f;
            Vector2   drawPos = GetHandPosition() - Main.screenPosition;

            // ── Origin: anchor the HANDLE (not centre) at the animated hand position ──
            //
            // Terraria sword sprites run diagonally in their texture — handle at lower-left,
            // blade tip at upper-right.  We normalise that corner as MeleeHandleNorm (0.10, 0.85).
            //
            // With SpriteEffects.FlipHorizontally the texture is mirrored, but the origin
            // parameter remains in pre-flip texture space.  The pixel rendered at drawPos is
            // the one whose mirrored X equals originX, i.e. originalX = texWidth − originX.
            // Mirroring origin.X keeps the handle pixel at the hand for both facing directions:
            //   Right (no flip) → originX = width * handleNorm.X        (lower-left corner)
            //   Left  (flip)    → originX = width * (1 − handleNorm.X)  (lower-right, which
            //                     after the flip maps to the handle side)
            Vector2 origin;
            if (_heldItemType == RangedWeaponItemType)
            {
                origin = tex.Bounds.Center.ToVector2(); // symmetric throwing items — keep centred
            }
            else
            {
                Vector2 hn = MeleeHandleNorm;
                float   hx = NPC.direction == 1
                    ? tex.Width * hn.X
                    : tex.Width * (1f - hn.X);
                origin = new Vector2(hx, tex.Height * hn.Y);
            }

            SpriteEffects fx = NPC.direction == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            drawInfo.DrawDataCache.Add(new DrawData(
                tex,
                drawPos,
                null,
                _layerDrawColor,
                _weaponRotation,
                origin,
                NPC.scale * scale,
                fx,
                0));
        }

        /// <summary>
        /// Draws the estus flask sprite at the hand during the Healing animation phase.
        /// Reuses the same <c>EstusFlask_drinking</c> texture the player uses, centred on
        /// the Use2 hand position and rotated to look like it's being raised to the mouth.
        /// </summary>
        private void DrawEstusFlaskToLayer(ref PlayerDrawSet drawInfo)
        {
            var textures = TransparentTextureHandler.TransparentTextures;
            var key      = TransparentTextureHandler.TransparentTextureType.EstusFlask;
            if (!textures.TryGetValue(key, out Texture2D tex) || tex == null)
                return;

            // The drinking texture is a 3-frame vertical strip (full / half / near-empty).
            int   frameCount  = 3;
            int   frameHeight = tex.Height / frameCount;
            // Pick frame based on remaining charges.
            int frame = _estusCharges >= (int)(EstusChargesMax * 0.6f) ? 0
                      : _estusCharges >= (int)(EstusChargesMax * 0.3f) ? 1
                      : 2;
            Rectangle src = new Rectangle(0, frame * frameHeight, tex.Width, frameHeight);

            // Position: Use2 hand offset is (4, -8) from NPC.Center, scaled by direction.
            // Add a small nudge toward the mouth (a few pixels up).
            Vector2 handWorld = NPC.Center + new Vector2(10f * NPC.direction, -14f);
            Vector2 drawPos   = handWorld - Main.screenPosition;
            Vector2 origin    = new Vector2(src.Width / 2f, src.Height / 2f);

            // Rotate the flask so it looks like it's tipped toward the mouth.
            // -π/2 (= −90°) points the sprite's "top" to the right for a right-facing invader.
            float rotation = MathHelper.PiOver2 * -NPC.direction;

            SpriteEffects fx = NPC.direction == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            drawInfo.DrawDataCache.Add(new DrawData(tex, drawPos, src,
                _layerDrawColor * 0.85f, rotation, origin, 0.75f, fx, 0));
        }

        /// <summary>
        /// Normalised (0–1) handle position within the melee-weapon texture.
        /// Default (0.10, 0.85) anchors near the lower-left corner — correct for most
        /// Terraria broadsword / katana sprites whose grip occupies that region.
        /// Override in a subclass to fine-tune the grip point for a specific weapon.
        /// </summary>
        protected virtual Vector2 MeleeHandleNorm => new Vector2(0.10f, 0.85f);
    }
}
