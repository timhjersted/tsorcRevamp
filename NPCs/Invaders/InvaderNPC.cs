using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

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

        protected virtual int MeleeTelegraphTicks  => 20;
        // MeleeAttackTicks matches WeaponAnimMax so the full swing arc completes
        // within the attack phase rather than bleeding into recovery.
        protected virtual int MeleeAttackTicks     => WeaponAnimMax; // 22 ticks = one full swing
        protected virtual int MeleeRecoveryTicks   => 25;

        protected virtual int StabTelegraphTicks   => 22;
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
        protected virtual int   SpearTelegraphTicks => 22;
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

        // ── Navigation (Tier 2 = waypoint routing, best available) ───────────────
        /// <summary>NavigationTier: 1 = smart jumps, 2 = adds ledge/gap waypoint routing.</summary>
        protected virtual float InvaderJumpPower      => 10f;
        protected virtual float InvaderJumpBoost      => 6f;
        protected virtual bool  InvaderCanDoubleJump  => false;
        protected virtual float InvaderDoubleJumpPower => 6f;

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
            Healing
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
            Phase == AttackPhase.MagicTelegraph  || Phase == AttackPhase.MagicAttack;

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
            gnpc.NavigationTier  = 2;
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
            float speedMult = (Phase == AttackPhase.CasualStroll || Phase == AttackPhase.Healing)
                ? CasualStrollSpeedMult : 1f;
            if (Phase == AttackPhase.Idle && distToTarget > RunDistance)
                speedMult *= RunSpeedMult;

            // Capture direction before FighterAI might change it.
            int dirBefore = NPC.direction;

            tsorcRevampAIs.FighterAI(NPC,
                topSpeed:     TopSpeed * speedMult,
                acceleration: Acceleration,
                brakingPower: BrakingPower,
                canTeleport:  true,
                doorBreakingDamage: 4,
                canDodgeroll: Phase != AttackPhase.FleeToHeal && Phase != AttackPhase.Healing,
                canPounce:    Phase == AttackPhase.Idle);

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
                else if (wallLOS || gnpc.WaypointTimer > 0)
                    _wallBlockedTimer = Math.Max(0, _wallBlockedTimer - 3);

                // > 1 s stuck: freeze so the Tier-2 route scan can work without jump-bounce noise.
                if (_wallBlockedTimer > 60 && !wallLOS)
                {
                    NPC.velocity.X *= 0.15f;            // nearly stop — jump code needs velocity > 0
                    if (gnpc.BoredTimer < 21) gnpc.BoredTimer = 21; // wake scanner
                }

                // > 5 s with no progress: teleport.
                if (_wallBlockedTimer > 300)
                {
                    _wallBlockedTimer = 0;
                    if (gnpc.TeleportCountdown == 0 && Main.netMode != NetmodeID.MultiplayerClient)
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
                    if (_retreatBeforeTeleportTimer == 0 && gnpc.TeleportCountdown == 0
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
                        _usingSecondaryRanged = wantSecondary &&
                            (!wantPrimary || Main.rand.Next(100) < SecondaryRangedChance);

                        // Cache all burst-level values so every phase in this burst is consistent.
                        _activeRangedItemType       = _usingSecondaryRanged ? SecondaryRangedWeaponItemType : RangedWeaponItemType;
                        _activeRangedStyle          = _usingSecondaryRanged ? SecondaryRangedAnimStyle      : RangedAnimStyle;
                        _activeRangedFlashColor     = _usingSecondaryRanged ? SecondaryRangedFlashColor     : RangedTelegraphFlashColor;
                        _activeRangedTelegraphTicks = _usingSecondaryRanged ? SecondaryRangedTelegraphTicks : RangedTelegraphTicks;
                        _activeRangedAttackTicks    = _usingSecondaryRanged ? SecondaryRangedAttackTicks    : RangedAttackTicks;
                        _activeRangedRecoveryTicks  = _usingSecondaryRanged ? SecondaryRangedRecoveryTicks  : RangedRecoveryTicks;

                        int standingChance = _usingSecondaryRanged ? SecondaryStandingRangedChance : StandingRangedChance;
                        _standingShot = Main.rand.Next(100) < standingChance;

                        // Shot count: secondary weapons default to 1 shot (e.g. crossbow).
                        int burstMax = _usingSecondaryRanged ? SecondaryMaxRangedBurst : MaxRangedBurst;
                        _rangedShotsRemaining = !_usingSecondaryRanged && SingleRangedBurstChance > 0
                                                && Main.rand.Next(100) < SingleRangedBurstChance
                            ? 1
                            : Math.Max(1, Main.rand.Next(1, burstMax + 1));

                        // ── Crossbow burst-pattern selection (secondary ranged only) ──────
                        // When SecondaryRangedBurstPatterns is defined the classic _rangedShotsRemaining
                        // counter is replaced by a pause-array that drives the shot sequence.
                        _interShotPauses     = null;
                        _interShotPauseIndex = 0;
                        if (_usingSecondaryRanged
                            && SecondaryRangedBurstPatterns != null
                            && SecondaryRangedBurstPatterns.Length > 0)
                        {
                            int patIdx = PickPatternByChance(SecondaryRangedBurstChances,
                                                             SecondaryRangedBurstPatterns.Length);
                            _interShotPauses = SecondaryRangedBurstPatterns[patIdx];

                            // Add extra telegraph ticks for this pattern (e.g. heavier patterns telegraph longer).
                            if (SecondaryRangedBurstTelegraphExtras != null
                                && patIdx < SecondaryRangedBurstTelegraphExtras.Length)
                                _activeRangedTelegraphTicks += SecondaryRangedBurstTelegraphExtras[patIdx];

                            // Pattern-specific flash color overrides the base secondary flash color.
                            if (SecondaryRangedBurstFlashColors != null
                                && patIdx < SecondaryRangedBurstFlashColors.Length)
                                _activeRangedFlashColor = SecondaryRangedBurstFlashColors[patIdx];
                        }

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
                    if (_standingShot)
                        NPC.velocity.X *= 0.10f; // planted shot — brake to nearly stopped
                    else
                        SlowDown();               // mobile shot — decelerate but still drifting
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
                    if (_standingShot)
                        NPC.velocity.X *= 0.25f; // planted shot — bleed off any residual momentum
                    else
                        SlowDown();               // mobile shot — continue decelerating through follow-through
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
                    if (_standingShot)
                        NPC.velocity.X *= 0.10f; // stay planted during the pause
                    else
                        SlowDown();
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
            }
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

        protected void EnterPhase(AttackPhase phase, int duration)
        {
            Phase      = phase;
            PhaseTimer = duration;
            // Each new telegraph phase gets its own flash.
            if (phase == AttackPhase.MeleeTelegraph || phase == AttackPhase.StabTelegraph
                || phase == AttackPhase.RangedTelegraph
                || phase == AttackPhase.SpearTelegraph || phase == AttackPhase.MagicTelegraph)
                _flashFired = false;
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
        protected void TryMeleeHit(float reach = -1f)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            float r = reach < 0 ? MeleeRange * 0.7f : reach;
            Vector2 tip = NPC.Center + new Vector2(NPC.direction * r, -8f);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), tip, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.InvaderMeleeHitbox>(),
                MeleeDamage, 3f, Main.myPlayer, NPC.whoAmI);
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
