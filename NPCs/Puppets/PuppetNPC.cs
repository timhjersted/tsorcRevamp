using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Abstract base for human-sized puppet enemies rendered via the player draw pipeline.
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
    /// • Any projectile fired BY a puppet must be hostile=true, friendly=false.
    ///   Player-friendly projectiles will damage the puppet itself and not the player.
    ///   Create a Projectiles/Enemy/ copy for every weapon the puppet uses at range.
    /// </summary>
    public abstract class PuppetNPC : ModNPC, IStaggerable
    {
        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        // ── Invasion banner ───────────────────────────────────────────────────────
        /// <summary>
        /// The name shown in the "INVADED BY ____" banner when this puppet spawns.
        /// Override in each concrete class.  Rendered in uppercase automatically.
        /// </summary>
        protected virtual string InvaderTitle => "UNKNOWN INVADER";

        /// <summary>
        /// When <c>false</c>, this puppet enemy is a REGULAR enemy, not an invasion: it never shows the
        /// "INVADED BY ___" banner. It still gets the full puppet rendering / combat kit — it just spawns
        /// via its own <see cref="ModNPC.SpawnChance"/> like any normal NPC. Default <c>true</c> (invasion encounter).
        /// </summary>
        protected virtual bool AnnounceInvasion => true;
        // Ordinary enemies converted to puppets can retain their original zero-contact-damage scaling.
        public virtual bool UsesPuppetDifficultyScaling => true;

        /// <summary>Override for phase-transition puppets which are not the end of an invasion.</summary>
        protected virtual bool AnnounceInvaderDefeat => AnnounceInvasion;

        /// <summary>Only specifically designated invaders use the great-invader banner.</summary>
        protected virtual bool IsGreatInvader => false;

        internal bool ShouldAnnounceInvaderDefeat => AnnounceInvaderDefeat;
        internal EncounterBannerStyle InvaderDefeatBanner => IsGreatInvader
            ? EncounterBannerStyle.GreatInvaderVanquished
            : EncounterBannerStyle.InvaderVanquished;

        // ── Death & despawn dust ─────────────────────────────────────────────────
        // Two distinct events share one dust-burst shape: a real kill (NPC.life <= 0, via HitEffect)
        // and a party-wipe despawn (NPC.life still positive, the fight is simply given up on — see
        // PartyWipeDespawnHandler below). Every subclass gets both for free; override the dust type
        // (and, if a subclass is thematically distinct like a ghost, the tint) rather than re-deriving
        // this per invader. The two defaults are deliberately DIFFERENT dust, not one inheriting from
        // the other: most invaders bleed when actually killed, but a party-wipe despawn is a fade-away,
        // not a wound, so it defaults to Shadowflame instead of Blood.
        protected virtual int OnKillDustType => DustID.Blood;
        protected virtual Color OnKillDustColor => default; // default(Color) = dust's own natural color
        protected virtual int OnKillDustCount => 150;
        protected virtual float OnKillDustTravelMult => 2f; // multiple of NPC.height the burst travels

        protected virtual int DespawnDustType => DustID.Shadowflame;
        protected virtual Color DespawnDustColor => default;
        protected virtual int DespawnDustCount => 300;
        protected virtual float DespawnDustTravelMult => 3.5f;

        /// <summary>Broadcast when the party-wipe despawn below fires. Override for a bespoke line
        /// (and localize it, like DreadWraith does) — this default is intentionally generic and NOT
        /// localized, since most invaders don't need a unique line for an edge case this rare.</summary>
        protected virtual string DespawnFlavorText => $"{InvaderTitle} melts away, its hunt unfinished...";
        protected virtual Color DespawnFlavorColor => Color.White;

        /// <summary>False for summoned copies whose owner already runs the encounter's despawn (and removes
        /// them): they skip the party-wipe check, so they never broadcast a second despawn line.</summary>
        protected virtual bool DespawnsOnPartyWipe => true;

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            EmitDeathDustBurst(OnKillDustType, OnKillDustColor, OnKillDustCount, OnKillDustTravelMult);
        }

        /// <summary>One-shot radial dust explosion shared by HitEffect and the party-wipe despawn.
        /// Spawns across the NPC's own bounding box (matching vanilla's own Dust.NewDust spread) with an
        /// outward speed picked so the dust travels roughly travelMult * NPC.height before drag stops it.
        /// Generic (non-special-cased) dust decays ~0.97/tick under noGravity, which sums to roughly
        /// speed * 33 total displacement — an approximation from vanilla's drag constant, not measured
        /// on screen, so treat travelMult as a starting point rather than an exact figure.</summary>
        private void EmitDeathDustBurst(int dustType, Color tint, int count, float travelMult)
        {
            // +-0.5 per particle so a single call already reads as "3 to 4x", not one fixed radius.
            float travelDistance = NPC.height * (travelMult + Main.rand.NextFloat(-0.5f, 0.5f));
            float speed = travelDistance / 33f;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(speed, speed) * Main.rand.NextFloat(0.4f, 1f);
                int dustIndex = Dust.NewDust(
                    NPC.position, NPC.width, NPC.height,
                    dustType, velocity.X, velocity.Y, 0, tint, Main.rand.NextFloat(1.1f, 2.2f));
                Main.dust[dustIndex].noGravity = true;
            }
        }

        // ── Despawn on party wipe ────────────────────────────────────────────────
        // The mod-wide convention every boss already uses — see NPCDespawnHandler (NPCs/NPCDespawnHandler.cs)
        // — reused as-is rather than reinvented: it tracks whichever players were present and only
        // despawns once every one of them has died at least once, so nobody can dodge the fight by
        // staying out of range while a partner keeps it alive. Lazily built (rather than requiring every
        // subclass to construct one in SetDefaults, like BossBase does) so this is free for every invader.
        private NPCDespawnHandler _partyWipeDespawnHandler;
        private NPCDespawnHandler PartyWipeDespawnHandler
            => _partyWipeDespawnHandler ??= new NPCDespawnHandler(DespawnFlavorText, DespawnFlavorColor, DespawnDustType);

        /// <summary>Called once when every tracked player has died and this encounter begins its
        /// fade-out. Subclasses can stop encounter-owned projectiles/sounds here; this is deliberately
        /// separate from <see cref="OnKill"/>, because a party wipe is not an NPC death.</summary>
        protected virtual void OnPartyWipeDespawnStarted()
        {
            // The handler takes 240 ticks to fade the encounter out. Do not leave a telegraph, attack
            // runtime, or held weapon frozen on-screen for that interval.
            // NPCDespawnHandler describes this as setting a 240-tick lifetime, but the handler itself
            // only owns an internal counter. Give the NPC that lifetime here as a deterministic
            // backstop: a puppet can never remain active forever if a specialised AI stops reaching
            // the handler while it is disappearing.
            NPC.timeLeft = 240;
            CancelAttackRuntimeV2(clearCombo: true);
            _bladeArmed = false;
            _jumpSlashLaunched = false;
            _comboLeapLaunched = false;
            _risingUppercutFallHoldTimer = 0;
            _weaponVisible = false;
            NPC.velocity = Vector2.Zero;
            EnterPhase(AttackPhase.Idle, 0);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.AttackCommitted = false;
            globalNPC.AttackTelegraphing = false;
            globalNPC.SuppressDodgeBlink = false;
            // A smoke teleport normally advances inside the main combat loop. Party-wipe fading exits
            // that loop deliberately, so its countdown must be cancelled rather than left holding an
            // invisible, frozen puppet at the departure cloud.
            globalNPC.TeleportCountdown = 0;
            globalNPC.TeleportAppearanceTimer = 0;
            globalNPC.TeleportTelegraph = Vector2.Zero;
            NPC.alpha = 0;
        }

        /// <summary>Runs the despawn check and returns whether the encounter is currently fading out.
        /// NPC.life is still positive here; this is a disappearance, not a kill, so it deliberately
        /// doesn't go through HitEffect.</summary>
        private bool UpdatePartyWipeDespawn()
        {
            bool wasDespawning = PartyWipeDespawnHandler.IsDespawning;
            bool aboutToDespawn = PartyWipeDespawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (!wasDespawning && PartyWipeDespawnHandler.IsDespawning)
            {
                OnPartyWipeDespawnStarted();
                if (Main.netMode == NetmodeID.Server)
                {
                    NPC.netUpdate = true;
                }
            }

            if (aboutToDespawn && !Main.dedServ)
            {
                EmitDeathDustBurst(DespawnDustType, DespawnDustColor, DespawnDustCount, DespawnDustTravelMult);
            }

            return PartyWipeDespawnHandler.IsDespawning;
        }

        // ── Layer coordination ────────────────────────────────────────────────────
        /// <summary>
        /// Set to <c>this</c> for the exact duration of a <see cref="Main.PlayerRenderer.DrawPlayer"/>
        /// call so that <see cref="PuppetWeaponDrawLayer"/> knows which puppet to draw for.
        /// Null at all other times — the layer is a no-op for normal players.
        /// </summary>
        internal static PuppetNPC DrawingPuppetFor;

        /// <summary>Draw color captured in PreDraw, read by <see cref="PuppetWeaponDrawLayer"/>.</summary>
        internal Color _layerDrawColor;

        // ── Puppet ────────────────────────────────────────────────────────────────
        private Player _puppet;
        private Item _meleeItemCache;
        private Item _rangedItemCache;
        private Item _secondaryRangedItemCache;
        private Item _magicItemCache;
        private int  _cachedMeleeType  = -1;
        private int  _cachedRangedType = -1;
        private int  _cachedSecondaryRangedType = -1;
        private int  _cachedMagicType = -1;
        private Player.CompositeArmStretchAmount _twoHandedBackStretch = Player.CompositeArmStretchAmount.ThreeQuarters;
        private float _twoHandedBackRotation;
        private Vector2 _twoHandedBackHandWorld;
        private Vector2 _twoHandedBackGripTargetWorld;
        private float _twoHandedBackGripError;
        // Bow string-draw pose (UseBowStringDrawPose). Visual only: re-derived every frame from the
        // synced Phase/PhaseTimer in TickBowStringDrawPose, so none of it goes over the network.
        private AttackPhase _bowPoseTrackedPhase;
        private int _bowPoseLastTimer;
        private int _bowPosePhaseTicks = 1;
        private Player.CompositeArmStretchAmount _bowStringHandStretch = Player.CompositeArmStretchAmount.Full;
        private float _bowStringHandRotation;
        private bool _bowStringHeld;

        // ── Loadout ───────────────────────────────────────────────────────────────
        protected abstract int HeadArmorItemType  { get; }
        protected abstract int BodyArmorItemType  { get; }
        protected abstract int LegsArmorItemType  { get; }
        /// <summary>Source-cell multiplier used by an alternate armor template. Terraria's player
        /// pipeline is hard-coded around 40x56 cells, so large templates are expanded only after it
        /// has built its draw data. The source sheets must use this exact multiple on every cell.</summary>
        protected virtual int ArmorTemplateScale => 1;
        /// <summary>Vanilla dye items applied to the puppet's visible armor slots.  Zero leaves the slot undyed.</summary>
        protected virtual int HeadArmorDyeItemType => 0;
        protected virtual int BodyArmorDyeItemType => 0;
        protected virtual int LegsArmorDyeItemType => 0;
        /// <summary>Vanilla dye item applied to <see cref="WingsAccessoryItemType"/>.  Zero leaves the wings undyed.</summary>
        protected virtual int WingsDyeItemType => 0;
        /// <summary>Additional always-visible vanilla accessories and their corresponding dye items.</summary>
        protected virtual int[] AccessoryItemTypes => Array.Empty<int>();
        protected virtual int[] AccessoryDyeItemTypes => Array.Empty<int>();
        protected virtual Color PuppetSkinColor => new Color(255, 125, 90);
        protected virtual Color PuppetEyeColor => new Color(105, 90, 75);
        protected virtual int PuppetSkinVariant => 0;
        /// <summary>Visual-only scale passed through Terraria's complete player draw pipeline.
        /// This enlarges the puppet, armor, accessories, and custom held-weapon layer together
        /// without changing the NPC hitbox or any combat reach calculations.</summary>
        protected virtual float PuppetDrawScale => 1f;
        /// <summary>Visual-only world-space offset applied to the complete puppet rig, including its
        /// held weapon. It does not move the NPC hitbox; use for small authored tell vibrations.</summary>
        protected virtual Vector2 PuppetVisualOffset => Vector2.Zero;

        /// <summary>Which half of the body sheet's torso/shoulder cells this puppet draws from.
        /// Vanilla's CreateCompositeData shifts ONLY the torso and the two shoulder caps down two
        /// rows for a female frame (`pt.Y += 2`), so on a 9x4 composite sheet male reads rows 1-2 of
        /// columns 1-2 and female reads rows 3-4. The composite arm columns (8 and 9) are indexed by
        /// stretch amount, not gender, and are shared by both. Exists so a boss whose sheet has
        /// better art on one half can pick it without re-cutting the sheet.</summary>
        protected virtual bool PuppetIsMale => true;

        /// <summary>Suppresses the two static composite shoulder-cap cells (columns 1-2, row 2 male /
        /// row 4 female on a 9x4 body sheet) for this puppet's whole draw. Set this when the sheet
        /// draws its shoulder into the TORSO cell instead of the cap cell: vanilla draws both
        /// unconditionally, so a sheet carrying the pad in the torso renders two overlapping
        /// shoulders. Suppressing the cap also sidesteps vanilla hiding it on the jump frame
        /// (CreateCompositeData case 5), so the shoulder then looks identical grounded and airborne.
        /// Leave false for a sheet authored the vanilla way, with a bare torso and a filled cap cell.</summary>
        internal virtual bool SuppressCompositeShoulderCaps => false;
        /// <summary>Source dimensions for the player-draw rig. Normally these mirror the NPC body,
        /// but a spectral puppet can enlarge its physical body while retaining an unscaled source
        /// that is then enlarged once by its overlay transform.</summary>
        protected virtual int PuppetVisualWidth => NPC.width;
        protected virtual int PuppetVisualHeight => NPC.height;
        private Vector2 PuppetVisualPosition =>
            NPC.Bottom - new Vector2(PuppetVisualWidth, PuppetVisualHeight) + PuppetVisualOffset;
        private Vector2 PuppetVisualCenter =>
            NPC.Bottom - new Vector2(0f, PuppetVisualHeight * 0.5f) + PuppetVisualOffset;
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

        /// <summary>Thickness (px) of the tracked melee "blade" capsule used by <see cref="TickBladeHit"/>.
        /// Chosen generously enough that the fastest continuous motion (Spin, +0.28 rad/tick) can't
        /// tunnel a stationary target between ticks at typical combo reach — override per weapon
        /// for a visibly thinner/wider blade once playtested.</summary>
        protected virtual float MeleeBladeWidth => 48f;

        // ── Swing-polish opt-ins (2026-07) ──────────────────────────────────────────
        // All default OFF so every existing puppet keeps its exact current swing behavior.
        // An puppet flips these on individually to pilot the new shared swing-arc math
        // (ported from the BroadswordRework player-weapon system) without touching anyone else.

        /// <summary>Ease combo-swing rotation through <see cref="SwingEase.DefaultCurve"/> (slow
        /// wind-up, fast strike, slight settle) instead of a constant-speed linear lerp. Purely
        /// reshapes the timing of the (already tracked) blade capsule — no reach/damage change.</summary>
        protected virtual bool UseSwingEasing => false;

        /// <summary>When true, <see cref="ComboMotion.Spin"/> ramps its angular speed up across the
        /// telegraph and back down over its own <c>EaseOutTicks</c> (reused as "ticks to ease down"),
        /// instead of spinning at one constant rate through wind-up, strike and stop alike. Default
        /// false preserves every existing Spin user (it never reads <see cref="MeleeComboStep.Ease"/>
        /// at all) exactly as authored.</summary>
        protected virtual bool UseEasedSpin => false;

        /// <summary>Flip which direction a combo's arc runs on alternating swings (same arc shape,
        /// mirrored start/end) so the same combo doesn't read identically every single time.</summary>
        protected virtual bool UseAlternateFlip => false;

        /// <summary>Bias a combo's arc center toward the target's relative height, captured once
        /// per swing at combo start — so the puppet's swing actually reaches for an airborne or
        /// crouched target instead of always arcing level with the ground.</summary>
        protected virtual bool UseAimAdaptiveArc => false;

        /// <summary>Draw an arc-shaped slash swoosh (see <see cref="DrawSlashToLayer"/>) tinted to
        /// <see cref="SlashVFXColor"/>, tracking the same angle/reach TickBladeHit uses for real hit
        /// detection — so the VFX is an honest preview of the actual hitbox, not just decoration.</summary>
        protected virtual bool HasSlashVFX => false;

        /// <summary>Tint for the slash swoosh when <see cref="HasSlashVFX"/> is true.</summary>
        protected virtual Color SlashVFXColor => Color.White;

        /// <summary>Peak opacity and draw scale for the slash swoosh.</summary>
        protected virtual float SlashVFXOpacity => 0.4f;
        protected virtual float SlashVFXScale => 1f;
        protected virtual bool SuppressSlashVFXForCurrentPhase => false;

        /// <summary>Per-phase escape hatch for attacks that temporarily replace the melee weapon
        /// with a bespoke holdout. Returning true suppresses the ordinary held-item sprite.</summary>
        protected virtual bool DrawSpecialHeldWeapon(ref PlayerDrawSet drawInfo) => false;

        /// <summary>Per-puppet cadence multiplier for the vanilla 14-frame walk cycle. Fast bosses
        /// can lower this without changing their actual movement speed.</summary>
        protected virtual float WalkAnimationSpeedMultiplier => 1f;

        /// <summary>Extra rotation applied only to overhead-style windups. Zero preserves the
        /// established archetype pose; individual bosses can pull the blade farther behind the head.</summary>
        protected virtual float OverheadWindupOvershoot => 0f;

        /// <summary>Carry a leap slam overhead until terrain contact is imminent.</summary>
        protected virtual bool UseLandingTimedLeapSlam => false;
        private float _leapSlamSwingProgress;
        private const int LeapSlamDownswingTicks = 10;

        /// <summary>A landing-timed LeapSlam whose step sets LeapStrikeRange began its downswing in
        /// the air because the target came into range. The blade then sweeps every tick until the
        /// landing, and the landing does not hit a second time.</summary>
        private bool _leapStrikeStarted;

        /// <summary>This LeapSlam/LeapThrust started in wing flight: BeginLeapAttack ended the flight and
        /// launched a ballistic dive straight at the target instead of a jump. The in-range strike may then
        /// fire before the apex, because a dive at a raised target is still rising when it arrives.</summary>
        private bool _leapLaunchedFromAir;

        /// <summary>Swing-space offset added to the landing-timed leap-slam pose so an aerial dive's
        /// overhead carry and downswing point at the target in any direction. Tracks the target during
        /// the carry and freezes when the strike starts. Always 0 for a ground leap.</summary>
        private float _leapSlamAimOffset;

        /// <summary>A LeapSlam step whose LeapApexRetargetStrength &gt; 0 has already re-aimed its
        /// descent this flight. Fires once, on the exact tick the apex is reached.</summary>
        private bool _leapApexRetargeted;
        /// <summary>Ticks spent rising during the current LeapSlam/LeapThrust flight (frozen once the
        /// apex is reached). Used to size the boosted-gravity descent's remaining airtime at apex
        /// retarget — see <see cref="RetargetLeapAtApex"/>.</summary>
        private int _comboLeapAscentTicks;

        // Logical arm angles that put the blade above the head, then down-forward at impact.
        // Ground-directed slams deliberately exclude player-height aim bias. Virtual so a combo can
        // carry the blade in its own cocked pose (Gwyn's Wrath Flurry holds its raised swipe pose).
        protected virtual float LeapSlamCarryRotation => MathHelper.ToRadians(-105f + MeleeNaturalRestAngleDeg)
            - FrontHandWeapon.RotationOffset;
        protected virtual float LeapSlamImpactRotation => MathHelper.ToRadians(75f + MeleeNaturalRestAngleDeg)
            - FrontHandWeapon.RotationOffset;

        private void UpdateLeapSlamPose(bool landed)
        {
            if (landed)
                _leapSlamSwingProgress = 1f;
            else if (_leapStrikeStarted)
            {
                // In-range strike: the downswing runs on its own clock, landing or not.
                _leapSlamSwingProgress = Math.Min(1f, _leapSlamSwingProgress + 1f / LeapSlamDownswingTicks);
            }
            else if (NPC.velocity.Y > 0f)
            {
                // Project the same body collision a few frames ahead, including platforms and
                // horizontal travel. A fixed launch-height timer fails on raised/lowered terrain.
                Vector2 position = NPC.position;
                Vector2 velocity = NPC.velocity;
                for (int tick = 1; tick <= LeapSlamDownswingTicks; tick++)
                {
                    velocity.Y = Math.Min(velocity.Y + 0.3f, 10f);
                    Vector2 permitted = Collision.TileCollision(position, velocity, NPC.width, NPC.height);
                    if (permitted.Y < velocity.Y)
                    {
                        _leapSlamSwingProgress = Math.Max(_leapSlamSwingProgress,
                            1f - tick / (float)LeapSlamDownswingTicks);
                        break;
                    }
                    position += permitted;
                }
            }
            // _leapSlamAimOffset is 0 for ground leaps; an aerial dive rotates the whole arc at the target.
            float slamPose = MathHelper.SmoothStep(LeapSlamCarryRotation, LeapSlamImpactRotation,
                _leapSlamSwingProgress);
            _weaponRotation = slamPose + _leapSlamAimOffset;
        }

        /// <summary>Lets a puppet widen the authored start/end rotations of ordinary combo arcs
        /// without changing the shared weapon-archetype tables. Damage still follows the same
        /// active clock and swept blade used by the draw pose.</summary>
        protected virtual void ModifyMeleeArcEndpoints(
            ComboMotion motion, ref float startRotation, ref float endRotation) { }

        /// <summary>How long the weapon remains at the completed strike pose before beginning the
        /// next combo step's windup. The combo's PostStepPause must be longer than this value when
        /// a visible transition to the next start pose is desired.</summary>
        protected virtual int MeleeComboInterStepLingerTicks => 0;

        /// <summary>How long the completed strike remains visibly planted at the start of melee
        /// recovery. This is visual-only: the blade is no longer armed during the hold.</summary>
        protected virtual int MeleeRecoveryLingerTicks => 0;

        // Telegraph minimum: 30 ticks = 0.5 s.  Long enough for the player to read the
        // wind-up arc (sword raises, holds at apex, then swings).  Heavier attacks override
        // upward; lighter attacks should not go below this floor.
        protected virtual int MeleeTelegraphTicks  => 35;
        // MeleeAttackTicks is the fallback swing duration when item useAnimation is unavailable
        // within the attack phase rather than bleeding into recovery.
        protected virtual int MeleeAttackTicks     => DefaultWeaponAnimMax;
        /// <summary>Optional Weighted curve for the plain one-shot swing (MeleeAttack). Unset (default) keeps the
        /// legacy ease over the weapon's use animation. Set, MeleeAttackTicks should equal its TotalTicks, and the
        /// blade disarms after its LiveTicks.</summary>
        protected virtual WeightedSwing MeleeAttackCurve => default;
        protected virtual int MeleeRecoveryTicks   => 25;

        /// <summary>Recovery for the combo that just finished. Prefers the combo's own
        /// <see cref="MeleeCombo.RecoveryTicks"/> and falls back to the flat
        /// <see cref="MeleeRecoveryTicks"/>, so combo tables that do not set it are unchanged.
        /// Single-clock V2 clips already carry their own recovery and never reach this.</summary>
        private int ActiveComboRecoveryTicks =>
            _activeMeleeCombo.RecoveryTicks > 0 ? _activeMeleeCombo.RecoveryTicks : MeleeRecoveryTicks;

        protected virtual int StabTelegraphTicks   => 38;
        protected virtual int StabAttackTicks      => 8;
        protected virtual int StabRecoveryTicks    => 32;

        protected virtual int RangedTelegraphTicks => 38;
        protected virtual int RangedAttackTicks    => 10;
        protected virtual int RangedRecoveryTicks  => 55;

        /// How many ticks must pass after a ranged burst before ranged is allowed again.
        /// Gives the puppet time to close distance and fight in melee first.
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
        protected enum RangedStyle { Throw, Crossbow, Bow, Staff }
        /// <summary>Animation style for the primary ranged weapon.  Override per subclass.</summary>
        protected virtual RangedStyle RangedAnimStyle => RangedStyle.Throw;
        /// <summary>Client-side cosmetic hook for a ranged charge. Progress reaches 1 immediately before release.</summary>
        protected virtual void DoRangedTelegraphVFX(bool secondary, float progress) { }

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

        /// <summary>Gate for using the secondary ranged weapon at all (both the normal band and the
        /// panic backhop).  Override to unlock the secondary conditionally — e.g. only below half HP.
        /// Default true.</summary>
        protected virtual bool SecondaryRangedAvailable => true;

        // ── Secondary-ranged "panic backhop" (optional) ───────────────────────────
        // A proactive spacing tool: when the player closes to within SecondaryRangedBackhopRange and
        // the secondary weapon is off cooldown, the puppet hops AWAY (still facing the player) and
        // fires a single secondary shot — independent of the normal SecondaryRangedMinRange band, so it
        // works point-blank.  Default range 0 = disabled (no behavior change for other puppets).
        protected virtual float SecondaryRangedBackhopRange   => 0f;
        /// <summary>Chance (0–100) to backhop when the player is inside <see cref="SecondaryRangedBackhopRange"/>.</summary>
        protected virtual int   SecondaryRangedBackhopChance  => 50;
        /// <summary>Horizontal hop-away speed (px/frame).</summary>
        protected virtual float SecondaryRangedBackhopSpeed   => 6f;
        /// <summary>Upward hop speed (px/frame) for the little arc.</summary>
        protected virtual float SecondaryRangedBackhopUpSpeed => 5f;

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
        /// <summary>Optional primary ranged burst patterns. Same format as SecondaryRangedBurstPatterns.</summary>
        protected virtual int[][]  PrimaryRangedBurstPatterns          => null;
        protected virtual int[]    PrimaryRangedBurstTelegraphExtras    => null;
        protected virtual Color[]  PrimaryRangedBurstFlashColors        => null;
        protected virtual int[]    PrimaryRangedBurstChances            => null;
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
        /// <summary>Chance (0–100) to choose magic over a ranged attack when BOTH are in range.  0 (default)
        /// keeps magic as a gap-filler (only when no ranged option is available).</summary>
        protected virtual int   MagicPreferenceChance => 0;
        /// <summary>Flash color for the spear-poke and magic-cast telegraphs.</summary>
        protected virtual Color SpearTelegraphFlashColor => Color.LightYellow;
        protected virtual Color MagicTelegraphFlashColor => Color.MediumPurple;
        /// <summary>How many ticks before release the magic warning flash appears.</summary>
        protected virtual int MagicTelegraphFlashLeadTicks => 30;
        /// <summary>Opt-in for a continuous, subclass-authored staff/arm raising pose.</summary>
        protected virtual bool UseAuthoredMagicCastPose => false;
        /// <summary>World-facing arm angles used by the authored magic pose.</summary>
        protected virtual float MagicCastStartRotation => 0f;
        protected virtual float MagicCastEndRotation => -1.4f;
        /// <summary>Draw-only correction for a magic weapon's natural texture angle.</summary>
        protected virtual float MagicWeaponRotationOffset => 0f;
        protected virtual bool MirrorMagicWeaponRotationByFacing => false;
        /// <summary>Number of recovery ticks for which the magic weapon remains visibly held.</summary>
        protected virtual int MagicWeaponRecoveryHoldTicks => 0;
        protected bool IsHoldingMagicWeaponDuringRecovery =>
            Phase == AttackPhase.MagicRecovery
            && PhaseTimer > Math.Max(0, MagicRecoveryTicks - MagicWeaponRecoveryHoldTicks);
        /// <summary>Client-side cosmetic hook sampled after the magic staff pose updates.</summary>
        protected virtual void DoMagicTelegraphVFX(float progress) { }
        /// <summary>Whether the magic telegraph and cast brake the puppet to a stop. Default true keeps the
        /// planted cast. False lets RunMovementAI (which runs every tick before the phase switch) keep
        /// kiting through the whole cast, so a caster can backpedal while it channels and fires.</summary>
        protected virtual bool BrakeDuringMagicCast => true;

        // ── Fire breath (optional, opt-in per subclass) ───────────────────────────
        /// <summary>Master toggle: when true this puppet can perform a sustained fire-breath attack
        /// (BreathTelegraph → Breathing → BreathRecovery).  Override <see cref="DoBreathWindup"/> and
        /// <see cref="DoBreathTick"/> to supply the telegraph VFX and the breath projectiles.</summary>
        protected virtual bool  CanBreathe            => false;
        /// <summary>Max distance at which breath can begin.</summary>
        protected virtual float BreathRange           => 560f;
        /// <summary>Won't breathe if the player is closer than this (point-blank).</summary>
        protected virtual float MinBreathRange        => 0f;
        /// <summary>Charge-up duration (the swelling-ember telegraph) before the stream releases.</summary>
        protected virtual int   BreathTelegraphTicks  => 150;
        /// <summary>Length of the active breath stream.</summary>
        protected virtual int   BreathDurationTicks   => 70;
        /// <summary>Recovery after the stream ends, before <see cref="BreathCooldownAfterUse"/> applies.</summary>
        protected virtual int   BreathRecoveryTicks   => 40;
        /// <summary>Ticks that must pass after a breath before another is allowed.</summary>
        protected virtual int   BreathCooldownAfterUse => 330;
        /// <summary>Chance (0–100) per eligible Idle tick to begin a breath when in range and off cooldown.</summary>
        protected virtual int   BreathChance          => 3;
        /// <summary>Flash color for the breath telegraph.</summary>
        protected virtual Color BreathTelegraphFlashColor => Color.OrangeRed;
        /// <summary>Opt-in pose for hand-cast breath attacks. Keeps the default breath presentation
        /// unchanged, while allowing a puppet to visibly extend its empty casting hand through the tell.</summary>
        protected virtual bool  UseAuthoredBreathHandPose => false;
        protected virtual float BreathHandStartRotation => -0.35f;
        protected virtual float BreathHandEndRotation => 0f;
        /// <summary>When true the puppet may begin a breath while airborne (and will strafe across to
        /// sweep the stream).  When false breath is grounded-only.</summary>
        protected virtual bool  BreathAllowedAirborne => false;
        /// <summary>When true, grounded Breathing walks forward at half TopSpeed instead of bleeding
        /// off momentum to a standstill - the stream then doubles as advancing pressure. Default false
        /// preserves every existing breather's planted-hose behavior.</summary>
        protected virtual bool  AdvanceDuringGroundedBreath => false;

        /// <summary>True for the full duration of the entire telegraph+attack window (not just the
        /// post-flash commit).  Set true to give this puppet hyper-armor — zero knockback, no poise
        /// build, can't be staggered — from the moment an attack telegraph starts until recovery.
        /// Default false preserves the original behavior (interruptible during the wind-up).</summary>
        protected virtual bool  HyperArmorDuringTelegraph => false;

        // ── Proactive projectile evasion (optional) ───────────────────────────────
        // SF4-mover puppets don't run the legacy BasicAI Agility dodge scan, so this is a self-
        // contained version: each idle tick, scan for an incoming aimed friendly projectile and (rolling
        // the GlobalNPC Agility stat) either jump over it or i-frame dodge in place.  Reuses the existing
        // mover-agnostic DodgeTimer i-frames.  Default off; set Agility on the NPC to tune frequency.
        protected virtual bool EvadesProjectiles => false;
        /// <summary>Detection radius (px) for an incoming aimed projectile.</summary>
        protected virtual float ProjectileEvadeRange => 200f;

        /// <summary>Ordinary hits must not seize a puppet's neutral state or delay its next attack.
        /// Poise break is the shared interruption contract. Override only for a deliberately authored
        /// puppet whose reactive guard/evasion is bounded and cannot be repeatedly rearmed.</summary>
        protected virtual bool AllowReactiveDefense => false;
        internal bool AllowsReactiveDefense => AllowReactiveDefense;

        // ── Quick-step dodge (shared with FighterAI via tsorcRevampAIs.TickQuickStep) ──────
        // ON-HIT quick-step additionally requires AllowReactiveDefense: ordinary hits otherwise keep
        // the puppet's attack sequencer running. PREEMPTIVE quick-step is separately opt-in: when the
        // player is mid-swing in melee range, roll PreemptiveQuickStepChance to dash THROUGH them.
        /// <summary>Chance (0–100) to preemptively quick-step when the player is mid-swing in melee range.
        /// 0 = no proactive quick-step.</summary>
        protected virtual int   PreemptiveQuickStepChance => 0;
        /// <summary>Post-step recovery (ticks) before attacks/pursuit resume — the "can't instantly re-attack
        /// after the dash" window.  Tune with <see cref="QuickStepForwardRoom"/> for the spacing feel.</summary>
        protected virtual int   QuickStepRecoveryTicks => 30;
        /// <summary>Extra px past the player when dashing THROUGH them, so the step lands with room.</summary>
        protected virtual float QuickStepForwardRoom => 16f;

        // ── Cursed Knives (optional sustained throw attack) ────────────────────────
        // Hold a knife (telegraph) → throw 1–3 volleys of 3 knives that stick in tiles, swell with
        // black/purple dust for ~1.5 s, then burst into a lingering plague cloud.  Volley count + gap
        // scale with range.  Subclass supplies the projectile + held-knife sprite via the hooks below.
        protected virtual bool  CanThrowCursedKnives        => false;
        protected virtual int   CursedKnivesWeaponItemType  => -1;   // held-knife display sprite (-1 = none)
        protected virtual float CursedKnivesRange           => 760f;
        protected virtual float CursedKnivesMinRange        => 0f;
        protected virtual float CursedKnivesCloseRange      => 220f;  // ≤ this: 1 volley, tight spread
        protected virtual float CursedKnivesMidRange        => 470f;  // ≤ this: 2 volleys back-to-back
        protected virtual int   CursedKnivesChance          => 4;     // % per eligible idle tick
        protected virtual int   CursedKnivesTelegraphTicks  => 45;
        protected virtual int   CursedKnivesThrowTicks      => 12;    // active frames per volley
        protected virtual int   CursedKnivesBackToBackGap   => 10;    // pause between mid-range volleys
        protected virtual int   CursedKnivesFarGap          => 30;    // pause between far-range volleys
        protected virtual int   CursedKnivesRecoveryTicks   => 40;
        protected virtual int   CursedKnivesCooldownAfterUse => 420;
        protected virtual bool  CursedKnivesAllowedAirborne => true;
        protected virtual Color CursedKnivesTelegraphFlashColor => new Color(150, 60, 210);

        // ── Piercing Dash (optional medium/long-range sword-lunge attack) ──────────
        // Hold the weapon out toward the player (telegraph, sprite jitters in place) → dash straight
        // through them (a fresh melee hitbox each tick so a fast target can still be caught) → brief
        // recovery.  A stab variant may be rolled at telegraph start: on the first connecting hit it
        // stops the dash, holds the target impaled while the weapon arm slowly raises, then flicks them
        // away.  Subclass supplies all VFX/damage/heal specifics via the hooks below.
        protected virtual bool  CanPierce                  => false;
        protected virtual float PierceRange                => 700f;
        protected virtual float MinPierceRange             => 250f;
        protected virtual int   PierceChance                => 4;     // % per eligible idle tick
        protected virtual int   PierceTelegraphTicks       => 60;
        protected virtual int   PierceDashTicks            => 40;     // travel duration of the lunge
        protected virtual float PierceDashSpeed            => 15f;
        protected virtual int   PierceRecoveryTicks        => 90;
        /// <summary>Chance (0–100), rolled once at telegraph start, that this pierce becomes the
        /// grab-and-impale variant instead of a simple pass-through lunge.</summary>
        protected virtual int   PierceStabChance           => 50;
        /// <summary>Ticks spent holding the impaled target while the sword arm raises 0→90°.</summary>
        protected virtual int   PierceStabRaiseTicks       => 180;
        /// <summary>How long the 0→90° raise POSE actually takes to reach its end point, separate
        /// from <see cref="PierceStabRaiseTicks"/> (the whole hold's duration). Defaults to the full
        /// hold, i.e. today's behavior: the target visibly drifts into place for the entire hold.
        /// Shorten this on a subclass to snap into the raised pose quickly and then just HOLD there
        /// for the remainder — the target reads as stabbed in place immediately instead of needing
        /// several seconds to visibly arrive.</summary>
        protected virtual int   PierceStabRaiseAnimTicks   => PierceStabRaiseTicks;
        /// <summary>Ticks spent rotating back down and flicking the target away.</summary>
        protected virtual int   PierceStabFlickTicks       => 20;
        /// <summary>Optional hold between the start-of-flick effect and the actual release. The
        /// default remains immediate; bosses with a lingering blast can delay the launch so its
        /// timing matches the visual.</summary>
        protected virtual int   PierceStabFlickDelayTicks  => 0;
        /// <summary>Cooldown after the whole sequence (dash or stab) ends before another can begin.</summary>
        protected virtual int   PierceCooldownAfterUse     => 480;
        /// <summary>Extra dashes after one that doesn't connect (touched no one, or only a player in
        /// i-frames). Each repeat re-faces the current target through its own telegraph. 0 = never.</summary>
        protected virtual int   PierceWhiffRepeatCount     => 0;
        /// <summary>Telegraph length for those repeats; the first dash keeps <see cref="PierceTelegraphTicks"/>.</summary>
        protected virtual int   PierceRepeatTelegraphTicks => PierceTelegraphTicks;

        /// <summary>Called every tick of the PierceTelegraph phase, with elapsed ticks counting up from 0.
        /// Override to spawn the wind-up VFX (dust color etc. — vary by <see cref="IsPierceStab"/>).</summary>
        protected virtual void DoPierceWindup(int elapsed) { }
        /// <summary>Called every tick of the active dash, after the hitbox/impale logic has run for
        /// that tick.  Override for trail dust / sound.</summary>
        protected virtual void DoPierceDashTick() { }
        /// <summary>Fired once the instant the dash connects (before contact damage is applied).
        /// <paramref name="isStab"/> is <see cref="IsPierceStab"/> at the moment of the hit.</summary>
        protected virtual void OnPierceContact(Player target, bool isStab) { }
        /// <summary>Fired once per tick while the target is held impaled (PierceStabHold phase),
        /// with the current raise progress 0–1.  Override to keep the impaling weapon's visual in sync.</summary>
        protected virtual void DoPierceStabHoldTick(Player target, float raiseProgress01) { }
        /// <summary>Fired once when PierceStabFlick begins, before any optional release delay.</summary>
        protected virtual void OnPierceFlickStarted(Player target) { }
        /// <summary>Fired while a delayed flick is still holding its target.</summary>
        protected virtual void DoPierceStabFlickDelayTick(Player target, int elapsed) { }
        /// <summary>Fired once when the flick actually releases the target.
        /// Override for the heal / screenshake / launch — the base class does not apply these itself
        /// since the exact numbers are boss-specific.</summary>
        protected virtual void OnPierceFlick(Player target) { }

        /// <summary>True from the moment a pierce telegraph rolls the stab variant until the whole
        /// pierce sequence ends.  Read this from the VFX hooks above to branch color/behavior.</summary>
        protected bool IsPierceStab => _pierceIsStab;
        private bool  _pierceIsStab;
        private bool  _pierceHitConnected;
        private bool  _pierceContactDodged;       // the contact landed on a player in i-frames (rolled through)
        private int   _pierceRepeatsUsed;         // whiff repeats spent this sequence (PierceWhiffRepeatCount)
        private int   _pierceTelegraphTotalTicks; // length of the telegraph now playing (first or repeat)
        private int   _pierceDir;
        private Vector2 _pierceAnchorPos;
        private Player _pierceTarget;
        protected int  _pierceCooldown;

        // ── Jumping Downward Slash (optional jump attack, any range within a min/max band) ──
        // A genuine dodgeroll (backward movement + real i-frames via the shared DodgeTimer mechanism,
        // not a cosmetic hop) leads into a jump toward the player holding the sword cocked up-and-back,
        // then a half-circle downward swipe once close or on landing.
        protected virtual bool  CanJumpSlash              => false;
        protected virtual float JumpSlashMinRange         => 200f;
        protected virtual float JumpSlashMaxRange         => 500f;
        protected virtual int   JumpSlashChance           => 4;      // % per eligible idle tick
        protected virtual int   JumpSlashCooldownAfterUse => 420;

        /// <summary>Ticks of the backward dodgeroll (also the i-frame window).</summary>
        protected virtual int   JumpSlashDodgebackTicks    => 24;
        protected virtual float JumpSlashDodgebackSpeed    => 6f;
        /// <summary>Max airborne ticks before the swipe fires regardless (normally cut short by
        /// <see cref="JumpSlashTriggerRange"/> or landing).</summary>
        protected virtual int   JumpSlashRiseTicks         => 60;
        protected virtual float JumpSlashLaunchUpSpeed     => 9f;
        protected virtual float JumpSlashLaunchForwardSpeed => 6f;
        protected virtual float JumpSlashMaxForwardSpeed   => JumpSlashLaunchForwardSpeed;
        protected virtual float JumpSlashMaxUpSpeed        => JumpSlashLaunchUpSpeed;
        protected virtual float JumpSlashGravity           => 0.3f;
        /// <summary>Distance at which the swipe triggers mid-air, before landing.</summary>
        protected virtual float JumpSlashTriggerRange      => 90f;
        protected virtual int   JumpSlashAttackTicks       => 18;
        protected virtual int   JumpSlashRecoveryTicks     => 70;
        /// <summary>Optional Weighted curve for the swipe. Unset (default) keeps the legacy Smooth sweep; set,
        /// JumpSlashAttackTicks should equal its TotalTicks, and the blade disarms after its LiveTicks.</summary>
        protected virtual WeightedSwing JumpSlashCurve     => default;
        /// <summary>Swipe end pose, swing-space radians. The start is the -60° cocked pose the rise carries.</summary>
        protected virtual float JumpSlashEndRotation       => MathHelper.ToRadians(55f);

        /// <summary>Called every tick of the backward dodgeroll. Override for VFX/sound.</summary>
        protected virtual void DoJumpSlashDodgebackTick() { }
        /// <summary>Called every tick while airborne and rising/traveling toward the player.</summary>
        protected virtual void DoJumpSlashRiseTick() { }
        /// <summary>Fired once, the instant the half-circle swipe begins. Spawn the hit here
        /// (e.g. <c>TryMeleeHit()</c>) - the base class doesn't apply damage itself.</summary>
        protected virtual void DoJumpSlashAttack() { }

        private int _jumpSlashDir;
        private bool _jumpSlashLaunched;
        private float _jumpSlashFlightSpeed;
        protected int _jumpSlashCooldown;

        // ── Forward Flip Slash (optional spin-through jump attack) ─────────────────
        // A forward mid-air roll — genuinely i-framed via DodgeTimer, same as JumpSlashDodgeback —
        // held out and spinning continuously with damage active, closing distance THROUGH the target.
        // Lands into a held slam pose (screenshake + dirt) rather than an immediate recovery.
        protected virtual bool  CanFlipSlash               => false;
        protected virtual float FlipSlashMinRange          => 150f;
        protected virtual float FlipSlashMaxRange          => 450f;
        protected virtual int   FlipSlashChance            => 4;      // % per eligible idle tick
        protected virtual int   FlipSlashCooldownAfterUse  => 420;

        protected virtual float FlipSlashLaunchUpSpeed      => 8f;
        protected virtual float FlipSlashLaunchForwardSpeed => 7f;
        /// <summary>Safety cap on airborne ticks in case it never reads a clean landing.</summary>
        protected virtual int   FlipSlashRiseMaxTicks       => 70;
        /// <summary>Radians/tick the sword spins while airborne — tuned so a typical flight completes
        /// roughly one full revolution before landing.</summary>
        protected virtual float FlipSlashSpinSpeed          => 0.15f;
        /// <summary>Ticks the sword is held in the landing slam pose before returning to normal behavior.</summary>
        protected virtual int   FlipSlashLandHoldTicks      => 30;

        /// <summary>Called every tick while airborne and spinning. Override for trail VFX/sound.</summary>
        protected virtual void DoFlipSlashRiseTick() { }
        /// <summary>Fired once as the flip launches. Arm the spinning blade here (e.g. <c>TryMeleeHit()</c>):
        /// the base class then tests the real swept blade every airborne tick, so the spin only hits where
        /// the sword actually is. The base class doesn't apply damage itself.</summary>
        protected virtual void DoFlipSlashHit() { }
        /// <summary>Fired once on landing, before the slam pose hold begins. Override for the
        /// screenshake / dirt dust, and (with <see cref="UseFlipSlashLandingStrike"/>) to arm the strike.</summary>
        protected virtual void OnFlipSlashLand() { }

        // Opt-in landing strike. The airborne spin is phase-locked so the blade reaches
        // FlipSlashStrikeStartRotation at touchdown, then FlipSlashLand plays a Weighted overhead into the
        // ground (EaseIn + EaseOut ticks), live while blade speed is >= 30% of peak. FlipSlashLandHoldTicks
        // must cover the strike; whatever is left over is the planted hold.
        protected virtual bool  UseFlipSlashLandingStrike    => false;
        protected virtual float FlipSlashStrikeStartRotation => LeapSlamCarryRotation;
        protected virtual float FlipSlashStrikeEndRotation   => LeapSlamImpactRotation;
        protected virtual int   FlipSlashStrikeEaseInTicks   => 6;
        protected virtual int   FlipSlashStrikeEaseOutTicks  => 28;
        protected virtual float FlipSlashStrikeEaseOutDecay  => 6f;
        /// <summary>Fired once on the strike's peak-speed tick (EaseInTicks after touchdown), the frame the
        /// blade meets the ground. Override for the impact VFX / shake.</summary>
        protected virtual void OnFlipSlashStrikeContact() { }

        /// <summary>Strike ticks the blade stays live: the ease-in plus the ease-out span where speed is still
        /// >= 30% of peak, out * ln(1/0.3) / k = out * 1.204 / k (attack-timing-design §3).</summary>
        protected int FlipSlashStrikeLiveTicks => (int)Math.Ceiling(FlipSlashStrikeEaseInTicks
            + FlipSlashStrikeEaseOutTicks * 1.204f / Math.Max(0.1f, FlipSlashStrikeEaseOutDecay));

        private int  _flipSlashDir;
        protected int _flipSlashCooldown;
        private float _flipSlashLaunchBottomY;        // feet height at launch; the phase-lock assumes landing back at it
        private float _flipSlashSolvedSpinSpeed;      // rad/tick the phase-lock last solved
        private float _flipSlashStrikeStartRotation;  // blade angle at touchdown, re-wrapped beside the cocked pose

        // ── Abyss Slash (optional ranged sword-projectile chain) ───────────────────
        // Underhand → 170° arc → held "post" pose (still walking) → a swipe that fires a projectile
        // (via DoAbyssSlashFire) → NextAbyssSlashDelay decides whether another swipe follows and after
        // how long, so a subclass can chain any number of swipes with per-gap timing (or none at all).
        protected virtual bool  CanAbyssSlash              => false;
        protected virtual float AbyssSlashMinRange         => 250f;
        protected virtual float AbyssSlashMaxRange         => 900f;
        protected virtual int   AbyssSlashChance           => 4;      // % per eligible idle tick
        protected virtual int   AbyssSlashCooldownAfterUse => 300;

        /// <summary>Duration of the 170° arc portion of the wind-up.</summary>
        protected virtual int   AbyssSlashArcTicks         => 40;
        /// <summary>Ticks the "post" pose is held (still walking) after the arc completes.</summary>
        protected virtual int   AbyssSlashHoldTicks         => 20;
        protected virtual int   AbyssSlashSwipeTicks        => 16;
        /// <summary>Optional Weighted curve for each release swipe. Unset keeps the legacy Snap; set,
        /// AbyssSlashSwipeTicks should equal its TotalTicks.</summary>
        protected virtual WeightedSwing AbyssSlashSwipeCurve => default;
        protected virtual int   AbyssSlashRecoveryTicks     => 60;

        /// <summary>Fired once, the instant a swipe begins (index 0 = the first swipe out of the
        /// wind-up). Spawn the projectile / finisher here - the base class fires nothing itself.</summary>
        protected virtual void DoAbyssSlashFire(int swipeIndex) { }
        /// <summary>Called right after swipe <paramref name="completedSwipeIndex"/> finishes. Return
        /// the tick gap before the next swipe, or a negative value to end the sequence (→ recovery).
        /// Default -1 = always just one swipe.</summary>
        protected virtual int   NextAbyssSlashDelay(int completedSwipeIndex) => -1;

        private int   _abyssSlashIndex;
        protected int _abyssSlashCooldown;

        // ── Abyss Tendril Grab (optional grab-and-punish attack) ───────────────────
        // Arm dissolves into shadow, a tendril projectile reaches out and (on contact) gently yanks
        // the target closer; once the tendril's self-contained fly/yank/retract sequence finishes
        // (timed via TendrilReachTicks - the projectile doesn't report back), the arm returns and
        // throws the SAME 180°→10°→170° swing shape as Abyss Slash's wind-up/release, punishing
        // whoever just got pulled in. All VFX/spawn specifics are subclass hooks; the base class only
        // owns timing and the melee-swing rotation.
        protected virtual bool  CanTendrilGrab              => false;
        protected virtual float TendrilMinRange             => 150f;
        protected virtual float TendrilMaxRange             => 500f;
        protected virtual int   TendrilChance               => 4;
        protected virtual int   TendrilCooldownAfterUse     => 480;

        protected virtual int   TendrilTelegraphTicks       => 24;
        /// <summary>Covers the tendril projectile's whole fly+yank+retract lifecycle. Not
        /// synchronized with the projectile (which is self-contained) - just long enough to
        /// comfortably outlast it.</summary>
        protected virtual int   TendrilReachTicks           => 90;
        protected virtual int   TendrilSwingArcTicks        => 40;   // 180° -> 10°
        protected virtual int   TendrilSwingHoldTicks       => 12;   // held at 10° before releasing
        protected virtual int   TendrilSwingTicks           => 16;   // 10° -> 170° release
        /// <summary>Optional Weighted curve for the finishing swing. Unset keeps the legacy Snap; set,
        /// TendrilSwingTicks should equal its TotalTicks, and the blade disarms after its LiveTicks.</summary>
        protected virtual WeightedSwing TendrilSwingCurve   => default;
        /// <summary>Finishing swing end pose, swing-space radians (the "170°" in the labels above).</summary>
        protected virtual float TendrilSwingEndRotation     => MathHelper.ToRadians(170f - 45f);
        protected virtual int   TendrilRecoveryTicks        => 60;

        /// <summary>Called every tick of the arm-dissolve wind-up, elapsed counting up from 0.</summary>
        protected virtual void DoTendrilTelegraphTick(int elapsed) { }
        /// <summary>Fired once when the wind-up ends - spawn the tendril projectile here.</summary>
        protected virtual void DoTendrilLaunch() { }
        /// <summary>Called every tick while the tendril is out (whole TendrilReach phase).</summary>
        protected virtual void DoTendrilReachTick() { }
        /// <summary>Fired once, the instant the finishing swing releases. Apply the hit here
        /// (e.g. <c>TryMeleeHit()</c>) - the base class doesn't apply damage itself.</summary>
        protected virtual void DoTendrilSwing() { }

        protected int _tendrilCooldown;

        // ── Dodge-punish chain (generic) ────────────────────────────────────────────
        // Tracks whether the CURRENT committed swing/volley actually connected. Reset to false
        // whenever a chain-eligible attack begins (JumpSlashDodgeback / AbyssSlashTelegraph /
        // TendrilTelegraph); set true by TickBladeHit's real per-tick overlap check for melee
        // swings/combos, or by ranged projectiles reporting back via ReportAttackHit() on hit. A
        // clean dodge (this stays false) can then be punished by chaining straight into a different
        // attack instead of handing the player a free breather in Recovery.
        private bool _lastAttackHitConnected;
        private bool _currentComboStepHitConnected;

        /// <summary>% chance, on a fully whiffed JumpingDownwardSlash or Abyss Slash, to chain
        /// straight into the other one (if in range and off cooldown) instead of returning to
        /// neutral recovery.</summary>
        protected virtual int DodgePunishChainChance => 50;

        /// <summary>Called by a hostile projectile that hit its target, to credit this puppet's
        /// current attack as having connected (see <see cref="_lastAttackHitConnected"/>).</summary>
        public void ReportAttackHit()
        {
            // Hostile projectile hits resolve on the victim's client, but the result feeds server decisions (dodge-punish
            // chains, hit-confirms): forward it. tsorcRevamp.HandlePacket calls this again on the server.
            if (IsMultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write(tsorcPacketID.ReportPuppetAttackHit);
                packet.Write((short)NPC.whoAmI);
                packet.Write(NPC.type);
                packet.Send();
                return;
            }

            _lastAttackHitConnected = true;
            if (IsMeleeComboPhase)
                _currentComboStepHitConnected = true;
        }

        /// <summary>
        /// If the swing that just ended fully whiffed, rolls <see cref="DodgePunishChainChance"/> and
        /// (on success) immediately re-enters whichever of {AbyssSlash, JumpingDownwardSlash} is off
        /// cooldown and in range, skipping the normal recovery. Caller must set the cooldown of the
        /// attack that just ended BEFORE calling this (so it can't immediately chain into itself).
        /// </summary>
        private bool TryDodgePunishChain(float dist)
        {
            // A roll on server-only hit results. A client predicts "no chain" (the plain recovery) and adopts
            // the chained attack if the server picks one.
            if (IsMultiplayerClient)
            {
                return false;
            }

            if (_lastAttackHitConnected || Main.rand.Next(100) >= DodgePunishChainChance)
                return false;

            if (CanAbyssSlash && _abyssSlashCooldown <= 0
                && dist >= AbyssSlashMinRange && dist <= AbyssSlashMaxRange)
            {
                _lastAttackHitConnected = false;
                EnterPhase(AttackPhase.AbyssSlashTelegraph, AbyssSlashArcTicks + AbyssSlashHoldTicks);
                return true;
            }

            if (CanJumpSlash && _jumpSlashCooldown <= 0
                && dist >= JumpSlashMinRange && dist <= JumpSlashMaxRange)
            {
                _lastAttackHitConnected = false;
                EnterPhase(AttackPhase.JumpSlashDodgeback, JumpSlashDodgebackTicks);
                return true;
            }

            return false;
        }

        // ── Charge-up Nova (health-threshold interrupt) ─────────────────────────────
        // Highest-priority interrupt, checked alongside Healing: the puppet roots in place while
        // dust gathers and thickens around it and an outer telegraph ring races out ahead over
        // NovaChargeTicks, then the whole radius detonates in one huge blast. Trigger conditions
        // (e.g. specific HP thresholds) and exact numbers are entirely subclass-owned - the base
        // class just owns the three-phase timing skeleton.
        protected virtual bool  CanNova            => false;
        protected virtual int   NovaChargeTicks     => 4 * 60;
        protected virtual int   NovaBlastHoldTicks  => 24;
        protected virtual int   NovaRecoveryTicks   => 90;

        /// <summary>Return true (once) when the puppet should interrupt whatever it's doing to
        /// charge up a nova. Treat this like <c>ShouldHeal</c> - track your own one-shot state.</summary>
        protected virtual bool ShouldTriggerNova() => false;
        /// <summary>Called every tick of the charge-up, elapsed/total counting the wind-up progress.</summary>
        protected virtual void DoNovaChargeTick(int elapsed, int total) { }
        /// <summary>Fired once when the charge completes - spawn the actual blast here.</summary>
        protected virtual void DoNovaBlast() { }

        // ── Abyss Shard (ground-spike combo attack, optional) ───────────────────────
        // Same shape as Abyss Slash: a short wind-up, then a subclass-defined sequence of "fire
        // events" spaced by NextAbyssShardDelay - each event calls DoAbyssShardFire once. Deliberately
        // has NO recovery phase: the moment the sequence ends (negative delay), control returns
        // straight to Idle/CasualStroll so a different attack can follow immediately.
        protected virtual bool  CanAbyssShard             => false;
        protected virtual float AbyssShardMinRange        => 80f;
        protected virtual float AbyssShardMaxRange        => 900f;
        protected virtual int   AbyssShardChance          => 10;
        protected virtual int   AbyssShardCooldownAfterUse => 420;
        protected virtual int   AbyssShardTelegraphTicks  => 30;
        protected virtual int   AbyssShardFireTicks       => 6;

        /// <summary>Returns the delay (ticks) before the next fire event, or a negative value to end
        /// the sequence (with no recovery phase). completedFireIndex is the index that just fired.</summary>
        protected virtual int  NextAbyssShardDelay(int completedFireIndex) => -1;
        /// <summary>Fired once per event (index 0, 1, 2...) - spawn ground shard(s) here.</summary>
        protected virtual void DoAbyssShardFire(int fireIndex) { }

        protected int _abyssShardIndex;
        protected int _abyssShardCooldown;

        // ── Homing Volley (dodgeback + overhead swing + delayed-homing orb volley) ──
        // Backs away with a real dodgeroll (i-frames), then raises the sword fully overhead and
        // brings it down in one big chop; partway through that chop (HomingVolleyFireProgress) it
        // fires a subclass-chosen pattern of delayed-homing orbs. No inter-attack chaining - a fixed
        // recovery always follows.
        protected virtual bool  CanHomingVolley                => false;
        protected virtual float HomingVolleyMinRange            => 280f;
        protected virtual float HomingVolleyMaxRange            => 650f;
        protected virtual int   HomingVolleyChance              => 10;
        protected virtual int   HomingVolleyCooldownAfterUse    => 300;
        protected virtual int   HomingVolleyDodgebackTicks      => 24;
        protected virtual float HomingVolleyDodgebackSpeed      => 6f;
        protected virtual int   HomingVolleySwingTelegraphTicks => 20;
        protected virtual int   HomingVolleySwingTicks          => 30;
        /// <summary>Optional Weighted curve for the chop. Unset keeps the legacy Smooth; set, HomingVolleySwingTicks
        /// should equal its TotalTicks, and HomingVolleyFireProgress should put the release on its peak.</summary>
        protected virtual WeightedSwing HomingVolleySwingCurve  => default;
        /// <summary>Fraction (0-1) through the swing at which <see cref="DoHomingVolleyFire"/> fires.</summary>
        protected virtual float HomingVolleyFireProgress        => 0.5f;
        protected virtual int   HomingVolleyRecoveryTicks       => 120;
        /// <summary>Uses the player-style raised-hand hold followed by Use1-Use4 release frames.
        /// The held visual itself is supplied by <see cref="DrawSpecialHeldWeapon"/>.</summary>
        protected virtual bool  UseRaisedHomingVolleyHoldoutPose => false;

        /// <summary>Called every tick of the overhead chop, elapsed/total counting swing progress -
        /// override to spawn dust along the blade's current arc position.</summary>
        protected virtual void DoHomingVolleySwingTick(int elapsed, int total) { }
        /// <summary>Fired once, partway through the swing - spawn the orb volley here.</summary>
        protected virtual void DoHomingVolleyFire() { }

        private int  _homingVolleyDir = 1;
        private bool _homingVolleyFired;
        protected int _homingVolleyCooldown;

        // ── Sword Launch Reposition (shared by Boomerang / Spiral Fan) ──────────────
        // A mostly-horizontal backward hop, facing the player, used ONLY when they're closer than
        // SwordLaunchRepositionTooCloseRange when one of those attacks is selected - creates room
        // for the overhead chop + projectile(s) instead of letting them fire from point-blank.
        // Unlike Homing Volley's dodgeback this is a plain hop (no i-frames): it's a spacing
        // correction, not a defensive dodge.
        protected virtual float SwordLaunchRepositionTooCloseRange => 150f;
        protected virtual int   SwordLaunchRepositionTicks         => 20;
        protected virtual float SwordLaunchRepositionBackSpeed     => 7.5f;
        protected virtual float SwordLaunchRepositionUpSpeed       => 4.5f;

        private AttackPhase _swordLaunchNextPhase;
        private int         _swordLaunchNextTicks;
        private int         _swordLaunchDir = 1;

        // ── Boomerang Crescent (dodge-free overhead swing + curving/returning crescent(s)) ──
        // Reuses the same overhead-chop shape as Homing Volley. Fires partway through the chop;
        // the crescent(s) arc out in a wide curl, then home back toward the puppet's OWN current
        // position for a second, separately-timed pass.
        protected virtual bool  CanBoomerang                => false;
        protected virtual float BoomerangMinRange           => 60f;
        protected virtual float BoomerangMaxRange           => 650f;
        protected virtual int   BoomerangChance             => 9;
        protected virtual int   BoomerangCooldownAfterUse   => 330;
        protected virtual int   BoomerangSwingTelegraphTicks => 20;
        protected virtual int   BoomerangSwingTicks         => 30;
        /// <summary>Optional Weighted curve for the chop. Unset keeps the legacy Smooth; set, BoomerangSwingTicks
        /// should equal its TotalTicks, and BoomerangFireProgress should put the release on its peak.</summary>
        protected virtual WeightedSwing BoomerangSwingCurve => default;
        protected virtual float BoomerangFireProgress       => 0.5f;
        protected virtual int   BoomerangRecoveryTicks      => 110;

        protected virtual void DoBoomerangSwingTick(int elapsed, int total) { }
        protected virtual void DoBoomerangFire() { }

        private bool _boomerangFired;
        protected int _boomerangCooldown;

        // ── Spiral Fan (dodge-free overhead swing + a rotating-angle burst of shots) ──────
        // Same overhead-chop wind-up as the others, but the swing itself doesn't fire anything -
        // once it completes, a dedicated burst sequence (SpiralFanBurst/Pause, mirroring Abyss
        // Shard's fire-event loop) fires each shot with an incrementing launch angle over time,
        // producing a rotating stream instead of a static simultaneous fan.
        protected virtual bool  CanSpiralFan                => false;
        protected virtual float SpiralFanMinRange           => 60f;
        protected virtual float SpiralFanMaxRange           => 650f;
        protected virtual int   SpiralFanChance             => 8;
        protected virtual int   SpiralFanCooldownAfterUse   => 360;
        protected virtual int   SpiralFanSwingTelegraphTicks => 20;
        protected virtual int   SpiralFanSwingTicks         => 30;
        /// <summary>Optional Weighted curve for the wind-up chop. Unset keeps the legacy Smooth; set,
        /// SpiralFanSwingTicks should equal its TotalTicks.</summary>
        protected virtual WeightedSwing SpiralFanSwingCurve => default;
        protected virtual int   SpiralFanFireTicks          => 4;
        protected virtual int   SpiralFanRecoveryTicks      => 90;

        protected virtual void DoSpiralFanSwingTick(int elapsed, int total) { }
        protected virtual int SpiralFanWeaponItemType => MeleeWeaponItemType;
        protected virtual bool UseAuthoredSpiralFanCastPose => false;
        protected virtual float SpiralFanCastStartRotation => -0.20f;
        protected virtual float SpiralFanCastEndRotation => -0.80f;
        /// <summary>Returns the delay (ticks) before the next shot, or negative to end the burst.</summary>
        protected virtual int  NextSpiralFanDelay(int completedShotIndex) => -1;
        /// <summary>Fired once per shot (index 0, 1, 2...) - spawn that shot here.</summary>
        protected virtual void DoSpiralFanFire(int shotIndex) { }
        /// <summary>Fired once, right when a FRESH (non-chained) Spiral Fan sequence is chosen from
        /// the attack-selection decision tree - override to reset any chain-tracking state (e.g.
        /// <see cref="TryContinueSpiralFanChain"/>'s escalation counter) before the first volley.</summary>
        protected virtual void OnSpiralFanSequenceStart() { }

        protected int _spiralFanIndex;
        protected int _spiralFanCooldown;

        // ── Fire Volley Chain (reposition + arc-jump escalation after a Spiral Fan volley) ──
        // Hooked into SpiralFanRecovery: instead of always returning to idle, an override of
        // TryContinueSpiralFanChain can take over and re-enter SpiralFanSwingTelegraph (via one of
        // the reposition phases below) for a second volley, then optionally escalate into a big
        // arc-jump-over for a third. All no-ops/false by default - zero effect unless overridden.
        protected virtual bool TryContinueSpiralFanChain() => false;

        protected virtual int   FireVolleyBackLeapTicks       => 26;
        protected virtual float FireVolleyBackLeapSpeed       => 5.5f; // ~8 tiles over the leap's airtime
        protected virtual float FireVolleyBackLeapUpSpeed     => 6f;
        protected virtual float FireVolleyDodgeThroughUpSpeed => 7f;
        /// <summary>Safety-net cap on FireVolleyDodgeThrough — the real duration comes from
        /// ArmQuickStep/TickQuickStep's own timers, this just guarantees the phase can't get stuck.</summary>
        protected virtual int   FireVolleyDodgeThroughTimeoutTicks => 90;
        protected virtual float FireVolleyArcJumpUpSpeed      => 13f;
        protected virtual float FireVolleyArcJumpForwardSpeed => 6f;

        /// <summary>Fired once, right when NPC.velocity.Y crosses from rising to falling (the jump's
        /// apex) during <see cref="AttackPhase.FireVolleyArcJump"/> - spawn the mid-air volley here.</summary>
        protected virtual void DoFireVolleyArcFire() { }
        /// <summary>Fired when <see cref="AttackPhase.FireVolleyBackLeap"/> or
        /// <see cref="AttackPhase.FireVolleyDodgeThrough"/> finishes (landed / quick-step fully
        /// resolved) - override to re-enter SpiralFanSwingTelegraph for another volley.</summary>
        protected virtual void OnFireVolleyRepositionLanded() { }
        /// <summary>Fired when <see cref="AttackPhase.FireVolleyArcJump"/> lands.</summary>
        protected virtual void OnFireVolleyArcJumpLanded() { }

        private int   _fireVolleyDir = 1;
        private bool  _fireVolleyArcFired;
        private float _fireVolleyArcVx;

        // ── Estus healing ─────────────────────────────────────────────────────────
        /// <summary>How many estus drinks the puppet starts with.</summary>
        protected virtual int   EstusChargesMax         => 3;
        /// <summary>Fraction of max HP restored per drink.</summary>
        protected virtual float EstusHealFraction       => 0.20f;
        /// <summary>Primary and secondary health thresholds that can request an Estus attempt.</summary>
        protected virtual float FirstHealThreshold      => 0.50f;
        protected virtual float SecondHealThreshold     => 0.25f;
        /// <summary>Maximum post-heal life fraction. Defaults uncapped; bosses may keep a phase
        /// active by capping healing below that phase's upper boundary.</summary>
        protected virtual float HealLifeCapFraction     => 1f;
        /// <summary>Ticks of cooldown between use attempts (10 seconds by default).</summary>
        protected virtual int   HealCooldownTicks       => 10 * 60;
        /// <summary>Ticks the interruptible drinking channel lasts before HP is restored.</summary>
        protected virtual int   HealAnimationTicks      => 130;
        /// <summary>
        /// Accumulated recent damage (as a fraction of max HP) that triggers an emergency heal.
        /// E.g. 0.15 = trigger heal if more than 15 % max HP taken in the recent burst window.
        /// </summary>
        protected virtual float RecentDamageThreshold   => 0.18f;
        /// <summary>How fast recent-damage memory decays per tick (fraction of max HP).</summary>
        protected virtual float RecentDamageDecayRate   => 0.0025f;
        /// <summary>Minimum pixel distance from the player before the puppet stops fleeing and drinks.</summary>
        protected virtual float FleeToHealDistance      => 40 * 16f;  // 40 tiles
        /// <summary>Max ticks spent fleeing before drinking anyway (in case player is chasing).</summary>
        protected virtual int   FleeToHealMaxTicks      => 150;

        // ── Navigation (SF4 A* pathfinding; jump tuning below) ───────────────────
        protected virtual float PuppetJumpPower      => 10f;
        protected virtual float PuppetJumpBoost      => 6f;
        protected virtual bool  PuppetCanDoubleJump  => false;
        protected virtual float PuppetDoubleJumpPower => 6f;

        // ── Gap-closer attack tuning (LeapSlam / ChargeChop combo motions) ──────────
        /// <summary>Horizontal speed held during a LeapSlam's airborne arc (px/frame).</summary>
        protected virtual float LeapAttackForwardSpeed => TopSpeed * 1.7f;
        /// <summary>Upward launch speed at the start of a LeapSlam (px/frame).</summary>
        protected virtual float LeapAttackUpSpeed      => 9.5f;
        /// <summary>Minimum forward speed for a distant leap. Lower values prevent short hops from
        /// sailing past a nearby target; close leaps calculate their exact required speed down to zero.</summary>
        protected virtual float LeapAttackMinimumForwardSpeed => 1.5f;
        /// <summary>How many ticks of the target's current horizontal velocity to include in leap aim.</summary>
        protected virtual float LeapAttackTargetLeadTicks => 0f;
        /// <summary>Bounded horizontal correction while a LeapSlam is still ascending. The descent
        /// remains locked so the player can read and dodge the committed landing line.</summary>
        protected virtual float LeapAttackAscentTrackingStrength => 0f;
        /// <summary>Equivalent ascent-only correction for ApexDiveCleave.</summary>
        protected virtual float ApexDiveAscentTrackingStrength => 0.08f;
        /// <summary>Run speed during a ChargeChop, as a multiplier of TopSpeed.</summary>
        protected virtual float ChargeAttackSpeedMult  => 1.85f;

        // ── Shield (optional, opt-in per subclass) ──────────────────────────────────
        /// <summary>Item type whose sprite is drawn as this puppet's off-hand shield.  -1 = no
        /// shield. Override in a subclass to draw a carried shield. A reactive guard also requires
        /// <see cref="AllowReactiveDefense"/> and a <c>ShieldProfile.*</c> call in SetDefaults.</summary>
        protected virtual int ShieldItemType => -1;
        /// <summary>True when this puppet carries a shield.</summary>
        protected bool HasShield => ShieldItemType > 0;
        /// <summary>Damage multiplier applied to FRONT hits while actively guarding (0.5 = take half).
        /// Backstabs bypass the block entirely.</summary>
        protected virtual float ShieldDamageReduction => 0.5f;
        /// <summary>Path to the held-shield draw sprite — a 20-frame vertical strip sharing the
        /// player body's frame layout (frame N overlays body frame N), like vanilla equip shields.
        /// Defaults to the shield item's texture + "_Shield"; override to point elsewhere.</summary>
        protected virtual string ShieldDrawTexturePath =>
            HasShield ? TextureAssets.Item[ShieldItemType].Name + "_Shield" : null;
        private Texture2D _shieldDrawTex;
        private bool _shieldDrawTexLoaded;
        /// <summary>Minimum guard hold (ticks) when a block is raised away from melee range — long
        /// enough to catch a ranged shot, short enough to stay mobile.  90 ticks = 1.5 s.</summary>
        protected virtual int ShieldGuardTicksRanged => 90;
        /// <summary>Minimum guard hold (ticks) once the guard is committed in melee range.  The
        /// puppet plants + locks facing for this whole window, so the player can dodgeroll through
        /// and backstab.  150 ticks = 2.5 s.</summary>
        protected virtual int ShieldGuardTicksMelee => 150;
        /// <summary>Minimum delay after a completed or stagger-cancelled guard before another
        /// reactive guard can start. This keeps repeated hits from holding the puppet in neutral.</summary>
        protected virtual int ShieldGuardCooldownTicks => 180;
        /// <summary>Slow advance speed (px/frame) toward the player while guarding OUT of melee
        /// range.  In melee range the puppet plants instead.</summary>
        protected virtual float ShieldAdvanceSpeed => 0.9f;
        /// <summary>True this frame while the guard is up (drives the raised-shield pose + the
        /// front damage / poise reduction).  Driven by the reactive block timer.</summary>
        private bool _shielding;
        /// <summary>Facing captured when the guard goes up and held through a melee commit, so the
        /// puppet doesn't track a player dodgerolling through to its back.</summary>
        private int  _shieldLockedDir;
        /// <summary>Rising-edge tracker for the guard (false→true = just raised).</summary>
        private bool _shieldWasGuarding;
        /// <summary>True once the current guard has had its hold bumped to the melee minimum (so the
        /// extension is applied once, not re-applied every tick the player stays adjacent).</summary>
        private bool _shieldMeleeExtended;
        /// <summary>Rearm lockout for the authored shield action. Unlike ReactiveBlockTimer, hits
        /// cannot refresh this while the guard is active.</summary>
        private int _shieldGuardCooldown;

        /// <summary>
        /// Run the ground-movement AI for this puppet.  All puppets navigate with
        /// <c>SmartFighter4AI</c> (A* pathfinding + ropes + stuck/teleport recovery); subclasses
        /// override to tune search radius / door-break / attack range.  Called once per tick from
        /// <see cref="AI"/> while grounded.
        /// </summary>
        protected virtual void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(NPC,
                topSpeed:           TopSpeed * speedMult,
                acceleration:       Acceleration,
                doorBreakingDamage: 4,
                attackRange:        RangedRange);
        }

        // ── State machine ─────────────────────────────────────────────────────────
        protected enum AttackPhase
        {
            Idle,
            /// <summary>A short, committed defensive action. It is neither an attack telegraph nor
            /// a poise stagger, and exits directly back into normal action selection.</summary>
            ShieldGuard,
            MeleeTelegraph, MeleeAttack, MeleeRecovery,
            StabTelegraph,  StabAttack,  StabRecovery,
            RangedTelegraph, RangedAttack, RangedRecovery,
            SpearTelegraph,  SpearAttack,  SpearRecovery,
            MagicTelegraph,  MagicAttack,  MagicRecovery,
            /// <summary>Charge-up before a sustained fire-breath stream: the puppet plants (or, when
            /// airborne, keeps flying) and emits a swelling mouth ember via <see cref="DoBreathWindup"/>.</summary>
            BreathTelegraph,
            /// <summary>Active fire-breath stream: <see cref="DoBreathTick"/> spawns the breath projectiles
            /// each tick for <see cref="BreathDurationTicks"/>.  When airborne the dragon strafes to sweep.</summary>
            Breathing,
            /// <summary>Recovery after the breath stream ends; applies <see cref="BreathCooldownAfterUse"/>.</summary>
            BreathRecovery,
            /// <summary>Hold-the-knife charge before the first Cursed Knives volley.</summary>
            KnivesTelegraph,
            /// <summary>Active throw frame: <see cref="DoCursedKnivesThrow"/> spawns a 3-knife volley.</summary>
            KnivesThrow,
            /// <summary>Pause between Cursed Knives volleys (next volley fires when it expires — no re-telegraph).</summary>
            KnivesThrowPause,
            /// <summary>Recovery after the last Cursed Knives volley; applies <see cref="CursedKnivesCooldownAfterUse"/>.</summary>
            KnivesRecovery,
            /// <summary>Piercing Dash wind-up: weapon held out toward the player, sprite jitters in place.</summary>
            PierceTelegraph,
            /// <summary>Active lunge: a fresh melee hitbox is spawned every tick so the fast-moving dash
            /// can still catch the target.  A stab-variant hit breaks out into <see cref="PierceStabHold"/>.</summary>
            PierceDash,
            /// <summary>Stab variant only: the target is held impaled while the sword arm raises 0→90°
            /// over <see cref="PierceStabRaiseTicks"/>.</summary>
            PierceStabHold,
            /// <summary>Stab variant only: rotates back down and releases/launches the target.</summary>
            PierceStabFlick,
            /// <summary>Shared recovery after either pierce variant ends — can walk, can't attack yet.</summary>
            PierceRecovery,
            /// <summary>Jumping Downward Slash wind-up: a backward roll WITH i-frames (a genuine
            /// dodgeroll, not a hop) before the jump. See <see cref="JumpSlashDodgebackTicks"/>.</summary>
            JumpSlashDodgeback,
            /// <summary>Airborne: launches toward the player holding the cocked "up and back" pose;
            /// breaks into <see cref="JumpSlashAttack"/> once close or on landing.</summary>
            JumpSlashRise,
            /// <summary>The half-circle downward swipe itself (fires once on entry via <see cref="DoJumpSlashAttack"/>).</summary>
            JumpSlashAttack,
            /// <summary>Recovery after landing/swiping.</summary>
            JumpSlashRecovery,
            /// <summary>Forward flipping spin-through: airborne, i-framed (a real dodgeroll, not just
            /// hyper-armor), sword held out and spinning continuously with damage active. Breaks into
            /// <see cref="FlipSlashLand"/> on landing.</summary>
            FlipSlashRise,
            /// <summary>The sword snaps into the landing slam pose and is held there for
            /// <see cref="FlipSlashLandHoldTicks"/> (screenshake + dirt dust fire once on entry).</summary>
            FlipSlashLand,
            /// <summary>Abyss Slash wind-up: underhand → 170° arc → held "sword post" pose for the
            /// last <see cref="AbyssSlashHoldTicks"/>. Deliberately does NOT stop movement (SF4's
            /// normal pursuit from earlier this tick is left untouched) so the puppet keeps walking.</summary>
            AbyssSlashTelegraph,
            /// <summary>One quick swing that fires <see cref="DoAbyssSlashFire"/> once on entry.</summary>
            AbyssSlashSwipe,
            /// <summary>Inter-swipe gap; duration comes from <see cref="NextAbyssSlashDelay"/>.</summary>
            AbyssSlashPause,
            /// <summary>Recovery once <see cref="NextAbyssSlashDelay"/> returns a negative delay.</summary>
            AbyssSlashRecovery,
            /// <summary>Abyss Tendril Grab wind-up: the arm dissolves into shadow dust before reaching out.</summary>
            TendrilTelegraph,
            /// <summary>The tendril projectile is out (flying/yanking/retracting is entirely self-contained
            /// on the projectile); the arm holds an outstretched, dust-covered pose for the duration.</summary>
            TendrilReach,
            /// <summary>The arm/sword winds back up for the finishing swing (same 180°→10° arc as
            /// <see cref="AbyssSlashTelegraph"/>) once the tendril sequence is done.</summary>
            TendrilSwingTelegraph,
            /// <summary>The finishing underhand release (same 10°→170° arc as <see cref="AbyssSlashSwipe"/>).</summary>
            TendrilSwing,
            /// <summary>Recovery after the finishing swing.</summary>
            TendrilRecovery,
            /// <summary>A melee attack was chosen but the player is out of hittable reach: sprint toward
            /// them (no swing) until in range, then start the attack.  Prevents whiffing at distance.</summary>
            ClosingDistance,
            /// <summary>
            /// Inter-shot pause during a crossbow burst pattern.
            /// The puppet holds horizontal aim and waits for the timer; the next shot fires
            /// automatically when it expires (no new telegraph phase).
            /// </summary>
            CrossbowBurstPause,
            /// <summary>
            /// Slow walk toward player after a recovery phase; no attacks allowed.
            /// Gives variety: the puppet advances calmly before re-engaging.
            /// </summary>
            CasualStroll,
            /// <summary>
            /// Sprint away from the player until far enough to drink safely, then enter Healing.
            /// Interrupted if the puppet can't reach safe distance within <see cref="FleeToHealMaxTicks"/>.
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
            /// <summary>Charge-up Nova wind-up: rooted in place, engulfed in thickening dust with an
            /// outer telegraph ring racing ahead of it. Fires <see cref="DoNovaBlast"/> once on exit.</summary>
            NovaCharge,
            /// <summary>Brief hold at the moment of detonation.</summary>
            NovaBlast,
            /// <summary>Recovery after the nova.</summary>
            NovaRecovery,
            /// <summary>Abyss Shard wind-up: a short ground-facing gesture before the first fire event.</summary>
            AbyssShardTelegraph,
            /// <summary>Brief active frame for a fire event - fires <see cref="DoAbyssShardFire"/> once on entry.</summary>
            AbyssShardFire,
            /// <summary>Inter-event gap; duration comes from <see cref="NextAbyssShardDelay"/>. When that
            /// returns negative there is NO recovery phase - control returns straight to Idle/CasualStroll.</summary>
            AbyssShardPause,
            /// <summary>Homing Volley wind-up: a backward roll WITH i-frames (a genuine dodgeroll)
            /// before the overhead chop.</summary>
            HomingVolleyDodgeback,
            /// <summary>Sword raises fully overhead, cocked back.</summary>
            HomingVolleySwingTelegraph,
            /// <summary>The overhead chop itself - fires <see cref="DoHomingVolleyFire"/> once,
            /// partway through, via <see cref="HomingVolleyFireProgress"/>.</summary>
            HomingVolleySwing,
            /// <summary>Fixed recovery after the chop - no chaining into another attack.</summary>
            HomingVolleyRecovery,
            /// <summary>Shared wind-up for any "sword launch" attack: a mostly-horizontal backward
            /// hop, facing the player, used ONLY when they're too close for the swing that follows.
            /// Transitions to whichever telegraph phase was queued via <see cref="_swordLaunchNextPhase"/>.</summary>
            SwordLaunchReposition,
            /// <summary>Boomerang Crescent wind-up: sword raised fully overhead (same shape as
            /// Homing Volley's).</summary>
            BoomerangSwingTelegraph,
            /// <summary>The overhead chop - fires <see cref="DoBoomerangFire"/> once, partway
            /// through, via <see cref="BoomerangFireProgress"/>.</summary>
            BoomerangSwing,
            /// <summary>Fixed recovery after the chop.</summary>
            BoomerangRecovery,
            /// <summary>Spiral Fan wind-up: sword raised fully overhead (same shape as the others).</summary>
            SpiralFanSwingTelegraph,
            /// <summary>The overhead chop itself - purely a visual wind-up, no firing (the burst
            /// that follows carries the actual sequence).</summary>
            SpiralFanSwing,
            /// <summary>Active frame for one burst event - fires <see cref="DoSpiralFanFire"/> once
            /// on entry.</summary>
            SpiralFanBurst,
            /// <summary>Inter-shot gap; duration comes from <see cref="NextSpiralFanDelay"/>.</summary>
            SpiralFanPause,
            /// <summary>Fixed recovery after the whole spiral sequence ends.</summary>
            SpiralFanRecovery,
            /// <summary>Fire Volley chain: backward hop (facing the player) before re-entering
            /// SpiralFanSwingTelegraph for another volley. See <see cref="TryContinueSpiralFanChain"/>.</summary>
            FireVolleyBackLeap,
            /// <summary>Fire Volley chain: leaping dodge-roll THROUGH the player (i-frames, no
            /// contact damage - reuses ArmQuickStep/TickQuickStep) landing on their far side before
            /// re-entering SpiralFanSwingTelegraph for another volley.</summary>
            FireVolleyDodgeThrough,
            /// <summary>Fire Volley chain: a big jump arc over the player; fires
            /// <see cref="DoFireVolleyArcFire"/> once at the apex, mid-air.</summary>
            FireVolleyArcJump,
            /// <summary>Generic scriptable set-piece phase (see <see cref="StartCustomAttack"/>). A
            /// subclass parks the puppet here to run a bespoke, timed behavior — a self-encase, an
            /// ally-channel, a death-burst, etc. — that is NOT one of the built-in melee/ranged/magic
            /// attacks. Fires <see cref="DoCustomAttack"/> once on entry, <see cref="DoCustomTick"/> each
            /// tick, and returns to Idle/CasualStroll on expiry. Optionally holds a weapon pose.</summary>
            Custom,
        }
        protected AttackPhase Phase = AttackPhase.Idle;
        protected int PhaseTimer;

        // ── Multiplayer phase sync ────────────────────────────────────────────────
        // Server-authoritative: attack selection, rolls and branch decisions run only where netMode !=
        // MultiplayerClient. Every EnterPhase bumps _phaseSequence on every machine, and the server flushes
        // a SyncNPC from PostAI whenever it (or a flight/mount mode) changed, bypassing vanilla's netSpam
        // throttle (one queued packet per 30 ticks for non-boss NPCs). Clients keep ticking committed phases
        // and predict timer-driven transitions (so sounds, dust and hooks still fire); ReceiveExtraAI reconciles.
        private int _phaseSequence;
        private int _lastSentPhaseSequence;
        private int _lastSentFlightSequence;
        private int _lastSentMountSequence;
        /// <summary>Client only: the server sequence last adopted or confirmed.</summary>
        private int _lastAcceptedPhaseSequence;
        /// <summary>Client only: the tick the client first predicted past _lastAcceptedPhaseSequence.</summary>
        private uint _predictionStartTick;
        /// <summary>A snapshot one transition ahead that arrives within this many ticks of the client's own timer running
        /// out waits for that local transition instead of cutting it short, so the expiry hooks (swing sounds, Do*/On*
        /// cues) still fire here. Covers the arrival jitter around a transition.</summary>
        private const int PredictionDeferWindowTicks = 6;
        // Client only: a phase snapshot waiting on that local transition (null state = nothing waiting).
        private byte[] _deferredSnapshotState;
        private int _deferredSnapshotSequence;
        private AttackPhase _deferredSnapshotPhase;
        private int _deferredSnapshotTimer;
        private int _deferredSnapshotComboIndex;
        private int _deferredSnapshotStepIndex;
        private int _deferredSnapshotTicksWaited;
        private int _deferredSnapshotMaxWait;
        /// <summary>Client only: an adopted phase whose entry cues still have to play. Runs at the start of the next AI
        /// tick, after the subclass has read its own fields from the same packet.</summary>
        private bool _adoptedPhaseEntryPending;
        /// <summary>Server only: a subclass asked for a snapshot this tick (see RequestNetworkSnapshot).</summary>
        private bool _snapshotRequested;

        /// <summary>Sends a snapshot from PostAI this tick even without a phase change — for subclass set-piece state (an
        /// aerial stage, a pursuit) that clients must follow promptly. No-op outside the server.</summary>
        protected void RequestNetworkSnapshot()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                _snapshotRequested = true;
            }
        }
        /// <summary>A snapshot older than the client's prediction is normally just latency. Once the prediction
        /// has gone this many ticks unconfirmed the server has held the phase (a subclass PhaseTimer hold,
        /// a pursuit) and the client adopts the snapshot instead.</summary>
        private const int StalePhaseSnapshotToleranceTicks = 20;

        private bool IsMultiplayerClient => Main.netMode == NetmodeID.MultiplayerClient;

        // Custom (set-piece) phase pose state — see StartCustomAttack / DoCustomAttack / DoCustomTick.
        private int  _customPoseWeapon = -1;   // item type held during the Custom phase, or -1 = bare-handed
        private bool _customSwingPose;         // hold (false) vs swing (true) pose for that weapon

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
        // Counts down; fire breath is blocked while > 0
        protected int _breathCooldown;
        // Counts down; Cursed Knives is blocked while > 0
        protected int _cursedKnivesCooldown;

        // Cursed Knives volley state (set at telegraph start from range).
        private int _cursedKnivesVolleysLeft;
        private int _cursedKnivesGap;

        // Per-cast override for the MagicAttack phase duration (-1 = use MagicAttackTicks).  A subclass
        // sets this inside DoMagicAttack to channel a sustained cast (e.g. a multi-second meteor rain),
        // during which DoMagicTick is called every MagicAttack tick.  Reset to -1 in MagicRecovery.
        protected int _magicAttackTicksOverride = -1;

        // Index of the active ranged burst pattern (into the pattern pool), or -1 when not using a
        // pattern.  Exposed via ActiveBurstPatternIndex so a subclass can vary the projectile per shot
        // (e.g. a cross-weapon "finisher" on the last shot of a specific pattern).
        private int _activeBurstPatternIndex = -1;

        // Secondary-ranged panic-backhop momentum.  Counts down; while > 0, AI() re-asserts an away-from-
        // player X velocity each tick (overriding the SF4 mover's pursuit, same way FleeToHeal does) so the
        // hop actually carries instead of being cancelled by pursuit on the next tick.
        private int _backhopTicks;
        private int _backhopDir;
        private const int BackhopMomentumTicks = 16;

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
        // -1 = none, 0 = primary, 1 = secondary. Projectile callbacks can queue a deliberate
        // response without reaching into the phase machine or bypassing the normal telegraph.
        private int         _queuedRangedFollowupSlot = -1;
        private int         _queuedRangedFollowupTicks;
        private int         _queuedRangedFollowupTarget = -1;

        /// <summary>
        /// True while the current ranged burst is using the secondary weapon.
        /// Read this in <see cref="DoRangedAttack"/> to decide which projectile to fire.
        /// </summary>
        protected bool IsSecondaryRangedActive => _usingSecondaryRanged;

        /// <summary>Queue a single, fully telegraphed ranged response. The request is consumed as
        /// soon as the puppet is neutral or finishes its current ranged recovery, and expires if it
        /// cannot be used promptly. This intentionally bypasses the ordinary weapon cooldown because
        /// it represents an authored follow-up rather than a new neutral selection.</summary>
        protected void QueueRangedFollowUp(bool useSecondary, int targetPlayer = -1, int lifetimeTicks = 150)
        {
            _queuedRangedFollowupSlot = useSecondary ? 1 : 0;
            _queuedRangedFollowupTarget = targetPlayer;
            _queuedRangedFollowupTicks = Math.Max(1, lifetimeTicks);
            NPC.netUpdate = true;
        }

        /// <summary>Index of the active ranged burst pattern (into the active weapon's pattern pool),
        /// or -1 when this burst isn't using a pattern.  Read in <see cref="DoRangedAttack"/> to fire a
        /// different projectile on a specific pattern.</summary>
        protected int ActiveBurstPatternIndex => _activeBurstPatternIndex;

        /// <summary>True when the shot currently being fired in <see cref="DoRangedAttack"/> is the LAST
        /// shot of the active burst pattern — lets a subclass fire a cross-weapon "finisher" projectile
        /// on the final shot of a pattern.</summary>
        protected bool IsFinalBurstShot => _interShotPauses != null && _interShotPauseIndex >= _interShotPauses.Length;

        /// <summary>0-based index of the shot currently being fired within the active burst pattern
        /// (0 for the first shot), or 0 when not using a pattern. Lets a subclass vary a shot's aim,
        /// spread or speed progressively across a multi-shot pattern.</summary>
        protected int CurrentBurstShotIndex => _interShotPauseIndex;

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
        private int   _weaponAnim;        // counts down current weapon use animation during swings
        private float _prevWeaponRotation; // last tick's _weaponRotation, for swing-speed-gated VFX
        // Projectile-owned flails update this pose after the NPC tick. Keeping the last pose for one
        // tick makes the hand/chain anchor stable regardless of whether NPCs or projectiles update first.
        private bool _flailArmPoseActive;
        private long _flailArmPoseTick = -1;
        private float _flailArmPoseRotation;
        private float _spearGrip = 0.5f;  // 0=grip at head, 1=grip at base; animated for spear extend/retract
        private int   _weaponAnimMax = DefaultWeaponAnimMax;
        private bool  _forceExportHeldWeapon;
        private const int DefaultWeaponAnimMax = 22;

        // ── Tracked blade hit detection ───────────────────────────────────────────
        // Set once per swing/step by ArmBladeHit (via TryMeleeHit / DoComboMeleeHit); consumed
        // every tick by TickBladeHit while the swing is active.  _bladeArmed is explicitly reset
        // to false right before each DoMeleeAttack/DoStabAttack/DoSpearAttack/DoComboMeleeHit call
        // so an override that skips ArmBladeHit (e.g. BlackNinja's flail, which fires a real
        // projectile instead of a blade sweep) correctly leaves TickBladeHit a no-op that tick.
        private bool  _bladeArmed;
        private float _activeBladeReach;
        private float _activeBladeKnockback;
        private int   _activeBladeDamage;
        private Vector2 _previousBladeOrigin;
        private Vector2 _previousBladeTip;
        private bool _hasPreviousBladeSample;
        private readonly HashSet<int> _bladeHitPlayers = new HashSet<int>();
        /// <summary>
        /// Resting hold angle when not attacking (≈ −17° — arm slightly raised, weapon pointing
        /// diagonally forward). The weapon smoothly eases to this during walk / jump / idle.
        /// </summary>
        private const float HoldRotation  = -0.30f;

        // ── Debug instrumentation (DebugMode config only) ───────────────────────────
        // Snapshot of the last DrawWeaponToLayer call's rotation/position math, so the
        // DebugMode overlay (lower-left HUD text) and the weapon-debug log file can report
        // exactly what angle/position decisions produced the on-screen sprite, instead of
        // having to be reverse-engineered from a screenshot.
        internal float   DebugWeaponRotationDeg;
        internal float   DebugDrawRotationDeg;
        internal bool    DebugHoldingSpearNow;
        internal bool    DebugHeldRangedLike;
        internal int     DebugHeldItemType;
        internal float   DebugSpearGrip;
        internal Vector2 DebugHandPos;
        internal Vector2 DebugOrigin;
        internal int     DebugDirection;
        internal string  DebugPhaseName => Phase.ToString();

        /// <summary>Extra pixels to raise the above-head debug readout. Tall puppets (e.g. mounted ones)
        /// override this so the label clears the sprite instead of sitting on top of it.</summary>
        internal virtual float DebugLabelRise => 0f;
        internal int     DebugPhaseTimer => PhaseTimer;
        /// <summary>Friendly name of the set-piece attack currently firing (for the DebugMode HUD),
        /// or null. Subclasses set this when a bespoke attack triggers; the overlay shows it prominently
        /// and alternates its color each time it changes so consecutive attacks are easy to tell apart.</summary>
        internal string  DebugAttackLabel;
        /// <summary>Named combo + motion currently playing, e.g. "Charged Chop/OverheadArc (step 0/1)" —
        /// empty outside combo phases.  Every combo shares the same Phase enum value, so this is the
        /// only way to tell which specific attack is on screen from the overlay/log.</summary>
        internal string DebugComboTag
        {
            get
            {
                if (_attackRuntimeV2.Active)
                {
                    return $"{_activeMeleeCombo.Name}/V2:{_attackRuntimeV2.Clip.Pose} "
                         + $"({_attackRuntimeV2.Stage} {_attackRuntimeV2.StageTick}/{_attackRuntimeV2.StageDuration}, "
                         + $"aim={MathHelper.ToDegrees(_attackRuntimeV2.AimCorrection):F0} deg"
                         + (_attackRuntimeV2.AimLocked ? " locked)" : " tracking)");
                }

                bool inCombo = Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                            || Phase == AttackPhase.MeleeComboPause    || Phase == AttackPhase.MeleeComboRecovery;
                if (!inCombo || _activeMeleeComboIndex < 0 || _activeMeleeCombo.Steps == null
                    || _meleeComboStepIndex >= _activeMeleeCombo.Steps.Length)
                    return "";
                var step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                return $"{_activeMeleeCombo.Name}/{step.Motion} (step {_meleeComboStepIndex}/{_activeMeleeCombo.Steps.Length})";
            }
        }

        /// <summary>True while the puppet has put its weapon away inside a phase that would normally
        /// show it - e.g. a long combo recovery played as unarmed walking. Hides the weapon AND drops
        /// every weapon pose (the Use1-Use4 body frames and the composite arms), so the arms fall
        /// back to the natural walk/idle draw. Default false.</summary>
        protected virtual bool WeaponSheathed => false;
        protected virtual bool ShowWeaponDuringNeutral => false;

        private bool IsWeaponVisiblePhase => !WeaponSheathed && (
            (ShowWeaponDuringNeutral && (Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll)) ||
            Phase == AttackPhase.MeleeTelegraph || Phase == AttackPhase.MeleeAttack ||
            (Phase == AttackPhase.MeleeRecovery && MeleeRecoveryLingerTicks > 0) ||
            Phase == AttackPhase.StabTelegraph  || Phase == AttackPhase.StabAttack  ||
            Phase == AttackPhase.StabRecovery   ||
            Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack ||
            Phase == AttackPhase.CrossbowBurstPause ||
            Phase == AttackPhase.SpearTelegraph  || Phase == AttackPhase.SpearAttack  ||
            Phase == AttackPhase.MagicTelegraph  || Phase == AttackPhase.MagicAttack  ||
            IsHoldingMagicWeaponDuringRecovery ||
            Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack ||
            Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery ||
            Phase == AttackPhase.KnivesTelegraph || Phase == AttackPhase.KnivesThrow ||
            Phase == AttackPhase.KnivesThrowPause ||
            Phase == AttackPhase.PierceTelegraph || Phase == AttackPhase.PierceDash ||
            Phase == AttackPhase.PierceStabHold  || Phase == AttackPhase.PierceStabFlick ||
            Phase == AttackPhase.JumpSlashDodgeback || Phase == AttackPhase.JumpSlashRise ||
            Phase == AttackPhase.JumpSlashAttack ||
            Phase == AttackPhase.FlipSlashRise || Phase == AttackPhase.FlipSlashLand ||
            Phase == AttackPhase.AbyssSlashTelegraph || Phase == AttackPhase.AbyssSlashSwipe ||
            Phase == AttackPhase.AbyssSlashPause ||
            Phase == AttackPhase.TendrilTelegraph || Phase == AttackPhase.TendrilReach ||
            Phase == AttackPhase.TendrilSwingTelegraph || Phase == AttackPhase.TendrilSwing ||
            Phase == AttackPhase.HomingVolleyDodgeback || Phase == AttackPhase.HomingVolleySwingTelegraph ||
            Phase == AttackPhase.HomingVolleySwing ||
            Phase == AttackPhase.SwordLaunchReposition ||
            Phase == AttackPhase.BoomerangSwingTelegraph || Phase == AttackPhase.BoomerangSwing ||
            Phase == AttackPhase.SpiralFanSwingTelegraph || Phase == AttackPhase.SpiralFanSwing ||
            Phase == AttackPhase.SpiralFanBurst || Phase == AttackPhase.SpiralFanPause ||
            Phase == AttackPhase.FireVolleyBackLeap || Phase == AttackPhase.FireVolleyDodgeThrough ||
            Phase == AttackPhase.FireVolleyArcJump ||
            (Phase == AttackPhase.Custom && _customPoseWeapon >= 0) ||
            (Phase == AttackPhase.ClosingDistance && ShowMeleeWeaponWhileClosingDistance) ||
            // Keep the blade on screen for the follow-through hold of the bespoke sword recoveries.
            // Without this the greatsword vanished on the frame the swing ended — the pose was still
            // being held, but nothing was drawing it. Gated on the same opt-in hold, so puppets that
            // leave MeleeRecoveryLingerTicks at 0 are unaffected.
            (IsWeaponRecoveryPhase && IsHoldingMeleeRecoveryFollowThrough()) ||
            // A recovery that keeps the composite arm posed (Artorias lists every bespoke recovery in
            // UseCompositeArmForAdditionalPhase) keeps the sword for its whole length. Hiding it after the
            // linger left the arm raised toward HoldRotation gripping nothing.
            (IsWeaponRecoveryPhase && CompositeArmActive) ||
            (_flight != null && _flight.IsDiving && MeleeWeaponItemType >= 0));

        private bool IsMeleeComboPhase =>
            Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack ||
            Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery;

        /// <summary>Recovery phases that end a weapon swing, and so can hold the finished pose for a
        /// beat before easing back to the carried hold — see
        /// <see cref="IsHoldingMeleeRecoveryFollowThrough"/>. None of these have a rotation branch of
        /// their own, so without the hold they fall straight into the idle ease on frame one.</summary>
        private bool IsWeaponRecoveryPhase =>
            Phase == AttackPhase.MeleeRecovery || Phase == AttackPhase.MeleeComboRecovery ||
            Phase == AttackPhase.JumpSlashRecovery || Phase == AttackPhase.AbyssSlashRecovery ||
            Phase == AttackPhase.TendrilRecovery || Phase == AttackPhase.HomingVolleyRecovery ||
            Phase == AttackPhase.BoomerangRecovery ||
            Phase == AttackPhase.StabRecovery || Phase == AttackPhase.SpearRecovery ||
            Phase == AttackPhase.PierceRecovery || Phase == AttackPhase.SpiralFanRecovery;

        private bool IsTelemetryAttackPhase =>
            Phase != AttackPhase.Idle && Phase != AttackPhase.ClosingDistance
            && Phase != AttackPhase.CasualStroll && Phase != AttackPhase.FleeToHeal
            && Phase != AttackPhase.Healing && Phase != AttackPhase.ShieldGuard;

        private bool IsWeaponPosePhase => IsWeaponVisiblePhase;

        // ── Direction hold (anti-bounce) ──────────────────────────────────────────
        /// <summary>
        /// Ticks remaining during which the current facing direction is locked.
        /// Prevents rapid flip-flopping when the player dodgerolls past the puppet —
        /// the navigator would otherwise flip direction every single tick.
        /// </summary>
        private int _directionHoldTicks;

        /// <summary>Neutral re-face radius (px) and horizontal dead zone (px); see the Idle phase.</summary>
        private const float NeutralRefaceRange = 320f;
        private const float NeutralRefaceDeadZone = 8f;

        /// <summary>A melee combo telegraph tracks the player until this many ticks remain, then commits its
        /// facing. Matches the telegraph flash lead (CheckAndFireFlash's default 30) so the flash IS the commit.</summary>
        private const int TelegraphFacingCommitTicks = 30;

        /// <summary>
        /// Facing captured when the current committed attack began, and re-applied every tick of
        /// that attack by <see cref="LockAttackFacing"/>. The navigator runs BEFORE the attack
        /// state machine each tick and always steers toward the player, so without this the sprite
        /// flipped mid-swing the moment the player got behind — most visibly when a jump attack
        /// leaps clean over them. 0 = nothing committed.
        /// </summary>
        private int _attackFacingDir;

        /// <summary>
        /// Phases that arm their OWN <c>DodgeTimer</c> i-frames as part of the attack (Jump Slash's
        /// backward roll and the airborne leap that follows it, the Forward Flip Slash, Homing
        /// Volley's roll). The dodge-roll movement guard in <see cref="AI"/> skips PuppetAttackAI
        /// while DodgeTimer runs so a REACTIVE dodge's velocity isn't overridden — but these phases
        /// set that timer themselves, so obeying it there froze the attack that armed it: PhaseTimer
        /// stopped counting, the launch velocity stopped being re-asserted, and the navigator took
        /// the puppet back mid-attack (walking toward the player, sprite flipped) until the timer ran
        /// out. Flip Slash was the worst case — it refreshes DodgeTimer to 5 every tick, so its phase
        /// logic only advanced one tick in six.
        /// </summary>
        private bool AttackOwnsDodgeIFrames =>
            Phase == AttackPhase.JumpSlashDodgeback || Phase == AttackPhase.JumpSlashRise ||
            Phase == AttackPhase.FlipSlashRise ||
            Phase == AttackPhase.HomingVolleyDodgeback || Phase == AttackPhase.HomingVolleySwingTelegraph;

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
        /// <summary>Single-clock executor used only by combos that explicitly opt into a V2 clip.</summary>
        private readonly PuppetAttackRuntime _attackRuntimeV2 = new PuppetAttackRuntime();
        private Vector2 _runtimePreviousBladeOrigin;
        private Vector2 _runtimePreviousBladeTip;
        private bool _runtimeHasPreviousBladeSample;

        // ── Hand slots (dual-wield foundation) ───────────────────────────────────
        // Each hand owns the weapon it currently holds plus that arm's blade tracking. The back hand holds
        // PuppetWeapon.None until a puppet equips a second weapon — see the Dual wield section below.
        private readonly PuppetHand _frontHand = new PuppetHand(PuppetHandSlot.Front);
        private readonly PuppetHand _backHand = new PuppetHand(PuppetHandSlot.Back);

        /// <summary>Cached synthesis of the legacy melee virtuals into a weapon descriptor. Built
        /// once on first use — every virtual it reads is a compile-time constant per subclass, so
        /// rebuilding it per tick would only allocate. Damage and reach deliberately stay as
        /// inherit-sentinels because several bosses vary those at runtime.</summary>
        private PuppetWeapon _defaultFrontWeapon;

        /// <summary>True once the front hand has adopted a weapon, so an explicit EquipWeapon —
        /// including one that empties the hand — is never silently overwritten by the default.</summary>
        private bool _frontHandWeaponResolved;

        protected PuppetWeapon DefaultFrontWeapon
        {
            get
            {
                if (_defaultFrontWeapon == null)
                {
                    _defaultFrontWeapon = new PuppetWeapon(
                        MeleeWeaponItemType,
                        damage: -1,     // inherit MeleeDamage — Gwyn and friends vary it per tick
                        handleNorm: MeleeHandleNorm,
                        drawScale: MeleeWeaponDrawScale,
                        bladeWidth: MeleeBladeWidth,
                        archetype: MeleeArchetype,
                        reachBase: -1f, // inherit ComboReachBase for the same reason
                        rotationOffset: MeleeWeaponRotationOffset,
                        singleBladed: MeleeWeaponIsSingleBladed,
                        hideHeldSprite: HideHeldMeleeSprite);
                }

                return _defaultFrontWeapon;
            }
        }

        /// <summary>The weapon in the front hand right now. Until something equips over it this is
        /// the synthesized default, which resolves to exactly what the old virtuals returned.</summary>
        protected PuppetWeapon FrontHandWeapon
        {
            get
            {
                if (!_frontHandWeaponResolved)
                {
                    _frontHand.Weapon = DefaultFrontWeapon;
                    _frontHandWeaponResolved = true;
                }

                return _frontHand.Weapon;
            }
        }

        protected PuppetWeapon BackHandWeapon => _backHand.Weapon;

        /// <summary>Item type currently held in the front hand, or negative when it's empty.</summary>
        protected int FrontHandWeaponType => FrontHandWeapon.ItemType;

        /// <summary>Puts a weapon in a hand for the attack about to play — the per-attack swap entry
        /// point, so one combo can strike with a sword and the next with a mace. Null empties the
        /// hand rather than restoring the default.</summary>
        protected void EquipWeapon(PuppetHandSlot slot, PuppetWeapon weapon)
        {
            PuppetHand hand = _backHand;

            if (slot == PuppetHandSlot.Front)
            {
                hand = _frontHand;
                _frontHandWeaponResolved = true;
            }

            hand.Weapon = weapon ?? PuppetWeapon.None;
        }

        // ── Dual wield ────────────────────────────────────────────────────────────
        // A puppet dual-wields once a weapon is equipped in the BACK hand (EquipWeapon(Back, ...), e.g. from
        // SetDefaults for a permanent off-hand). Both weapons are drawn whenever the melee weapon is out. There
        // is still ONE swing clock (_weaponRotation): each combo step's Hand picks which arm follows it, and the
        // other arm crossfades to OffHandCarryRotation. Everything a step authors — arcs, easing, hit window,
        // aim bias, blade flip — therefore applies to whichever hand swings. Needs the composite arm (the back
        // hand has no legacy 4-frame pose) and is mutually exclusive with the two-handed great-weapon grip.

        /// <summary>Draw the melee weapon(s) while running in to start a melee combo (ClosingDistance), not
        /// only from the telegraph on. Default false keeps the weapon hidden until the tell.</summary>
        protected virtual bool ShowMeleeWeaponWhileClosingDistance => false;

        /// <summary>Weapon-space angle the carried melee weapon eases to outside attacks, and that a logical
        /// telegraph settles from. Default HoldRotation (-0.30). Positive turns the weapon clockwise when facing
        /// right (mirrored when facing left), e.g. +PiOver4 = 45° lower.</summary>
        protected virtual float MeleeCarryRotation => HoldRotation;

        /// <summary>Pose (weapon-space radians) the non-swinging hand carries its weapon at.</summary>
        protected virtual float OffHandCarryRotation => HoldRotation;

        /// <summary>Ticks for a hand to crossfade between its carry pose and the live swing when the swinging
        /// hand changes. A chained step that switches hands needs a PostStepPause at least this long, or the
        /// incoming hand is snapped onto the swing when its attack begins.</summary>
        protected virtual int DualWieldHandBlendTicks => 4;

        // 1 = this hand follows the live swing exactly, 0 = it sits at OffHandCarryRotation.
        private float _frontHandSwingBlend = 1f;
        private float _backHandSwingBlend;

        protected bool DualWieldActive =>
            !_backHand.Weapon.IsEmpty
            && UseCompositeArmSwing
            && CompositeArmSwingMasterEnable
            && !UseTwoHandedCompositeSwing;

        /// <summary>Hand(s) the current combo step swings with. During an inter-step pause this is already
        /// the NEXT step's hand, so the incoming hand crossfades in while the outgoing one settles.</summary>
        protected ComboHand ActiveComboSwingHand
        {
            get
            {
                if (!DualWieldActive || !IsMeleeComboPhase || _activeMeleeComboIndex < 0 || _activeMeleeCombo.Steps == null)
                {
                    return ComboHand.Front;
                }

                int stepIndex = _meleeComboStepIndex;
                bool pausingIntoNextStep = Phase == AttackPhase.MeleeComboPause
                    && stepIndex + 1 < _activeMeleeCombo.Steps.Length;
                if (pausingIntoNextStep)
                {
                    stepIndex++;
                }

                if (stepIndex < 0 || stepIndex >= _activeMeleeCombo.Steps.Length)
                {
                    return ComboHand.Front;
                }

                return _activeMeleeCombo.Steps[stepIndex].Hand;
            }
        }

        /// <summary>The front arm/weapon pose. Identical to _weaponRotation for every puppet that isn't
        /// dual-wielding, and for any non-melee item (staff, bow) held in the front hand.</summary>
        private float FrontHandPoseRotation
        {
            get
            {
                if (!DualWieldActive || _heldItemType != FrontHandWeaponType)
                {
                    return _weaponRotation;
                }

                return MathHelper.Lerp(OffHandCarryRotation, _weaponRotation, _frontHandSwingBlend);
            }
        }

        private float BackHandPoseRotation => MathHelper.Lerp(OffHandCarryRotation, _weaponRotation, _backHandSwingBlend);

        /// <summary>The back weapon is drawn exactly when the front melee weapon is.</summary>
        private bool BackHandWeaponVisible =>
            DualWieldActive
            && _weaponVisible
            && !WeaponSheathed
            && _heldItemType > 0
            && _heldItemType == FrontHandWeaponType
            && Phase != AttackPhase.Healing
            && Phase != AttackPhase.FleeToHeal;

        // Runs once per tick after the swing clock updates. Moves each hand's blend toward "swinging" or
        // "carrying" at 1/DualWieldHandBlendTicks per tick. A hand that is live in an attack phase is forced
        // fully onto the swing so the hitbox never tracks a half-blended pose.
        private void TickHandPoses()
        {
            if (!DualWieldActive)
            {
                _frontHandSwingBlend = 1f;
                _backHandSwingBlend = 0f;
                return;
            }

            ComboHand swingHand = ActiveComboSwingHand;
            float blendStep = 1f / Math.Max(1, DualWieldHandBlendTicks);
            float frontTarget = 1f;
            float backTarget = 0f;

            if (swingHand == ComboHand.Back)
            {
                frontTarget = 0f;
            }
            if (swingHand != ComboHand.Front)
            {
                backTarget = 1f;
            }

            _frontHandSwingBlend = MoveBlendToward(_frontHandSwingBlend, frontTarget, blendStep);
            _backHandSwingBlend = MoveBlendToward(_backHandSwingBlend, backTarget, blendStep);

            if (Phase == AttackPhase.MeleeComboAttack)
            {
                if (frontTarget > 0f)
                {
                    _frontHandSwingBlend = 1f;
                }
                if (backTarget > 0f)
                {
                    _backHandSwingBlend = 1f;
                }
            }
        }

        private static float MoveBlendToward(float current, float target, float maxStep)
        {
            if (current < target)
            {
                return Math.Min(target, current + maxStep);
            }
            return Math.Max(target, current - maxStep);
        }

        /// <summary>Direction locked for the entire active combo.  Player can dodgeroll through
        /// and end up safely behind the swing arc — FighterAI's direction flips are overridden
        /// across all four MeleeCombo* phases.</summary>
        private int _comboLockedDir;
        /// <summary>Toggled every combo start; when <see cref="UseAlternateFlip"/> is on, the arc
        /// case in TickWeaponAnim swaps its start/end endpoints on alternating swings.</summary>
        private bool _comboSwingFlipped;
        /// <summary>Captured once at combo start from the target's relative height; when
        /// <see cref="UseAimAdaptiveArc"/> is on, added to both arc endpoints so the swing biases
        /// toward an airborne/crouched target instead of always arcing level with the ground.</summary>
        private float _comboAimBias;
        private int _thrownComboWeaponIndex = -1;
        private int _thrownWeaponStage;
        private int _thrownWeaponReadTimer;
        private float _thrownWeaponLeapVelocityX;
        private bool _thrownWeaponWasSeen;

        /// <summary>Toggles the alternate-flip parity and (re)computes the aim-adaptive bias for the
        /// swing that's about to play. Called at the start of every swing/combo — both the combo
        /// system (TryStartMeleeCombo) and the plain one-shot MeleeAttack path — so an puppet that
        /// opts into UseAlternateFlip/UseAimAdaptiveArc gets consistent behavior regardless of which
        /// path picked the attack. heightDiff: NPC.Center.Y - target.Center.Y (positive = target
        /// above).</summary>
        private void ArmSwingVariation(float heightDiff)
        {
            _comboSwingFlipped = !_comboSwingFlipped;

            if (AimSwingActive && NPC.HasValidTarget)
            {
                // Full facing-relative aim pitch (BroadswordRework model): reorient the whole arc
                // toward wherever the player actually is (up / level / down), not a clamped nudge.
                // X is forced forward (|dx|) because facing already aims the swing at the player
                // horizontally — only the vertical component varies. Clamped just shy of vertical
                // so a player nearly overhead can't wrap the arc into a broken pose.
                Player target = Main.player[NPC.target];
                float dx = Math.Abs(target.Center.X - NPC.Center.X);
                float dy = target.Center.Y - NPC.Center.Y; // + = player below
                _comboAimBias = MathHelper.Clamp((float)Math.Atan2(dy, Math.Max(dx, 8f)), -MaxAimPitch, MaxAimPitch);
            }
            else
            {
                _comboAimBias = MathHelper.Clamp(-heightDiff / 260f, -0.35f, 0.35f);
            }
        }
        /// <summary>True while a LeapSlam step is mid-air (set at launch, cleared when it lands or
        /// the airtime cap expires).  Gates the landing check that fires the slam hit.</summary>
        private bool _comboLeapLaunched;
        /// <summary>Horizontal velocity locked in at LeapSlam launch (sized to land near the player),
        /// re-asserted each airborne tick so SF4 doesn't steer the arc.</summary>
        private float _comboLeapVx;
        private int _activeComboStepTotalTicks;
        private int _apexDiveFallTicks;
        private bool _apexDiveStrikeStarted;
        /// <summary>Counts only the descending portion of a RisingUppercutLeap. The weapon remains
        /// held overhead until this reaches 30 ticks, independent of how long the ascent took.</summary>
        private int _risingUppercutFallHoldTimer;

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
        /// Weapon archetype that determines the melee combo pool for this puppet.
        /// Auto-detected from the item's useStyle/damage class; override to force a specific archetype.
        /// </summary>
        protected virtual WeaponArchetype MeleeArchetype
        {
            get
            {
                if (_autoMeleeArchetype.HasValue)
                {
                    return _autoMeleeArchetype.Value;
                }
                if (MeleeWeaponItemType <= 0)
                {
                    _autoMeleeArchetype = WeaponArchetype.None;
                    return WeaponArchetype.None;
                }
                var probe = new Item();
                probe.SetDefaults(MeleeWeaponItemType);
                var a = WeaponArchetypeTables.DetectMelee(probe);
                _autoMeleeArchetype = a;
                return a;
            }
        }

        /// <summary>
        /// Weapon archetype that determines the ranged combo pool for this puppet.
        /// Auto-detected from the primary ranged item; override to force a specific archetype.
        /// </summary>
        protected virtual WeaponArchetype RangedArchetype
        {
            get
            {
                if (_autoRangedArchetype.HasValue)
                {
                    return _autoRangedArchetype.Value;
                }
                if (RangedWeaponItemType <= 0)
                {
                    _autoRangedArchetype = WeaponArchetype.None;
                    return WeaponArchetype.None;
                }
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
        /// <summary>Separate chance for physical melee-combo openers selected outside normal melee
        /// reach. Keeping this below 100 lets ordinary bows, crossbows, and throws still compete.</summary>
        protected virtual int RangedStartMeleeComboChance => MeleeComboChance;

        /// <summary>While true, neutral attack selection is skipped: Idle picks nothing and ClosingDistance
        /// drops back to Idle instead of starting a combo. Movement still runs (override RunMovementAI to
        /// stand still). For authored holds: an owner idling while its summon arrives, a summon fading in/out.</summary>
        protected virtual bool HoldAttackSelection => false;

        /// <summary>Optional bespoke combo pool that REPLACES the shared archetype table (for boss
        /// movesets).  null = use the archetype table.  Length defines the cooldown array.</summary>
        protected virtual MeleeCombo[] MeleeComboPoolOverride => null;

        /// <summary>When true, overhead/underhand combo telegraphs first move to the opposite end
        /// of the arc, then visibly wind back to the attack's starting pose. This makes the wind-up
        /// communicate the direction of the upcoming swing instead of merely holding at its apex.</summary>
        protected virtual bool UseLogicalMeleeTelegraphs => false;

        /// <summary>Projectile type used by <see cref="ComboMotion.ThrownWeaponRetrieve"/>. The
        /// projectile must expose its embedded state as <c>ai[0] == 1</c> and record this puppet's
        /// <see cref="NPC.whoAmI"/> in <c>ai[1]</c>. A negative type disables the motion.</summary>
        protected virtual int ThrownComboWeaponProjectileType => -1;
        /// <summary>Spawn the physical thrown weapon and return its projectile slot. Called on the
        /// server when a ThrownWeaponRetrieve step begins.</summary>
        protected virtual int SpawnThrownComboWeapon() => -1;
        /// <summary>Ticks the embedded weapon remains still before the puppet leaps to retrieve it.</summary>
        protected virtual int ThrownWeaponReadTicks => 12;
        protected virtual float ThrownWeaponRetrieveLeapUpSpeed => 8f;
        protected virtual float ThrownWeaponRetrieveLeapMaxSpeed => 9f;
        /// <summary>Final eligibility gate after range/cooldown classification. Useful for health
        /// phase unlocks and attacks that require live projectiles or another encounter resource.</summary>
        protected virtual bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction) => true;
        /// <summary>Whether the normal melee telegraph may re-face during its early read window.
        /// Projectile-authored paths that are already visibly orbiting a locked side can disable this
        /// per combo so the body, arm, chain, and eventual release keep one coherent mirror.</summary>
        protected virtual bool TrackMeleeComboFacingDuringTelegraph(MeleeCombo combo) => true;
        /// <summary>Facing captured when a legacy combo starts. Most attacks face the target; a
        /// reactive follow-up may preserve the previous committed facing so its rear attack remains
        /// honest. Return values are normalized to -1 or +1 by the caller.</summary>
        protected virtual int GetMeleeComboStartFacing(MeleeCombo combo, Player target)
            => target.Center.X < NPC.Center.X ? -1 : 1;
        /// <summary>Per-tick events driven by the same combo clocks as animation and collision.</summary>
        protected virtual void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total) { }
        protected virtual void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total) { }
        /// <summary>Runs after a landing-timed LeapSlam has sampled its real downswing pose for the
        /// current tick. This is the correct point for blade-attached VFX: the ordinary combo tick
        /// happens before terrain prediction updates <c>_leapSlamSwingProgress</c>.</summary>
        protected virtual void OnLandingTimedLeapSlamSwingTick(MeleeComboStep step, float progress) { }
        /// <summary>Called on the exact physics tick a landing-timed LeapSlam touches supported ground.</summary>
        protected virtual void OnLeapSlamLanded(MeleeComboStep step) { }
        /// <summary>Called once as a combo step completes, before its pause/recovery begins.</summary>
        protected virtual void OnComboStepCompleted(MeleeComboStep step) { }
        /// <summary>Called exactly once when a combo is chosen — both on the roll (host/singleplayer)
        /// and when a client receives the matching network snapshot. `ActiveMeleeComboName` goes stale
        /// between combos, so a subclass that needs to know "which weapon is this combo" should latch
        /// that decision here instead of re-deriving it from the name on every read.
        /// A subclass override MUST call base.OnMeleeComboStarted(combo) — this rebuilds the cached
        /// FrontHandWeapon (see DefaultFrontWeapon above) so MeleeWeaponItemType/HideHeldMeleeSprite/etc
        /// reflect the combo that just started rather than whichever weapon happened to be active the
        /// very first time anything ever read FrontHandWeapon. Cheap for a puppet with one fixed melee
        /// weapon (nothing actually changes on rebuild); required for one that switches weapons per
        /// combo (Dread Wraith mace-vs-glaive) — without it, the wrong weapon's icon/hide-flag can stick
        /// for the rest of the fight.</summary>
        protected virtual void OnMeleeComboStarted(MeleeCombo combo)
        {
            _defaultFrontWeapon = null;
            _frontHandWeaponResolved = false;
        }
        /// <summary>True only when the currently completing legacy combo step made a confirmed blade
        /// or reported projectile contact. Useful for effects and conditional branches.</summary>
        protected bool CurrentComboStepHitConnected => _currentComboStepHitConnected;
        protected string ActiveMeleeComboName => _activeMeleeCombo.Name;
        protected ComboMotion ActiveMeleeComboMotion =>
            _activeMeleeCombo.Steps != null
            && _meleeComboStepIndex >= 0
            && _meleeComboStepIndex < _activeMeleeCombo.Steps.Length
                ? _activeMeleeCombo.Steps[_meleeComboStepIndex].Motion
                : ComboMotion.OverheadArc;
        /// <summary>The active step's ReachMult, or 1 outside a combo. Lets a subclass's own cosmetic
        /// effects (e.g. a blade-glow anchored to the weapon tip) use the SAME reach the real hitbox
        /// uses (<c>ComboReachBase * 0.7f * ReachMult</c> — see <see cref="TickBladeHit"/>) instead of
        /// drifting from it by re-deriving their own tip distance.</summary>
        protected float ActiveMeleeComboReachMult =>
            _activeMeleeCombo.Steps != null
            && _meleeComboStepIndex >= 0
            && _meleeComboStepIndex < _activeMeleeCombo.Steps.Length
                ? _activeMeleeCombo.Steps[_meleeComboStepIndex].ReachMult
                : 1f;
        /// <summary>Called after a step completes but before its next step is scheduled. Returning
        /// false ends the combo in normal recovery, enabling honest hit-confirmed branches.
        /// Multiplayer: server only — clients predict the continuation and adopt a refusal.</summary>
        protected virtual bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit) => true;
        /// <summary>Called on an isolated copy of a newly selected combo. Subclasses may append
        /// health-phase follow-ups without mutating the shared static combo table.
        /// Multiplayer: server only — the customized copy reaches clients in every snapshot.</summary>
        protected virtual void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction) { }
        /// <summary>Called immediately before a later combo step begins. Subclasses may replace the
        /// copied step using current spacing, enabling conditional retrieval or pursuit follow-ups.
        /// Multiplayer: server only — the edited step reaches clients in every snapshot.</summary>
        protected virtual void ModifyNextMeleeComboStep(
            string comboName, int nextStepIndex, Player target, ref MeleeComboStep nextStep) { }

        /// <summary>Allows a subclass to give a particular ranged payload a distinct readable
        /// telegraph while retaining the shared ranged phase executor.</summary>
        protected virtual void ModifyRangedBurst(
            bool secondary,
            ref int itemType,
            ref RangedStyle style,
            ref Color flashColor,
            ref int telegraphTicks,
            ref int attackTicks,
            ref int recoveryTicks) { }

        /// <summary>True while a subclass wants to fully own NPC.velocity for a ranged telegraph/
        /// attack tick instead of the shared standing-shot brake / momentum drift — e.g. a scripted
        /// jump arc timed to fire at its apex. Checked ahead of the normal damping in both phases.</summary>
        protected virtual bool HasRangedJumpOverride => false;

        /// <summary>Called once per tick, in place of the normal ranged-phase velocity damping,
        /// while <see cref="HasRangedJumpOverride"/> is true. Default no-op.</summary>
        protected virtual void TickRangedJumpOverride() { }

        /// <summary>Reactive combo selection hook.  Called after the range/HP/cooldown weights are
        /// built but before the weighted roll; return a ready combo index chosen from LIVE player
        /// state (mid-dodgeroll direction, launched, flanking) to react to what the player is doing
        /// right now, or -1 to fall through to the default weighted roll.  <paramref name="ready"/>
        /// is the per-combo effective-weight array (0 = on cooldown / wrong band — don't pick those).</summary>
        protected virtual int ReactiveComboIndex(float dist, ComboRangeBand band, int[] ready) => -1;

        /// <summary>When true (default), generic melee/spear/combo phases brake the puppet so attacks
        /// read as planted swings. Set false to preserve normal movement velocity throughout melee use.
        /// Explicit attack movement such as lunges, leaps, charges, and forward pushes still applies.</summary>
        protected virtual bool SlowDownBeforeMelee => true;

        /// <summary>Base reach (px) for melee COMBO hitboxes before the step's ReachMult.  Default MeleeRange;
        /// override larger for long weapons (spears) so combo swings connect at their visual reach.</summary>
        protected virtual float ComboReachBase => MeleeRange;

        /// <summary>Optional forward pressure during the initial combo telegraph. The puppet only
        /// advances while the target remains on the originally committed side, so crossing through
        /// the wind-up still cleanly dodges the attack. Zero preserves the planted legacy behavior.</summary>
        protected virtual float ComboTelegraphAdvanceSpeedMult => 0f;
        protected virtual float ComboTelegraphAdvanceStopDistance => MeleeRange * 0.75f;

        // ── Closing distance (anti-whiff for melee combos) ────────────────────────
        /// <summary>Distance (px) at which a chosen melee combo is close enough to actually connect.  If a
        /// combo is rolled while farther than this (but within <see cref="ComboMaxStartRange"/>), the puppet
        /// first sprints in via <see cref="AttackPhase.ClosingDistance"/> and only swings once inside it.</summary>
        protected virtual float MeleeEngageRange => MeleeRange + 24f;
        /// <summary>Speed multiplier while closing distance to a melee target.</summary>
        protected virtual float ClosingDistanceSpeedMult => 1.4f;
        /// <summary>Max ticks spent closing before giving up and re-deciding (so it can switch to ranged).</summary>
        protected virtual int ClosingDistanceMaxTicks => 80;
        /// <summary>Chance (0-100) of preferring a ranged combo over the legacy random-burst path.
        /// Set to 0 to disable ranged combos entirely.</summary>
        protected virtual int RangedComboChance => 50;

        /// <summary>Multiplier applied to a melee combo's first-step TelegraphTicks at runtime.
        /// Default 1.35 = ~35% longer telegraph windows than the raw data table values, giving
        /// the player more time to read the incoming combo.  Lower for faster, more aggressive
        /// puppets.</summary>
        protected virtual float ComboTelegraphMultiplier => 1.35f;

        /// <summary>Hard floor on a combo's first-step telegraph duration (ticks).  Even after
        /// applying <see cref="ComboTelegraphMultiplier"/>, no combo can start with less than
        /// this many ticks of wind-up — guarantees the player sees the sword-raise / hold-at-apex
        /// arc clearly before damage is delivered.  30 ticks = 0.5 s minimum.</summary>
        protected virtual int MinComboTelegraphTicks => 30;

        /// <summary>Max distance (px) at which a melee combo can begin.  Beyond this, the
        /// puppet falls through to ranged/legacy attack paths.  Default = StabRange + 80
        /// covers thrusts + most dash lunges.</summary>
        protected virtual float ComboMaxStartRange => StabRange + 80f;

        /// <summary>Optional farther selection radius for committed <see cref="MeleeCombo.RangedStartOnly"/>
        /// gap-closers. If no such combo is ready beyond <see cref="ComboMaxStartRange"/>, the puppet
        /// falls through to its ranged/magic attacks instead of entering ordinary ClosingDistance.</summary>
        protected virtual float RangedStartComboMaxRange => ComboMaxStartRange;

        // ── Wings / flight ────────────────────────────────────────────────────────
        /// <summary>Master toggle: when true, this puppet can take off, hover, dive, and land.
        /// Subclasses can wire this to a static config flag or a ModConfig field.</summary>
        protected virtual bool HasWings => false;

        /// <summary>Vanilla wing item type whose sprite + wingSlot is used for the puppet draw.
        /// Default is AngelWings; subclasses override for thematic fit.</summary>
        protected virtual int WingsAccessoryItemType => ItemID.AngelWings;

        /// <summary>When false, the folded wings are hidden while grounded and only appear once
        /// airborne — for puppets whose grounded silhouette should stay clean (e.g. Gwyn's cape).</summary>
        protected virtual bool ShowWingsWhenGrounded => true;

        /// <summary>Flight tuning.  Override to customize hover altitude / dive speed / cooldowns.</summary>
        protected virtual EnemyFlightConfig FlightConfig => EnemyFlightConfig.Default;

        // ── Mount / cavalry ───────────────────────────────────────────────────────
        // Deliberately shaped like the wings block above: a master toggle, a vanilla item/ID for the
        // visual, and a config struct driving a controller in NPCs/AI. The mount renders for free —
        // PuppetNPC draws through Main.PlayerRenderer.DrawPlayer, and vanilla mounts are drawn by the
        // MountBack/MountFront player draw layers inside that pipeline.

        /// <summary>Master toggle: when true this puppet spawns riding <see cref="MountType"/> and uses the
        /// jousting movement controller instead of the ground navigator until the mount is destroyed.</summary>
        protected virtual bool HasMount => false;

        /// <summary>Vanilla <see cref="Terraria.ID.MountID"/> to ride. -1 disables.</summary>
        protected virtual int MountType => -1;

        /// <summary>Charge / overshoot / turnaround tuning.</summary>
        protected virtual EnemyMountConfig MountConfig => EnemyMountConfig.Default;

        /// <summary>Top speed a combo step's ForwardPushMult (lunges, charges, jousts) drives with,
        /// instead of the plain ground TopSpeed. Default is a no-op (TopSpeed) for every puppet without
        /// a mount; a mounted puppet should override this so a charge-flavored attack step actually
        /// moves at (roughly) the mount's own speed rather than the rider's on-foot pace — otherwise a
        /// combo allowed to start from mount-charge range can finish its whole animation stranded far
        /// short of the target it was aimed at.</summary>
        protected virtual float ComboForwardPushTopSpeed => TopSpeed;

        /// <summary>Separate health pool for the mount. All incoming damage drains this before any reaches
        /// the rider, so the mount reads as a destructible first phase.</summary>
        protected virtual int MountLifeMax => 0;

        /// <summary>Contact damage dealt while the mount is mid-charge. Applied ONLY during the committed
        /// run-past — outside that window the puppet keeps its usual zero contact damage.</summary>
        protected virtual int MountTrampleDamage => 0;

        /// <summary>Vertical correction (px) applied to the rider's hand anchor while mounted. Mounting
        /// raises the drawn rider above NPC.Center, but the hand rig is built off NPC.Center, so weapons
        /// and the flail anchor detach without this. Negative = up. Needs calibrating in-game per mount.</summary>
        protected virtual float MountedHandOffsetY => -22f;

        /// <summary>Top-speed multiplier applied while the rider is mid-attack. Keeps the mount moving
        /// during casts and swings instead of stopping dead.</summary>
        protected virtual float MountedAttackSpeedScale => 0.55f;

        /// <summary>Extra vertical nudge (px) for the whole mounted assembly on top of the mount's own
        /// heightBoost. Positive = down. The heightBoost correction in SyncPuppet does the real work;
        /// this exists to fine-tune how deep the rider sits in the saddle per mount.</summary>
        protected virtual float MountedSeatOffsetY => 0f;

        /// <summary>Top-left the puppet is drawn from. While mounted the rider is raised by the mount's
        /// heightBoost so the MOUNT's feet land on the ground instead of the rider's — without this the
        /// whole assembly renders about a tile too low and the goat sinks into the floor.</summary>
        private Vector2 PuppetDrawPosition
        {
            get
            {
                if (!IsMounted || _puppet == null || _puppet.mount == null || !_puppet.mount.Active)
                {
                    return PuppetVisualPosition;
                }

                float raise = _puppet.mount.HeightBoost - MountedSeatOffsetY;

                return new Vector2(NPC.position.X, NPC.position.Y - raise) + PuppetVisualOffset;
            }
        }

        /// <summary>Hook for the moment the mount's health pool is emptied — gore, phase-two setup, stat
        /// changes. The rider is already dismounted and its own health restored when this fires.</summary>
        protected virtual void OnMountDestroyed() { }

        private EnemyMountController _mount;
        /// <summary>Exposed so subclasses can read charge state when picking attacks.</summary>
        protected EnemyMountController MountAI => _mount;

        private bool _mountSpawned;
        private int _riderLifeMax;
        private int _mountTrampleCooldown;

        /// <summary>Ticks between trample hits so a single charge can't connect on consecutive frames.</summary>
        private const int MountTrampleCooldownTicks = 45;

        /// <summary>True while the mount is alive and carrying the rider. Note the mount has no separate
        /// health counter: during phase one NPC.life IS the mount's pool, which keeps one source of truth
        /// and lets the vanilla boss bar show it with no custom UI.</summary>
        public bool IsMounted => HasMount && _mountSpawned;

        /// <summary>Mount health/phase state lives here rather than in InitPuppet, because InitPuppet is only
        /// ever called from draw code (PreDraw / DrawExportPose) — it would never run on a dedicated server,
        /// and would run late in singleplayer. OnSpawn is server-authoritative and runs before any AI tick.</summary>
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);

            // Generic shader-based slash trail (see HasSlashTrailVFX / TryGetMeleeSlashTrailPose):
            // one visual-only ribbon projectile per puppet, reading this puppet's own live blade
            // pose every frame. Opt-in only, so puppets still on the sprite-strip HasSlashVFX system
            // are unaffected.
            if (HasSlashTrailVFX && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<PuppetSwordSlashTrail>(), 0, 0f,
                    Main.myPlayer, NPC.whoAmI);
            }

            if (!HasMount || MountType <= 0 || MountLifeMax <= 0)
            {
                return;
            }

            _mountSpawned = true;
            _riderLifeMax = NPC.lifeMax;

            NPC.lifeMax = MountLifeMax;
            NPC.life = MountLifeMax;
        }

        /// <summary>Contact damage, but ONLY during the committed run-past — outside that window the puppet
        /// keeps its usual zero contact damage, so closing to melee range stays safe and readable.</summary>
        private void TickMountTrample(Player target)
        {
            if (_mountTrampleCooldown > 0)
            {
                _mountTrampleCooldown--;
            }

            if (_mount == null || !_mount.IsCharging || MountTrampleDamage <= 0)
            {
                return;
            }

            if (target == null || !target.active || target.dead || _mountTrampleCooldown > 0)
            {
                return;
            }

            if (!NPC.Hitbox.Intersects(target.Hitbox))
            {
                return;
            }

            target.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), MountTrampleDamage, NPC.direction);
            _mountTrampleCooldown = MountTrampleCooldownTicks;
        }

        /// <summary>Break the mount and hand the fight to the rider. Restoring lifeMax/life makes the vanilla
        /// boss bar visibly refill, which is the phase-two tell.</summary>
        private void DestroyMount()
        {
            _mountSpawned = false;

            // MountAI must go null here, not just _mountSpawned false: a subclass's PostAI typically
            // gates ALL its mount-only effects (charge embers, glaive glow, ...) on "MountAI == null ->
            // return", and the controller was otherwise left alive frozen mid-charge — e.g. IsCharging
            // stuck true forever if the mount died mid-charge — so those effects kept firing every tick
            // for the rest of the fight instead of stopping when the mount actually broke.
            _mount = null;

            if (_puppet != null && _puppet.mount != null && _puppet.mount.Active)
            {
                _puppet.mount.Dismount(_puppet);
            }

            NPC.lifeMax = _riderLifeMax;
            NPC.life = _riderLifeMax;

            // Push the phase change to clients immediately — the synced _mountSpawned flag is what makes
            // their SyncPuppet stop drawing the mount.
            NPC.netUpdate = true;

            OnMountDestroyed();
        }

        public override bool CheckDead()
        {
            // Phase-one "death" is the mount breaking, not the rider dying: refuse the kill, dismount, and
            // let the restored rider health carry the fight into phase two.
            if (IsMounted)
            {
                DestroyMount();
                return false;
            }

            return base.CheckDead();
        }

        /// <summary>Chance per second of triggering a random idle takeoff burst (0-100).
        /// Set to 0 to only fly when tactically necessary (player above, blocked, low HP).</summary>
        protected virtual int RandomTakeoffChance => 8;

        /// <summary>Height differential (player above puppet) above which the puppet
        /// will preferentially take off rather than try to navigate up on foot.</summary>
        protected virtual float FlightHeightTrigger => 120f;

        /// <summary>HP fraction at or below which the puppet gains an aerial-phase preference
        /// (random takeoff chance is amplified, dive attacks more likely).</summary>
        protected virtual float FlightHpEscalationFrac => 0.50f;

        /// <summary>Flight-combat tuning shared by every winged puppet. Dives can target above,
        /// beside, or below the NPC; the waypoint leads moving players by this many ticks.</summary>
        protected virtual bool CanUseAerialMelee => true;
        protected virtual int AerialDiveCooldownTicks => 120;
        protected virtual float AerialDiveLeadTicks => 10f;
        protected virtual float AerialMeleeRange => 620f;
        protected virtual float AerialRangedVerticalBand => 96f;
        /// <summary>When true, the airborne ranged trigger rolls a full burst pattern (fan/twin-ring/
        /// accelerating chain/etc., whatever PrimaryRangedBurstPatterns or SecondaryRangedBurstPatterns
        /// define) the same way the grounded trigger does, instead of forcing a single standing shot.
        /// Default false preserves every existing winged puppet's aerial potshot exactly.</summary>
        protected virtual bool AllowRangedPatternsAirborne => false;

        private EnemyFlightController _flight;
        /// <summary>Subclass access to the flight controller (created lazily on the first tick
        /// HasWings is true) — lets a boss command a scripted takeoff/land for a set-piece
        /// (e.g. Gwyn's Sunlight Spear Storm) without owning its own controller.</summary>
        protected EnemyFlightController Flight => _flight;
        private Item _wingsItemCache;
        private int  _cachedWingsType = -1;
        /// <summary>Hot-swap support: (re)applies armor/dye whenever a subclass's
        /// Head/Body/LegsArmorItemType changes (e.g. a health-phase-driven set swap), the same idea
        /// as the wings cache above but for the three base equip slots. -1 forces the first apply.</summary>
        private int  _cachedHeadArmorItemType = -1;
        private int  _cachedBodyArmorItemType = -1;
        private int  _cachedLegsArmorItemType = -1;
        /// <summary>Cooldown between consecutive aerial-dive hits.  Ticks down each AI frame.</summary>
        private int  _aerialHitCooldown;
        private int  _aerialDiveCooldown;

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

        // ── Dash afterimage trail (opt-in) ────────────────────────────────────────
        // Sparse translucent echoes of the puppet at recent NPC.oldPos[] positions, drawn via
        // vanilla's own fractal-afterimage fields (Player.isFirstFractalAfterImage /
        // firstFractalAfterImageOpacity — the same mechanism vanilla dash accessories use).  Set
        // AfterimageTicks > 0 (e.g. at the start of a dash) to show the trail for that many ticks;
        // it counts itself down in AI() and is a no-op for any puppet that never sets it.
        protected int AfterimageTicks;
        /// <summary>Draw an echo every Nth cached old-position sample (NPC.oldPos has 10 slots by default).</summary>
        protected virtual int AfterimageSampleStep => 2;
        /// <summary>Maximum number of cached old-position slots eligible for afterimages. This lets
        /// one attack use a longer type-level trail cache without lengthening every other dash.</summary>
        protected virtual int AfterimageSampleLimit => NPC.oldPos.Length;
        protected virtual float AfterimageOpacity => 0.4f;

        // ─────────────────────────────────────────────────────────────────────────
        // AI
        // ─────────────────────────────────────────────────────────────────────────

        private bool TickPuppetTeleport(tsorcRevampGlobalNPC globalNPC, Player target)
        {
            if (!globalNPC.TeleportChargesInitialized)
            {
                globalNPC.TeleportChargesRemaining = globalNPC.TeleportMaxCharges < 0
                    ? int.MaxValue
                    : globalNPC.TeleportMaxCharges;
                globalNPC.TeleportChargesInitialized = true;
            }

            if (globalNPC.TeleportCooldownTimer > 0)
            {
                globalNPC.TeleportCooldownTimer--;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient
                && globalNPC.CanTeleport
                && globalNPC.TeleportCountdown == 0
                && globalNPC.TeleportAppearanceTimer == 0
                && globalNPC.TeleportChargesRemaining > 0
                && globalNPC.TeleportCooldownTimer == 0)
            {
                bool hasLineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height,
                    target.position, target.width, target.height)
                    && Collision.CanHitLine(NPC.position, NPC.width, NPC.height,
                        target.position, target.width, target.height);

                bool shouldTeleport = globalNPC.TeleportStyle switch
                {
                    TeleportStyle.Aggressive => !hasLineOfSight && globalNPC.DisengageTimer >= 30,
                    TeleportStyle.Relaxed => globalNPC.PursuitState == PursuitState.Patrol && globalNPC.PatrolElapsed >= 300,
                    TeleportStyle.RecoveryOnly => false,
                    _ => globalNPC.PursuitState == PursuitState.Patrol || globalNPC.PursuitState == PursuitState.Search,
                };

                if (shouldTeleport && !tsorcRevampAIs.TryTeleportReacquire(NPC, globalNPC))
                {
                    globalNPC.TeleportCooldownTimer = 30;
                }
            }

            if (globalNPC.TeleportCountdown > 0)
            {
                NPC.velocity *= 0.1f;
                if (NPC.velocity.LengthSquared() < 0.01f)
                {
                    NPC.velocity = Vector2.Zero;
                }
                if (globalNPC.TeleportVisualStyle != TeleportVisualStyle.Default
                    && globalNPC.TeleportVisualStyle != TeleportVisualStyle.MagicIllusion)
                {
                    NPC.alpha = 255;
                }

                globalNPC.TeleportCountdown--;
                if (globalNPC.TeleportCountdown == 0)
                {
                    tsorcRevampAIs.ExecuteQueuedTeleport(NPC);
                }
                return true;
            }

            if (globalNPC.TeleportAppearanceTimer > 0)
            {
                NPC.velocity *= 0.1f;
                if (NPC.velocity.LengthSquared() < 0.01f)
                {
                    NPC.velocity = Vector2.Zero;
                }
                NPC.alpha = 255;

                globalNPC.TeleportAppearanceTimer--;
                if (globalNPC.TeleportAppearanceTimer == 0)
                {
                    NPC.Center = globalNPC.TeleportTelegraph;
                    globalNPC.TeleportTelegraph = Vector2.Zero;
                    NPC.alpha = 0;
                    NPC.netUpdate = true;
                }
                return true;
            }

            return false;
        }

        public override void AI()
        {
            UpdateSpectralHistory();
            tsorcRevampGlobalNPC gnpc = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            // Attack-armed i-frames (AttackOwnsDodgeIFrames) must not blink the body out in GlobalNPC.PreDraw.
            // Set before AI's early returns so it can never stay stuck on after the attack ends.
            gnpc.SuppressDodgeBlink = AttackOwnsDodgeIFrames;

            // Once a party wipe starts the shared handler owns the remaining 240-tick fade. Returning
            // here prevents a frozen player from being targeted and, importantly, stops an in-progress
            // attack from continuing to spawn its own VFX or hazards during that fade.
            if (!gnpc.IsTeleportIllusion && DespawnsOnPartyWipe && UpdatePartyWipeDespawn())
            {
                return;
            }

            // Client: entry cues of a phase adopted from a snapshot — its transition ran on the server, not here. Deferred
            // from ReceiveExtraAI to this tick so the hooks read the subclass fields that arrived in the same packet.
            if (_adoptedPhaseEntryPending)
            {
                _adoptedPhaseEntryPending = false;
                switch (Phase)
                {
                    case AttackPhase.MeleeAttack:
                    case AttackPhase.StabAttack:
                    case AttackPhase.PierceDash:
                        SetDisplayWeapon(FrontHandWeaponType, swing: true);
                        break;

                    case AttackPhase.MeleeComboAttack:
                        SetDisplayWeapon(FrontHandWeaponType, swing: true);
                        ComboMotion adoptedMotion = _activeMeleeCombo.Steps[_meleeComboStepIndex].Motion;
                        if (IsSwingingMotion(adoptedMotion))
                        {
                            PlayMeleeSwingSound();
                        }
                        break;

                    case AttackPhase.SpearAttack:
                        SetDisplayWeapon(SpearWeaponItemType, swing: true);
                        break;

                    case AttackPhase.MagicAttack:
                        SetDisplayWeapon(MagicWeaponItemType, swing: true);
                        break;

                    case AttackPhase.RangedAttack:
                        SetDisplayWeapon(_activeRangedItemType, swing: true);
                        break;

                    case AttackPhase.KnivesThrow:
                        SetDisplayWeapon(CursedKnivesWeaponItemType, swing: true);
                        break;

                    case AttackPhase.PierceStabHold:
                        // Contact was resolved on the server; replay the hook for its local impale state, sound and shake
                        // (subclass spawns inside it are server-gated).
                        if (_pierceTarget != null && _pierceTarget.active)
                        {
                            OnPierceContact(_pierceTarget, _pierceIsStab);
                        }
                        break;

                    case AttackPhase.Custom:
                        if (_customPoseWeapon >= 0)
                        {
                            SetDisplayWeapon(_customPoseWeapon, swing: _customSwingPose);
                        }
                        DoCustomAttack();
                        break;
                }
            }

            if (gnpc.IsTeleportIllusion)
            {
                NPC.boss = false;
                NPC.value = 0f;
                NPC.dontTakeDamage = true;
                NPC.alpha = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient && --gnpc.TeleportIllusionTimeLeft <= 0)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<TeleportIllusionDissolve>(),
                        0, 0f, Main.myPlayer);
                    NPC.active = false;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                    }
                    return;
                }
            }
            // Cheap edge-detect every tick regardless of HasSlashTrailVFX — a puppet that never
            // opts in just never has anything read _meleeSlashTrailSequence.
            UpdateMeleeSlashTrailSequence();
            UpdateFireSlashFade();
            // ── First-spawn invasion banner ────────────────────────────────────────
            // localAI[0] is not synced across the network, so every client (and singleplayer)
            // initialises it at 0 independently.  On the very first tick we fire the banner
            // exactly once per NPC instance on each client — no packet required.
            if (!gnpc.IsTeleportIllusion && AnnounceInvasion
                && Main.netMode != NetmodeID.Server && NPC.localAI[0] == 0f)
            {
                NPC.localAI[0] = 1f;
                tsorcRevamp.ShowAnnouncementBanner(
                    Terraria.Localization.Language.GetTextValue("Mods.tsorcRevamp.EncounterPresentation.Banners.InvadedBy", InvaderTitle),
                    Color.White);
            }

            // ── Lazy estus init ────────────────────────────────────────────────────
            if (_estusCharges < 0)
                _estusCharges = EstusChargesMax;

            if (_rangedCooldown          > 0)
            {
                _rangedCooldown--;
            }
            if (_secondaryRangedCooldown > 0)
            {
                _secondaryRangedCooldown--;
            }
            if (_shieldGuardCooldown     > 0)
            {
                _shieldGuardCooldown--;
            }
            if (_queuedRangedFollowupTicks > 0)
            {
                _queuedRangedFollowupTicks--;
                if (_queuedRangedFollowupTicks <= 0)
                {
                    _queuedRangedFollowupSlot = -1;
                    _queuedRangedFollowupTarget = -1;
                }
            }
            if (_stabCooldown            > 0)
            {
                _stabCooldown--;
            }
            if (_spearCooldown           > 0)
            {
                _spearCooldown--;
            }
            if (_magicCooldown           > 0)
            {
                _magicCooldown--;
            }
            if (_healCooldown            > 0)
            {
                _healCooldown--;
            }
            if (_breathCooldown          > 0)
            {
                _breathCooldown--;
            }
            if (_cursedKnivesCooldown    > 0)
            {
                _cursedKnivesCooldown--;
            }
            if (_pierceCooldown          > 0)
            {
                _pierceCooldown--;
            }
            if (_jumpSlashCooldown       > 0)
            {
                _jumpSlashCooldown--;
            }
            if (_flipSlashCooldown       > 0)
            {
                _flipSlashCooldown--;
            }
            if (_abyssSlashCooldown      > 0)
            {
                _abyssSlashCooldown--;
            }
            if (_tendrilCooldown         > 0)
            {
                _tendrilCooldown--;
            }
            if (_abyssShardCooldown      > 0)
            {
                _abyssShardCooldown--;
            }
            if (_homingVolleyCooldown    > 0)
            {
                _homingVolleyCooldown--;
            }
            if (_boomerangCooldown       > 0)
            {
                _boomerangCooldown--;
            }
            if (_spiralFanCooldown       > 0)
            {
                _spiralFanCooldown--;
            }
            if (AfterimageTicks          > 0)
            {
                AfterimageTicks--;
            }
            if (_meleeComboCooldowns != null)
                for (int i = 0; i < _meleeComboCooldowns.Length; i++)
                    if (_meleeComboCooldowns[i] > 0)
                    {
                        _meleeComboCooldowns[i]--;
                    }
            if (_rangedComboCooldowns != null)
                for (int i = 0; i < _rangedComboCooldowns.Length; i++)
                    if (_rangedComboCooldowns[i] > 0)
                    {
                        _rangedComboCooldowns[i]--;
                    }
            if (_aerialHitCooldown > 0)
            {
                _aerialHitCooldown--;
            }
            if (_aerialDiveCooldown > 0)
            {
                _aerialDiveCooldown--;
            }
            if (gnpc.FighterEvasionCooldown > 0)
            {
                gnpc.FighterEvasionCooldown--;
            }

            // Decay recent-damage memory so old hits don't keep triggering heals forever.
            _recentDamage = Math.Max(0f, _recentDamage - RecentDamageDecayRate);

            // Reset HP-threshold flags once the puppet's HP rises back above each level
            // (heal restored it, or it just started above those values).
            float hpFrac = (float)NPC.life / NPC.lifeMax;
            if (_halfHpHealed    && hpFrac > 0.55f)
            {
                _halfHpHealed    = false;
            }
            if (_quarterHpHealed && hpFrac > 0.30f)
            {
                _quarterHpHealed = false;
            }

            if (_directionHoldTicks > 0)
                _directionHoldTicks--;

            // Push navigation settings onto the GlobalNPC instance so FighterAI uses them.
            // Tier 2 enables waypoint routing: when stuck, it scans for a horizontal opening,
            // an elevated ledge to jump to, or a platform edge to drop off.
            gnpc.CanUseRopes     = true; // Puppets default to rope-climbing (only takes effect on SF4-mover puppets)
            gnpc.MaxJumpPower    = PuppetJumpPower;
            gnpc.MaxJumpBoost    = PuppetJumpBoost;
            gnpc.CanDoubleJump   = PuppetCanDoubleJump;
            gnpc.DoubleJumpPower = PuppetDoubleJumpPower;
            gnpc.TeleportTelegraphTime = TeleportTelegraphTicks;
            gnpc.TeleportDustType      = TeleportDustTypeId;
            gnpc.TeleportDustColor     = TeleportDustTint;
            gnpc.TeleportDustScale     = TeleportDustScale;
            gnpc.TeleportDustCount     = TeleportDustCount;

            // Quick-step tuning (used by both on-hit and preemptive quick-steps via the shared executor).
            gnpc.QuickStepRecoveryTicks = QuickStepRecoveryTicks;
            gnpc.QuickStepForwardRoom   = QuickStepForwardRoom;

            // SF4 puppets don't run BasicAI, which normally winds these down. Tick every armed
            // dodge timer here, not only puppets opted into proactive evasion: several authored
            // attacks (Jump Slash, Flip Slash, Homing Volley) grant their own DodgeTimer. Leaving
            // those timers gated on EvadesProjectiles/PreemptiveQuickStepChance made them permanent
            // on puppets such as Soul of Cinder, suppressing all later attacks and granting endless
            // blinking i-frames after the first such move.
            if (gnpc.DodgeTimer > 0)
            {
                gnpc.DodgeTimer--;
            }
            if (gnpc.DodgeCooldown > 0)
            {
                gnpc.DodgeCooldown--;
            }

            Player target = Main.player[NPC.target];
            float distToTarget = NPC.Distance(target.Center);

            // A stagger owns the puppet until the stun expires. OnStagger has already cancelled
            // its current phase, so do not let neutral AI immediately start another action.
            if (gnpc.StaggerTimer > 0)
            {
                UpdateAttackCommitFlags();
                TickWeaponAnim();
                return;
            }

            if (TickPuppetTeleport(gnpc, target))
            {
                UpdateAttackCommitFlags();
                TickWeaponAnim();
                return;
            }

            // ── Flight tick (before ground movement) ──────────────────────────────
            // When airborne, the flight controller fully owns velocity / noGravity / direction.
            // We skip the ground navigator (SmartFighter4) entirely so it doesn't fight the
            // flight controller's intent.
            if (HasWings)
            {
                _flight ??= new EnemyFlightController(FlightConfig);
                _flight.Tick(NPC, target);
                if (_flight.IsAirborne)
                {
                    // Advance a quick-step (e.g. armed by an on-hit reaction) AFTER the flight tick so it
                    // overrides flight velocity; skip attacks while stepping / mid-dodge.
                    tsorcRevampAIs.TickQuickStep(NPC, gnpc);
                    if (gnpc.DodgeTimer <= 0 && gnpc.QuickStepTimer <= 0 && gnpc.QuickStepRecoveryTimer <= 0)
                        PuppetAttackAI();
                    UpdateAttackCommitFlags();
                    TickWeaponAnim();
                    return;
                }
            }

            // ── Mount tick (before ground movement) ───────────────────────────────
            // While mounted, the jouster controller owns intent (charge state, speed, and committed
            // overshoot waypoint) while SmartFighter4 owns physical traversal. Attacks still run — the
            // rider swings and casts from the saddle.
            if (IsMounted)
            {
                // SmartFighter4 owns target acquisition for the normal path (it calls TargetClosest
                // internally). This branch skips SF4 entirely, so without this the NPC never picks a
                // player, NPC.target stays invalid, and the controller idles forever. faceTarget is
                // false because the charge locks its own facing and must not be overridden here.
                NPC.TargetClosest(false);
                Player mountTarget = Main.player[NPC.target];

                _mount ??= new EnemyMountController(MountConfig);

                // The navigator owns movement at ALL times while mounted; the controller is intent-only.
                // The mount is an extension of movement (a speed/aggression profile), not a second
                // movement system. Letting it self-drive during charges meant no pathfinding for those
                // states, which is what walked it into pits and made it hop mindlessly at walls.
                _mount.DriveMovement = false;

                // Keep riding while attacking, just slower — a mount that stops dead to cast looks broken
                // and removes all the pressure the charge built up.
                bool committedToAttack = Phase != AttackPhase.Idle
                    && Phase != AttackPhase.CasualStroll
                    && Phase != AttackPhase.ClosingDistance;

                if (committedToAttack)
                {
                    _mount.SpeedScale = MountedAttackSpeedScale;
                }
                else
                {
                    _mount.SpeedScale = 1f;
                }

                _mount.Tick(NPC, mountTarget);

                // Navigator runs EVERY tick, in every mount state — full A*, doors, ledges, ropes and
                // SF4's own stuck recovery. The mount chooses how fast and Dread Wraith's movement
                // override opts into its committed charge waypoint; wind-up remains a full stop.
                RunMovementAI(_mount.ModeSpeedMultiplier * _mount.SpeedScale);

                TickMountTrample(mountTarget);

                tsorcRevampAIs.TickQuickStep(NPC, gnpc);
                if (gnpc.DodgeTimer <= 0 && gnpc.QuickStepTimer <= 0 && gnpc.QuickStepRecoveryTimer <= 0)
                {
                    PuppetAttackAI();
                }
                UpdateAttackCommitFlags();
                TickWeaponAnim();
                return;
            }

            float speedMult = (Phase == AttackPhase.CasualStroll || Phase == AttackPhase.Healing)
                ? CasualStrollSpeedMult : 1f;
            if (Phase == AttackPhase.Idle && distToTarget > RunDistance)
                speedMult *= RunSpeedMult;
            if (Phase == AttackPhase.ClosingDistance)
                speedMult *= ClosingDistanceSpeedMult;

            // Capture direction before the movement AI might change it.
            int dirBefore = NPC.direction;

            RunMovementAI(speedMult);

            // Anti-bounce: if FighterAI just reversed direction but the hold timer is still
            // running (e.g. the player dodgerolled past), revert to the previous direction so
            // the puppet doesn't flip back and forth every tick.
            // spriteDirection is reverted too: SmartFighter4 writes BOTH fields when it re-faces,
            // so restoring only NPC.direction left the body drawn backwards while every hitbox,
            // muzzle offset and swing arc still used the old facing.
            if (NPC.direction != dirBefore && _directionHoldTicks > 0)
            {
                NPC.direction = dirBefore;
                NPC.spriteDirection = dirBefore;
            }
            else if (NPC.direction != dirBefore)
            {
                _directionHoldTicks = 30; // lock new direction for ~½ s before allowing another flip
            }

            // ── Flee-to-heal movement override ────────────────────────────────────
            // FighterAI always moves TOWARD the player.  When fleeing we invert velocity.X
            // so the puppet sprints away instead.  Y velocity (jumping) from FighterAI is
            // preserved so it can still clear obstacles while fleeing.
            if (Phase == AttackPhase.FleeToHeal)
            {
                Player fleeFrom      = Main.player[NPC.target];
                float  awayDir       = Math.Sign(NPC.Center.X - fleeFrom.Center.X);
                if (awayDir == 0)
                {
                    awayDir = -NPC.direction;
                }
                NPC.velocity.X       = (float)awayDir * TopSpeed * 2.0f;
                NPC.direction        = (int)awayDir;
                _directionHoldTicks  = 5; // allow quick correction while fleeing
            }

            // ── Secondary-ranged backhop momentum ─────────────────────────────────
            // Re-assert the hop-away velocity for a short window so the SF4 mover's pursuit (which ran
            // just above) doesn't cancel it on the very next tick.  Decays linearly to 0.
            if (_backhopTicks > 0)
            {
                NPC.velocity.X = _backhopDir * SecondaryRangedBackhopSpeed * (_backhopTicks / (float)BackhopMomentumTicks);
                _backhopTicks--;
            }

            // Rope climbing, X-centering, stuck detection, and teleport recovery are all owned by
            // SmartFighter4 (RunMovementAI).  The old FighterAI-era rope-climb and wall-blocked
            // overrides that used to live here fought the navigator and have been removed.

            // Advance a quick-step (on-hit or preemptive) AFTER the mover so it overrides pursuit velocity.
            tsorcRevampAIs.TickQuickStep(NPC, gnpc);

            // ── Proactive evasion (neutral, off cooldown, not mid-backhop/quick-step) ─────────────
            // Both reactions roll (PreemptiveQuickStepChance, Agility): server only. DodgeTimer reaches clients
            // through GlobalNPC's sync.
            if (!IsMultiplayerClient
                && gnpc.DodgeTimer <= 0 && gnpc.DodgeCooldown <= 0 && _backhopTicks <= 0
                && gnpc.QuickStepTimer <= 0 && gnpc.QuickStepRecoveryTimer <= 0
                && (Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll))
            {
                // Preemptive quick-step THROUGH a player who's mid-swing in melee range.
                if (PreemptiveQuickStepChance > 0)
                    TryPreemptiveQuickStep(gnpc, target, distToTarget);

                // Jump / i-frame roll at incoming RANGED shots (gated >160px so melee swing hitboxes —
                // also "friendly projectiles" — don't trigger it; the legacy BasicAI scan uses the same gate).
                if (EvadesProjectiles && gnpc.DodgeTimer <= 0 && gnpc.QuickStepTimer <= 0 && distToTarget > 160f)
                    TryEvadeIncomingProjectile(gnpc, target);
            }

            // Reactive shield: raise/plant the guard after movement so it overrides pursuit velocity.
            UpdateShield();

            // While the guard is up the puppet commits to it — no attack starts until it drops
            // (the minimum hold), so the player gets a real block/backstab window.

            // ── Dodge-roll movement guard ──────────────────────────────────────────
            // When the GlobalNPC dodge system fires it sets velocity.X = 5 * direction and
            // grants invulnerability for DodgeTimer ticks.  If we then run PuppetAttackAI
            // it would call SlowDown() or set a stab-lunge velocity, overriding the dash.
            // Instead we skip attack AI entirely for those ticks so the dodge movement lands.
            // Also skip while the shield guard is committed so it holds its minimum window.
            // AttackOwnsDodgeIFrames is the exception: those phases set DodgeTimer THEMSELVES, so
            // honouring it here froze the very phase that armed it (see the property's remarks).
            bool dodgeOwnsMovement = gnpc.DodgeTimer > 0 && !AttackOwnsDodgeIFrames;

            if (!dodgeOwnsMovement && gnpc.QuickStepTimer <= 0 && gnpc.QuickStepRecoveryTimer <= 0 && !_shielding)
            {
                PuppetAttackAI();
            }

            UpdateAttackCommitFlags();
            TickWeaponAnim();
        }

        public override void PostAI()
        {
            // Server: flush a snapshot on the tick a phase, flight mode or mount mode changes. A direct SyncNPC
            // skips vanilla's netSpam throttle, so clients reconcile within network latency instead of up to
            // 30 ticks late. PostAI runs after the subclass AI, so the snapshot includes this tick's decisions.
            if (Main.netMode == NetmodeID.Server)
            {
                int flightSequence = 0;
                if (_flight != null)
                {
                    flightSequence = _flight.ModeSequence;
                }

                int mountSequence = 0;
                if (_mount != null)
                {
                    mountSequence = _mount.ModeSequence;
                }

                bool phaseChanged = _phaseSequence != _lastSentPhaseSequence;
                bool flightChanged = flightSequence != _lastSentFlightSequence;
                bool mountChanged = mountSequence != _lastSentMountSequence;
                if (phaseChanged || flightChanged || mountChanged || _snapshotRequested)
                {
                    _lastSentPhaseSequence = _phaseSequence;
                    _lastSentFlightSequence = flightSequence;
                    _lastSentMountSequence = mountSequence;
                    _snapshotRequested = false;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                }
            }

            // Client: a snapshot that waited on this client's own transition (see PredictionDeferWindowTicks). This tick's
            // AI has run, so either the local transition happened or the client is holding for the server.
            if (IsMultiplayerClient && _deferredSnapshotState != null)
            {
                _deferredSnapshotTicksWaited++;
                bool predictionCaughtUp = _phaseSequence >= _deferredSnapshotSequence;
                bool waitedLongEnough = _deferredSnapshotTicksWaited >= _deferredSnapshotMaxWait;
                if (predictionCaughtUp || waitedLongEnough)
                {
                    byte[] deferredState = _deferredSnapshotState;
                    _deferredSnapshotState = null;
                    int timerNow = Math.Max(0, _deferredSnapshotTimer - _deferredSnapshotTicksWaited);
                    ReconcilePhaseSnapshot(_deferredSnapshotSequence, _deferredSnapshotPhase, timerNow,
                        _deferredSnapshotComboIndex, _deferredSnapshotStepIndex, deferredState,
                        runtimeActive: false, allowDefer: false);
                }
            }

            if (Main.dedServ)
                return;

            if (!SwingDebugLog)
                return;

            if (!IsTelemetryAttackPhase)
            {
                PuppetAttackTelemetry.End(NPC.whoAmI, $"phase:{Phase}");
                return;
            }

            RecordAttackTelemetryAI();
        }

        private void RecordAttackTelemetryAI()
        {
            Player target = NPC.HasValidTarget ? Main.player[NPC.target] : null;
            Vector2 targetCenter = target?.Center ?? NPC.Center;
            Vector2 targetVelocity = target?.velocity ?? Vector2.Zero;
            bool hasLineOfSight = target != null && Collision.CanHit(
                NPC.position, NPC.width, NPC.height, target.position, target.width, target.height);

            string navAction = "", navReason = "", navPlan = "";
            SmartFighter4AI.TryGetTelemetryState(NPC, out navAction, out navReason, out navPlan);

            Projectile thrown = null;
            if (_thrownComboWeaponIndex >= 0 || _thrownWeaponWasSeen
                || TelemetryMotionName == ComboMotion.ThrownWeaponRetrieve.ToString())
            {
                thrown = FindThrownComboWeapon();
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool hitWindowOpen = (_attackRuntimeV2.Active && _attackRuntimeV2.HitWindowOpen) || _bladeArmed;
            bool bladeOverlapsTarget = false;
            if (target != null && hitWindowOpen && _activeBladeReach > 0f)
            {
                Vector2 bladeOrigin = GetHandPosition();
                Vector2 bladeTip = bladeOrigin + GetWeaponWorldDirection() * _activeBladeReach;
                bladeOverlapsTarget = MeleeBladeCollision.SegmentIntersectsRect(
                    bladeOrigin, bladeTip, FrontHandWeapon.BladeWidth, target.getRect());
            }

            var sample = new PuppetAttackAiSample
            {
                NpcId = NPC.whoAmI,
                NpcType = NPC.type,
                NpcName = NPC.TypeName,
                NpcContentName = NPC.ModNPC?.FullName ?? $"Terraria/{NPC.type}",
                AttackName = TelemetryAttackName,
                Phase = Phase.ToString(),
                PhaseTimer = PhaseTimer,
                Motion = TelemetryMotionName,
                ComboStep = TelemetryComboStep,
                ComboStepCount = TelemetryComboStepCount,
                Direction = NPC.direction,
                SpriteDirection = NPC.spriteDirection,
                LockedDirection = _comboLockedDir,
                TargetPlayerId = target != null ? NPC.target : -1,
                TargetSide = target != null ? Math.Sign(target.Center.X - NPC.Center.X) : 0,
                NpcCenter = NPC.Center,
                NpcVelocity = NPC.velocity,
                TargetCenter = targetCenter,
                TargetVelocity = targetVelocity,
                HasLineOfSight = hasLineOfSight,
                RawWeaponRotationDeg = MathHelper.ToDegrees(_weaponRotation),
                DrawWeaponRotationDeg = MathHelper.ToDegrees(GetMeleeDrawRotation()),
                CompositeArmRotationDeg = CompositeArmActive
                    ? MathHelper.ToDegrees(CompositeArmRotation)
                    : float.NaN,
                AimCorrectionDeg = MathHelper.ToDegrees(_attackRuntimeV2.Active
                    ? _attackRuntimeV2.AimCorrection
                    : _comboAimBias),
                AimLocked = _attackRuntimeV2.Active && _attackRuntimeV2.AimLocked,
                CompositeStretch = CompositeArmStretch.ToString(),
                CompositeArmActive = CompositeArmActive,
                WeaponVisible = _weaponVisible && _heldItemType > 0,
                HeldItemType = _heldItemType,
                BladeArmed = _bladeArmed,
                BladeReach = _activeBladeReach,
                HitWindowOpen = hitWindowOpen,
                BladeOverlapsTarget = bladeOverlapsTarget,
                HitConnected = _lastAttackHitConnected,
                AttackCommitted = globalNPC.AttackCommitted,
                AttackTelegraphing = globalNPC.AttackTelegraphing,
                NavAction = navAction,
                NavReason = navReason,
                NavPlan = navPlan,
                ThrownWeaponPresent = thrown != null,
                ThrownWeaponEmbedded = thrown?.ai[0] == 1f,
                ThrownWeaponStage = _thrownWeaponStage,
                ThrownWeaponCenter = thrown?.Center ?? Vector2.Zero,
                ThrownWeaponVelocity = thrown?.velocity ?? Vector2.Zero,
                TargetDistance = target != null ? NPC.Distance(target.Center) : 0f,
                SwingArmEnabled = CompositeArmSwingMasterEnable,
                BladeFlipEnabled = BladeFlipMasterEnable,
                AimSwingEnabled = AimSwingMasterEnable,
                ArmRotationOffset = CompositeArmRotationOffset,
            };
            PuppetAttackTelemetry.RecordAI(sample);
        }

        private string TelemetryAttackName
        {
            get
            {
                if (_activeMeleeComboIndex >= 0 && !string.IsNullOrEmpty(_activeMeleeCombo.Name))
                    return _activeMeleeCombo.Name;
                if (!string.IsNullOrEmpty(DebugAttackLabel))
                    return DebugAttackLabel;
                if (_usingSecondaryRanged)
                    return $"Secondary Ranged ({_activeRangedStyle})";
                if (_activeRangedItemType > 0
                    && (Phase == AttackPhase.RangedTelegraph
                        || Phase == AttackPhase.RangedAttack
                        || Phase == AttackPhase.RangedRecovery))
                {
                    return $"Primary Ranged ({_activeRangedStyle})";
                }
                return Phase.ToString();
            }
        }

        private string TelemetryMotionName =>
            IsMeleeComboPhase && _activeMeleeComboIndex >= 0
            && _activeMeleeCombo.Steps != null
            && _meleeComboStepIndex >= 0 && _meleeComboStepIndex < _activeMeleeCombo.Steps.Length
                ? _activeMeleeCombo.Steps[_meleeComboStepIndex].Motion.ToString()
                : "";

        private int TelemetryComboStep => IsMeleeComboPhase && _activeMeleeComboIndex >= 0
            ? _meleeComboStepIndex : -1;

        private int TelemetryComboStepCount => IsMeleeComboPhase && _activeMeleeComboIndex >= 0
            ? _activeMeleeCombo.Steps?.Length ?? 0 : 0;

        /// <summary>
        /// Returns true when the puppet should interrupt whatever it is doing to flee and heal.
        /// Checked at the top of PuppetAttackAI every tick.
        /// </summary>
        private bool ShouldHeal()
        {
            if (_estusCharges <= 0 || _healCooldown > 0)
            {
                return false;
            }
            // Don't interrupt an already-running heal sequence.
            if (Phase == AttackPhase.FleeToHeal || Phase == AttackPhase.Healing)
            {
                return false;
            }

            float hp = (float)NPC.life / NPC.lifeMax;

            if (!_halfHpHealed && hp <= FirstHealThreshold)
            {
                return true;
            }
            if (!_quarterHpHealed && hp <= SecondHealThreshold)
            {
                return true;
            }
            // Threshold 3: burst damage — took a lot of damage in a short window.
            if (_recentDamage >= RecentDamageThreshold)
            {
                return true;
            }

            return false;
        }

        private void CompleteEstusHeal()
        {
            if (_estusCharges <= 0)
            {
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int healAmount = (int)(NPC.lifeMax * EstusHealFraction);
                int lifeBefore = NPC.life;
                int configuredCeiling = (int)(NPC.lifeMax * MathHelper.Clamp(HealLifeCapFraction, 0f, 1f));
                int healCeiling = Math.Max(lifeBefore, configuredCeiling);
                NPC.life = Math.Min(healCeiling, NPC.life + healAmount);
                int restored = NPC.life - lifeBefore;
                if (restored > 0)
                    NPC.HealEffect(restored);
            }

            _estusCharges--;
            _healCooldown = HealCooldownTicks;
            _recentDamage = 0f;

            float hp = (float)NPC.life / NPC.lifeMax;
            if (hp > FirstHealThreshold)
            {
                _halfHpHealed = true;
            }
            if (hp > SecondHealThreshold)
            {
                _quarterHpHealed = true;
            }
            NPC.netUpdate = true;
        }

        /// <summary>Virtual so a subclass with its own set-piece state (a custom-phase stage machine, a
        /// pending spell, in-flight channel dust) can clear it too. Always call base.</summary>
        public virtual void OnStagger(NPC npc)
        {
            bool cancelledHeal = Phase == AttackPhase.FleeToHeal || Phase == AttackPhase.Healing;
            if (cancelledHeal)
            {
                // The charge is not consumed because completion is the transaction point, but a
                // cooldown prevents the puppet from restarting the same drink during the stun.
                _healCooldown = Math.Max(_healCooldown, HealCooldownTicks);
            }

            if (_thrownComboWeaponIndex >= 0 || _thrownWeaponWasSeen)
                FinishThrownWeaponRetrieve();
            CancelAttackRuntimeV2(clearCombo: true);
            _bladeArmed = false;
            _jumpSlashLaunched = false;
            _comboLeapLaunched = false;
            _risingUppercutFallHoldTimer = 0;
            _weaponVisible = false;
            EnterPhase(AttackPhase.Idle, 0);

            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            _shielding = false;
            _shieldWasGuarding = false;
            _shieldMeleeExtended = false;
            _shieldGuardCooldown = Math.Max(_shieldGuardCooldown, ShieldGuardCooldownTicks);
            globalNPC.ReactiveBlockTimer = 0;
            globalNPC.ShieldGuarding = false;
            globalNPC.AttackCommitted = false;
            globalNPC.AttackTelegraphing = false;
            npc.netUpdate = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            writer.Write((short)Math.Clamp(_shieldGuardCooldown, 0, short.MaxValue));
            // ReactiveBlockTimer is NOT written here: tsorcRevampGlobalNPC syncs it for every shield user, puppet or
            // not. Only the puppet-specific guard pose/cooldown belong in this packet.
            writer.Write(_shielding);
            writer.Write(_mountSpawned);
            writer.Write(_attackRuntimeV2.Active);

            // ── Phase header (see _phaseSequence) ──
            writer.Write(_phaseSequence);
            writer.Write((byte)Phase);
            writer.Write((short)Math.Clamp(PhaseTimer, short.MinValue, short.MaxValue));
            writer.Write((short)_activeMeleeComboIndex);
            writer.Write((byte)Math.Clamp(_meleeComboStepIndex, 0, byte.MaxValue));

            // Flight and mount controllers reconcile on their own mode sequence.
            bool hasFlight = _flight != null;
            writer.Write(hasFlight);
            if (hasFlight)
            {
                _flight.WriteNetworkState(writer);
            }

            bool hasMount = _mount != null;
            writer.Write(hasMount);
            if (hasMount)
            {
                _mount.WriteNetworkState(writer);
            }

            // ── Decision state ──
            // Everything the server rolled, aimed or edited for the current phase. Length-prefixed so a client
            // can skip it whole when the header is older than its own prediction.
            MemoryStream stateStream = new MemoryStream();
            BinaryWriter state = new BinaryWriter(stateStream);

            // The per-activation combo copy, as CustomizeMeleeCombo / ModifyNextMeleeComboStep left it. First,
            // because the client needs it before it can adopt a combo phase.
            bool sendComboSteps = _activeMeleeComboIndex >= 0
                && !_attackRuntimeV2.Active
                && _activeMeleeCombo.Steps != null;
            state.Write(sendComboSteps);
            if (sendComboSteps)
            {
                state.Write((short)_activeMeleeCombo.RecoveryTicks);
                state.Write(_activeMeleeCombo.HyperArmor);
                state.Write(_activeMeleeCombo.MoveBrake);
                state.Write((byte)_activeMeleeCombo.Steps.Length);
                for (int i = 0; i < _activeMeleeCombo.Steps.Length; i++)
                {
                    MeleeComboStep step = _activeMeleeCombo.Steps[i];
                    state.Write((byte)step.Motion);
                    state.Write((short)step.TelegraphTicks);
                    state.Write((short)step.AttackTicks);
                    state.Write((short)step.PostStepPause);
                    state.Write(step.DamageMult);
                    state.Write(step.ReachMult);
                    state.Write(step.ForwardPushMult);
                    state.Write(step.SwingSpeedMult);
                    state.Write((byte)step.Ease);
                    state.Write((short)step.EaseInTicks);
                    state.Write((short)step.EaseOutTicks);
                    state.Write(step.EaseOutDecay);
                    state.Write(step.LeapStrikeRange);
                    state.Write(step.HitWindowEnd);
                    state.Write(step.LeapHeightMult);
                    state.Write(step.LeapForwardSpeedMult);
                    state.Write(step.LeapDescentGravityMult);
                    state.Write(step.LeapApexRetargetStrength);
                }
            }

            state.Write((sbyte)_attackFacingDir);
            state.Write(_lastAttackHitConnected);
            state.Write(_currentComboStepHitConnected);

            // Combo motion: locked facing, leap launch and aim-adaptive arc.
            state.Write((sbyte)_comboLockedDir);
            state.Write(_comboLeapVx);
            state.Write(_comboLeapLaunched);
            state.Write((short)_activeComboStepTotalTicks);
            state.Write(_comboAimBias);
            state.Write(_comboSwingFlipped);
            state.Write(_apexDiveStrikeStarted);
            state.Write(_leapStrikeStarted);
            state.Write(_leapApexRetargeted);
            state.Write((short)_comboLeapAscentTicks);
            state.Write(_leapLaunchedFromAir);
            state.Write(_leapSlamAimOffset);

            // Bespoke attack phases: the directions, targets and counters their entry code rolled or aimed.
            int pierceTargetIndex = -1;
            if (_pierceTarget != null)
            {
                pierceTargetIndex = _pierceTarget.whoAmI;
            }
            state.Write(_pierceIsStab);
            state.Write(_pierceHitConnected);
            state.Write(_pierceContactDodged);
            state.Write((byte)Math.Clamp(_pierceRepeatsUsed, 0, byte.MaxValue));
            state.Write((short)_pierceTelegraphTotalTicks);
            state.Write((sbyte)_pierceDir);
            state.WriteVector2(_pierceAnchorPos);
            state.Write((sbyte)pierceTargetIndex);
            state.Write((sbyte)_jumpSlashDir);
            state.Write(_jumpSlashLaunched);
            state.Write(_jumpSlashFlightSpeed);
            state.Write((sbyte)_flipSlashDir);
            state.Write(_flipSlashLaunchBottomY);
            state.Write(_flipSlashSolvedSpinSpeed);
            state.Write(_flipSlashStrikeStartRotation);
            state.Write((byte)Math.Clamp(_abyssSlashIndex, 0, byte.MaxValue));
            state.Write((byte)Math.Clamp(_abyssShardIndex, 0, byte.MaxValue));
            state.Write((byte)Math.Clamp(_spiralFanIndex, 0, byte.MaxValue));
            state.Write((sbyte)_homingVolleyDir);
            state.Write(_homingVolleyFired);
            state.Write(_boomerangFired);
            state.Write((sbyte)_swordLaunchDir);
            state.Write((byte)_swordLaunchNextPhase);
            state.Write((short)_swordLaunchNextTicks);
            state.Write((sbyte)_fireVolleyDir);
            state.Write(_fireVolleyArcVx);
            state.Write(_fireVolleyArcFired);
            state.Write((sbyte)_stabLungeDir);
            state.Write((sbyte)_spearLungeDir);
            state.Write((sbyte)_backhopDir);
            state.Write((byte)Math.Clamp(_backhopTicks, 0, byte.MaxValue));
            state.Write((byte)Math.Clamp(_cursedKnivesVolleysLeft, 0, byte.MaxValue));
            state.Write((short)_cursedKnivesGap);
            state.Write((short)_magicAttackTicksOverride);
            state.Write((short)_customPoseWeapon);
            state.Write(_customSwingPose);

            // Ranged burst: SetupRangedBurst's rolled stance, shot count and pattern.
            state.Write(_usingSecondaryRanged);
            state.Write((byte)_activeRangedStyle);
            state.Write((short)_activeRangedItemType);
            state.Write(_activeRangedFlashColor.PackedValue);
            state.Write((short)_activeRangedTelegraphTicks);
            state.Write((short)_activeRangedAttackTicks);
            state.Write((short)_activeRangedRecoveryTicks);
            state.Write(_standingShot);
            state.Write((byte)Math.Clamp(_rangedShotsRemaining, 0, byte.MaxValue));
            state.Write((sbyte)_activeBurstPatternIndex);
            state.Write((byte)Math.Clamp(_interShotPauseIndex, 0, byte.MaxValue));

            state.Flush();
            byte[] stateBytes = stateStream.ToArray();
            writer.Write((ushort)stateBytes.Length);
            writer.Write(stateBytes);

            if (!_attackRuntimeV2.Active)
                return;

            writer.Write((byte)_attackRuntimeV2.Stage);
            writer.Write((short)_attackRuntimeV2.StageTick);
            writer.Write(_attackRuntimeV2.AimCorrection);
            writer.Write(_attackRuntimeV2.AimLocked);
            writer.Write((sbyte)_attackRuntimeV2.LockedFacing);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _shieldGuardCooldown = reader.ReadInt16();
            bool shieldActive = reader.ReadBoolean();
            _mountSpawned = reader.ReadBoolean();
            bool runtimeActive = reader.ReadBoolean();

            int receivedSequence = reader.ReadInt32();
            AttackPhase receivedPhase = (AttackPhase)reader.ReadByte();
            int receivedPhaseTimer = reader.ReadInt16();
            int receivedComboIndex = reader.ReadInt16();
            int receivedStepIndex = reader.ReadByte();

            bool hasFlight = reader.ReadBoolean();
            if (hasFlight)
            {
                _flight ??= new EnemyFlightController(FlightConfig);
                _flight.ReadNetworkState(reader);
            }

            bool hasMount = reader.ReadBoolean();
            if (hasMount)
            {
                _mount ??= new EnemyMountController(MountConfig);
                _mount.ReadNetworkState(reader);
            }
            else if (!_mountSpawned)
            {
                // The server dropped its controller when the mount broke. Drop ours too, or mount-only effects
                // gated on MountAI keep firing on this client (see DestroyMount).
                _mount = null;
            }

            int stateLength = reader.ReadUInt16();
            byte[] stateBytes = reader.ReadBytes(stateLength);

            // Shield flags. The ShieldGuard phase itself arrives with the phase snapshot below, and the guard's
            // remaining hold (ReactiveBlockTimer) lands a moment later: tModLoader reads this ModNPC block first and
            // the GlobalNPC blocks straight after (NPCLoader.ReceiveExtraAI), so nothing here may depend on it.
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (runtimeActive)
            {
                shieldActive = false;
            }
            if (shieldActive && !_shielding)
            {
                _shieldLockedDir = NPC.direction;
            }
            globalNPC.ShieldGuarding = shieldActive;
            _shielding = shieldActive;
            _shieldWasGuarding = shieldActive;

            bool hadRuntime = _attackRuntimeV2.Active;
            if (hadRuntime && !runtimeActive)
            {
                CancelAttackRuntimeV2(clearCombo: true);
                _weaponVisible = false;
            }

            // A newer snapshot supersedes one still waiting on this client's own transition.
            _deferredSnapshotState = null;
            ReconcilePhaseSnapshot(receivedSequence, receivedPhase, receivedPhaseTimer, receivedComboIndex,
                receivedStepIndex, stateBytes, runtimeActive, allowDefer: true);

            if (!runtimeActive)
            {
                return;
            }

            PuppetAttackStage stage = (PuppetAttackStage)reader.ReadByte();
            int stageTick = reader.ReadInt16();
            float aimCorrection = reader.ReadSingle();
            bool aimLocked = reader.ReadBoolean();
            int lockedFacing = reader.ReadSByte();

            EnsureMeleeComboPool();
            int comboIndex = receivedComboIndex;
            if (_meleeComboPool == null || comboIndex < 0 || comboIndex >= _meleeComboPool.Length
                || _meleeComboPool[comboIndex].RuntimeV2Clip == null)
            {
                CancelAttackRuntimeV2(clearCombo: true);
                return;
            }

            bool wasActive = _attackRuntimeV2.Active;
            // OnMeleeComboStarted fires once per strike, not on every snapshot of it (it resets subclass state).
            bool newRuntimeCombo = !wasActive || comboIndex != _activeMeleeComboIndex;
            _activeMeleeComboIndex = comboIndex;
            _activeMeleeCombo = _meleeComboPool[comboIndex];
            if (newRuntimeCombo)
            {
                OnMeleeComboStarted(_activeMeleeCombo);
            }
            _meleeComboStepIndex = 0;
            _comboLockedDir = lockedFacing < 0 ? -1 : 1;
            _attackRuntimeV2.LoadNetworkState(
                _activeMeleeCombo.RuntimeV2Clip,
                stage,
                stageTick,
                aimCorrection,
                aimLocked,
                _comboLockedDir);

            SetDisplayWeapon(FrontHandWeaponType, swing: stage == PuppetAttackStage.Active);
            _weaponAnimMax = _activeMeleeCombo.RuntimeV2Clip.ActiveTicks;
            _weaponAnim = stage == PuppetAttackStage.Active ? _attackRuntimeV2.TicksRemaining : _weaponAnimMax;
            _weaponRotation = _attackRuntimeV2.SampleRotation();
            _runtimeHasPreviousBladeSample = false;
            _weaponVisible = true;
            Phase = stage switch
            {
                PuppetAttackStage.Windup => AttackPhase.MeleeComboTelegraph,
                PuppetAttackStage.Active => AttackPhase.MeleeComboAttack,
                _ => AttackPhase.MeleeComboRecovery,
            };
            PhaseTimer = _attackRuntimeV2.TicksRemaining;
            if (!wasActive && stage == PuppetAttackStage.Windup)
                _flashFired = false;
            if (!wasActive && stage == PuppetAttackStage.Active)
                PlayMeleeSwingSound();
        }

        /// <summary>Client side of the phase snapshot (see _phaseSequence): confirms, adopts or skips the server's phase and
        /// applies its decision-state blob. allowDefer lets a snapshot one transition ahead wait for this client's own
        /// imminent transition; PostAI re-runs it with allowDefer false.</summary>
        private void ReconcilePhaseSnapshot(int receivedSequence, AttackPhase receivedPhase, int receivedPhaseTimer,
            int receivedComboIndex, int receivedStepIndex, byte[] stateBytes, bool runtimeActive, bool allowDefer)
        {
            // The same phase (and combo step) the client already predicted keeps the client's own timer. A snapshot
            // older than that prediction is latency and is skipped, unless the client idles on a different phase or
            // the prediction has gone unconfirmed long enough that the server must have held.
            bool receivedComboPhase = receivedPhase == AttackPhase.MeleeComboTelegraph
                || receivedPhase == AttackPhase.MeleeComboAttack
                || receivedPhase == AttackPhase.MeleeComboPause
                || receivedPhase == AttackPhase.MeleeComboRecovery;
            bool sameComboPosition = receivedComboIndex == _activeMeleeComboIndex
                && receivedStepIndex == _meleeComboStepIndex;
            bool samePhase = receivedPhase == Phase && (!receivedComboPhase || sameComboPosition);
            bool clientNeutral = Phase == AttackPhase.Idle
                || Phase == AttackPhase.CasualStroll
                || Phase == AttackPhase.ClosingDistance;

            bool olderThanPrediction = receivedSequence < _phaseSequence;
            bool divergedInNeutral = clientNeutral && !samePhase;
            uint ticksSincePrediction = Main.GameUpdateCount - _predictionStartTick;
            bool predictionUnconfirmed = ticksSincePrediction > StalePhaseSnapshotToleranceTicks;
            bool snapshotIsStale = olderThanPrediction && !divergedInNeutral && !predictionUnconfirmed;

            // A V2 strike is reconciled by its own stage data in ReceiveExtraAI, so its snapshot always applies.
            if (snapshotIsStale && !runtimeActive)
            {
                return;
            }

            // One transition ahead with this client's own timer about to run out: let the local transition happen first so
            // its expiry hooks fire. PostAI re-runs this afterwards and normally finds the two phases equal.
            bool oneTransitionAhead = receivedSequence == _phaseSequence + 1;
            bool localTransitionImminent = PhaseTimer >= 1 && PhaseTimer <= PredictionDeferWindowTicks;
            bool deferToPrediction = allowDefer
                && !runtimeActive
                && !samePhase
                && !clientNeutral
                && oneTransitionAhead
                && localTransitionImminent;
            if (deferToPrediction)
            {
                _deferredSnapshotState = stateBytes;
                _deferredSnapshotSequence = receivedSequence;
                _deferredSnapshotPhase = receivedPhase;
                _deferredSnapshotTimer = receivedPhaseTimer;
                _deferredSnapshotComboIndex = receivedComboIndex;
                _deferredSnapshotStepIndex = receivedStepIndex;
                _deferredSnapshotTicksWaited = 0;
                _deferredSnapshotMaxWait = PhaseTimer + 1;
                return;
            }

            BinaryReader state = new BinaryReader(new MemoryStream(stateBytes));

            bool hasComboSteps = state.ReadBoolean();
            MeleeComboStep[] receivedSteps = null;
            int receivedRecoveryTicks = 0;
            bool receivedHyperArmor = false;
            float receivedMoveBrake = 0f;
            if (hasComboSteps)
            {
                receivedRecoveryTicks = state.ReadInt16();
                receivedHyperArmor = state.ReadBoolean();
                receivedMoveBrake = state.ReadSingle();
                int stepCount = state.ReadByte();
                receivedSteps = new MeleeComboStep[stepCount];
                for (int i = 0; i < stepCount; i++)
                {
                    MeleeComboStep step = new MeleeComboStep();
                    step.Motion = (ComboMotion)state.ReadByte();
                    step.TelegraphTicks = state.ReadInt16();
                    step.AttackTicks = state.ReadInt16();
                    step.PostStepPause = state.ReadInt16();
                    step.DamageMult = state.ReadSingle();
                    step.ReachMult = state.ReadSingle();
                    step.ForwardPushMult = state.ReadSingle();
                    step.SwingSpeedMult = state.ReadSingle();
                    step.Ease = (SwingEaseStyle)state.ReadByte();
                    step.EaseInTicks = state.ReadInt16();
                    step.EaseOutTicks = state.ReadInt16();
                    step.EaseOutDecay = state.ReadSingle();
                    step.LeapStrikeRange = state.ReadSingle();
                    step.HitWindowEnd = state.ReadSingle();
                    step.LeapHeightMult = state.ReadSingle();
                    step.LeapForwardSpeedMult = state.ReadSingle();
                    step.LeapDescentGravityMult = state.ReadSingle();
                    step.LeapApexRetargetStrength = state.ReadSingle();
                    receivedSteps[i] = step;
                }
            }

            if (!runtimeActive)
            {
                EnsureMeleeComboPool();
                bool comboIndexValid = _meleeComboPool != null
                    && receivedComboIndex >= 0
                    && receivedComboIndex < _meleeComboPool.Length;
                bool comboUsable = hasComboSteps
                    && comboIndexValid
                    && receivedStepIndex < receivedSteps.Length;

                // A combo phase without its step data can't be ticked (Steps[...] would throw): wait in Idle.
                if (receivedComboPhase && !comboUsable)
                {
                    receivedPhase = AttackPhase.Idle;
                    receivedPhaseTimer = 0;
                    samePhase = Phase == AttackPhase.Idle;
                }

                if (comboUsable)
                {
                    // Same index back-to-back is still a fresh activation when the telegraph restarts.
                    bool newCombo = receivedComboIndex != _activeMeleeComboIndex
                        || (receivedPhase == AttackPhase.MeleeComboTelegraph && Phase != AttackPhase.MeleeComboTelegraph);
                    if (newCombo)
                    {
                        _activeMeleeCombo = _meleeComboPool[receivedComboIndex];
                    }
                    _activeMeleeCombo.Steps = receivedSteps;
                    _activeMeleeCombo.RecoveryTicks = receivedRecoveryTicks;
                    _activeMeleeCombo.HyperArmor = receivedHyperArmor;
                    _activeMeleeCombo.MoveBrake = receivedMoveBrake;
                    _activeMeleeComboIndex = receivedComboIndex;
                    _meleeComboStepIndex = receivedStepIndex;
                    if (newCombo)
                    {
                        OnMeleeComboStarted(_activeMeleeCombo);
                    }
                }
                else
                {
                    _activeMeleeComboIndex = -1;
                }

                // Adopt a phase this client did not reach itself, with the server's full timer so tick hooks that key off
                // elapsed == 0 still fire. Its entry cues play at the start of the next AI tick (_adoptedPhaseEntryPending).
                if (!samePhase)
                {
                    EnterPhase(receivedPhase, 0);
                    PhaseTimer = receivedPhaseTimer;
                    _adoptedPhaseEntryPending = true;
                }
            }

            _attackFacingDir = state.ReadSByte();
            _lastAttackHitConnected = state.ReadBoolean();
            _currentComboStepHitConnected = state.ReadBoolean();

            _comboLockedDir = state.ReadSByte();
            _comboLeapVx = state.ReadSingle();
            _comboLeapLaunched = state.ReadBoolean();
            _activeComboStepTotalTicks = state.ReadInt16();
            _comboAimBias = state.ReadSingle();
            _comboSwingFlipped = state.ReadBoolean();
            _apexDiveStrikeStarted = state.ReadBoolean();
            _leapStrikeStarted = state.ReadBoolean();
            _leapApexRetargeted = state.ReadBoolean();
            _comboLeapAscentTicks = state.ReadInt16();
            _leapLaunchedFromAir = state.ReadBoolean();
            _leapSlamAimOffset = state.ReadSingle();

            _pierceIsStab = state.ReadBoolean();
            _pierceHitConnected = state.ReadBoolean();
            _pierceContactDodged = state.ReadBoolean();
            _pierceRepeatsUsed = state.ReadByte();
            _pierceTelegraphTotalTicks = state.ReadInt16();
            _pierceDir = state.ReadSByte();
            _pierceAnchorPos = state.ReadVector2();
            int pierceTargetIndex = state.ReadSByte();
            _pierceTarget = null;
            if (pierceTargetIndex >= 0 && pierceTargetIndex < Main.maxPlayers)
            {
                _pierceTarget = Main.player[pierceTargetIndex];
            }
            _jumpSlashDir = state.ReadSByte();
            _jumpSlashLaunched = state.ReadBoolean();
            _jumpSlashFlightSpeed = state.ReadSingle();
            _flipSlashDir = state.ReadSByte();
            _flipSlashLaunchBottomY = state.ReadSingle();
            _flipSlashSolvedSpinSpeed = state.ReadSingle();
            _flipSlashStrikeStartRotation = state.ReadSingle();
            _abyssSlashIndex = state.ReadByte();
            _abyssShardIndex = state.ReadByte();
            _spiralFanIndex = state.ReadByte();
            _homingVolleyDir = state.ReadSByte();
            _homingVolleyFired = state.ReadBoolean();
            _boomerangFired = state.ReadBoolean();
            _swordLaunchDir = state.ReadSByte();
            _swordLaunchNextPhase = (AttackPhase)state.ReadByte();
            _swordLaunchNextTicks = state.ReadInt16();
            _fireVolleyDir = state.ReadSByte();
            _fireVolleyArcVx = state.ReadSingle();
            _fireVolleyArcFired = state.ReadBoolean();
            _stabLungeDir = state.ReadSByte();
            _spearLungeDir = state.ReadSByte();
            _backhopDir = state.ReadSByte();
            _backhopTicks = state.ReadByte();
            _cursedKnivesVolleysLeft = state.ReadByte();
            _cursedKnivesGap = state.ReadInt16();
            _magicAttackTicksOverride = state.ReadInt16();
            _customPoseWeapon = state.ReadInt16();
            _customSwingPose = state.ReadBoolean();

            _usingSecondaryRanged = state.ReadBoolean();
            _activeRangedStyle = (RangedStyle)state.ReadByte();
            _activeRangedItemType = state.ReadInt16();
            _activeRangedFlashColor = new Color { PackedValue = state.ReadUInt32() };
            _activeRangedTelegraphTicks = state.ReadInt16();
            _activeRangedAttackTicks = state.ReadInt16();
            _activeRangedRecoveryTicks = state.ReadInt16();
            _standingShot = state.ReadBoolean();
            _rangedShotsRemaining = state.ReadByte();
            _activeBurstPatternIndex = state.ReadSByte();
            _interShotPauseIndex = state.ReadByte();

            // The inter-shot pause array is a row of the pattern table: rebuild it from the synced index.
            int[][] patternPool = PrimaryRangedBurstPatterns;
            if (_usingSecondaryRanged)
            {
                patternPool = SecondaryRangedBurstPatterns;
            }
            _interShotPauses = null;
            bool patternIndexValid = patternPool != null
                && _activeBurstPatternIndex >= 0
                && _activeBurstPatternIndex < patternPool.Length;
            if (patternIndexValid)
            {
                _interShotPauses = patternPool[_activeBurstPatternIndex];
            }

            _phaseSequence = receivedSequence;
            _lastAcceptedPhaseSequence = receivedSequence;
        }

        private void PuppetAttackAI()
        {
            Player target = Main.player[NPC.target];
            float  dist   = NPC.Distance(target.Center);
            bool   hasLOS = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            // ── Healing intercept (highest priority) ──────────────────────────────
            // Check before the main switch so any phase can be interrupted to flee and heal.
            // Both intercepts are decisions (HP thresholds, one-shot subclass state): server only. A client
            // follows the FleeToHeal / NovaCharge phase from the snapshot.
            if (!IsMultiplayerClient && ShouldHeal())
            {
                CancelAttackRuntimeV2(clearCombo: true);
                EnterPhase(AttackPhase.FleeToHeal, FleeToHealMaxTicks);
                return;
            }

            // ── Charge-up Nova intercept (health-threshold set-piece) ─────────────
            // Also checked before the main switch: a nova can interrupt any in-progress attack.
            if (!IsMultiplayerClient && CanNova && Phase != AttackPhase.NovaCharge && Phase != AttackPhase.NovaBlast
                && Phase != AttackPhase.NovaRecovery && ShouldTriggerNova())
            {
                CancelAttackRuntimeV2(clearCombo: true);
                EnterPhase(AttackPhase.NovaCharge, NovaChargeTicks);
                return;
            }

            // V2 owns the whole strike. The mirrored legacy phase is for rendering and shared
            // attack-commit flags only; its switch case must not advance another timer.
            if (_attackRuntimeV2.Active)
            {
                TickAttackRuntimeV2(target);
                return;
            }

            // Weapon only exists visually during telegraphs and attack frames.
            _weaponVisible = IsWeaponVisiblePhase;

            switch (Phase)
            {
                case AttackPhase.Idle:
                    if (_heldItemType <= 0)
                        SetDisplayWeapon(MeleeWeaponItemType >= 0 ? MeleeWeaponItemType : RangedWeaponItemType, swing: false);

                    // Neutral re-face. The navigator only turns the sprite when it walks, so a puppet standing
                    // in range after an attack could keep its back to a player who rolled through. Once the
                    // post-attack hold (_directionHoldTicks) runs out, face a nearby, same-level, visible player
                    // directly. The 8px dead zone stops flip-flopping while the player overlaps the puppet;
                    // skipped airborne (the flight controller owns facing) and while mounted.
                    {
                        float horizontalGap = target.Center.X - NPC.Center.X;
                        bool clearlyToOneSide = Math.Abs(horizontalGap) > NeutralRefaceDeadZone;
                        bool sameLevel = Math.Abs(target.Center.Y - NPC.Center.Y) < 48f;
                        bool nearby = dist <= NeutralRefaceRange;
                        bool airborne = _flight != null && _flight.IsAirborne;
                        bool canReface = _directionHoldTicks <= 0 && clearlyToOneSide && sameLevel && nearby
                            && hasLOS && !airborne && !IsMounted;

                        if (canReface)
                        {
                            int faceTarget = Math.Sign(horizontalGap);
                            NPC.direction = faceTarget;
                            NPC.spriteDirection = faceTarget;
                        }
                    }

                    // The movement controller has already advanced the puppet this tick. A subclass's own
                    // authored hold (see HoldAttackSelection) skips neutral selection entirely.
                    if (HoldAttackSelection)
                    {
                        break;
                    }

                    // Everything below picks the next attack (rolls, range bands, takeoff): server only. A
                    // client idles here until the snapshot brings the chosen phase.
                    if (IsMultiplayerClient)
                    {
                        break;
                    }

                    // Authored reactions such as "snare connected -> throw flask" take priority over
                    // neutral selection, but still require footing, line of sight, and a full wind-up.
                    if (TryStartQueuedRangedFollowUp())
                        break;

                    // ── Wings: airborne behavior + takeoff triggers ───────────────
                    // Checked BEFORE the LOS gate below: a flying puppet must still be able to
                    // reposition (dive/strafe variety roll) and eventually land even when a single-ray
                    // LOS check is briefly blocked by minor terrain (a corner, an overhang).  Without
                    // this, losing LOS while airborne parks the puppet in Hover — doing nothing but
                    // the idle bob — until the flight time budget forces a landing, and it can read as
                    // a total freeze.  The individual attack triggers inside this block (breath/knives/
                    // ranged) already re-check hasLOS themselves before firing.
                    if (HasWings && _flight != null)
                    {
                        if (_flight.IsAirborne)
                        {
                            // Select actions from the actual loadout while airborne. Raised or
                            // flying targets favor ranged fire; melee dives work in any direction.
                            bool targetAirborne = Math.Abs(target.velocity.Y) > 0.1f
                                || target.wingTime > 0f || target.mount.Active;
                            bool targetAboveOrLevel = target.Center.Y <= NPC.Center.Y + AerialRangedVerticalBand;
                            float aerialDistance = NPC.Distance(target.Center);
                            bool primaryAerialRanged = RangedWeaponItemType >= 0
                                && aerialDistance <= RangedRange && aerialDistance >= MinRangedRange
                                && _rangedCooldown <= 0;
                            bool secondaryAerialRanged = SecondaryRangedWeaponItemType >= 0
                                && SecondaryRangedAvailable
                                && aerialDistance <= SecondaryRangedRange
                                && aerialDistance >= SecondaryRangedMinRange
                                && _secondaryRangedCooldown <= 0;
                            bool aerialMagic = MagicWeaponItemType >= 0
                                && aerialDistance <= MagicRange
                                && aerialDistance >= MinMagicRange
                                && _magicCooldown <= 0;

                            if (_flight.Mode == FlightMode.Hover && hasLOS
                                && (targetAirborne || targetAboveOrLevel)
                                && (primaryAerialRanged || secondaryAerialRanged || aerialMagic)
                                && Main.rand.Next(75) == 0)
                            {
                                bool useMagic = aerialMagic && (!(primaryAerialRanged || secondaryAerialRanged)
                                    || Main.rand.Next(100) < MagicPreferenceChance);
                                if (useMagic)
                                {
                                    OnMagicTelegraphStarting();
                                    EnterPhase(AttackPhase.MagicTelegraph, MagicTelegraphTicks);
                                }
                                else
                                {
                                    bool useSecondary = secondaryAerialRanged
                                        && (!primaryAerialRanged || Main.rand.Next(100) < SecondaryRangedChance);
                                    // -1 (roll a pattern) instead of the forced single shot when the
                                    // puppet opts in - see AllowRangedPatternsAirborne's doc comment.
                                    int aerialShots = AllowRangedPatternsAirborne ? -1 : 1;
                                    SetupRangedBurst(useSecondary, shotsOverride: aerialShots, forceStanding: true);
                                    EnterPhase(AttackPhase.RangedTelegraph, _activeRangedTelegraphTicks);
                                }
                            }
                            else if (_flight.Mode == FlightMode.Hover && hasLOS
                                && CanUseAerialMelee && MeleeWeaponItemType >= 0
                                && _aerialDiveCooldown <= 0 && aerialDistance <= AerialMeleeRange
                                && Main.rand.Next(70) == 0)
                            {
                                // A puppet with AirborneStartOnly combos uses those instead of the
                                // flight controller's generic dive: a real telegraphed combo whose
                                // LeapSlam dives at the target and swings on arrival (BeginLeapAttack).
                                EnsureMeleeComboPool();
                                bool hasAirborneCombos = false;

                                if (_meleeComboPool != null)
                                {
                                    for (int i = 0; i < _meleeComboPool.Length; i++)
                                    {
                                        if (_meleeComboPool[i].AirborneStartOnly)
                                        {
                                            hasAirborneCombos = true;
                                            break;
                                        }
                                    }
                                }

                                if (hasAirborneCombos)
                                {
                                    if (TryStartMeleeCombo(aerialDistance, airborneStart: true))
                                    {
                                        _aerialDiveCooldown = AerialDiveCooldownTicks;
                                    }
                                }
                                else
                                {
                                    Vector2 leadTarget = target.Center + target.velocity * AerialDiveLeadTicks;
                                    // Lets a subclass redirect the dive at a ground point instead (a
                                    // slam variant) rather than the default lead-through-the-player thrust.
                                    leadTarget = ModifyAerialDiveWaypoint(leadTarget);
                                    if (_flight.RequestDive(leadTarget))
                                    {
                                        _aerialDiveCooldown = AerialDiveCooldownTicks;
                                        DoAerialDiveTelegraph();
                                    }
                                }
                            }
                            else if (_flight.IsDiving && _aerialHitCooldown <= 0)
                            {
                                // Dive contact: place a thrust hitbox ahead of the weapon.
                                DoAerialDiveHit();
                                _aerialHitCooldown = 8; // brief inter-hit cooldown during dive
                            }
                            else if (_flight.Mode == FlightMode.Hover
                                  && CanBreathe && BreathAllowedAirborne
                                  && _breathCooldown <= 0
                                  && Main.rand.Next(130) == 0
                                  && NPC.Distance(target.Center) <= BreathRange
                                  && hasLOS)
                            {
                                // Aerial breath: charge while hovering, then the BreathTelegraph→Breathing
                                // transition requests a strafe so the stream sweeps across as it flies past.
                                EnterPhase(AttackPhase.BreathTelegraph, BreathTelegraphTicks);
                            }
                            else if (_flight.Mode == FlightMode.Hover
                                  && CanThrowCursedKnives && CursedKnivesAllowedAirborne
                                  && _cursedKnivesCooldown <= 0
                                  && Main.rand.Next(140) == 0
                                  && NPC.Distance(target.Center) <= CursedKnivesRange
                                  && hasLOS)
                            {
                                StartCursedKnives(NPC.Distance(target.Center), hasLOS);
                            }
                            else if (_flight.Mode == FlightMode.Hover && Main.rand.Next(110) == 0)
                            {
                                // No attack was selected this tick, so vary the hover line with a lateral pass.
                                float side = Math.Sign(NPC.Center.X - target.Center.X);
                                if (side == 0)
                                {
                                    side = -NPC.direction;
                                }
                                _flight.RequestStrafe(target.Center + new Vector2(-side * 240f, 0f));
                            }
                            break;
                        }
                        else
                        {
                            // Grounded: roll for tactical takeoff.
                            bool playerAbove = target.Center.Y < NPC.Center.Y - FlightHeightTrigger;
                            float hpFrac     = (float)NPC.life / NPC.lifeMax;
                            int   randChance = hpFrac <= FlightHpEscalationFrac
                                             ? RandomTakeoffChance * 3
                                             : RandomTakeoffChance;
                            bool randomBurst = randChance > 0
                                             && Main.GameUpdateCount % 60 == 0
                                             && Main.rand.Next(100) < randChance;

                            if (playerAbove || randomBurst)
                            {
                                if (_flight.RequestTakeoff())
                                    break;
                            }
                        }
                    }

                    if (!hasLOS)
                        break;

                    // ── Fire breath intercept (grounded) ──────────────────────────
                    // Reached only when grounded (the airborne block above breaks first).  Rolls
                    // BreathChance to plant and charge a sustained breath stream.
                    if (CanBreathe && _breathCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist <= BreathRange && dist >= MinBreathRange
                        && Main.rand.Next(100) < BreathChance)
                    {
                        EnterPhase(AttackPhase.BreathTelegraph, BreathTelegraphTicks);
                        break;
                    }

                    // ── Cursed Knives intercept (grounded) ────────────────────────
                    if (CanThrowCursedKnives && _cursedKnivesCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist <= CursedKnivesRange && dist >= CursedKnivesMinRange
                        && Main.rand.Next(100) < CursedKnivesChance)
                    {
                        StartCursedKnives(dist, hasLOS);
                        break;
                    }

                    // ── Piercing Dash intercept (grounded) ────────────────────────
                    if (CanPierce && _pierceCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist <= PierceRange && dist >= MinPierceRange
                        && Main.rand.Next(100) < PierceChance)
                    {
                        _pierceIsStab = Main.rand.Next(100) < PierceStabChance;
                        _pierceHitConnected = false;
                        _pierceContactDodged = false;
                        _pierceRepeatsUsed = 0;
                        _pierceTelegraphTotalTicks = PierceTelegraphTicks;
                        EnterPhase(AttackPhase.PierceTelegraph, PierceTelegraphTicks);
                        break;
                    }

                    // ── Jumping Downward Slash intercept (grounded) ───────────────
                    if (CanJumpSlash && _jumpSlashCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= JumpSlashMinRange && dist <= JumpSlashMaxRange
                        && Main.rand.Next(100) < JumpSlashChance)
                    {
                        _lastAttackHitConnected = false;
                        EnterPhase(AttackPhase.JumpSlashDodgeback, JumpSlashDodgebackTicks);
                        break;
                    }

                    // ── Forward Flip Slash intercept (grounded) ───────────────────
                    if (CanFlipSlash && _flipSlashCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= FlipSlashMinRange && dist <= FlipSlashMaxRange
                        && Main.rand.Next(100) < FlipSlashChance)
                    {
                        EnterPhase(AttackPhase.FlipSlashRise, FlipSlashRiseMaxTicks);
                        break;
                    }

                    // ── Abyss Slash intercept (grounded) ──────────────────────────
                    if (CanAbyssSlash && _abyssSlashCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= AbyssSlashMinRange && dist <= AbyssSlashMaxRange
                        && Main.rand.Next(100) < AbyssSlashChance)
                    {
                        _lastAttackHitConnected = false;
                        EnterPhase(AttackPhase.AbyssSlashTelegraph, AbyssSlashArcTicks + AbyssSlashHoldTicks);
                        break;
                    }

                    // ── Abyss Tendril Grab intercept (grounded) ───────────────────
                    if (CanTendrilGrab && _tendrilCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= TendrilMinRange && dist <= TendrilMaxRange
                        && Main.rand.Next(100) < TendrilChance)
                    {
                        _lastAttackHitConnected = false;
                        EnterPhase(AttackPhase.TendrilTelegraph, TendrilTelegraphTicks);
                        break;
                    }

                    // ── Abyss Shard intercept (grounded) ──────────────────────────
                    if (CanAbyssShard && _abyssShardCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= AbyssShardMinRange && dist <= AbyssShardMaxRange
                        && Main.rand.Next(100) < AbyssShardChance)
                    {
                        EnterPhase(AttackPhase.AbyssShardTelegraph, AbyssShardTelegraphTicks);
                        break;
                    }

                    // ── Homing Volley intercept (grounded, midrange) ──────────────
                    if (CanHomingVolley && _homingVolleyCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= HomingVolleyMinRange && dist <= HomingVolleyMaxRange
                        && Main.rand.Next(100) < HomingVolleyChance)
                    {
                        EnterPhase(AttackPhase.HomingVolleyDodgeback, HomingVolleyDodgebackTicks);
                        break;
                    }

                    // ── Boomerang Crescent intercept (grounded) ───────────────────
                    // Wide range band (down to point-blank) since SwordLaunchReposition handles
                    // spacing itself - hops back first only if the player is too close.
                    if (CanBoomerang && _boomerangCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= BoomerangMinRange && dist <= BoomerangMaxRange
                        && Main.rand.Next(100) < BoomerangChance)
                    {
                        if (dist < SwordLaunchRepositionTooCloseRange)
                        {
                            _swordLaunchNextPhase = AttackPhase.BoomerangSwingTelegraph;
                            _swordLaunchNextTicks = BoomerangSwingTelegraphTicks;
                            EnterPhase(AttackPhase.SwordLaunchReposition, SwordLaunchRepositionTicks);
                        }
                        else
                        {
                            EnterPhase(AttackPhase.BoomerangSwingTelegraph, BoomerangSwingTelegraphTicks);
                        }
                        break;
                    }

                    // ── Spiral Fan intercept (grounded) ───────────────────────────
                    if (CanSpiralFan && _spiralFanCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= SpiralFanMinRange && dist <= SpiralFanMaxRange
                        && Main.rand.Next(100) < SpiralFanChance)
                    {
                        OnSpiralFanSequenceStart();
                        if (dist < SwordLaunchRepositionTooCloseRange)
                        {
                            _swordLaunchNextPhase = AttackPhase.SpiralFanSwingTelegraph;
                            _swordLaunchNextTicks = SpiralFanSwingTelegraphTicks;
                            EnterPhase(AttackPhase.SwordLaunchReposition, SwordLaunchRepositionTicks);
                        }
                        else
                        {
                            EnterPhase(AttackPhase.SpiralFanSwingTelegraph, SpiralFanSwingTelegraphTicks);
                        }
                        break;
                    }

                    // ── Secondary-ranged panic backhop ────────────────────────────
                    // Player got too close: hop away (still facing them) and fire a single secondary
                    // shot to reset spacing.  Bypasses the normal secondary min-range band.
                    if (SecondaryRangedWeaponItemType >= 0
                        && SecondaryRangedAvailable
                        && SecondaryRangedBackhopRange > 0f
                        && _secondaryRangedCooldown <= 0
                        && NPC.velocity.Y == 0f
                        && dist <= SecondaryRangedBackhopRange
                        && Main.rand.Next(100) < SecondaryRangedBackhopChance)
                    {
                        int away = NPC.Center.X < target.Center.X ? -1 : 1; // direction AWAY from player
                        _backhopDir   = away;
                        _backhopTicks = BackhopMomentumTicks; // AI() carries the away-velocity over this window
                        NPC.velocity.Y = -SecondaryRangedBackhopUpSpeed; // one-time upward arc
                        NPC.direction = NPC.spriteDirection = -away; // keep facing the player to aim
                        _directionHoldTicks = Math.Max(_directionHoldTicks, BackhopMomentumTicks);
                        SetupRangedBurst(useSecondary: true, shotsOverride: 1, forceStanding: false);
                        _standingShot = false; // mobile shot so the telegraph doesn't hard-brake the hop
                        EnterPhase(AttackPhase.RangedTelegraph, _activeRangedTelegraphTicks);
                        break;
                    }

                    // ── Melee combo intercept ─────────────────────────────────────
                    // Roll first: combo system competes with the legacy slash/stab path.
                    // Combos require grounded, height-accessible target, archetype set, AND
                    // player within effective combo reach so the swings actually connect.
                    int comboStartChance = dist > MeleeEngageRange
                        ? RangedStartMeleeComboChance
                        : MeleeComboChance;
                    float comboInterceptRange = Math.Max(ComboMaxStartRange, RangedStartComboMaxRange);
                    if (MeleeArchetype != WeaponArchetype.None
                        && MeleeWeaponItemType >= 0
                        && NPC.velocity.Y == 0f
                        && NPC.Center.Y - target.Center.Y < 48f
                        && dist <= comboInterceptRange
                        && Main.rand.Next(100) < comboStartChance)
                    {
                        // Out of hittable reach → close the gap first (no swing until in range).
                        if (dist > MeleeEngageRange)
                        {
                            // A committed ranged-start combo (throw or bespoke running attack) may
                            // deliberately open from here. Otherwise preserve normal gap closing.
                            if (TryStartMeleeCombo(dist, rangedStartOnly: true))
                                break;

                            // The extended band is only for an authored gap-closer. If it is on
                            // cooldown or otherwise ineligible, retain access to ranged/magic attacks.
                            if (dist <= ComboMaxStartRange)
                            {
                                EnterPhase(AttackPhase.ClosingDistance, ClosingDistanceMaxTicks);
                                break;
                            }
                        }
                        else if (TryStartMeleeCombo(dist))
                            break;
                    }

                    // Gate melee/stab on vertical accessibility: if the player is standing on a
                    // ledge above the puppet, skip attack phases and let FighterAI navigate up
                    // first.  Without this the puppet telegraphs but can never connect.
                    // Also gate on grounded (velocity.Y == 0) — mid-air attacks cause the puppet
                    // to mid-air flip direction after overshooting and attack in the wrong direction.
                    float heightDiff = NPC.Center.Y - target.Center.Y; // positive = player above

                    bool wantSlash  = MeleeWeaponItemType  >= 0 && dist <= MeleeRange  && heightDiff < 36f
                                      && NPC.velocity.Y == 0f;
                    bool wantStab   = CanStab               && dist <= StabRange && dist > MeleeRange
                                      && _stabCooldown <= 0 && heightDiff < 48f && NPC.velocity.Y == 0f;
                    // Spear poke: medium-to-long melee reach, grounded, no lunge required.
                    bool wantSpear  = SpearWeaponItemType >= 0 && dist <= SpearRange
                                      && _spearCooldown <= 0 && heightDiff < 48f && NPC.velocity.Y == 0f;
                    // LOS-gated: without this a puppet on uneven terrain picks a ranged attack whose
                    // straight-line shot immediately clips the ground/a ledge between it and the
                    // player. Skipping the pick (rather than firing blind) leaves the puppet in its
                    // normal pursuit movement, which naturally closes into a clear sightline.
                    bool wantPrimary   = RangedWeaponItemType >= 0 && dist <= RangedRange
                                         && dist >= MinRangedRange && _rangedCooldown <= 0 && hasLOS;
                    bool wantSecondary = SecondaryRangedWeaponItemType >= 0 && SecondaryRangedAvailable
                                         && dist <= SecondaryRangedRange
                                         && dist >= SecondaryRangedMinRange && _secondaryRangedCooldown <= 0
                                         && hasLOS;
                    // Magic: fires from any elevation (no velocity.Y check), blocked at very close range.
                    bool wantMagic     = MagicWeaponItemType >= 0 && dist <= MagicRange
                                         && dist >= MinMagicRange && _magicCooldown <= 0;

                    if (wantSlash)
                    {
                        ArmSwingVariation(heightDiff);
                        EnterPhase(AttackPhase.MeleeTelegraph, MeleeTelegraphTicks);
                    }
                    else if (wantStab)
                        EnterPhase(AttackPhase.StabTelegraph, StabTelegraphTicks);
                    else if (wantSpear)
                    {
                        _spearLungeDir = NPC.direction;
                        EnterPhase(AttackPhase.SpearTelegraph, SpearTelegraphTicks);
                    }
                    else if (wantPrimary || wantSecondary || wantMagic)
                    {
                        // Magic competes with ranged: always when it's the only option in range, else on a
                        // MagicPreferenceChance roll (default 0 = magic only fills gaps, original behavior).
                        bool useMagic = wantMagic && (!(wantPrimary || wantSecondary)
                                        || Main.rand.Next(100) < MagicPreferenceChance);
                        if (useMagic)
                        {
                            OnMagicTelegraphStarting();
                            EnterPhase(AttackPhase.MagicTelegraph, MagicTelegraphTicks);
                        }
                        else
                        {
                            // Pick secondary when both are available (SecondaryRangedChance roll),
                            // or always if only secondary is in range.
                            bool useSecondary = wantSecondary &&
                                (!wantPrimary || Main.rand.Next(100) < SecondaryRangedChance);
                            SetupRangedBurst(useSecondary, shotsOverride: -1, forceStanding: false);
                            EnterPhase(AttackPhase.RangedTelegraph, _activeRangedTelegraphTicks);
                        }
                    }
                    break;

                // ── Melee slash ───────────────────────────────────────────────
                case AttackPhase.MeleeTelegraph:
                    // The wind-up still tracks; the swing that follows is locked to whichever way
                    // the sword was raised, so a player rolling behind ends up behind the arc rather
                    // than dragging it around with them.
                    _attackFacingDir = target.Center.X < NPC.Center.X ? -1 : 1;
                    LockAttackFacing();
                    if (SlowDownBeforeMelee)
                    {
                        SlowDown();
                    }
                    SetDisplayWeapon(FrontHandWeaponType, swing: false);
                    CheckAndFireFlash(MeleeTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(FrontHandWeaponType, swing: true);
                        _bladeArmed = false;
                        DoMeleeAttack();
                        EnterPhase(AttackPhase.MeleeAttack, GetMeleeSwingTicks(MeleeAttackTicks));
                    }
                    break;

                case AttackPhase.MeleeAttack:
                    LockAttackFacing();
                    // Keep opted-in fire slash visuals alive for the exact same window as the
                    // tracked blade hitbox. DoMeleeAttack only runs once at phase entry, which made
                    // the effect flash for one frame and then begin fading while the sword was
                    // still sweeping.
                    if (HasFireSlashVFX)
                    {
                        int meleeSwingTicks = Math.Max(1, GetMeleeSwingTicks(MeleeAttackTicks));
                        float meleeSwingProgress = 1f - PhaseTimer / (float)meleeSwingTicks;
                        // TryMeleeHit's default reach. Use the same expression instead of the
                        // server-only _activeBladeReach so multiplayer clients draw it too.
                        ArmFireSlashVFX(MeleeRange * 0.7f, meleeSwingProgress);
                    }

                    // An authored MeleeAttackCurve disarms the blade once it slows under 30% of peak speed;
                    // the rest of the swing is harmless follow-through. Unset = live for the whole phase.
                    int elapsedMeleeSwingTicks = GetMeleeSwingTicks(MeleeAttackTicks) - PhaseTimer;
                    bool meleeBladeLive = !MeleeAttackCurve.IsSet || elapsedMeleeSwingTicks <= MeleeAttackCurve.LiveTicks;
                    if (meleeBladeLive)
                    {
                        TickBladeHit();
                    }

                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.MeleeRecovery, MeleeRecoveryTicks);
                    break;

                case AttackPhase.MeleeRecovery:
                    // Facing stays committed through recovery: the follow-through is part of the
                    // swing, and re-facing here made a heavy greatsword appear to teleport its arc.
                    LockAttackFacing();
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Stab / lunge ──────────────────────────────────────────────
                case AttackPhase.StabTelegraph:
                    _attackFacingDir = target.Center.X < NPC.Center.X ? -1 : 1;
                    LockAttackFacing();
                    if (SlowDownBeforeMelee)
                    {
                        SlowDown();
                    }
                    SetDisplayWeapon(FrontHandWeaponType, swing: false);
                    SpawnTelegraphDust();
                    CheckAndFireFlash(MeleeTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        _stabLungeDir = NPC.direction; // lock direction so lunge can't rubber-band
                        _bladeArmed = false;
                        DoStabAttack();
                        EnterPhase(AttackPhase.StabAttack, StabAttackTicks);
                    }
                    break;

                case AttackPhase.StabAttack:
                    TickBladeHit();
                    // Use the direction locked at lunge start — not the live NPC.direction which
                    // FighterAI may flip if the player dodgerolls through to the other side.
                    // Lunge speed = TopSpeed × StabLungeSpeedMult.  Default 2.0 is firm but not
                    // overshooting; override per-subclass for more/less aggressive dashes.
                    NPC.velocity.X      = _stabLungeDir * (TopSpeed * StabLungeSpeedMult);
                    _attackFacingDir    = _stabLungeDir;
                    LockAttackFacing();
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.StabRecovery, StabRecoveryTicks);
                    break;

                case AttackPhase.StabRecovery:
                    LockAttackFacing();
                    if (--PhaseTimer <= 0)
                    {
                        _stabCooldown = StabCooldownAfterUse; // prevent instant re-stab after dodgeroll
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Ranged ────────────────────────────────────────────────────
                // Telegraph: raise arm and aim.  Always decelerate so the puppet
                // clearly "prepares to throw".  Standing shots brake harder to a full
                // stop; moving shots just slow — this still reads as deliberate aiming.
                case AttackPhase.RangedTelegraph:
                    FaceTargetForRangedShot(target);
                    if (HasRangedJumpOverride)
                    {
                        // A scripted jump arc (e.g. Owl Father's Jumping Bow Shot) owns velocity
                        // outright — neither the flight controller nor the stand/drift brake apply.
                        TickRangedJumpOverride();
                    }
                    // Skip ground velocity damping while airborne — flight controller owns velocity.
                    else if (_flight == null || !_flight.IsAirborne)
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
                    FaceTargetForRangedShot(target);
                    if (HasRangedJumpOverride)
                    {
                        TickRangedJumpOverride();
                    }
                    else if (_flight == null || !_flight.IsAirborne)
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
                        if (!TryStartQueuedRangedFollowUp())
                            EnterCasualOrIdle();
                    }
                    break;

                // ── Crossbow burst pause ──────────────────────────────────────
                // Hold horizontal aim between shots in a pattern burst.
                // When the timer expires the next shot fires immediately — no re-telegraph.
                // This gives a deliberate "controlled volley" feel without extra wind-up.
                case AttackPhase.CrossbowBurstPause:
                    FaceTargetForRangedShot(target);
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
                    if (SlowDownBeforeMelee)
                    {
                        SlowDown();
                    }
                    SetDisplayWeapon(SpearWeaponItemType, swing: false);
                    SpawnTelegraphDust();
                    CheckAndFireFlash(SpearTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(SpearWeaponItemType, swing: true);
                        _bladeArmed = false;
                        DoSpearAttack();
                        EnterPhase(AttackPhase.SpearAttack, SpearAttackTicks);
                    }
                    break;

                case AttackPhase.SpearAttack:
                    TickBladeHit();
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
                // Charge-up then fire a spell projectile.  The puppet brakes to a
                // halt during the telegraph so it reads as a deliberate cast.
                case AttackPhase.MagicTelegraph:
                    if (BrakeDuringMagicCast)
                    {
                        SlowDown();
                    }
                    SetDisplayWeapon(MagicWeaponItemType, swing: false);
                    CheckAndFireFlash(MagicTelegraphFlashColor, MagicTelegraphFlashLeadTicks);
                    if (--PhaseTimer <= 0)
                    {
                        if (UseAuthoredMagicCastPose)
                            _weaponRotation = MagicCastEndRotation;
                        SetDisplayWeapon(MagicWeaponItemType, swing: true);
                        DoMagicAttack();
                        // A subclass can set _magicAttackTicksOverride inside DoMagicAttack to channel a
                        // sustained cast (DoMagicTick runs each tick below); -1 keeps the instant cast.
                        EnterPhase(AttackPhase.MagicAttack,
                                   _magicAttackTicksOverride > 0 ? _magicAttackTicksOverride : MagicAttackTicks);
                    }
                    break;

                case AttackPhase.MagicAttack:
                    if (BrakeDuringMagicCast)
                    {
                        NPC.velocity.X *= 0.5f; // momentum bleeds off during cast follow-through
                    }
                    DoMagicTick(PhaseTimer);
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.MagicRecovery, MagicRecoveryTicks);
                    break;

                case AttackPhase.MagicRecovery:
                    if (--PhaseTimer <= 0)
                    {
                        _magicCooldown = MagicCooldownAfterUse;
                        _magicAttackTicksOverride = -1; // clear any sustained-cast override
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Custom set-piece ──────────────────────────────────────────
                // A subclass parked the puppet here via StartCustomAttack: hold the requested pose (if
                // any), brake like a channel, and hand each tick to the subclass. DoCustomAttack already
                // fired on entry; DoCustomTick gets ticksRemaining (last tick = 1), matching the magic
                // convention. Returns to Idle/CasualStroll when it expires.
                case AttackPhase.Custom:
                    if (SlowDownDuringCustom)
                    {
                        SlowDown();
                    }
                    if (_customPoseWeapon >= 0)
                    {
                        SetDisplayWeapon(_customPoseWeapon, swing: _customSwingPose);
                    }
                    DoCustomTick(PhaseTimer);
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Fire breath ───────────────────────────────────────────────
                // Telegraph: charge the mouth ember (DoBreathWindup).  Grounded → plant; airborne →
                // keep hovering (flight controller owns velocity).  Stream: DoBreathTick spawns the
                // breath each tick; airborne it strafes to sweep the fire across the player.
                case AttackPhase.BreathTelegraph:
                {
                    bool airborneBreath = _flight != null && _flight.IsAirborne;
                    if (!airborneBreath)
                        SlowDown();
                    int faceB = target.Center.X < NPC.Center.X ? -1 : 1;
                    NPC.direction = faceB;
                    NPC.spriteDirection = faceB;
                    DoBreathWindup(BreathTelegraphTicks - PhaseTimer);
                    CheckAndFireFlash(BreathTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        OnBreathStart();
                        if (airborneBreath && _flight != null)
                        {
                            // Sweep: strafe to the far side of the player so the stream rakes across.
                            int sweepDir = NPC.Center.X < target.Center.X ? 1 : -1;
                            _flight.RequestStrafe(target.Center + new Vector2(sweepDir * 420f, 0f));
                        }
                        EnterPhase(AttackPhase.Breathing, BreathDurationTicks);
                    }
                    break;
                }

                case AttackPhase.Breathing:
                {
                    bool airborneBreath = _flight != null && _flight.IsAirborne;
                    if (!airborneBreath)
                    {
                        int faceB = target.Center.X < NPC.Center.X ? -1 : 1;
                        if (AdvanceDuringGroundedBreath)
                        {
                            // Walk forward through the stream instead of bleeding to a standstill -
                            // the breath then doubles as advancing pressure, not a planted hose.
                            NPC.velocity.X = faceB * TopSpeed * 0.5f;
                        }
                        else
                        {
                            NPC.velocity.X *= 0.85f;
                        }
                        NPC.direction = faceB;
                        NPC.spriteDirection = faceB;
                    }
                    DoBreathTick(PhaseTimer);
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.BreathRecovery, BreathRecoveryTicks);
                    break;
                }

                case AttackPhase.BreathRecovery:
                    if (_flight == null || !_flight.IsAirborne)
                        SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _breathCooldown = BreathCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Cursed Knives ─────────────────────────────────────────────
                // Telegraph (hold knife) → 1–3 throw volleys with inter-volley pauses → recovery.
                case AttackPhase.KnivesTelegraph:
                {
                    bool airK = _flight != null && _flight.IsAirborne;
                    if (!airK)
                    {
                        SlowDown();
                    }
                    int faceK = target.Center.X < NPC.Center.X ? -1 : 1;
                    NPC.direction = faceK;
                    NPC.spriteDirection = faceK;
                    SetDisplayWeapon(CursedKnivesWeaponItemType, swing: false);
                    CheckAndFireFlash(CursedKnivesTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(CursedKnivesWeaponItemType, swing: true);
                        DoCursedKnivesThrow();
                        _cursedKnivesVolleysLeft--;
                        EnterPhase(AttackPhase.KnivesThrow, CursedKnivesThrowTicks);
                    }
                    break;
                }

                case AttackPhase.KnivesThrow:
                {
                    bool airK = _flight != null && _flight.IsAirborne;
                    if (!airK)
                    {
                        NPC.velocity.X *= 0.8f;
                    }
                    if (--PhaseTimer <= 0)
                    {
                        if (_cursedKnivesVolleysLeft > 0)
                            EnterPhase(AttackPhase.KnivesThrowPause, Math.Max(1, _cursedKnivesGap));
                        else
                            EnterPhase(AttackPhase.KnivesRecovery, CursedKnivesRecoveryTicks);
                    }
                    break;
                }

                case AttackPhase.KnivesThrowPause:
                {
                    bool airK = _flight != null && _flight.IsAirborne;
                    if (!airK)
                    {
                        SlowDown();
                    }
                    int faceK = target.Center.X < NPC.Center.X ? -1 : 1;
                    NPC.direction = faceK;
                    NPC.spriteDirection = faceK;
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(CursedKnivesWeaponItemType, swing: true);
                        DoCursedKnivesThrow();
                        _cursedKnivesVolleysLeft--;
                        EnterPhase(AttackPhase.KnivesThrow, CursedKnivesThrowTicks);
                    }
                    break;
                }

                case AttackPhase.KnivesRecovery:
                    if (_flight == null || !_flight.IsAirborne)
                        SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _cursedKnivesCooldown = CursedKnivesCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Piercing Dash ──────────────────────────────────────────────
                // Telegraph holds a fixed anchor position (weapon out toward the player) with a small
                // random jitter each tick for the "sprite vibrates" tell, then launches into the dash.
                case AttackPhase.PierceTelegraph:
                {
                    int faceP = target.Center.X < NPC.Center.X ? -1 : 1;
                    _pierceDir = faceP;
                    _attackFacingDir = faceP;
                    LockAttackFacing();
                    // A repeat telegraph is shorter than the first, so the first tick is detected against the
                    // length this telegraph actually started with.
                    if (PhaseTimer == _pierceTelegraphTotalTicks)
                    {
                        _pierceAnchorPos = NPC.position;
                        NPC.velocity.X = 0f;
                    }
                    NPC.position = _pierceAnchorPos + Main.rand.NextVector2Circular(1.5f, 1.5f);
                    SetDisplayWeapon(FrontHandWeaponType, swing: false);
                    DoPierceWindup(_pierceTelegraphTotalTicks - PhaseTimer);
                    if (--PhaseTimer <= 0)
                    {
                        NPC.position = _pierceAnchorPos;
                        NPC.velocity.X = _pierceDir * PierceDashSpeed;
                        SetDisplayWeapon(FrontHandWeaponType, swing: true);
                        EnterPhase(AttackPhase.PierceDash, PierceDashTicks);
                    }
                    break;
                }

                // Active lunge: fresh reach check every tick (rather than a spawned hitbox) so a stab-variant
                // connect can immediately break out into PierceStabHold the instant it touches the target.
                // A plain (non-stab) hit just marks _pierceHitConnected and keeps carrying through.
                case AttackPhase.PierceDash:
                {
                    // The dash usually overshoots THROUGH the player; without this the sprite spun
                    // around the instant it crossed, so the lance visibly pointed back the way it came.
                    LockAttackFacing();
                    NPC.velocity.X = _pierceDir * PierceDashSpeed;
                    DoPierceDashTick();

                    // Contact is hit resolution: server only. A client adopts PierceStabHold (and the synced
                    // _pierceTarget) from the snapshot, so the impaled player's own machine runs the hold.
                    if (!_pierceHitConnected && !IsMultiplayerClient)
                    {
                        Rectangle reach = NPC.Hitbox;
                        reach.Inflate(24, 12);
                        if (reach.Intersects(target.Hitbox))
                        {
                            _pierceHitConnected = true;
                            _pierceContactDodged = target.immune;
                            _pierceTarget = target;
                            OnPierceContact(target, _pierceIsStab);

                            if (_pierceIsStab)
                            {
                                NPC.velocity.X = 0f;
                                EnterPhase(AttackPhase.PierceStabHold, PierceStabRaiseTicks);
                                break;
                            }
                        }
                    }

                    if (--PhaseTimer <= 0)
                    {
                        // Repeat vs recovery rests on the server-only contact result: a client holds the end of
                        // the dash until the snapshot says which.
                        if (IsMultiplayerClient)
                        {
                            PhaseTimer = 0;
                            break;
                        }

                        NPC.velocity.X *= 0.4f;

                        // Didn't connect: nothing touched, or only a player mid-roll. The repeat re-enters the
                        // telegraph, which re-faces the target and re-anchors.
                        bool whiffed = !_pierceHitConnected || _pierceContactDodged;
                        bool repeatsLeft = _pierceRepeatsUsed < PierceWhiffRepeatCount;
                        bool targetInReach = NPC.HasValidTarget && dist <= PierceRange;
                        bool canRepeat = whiffed && repeatsLeft && targetInReach;

                        if (canRepeat)
                        {
                            _pierceRepeatsUsed++;
                            _pierceHitConnected = false;
                            _pierceContactDodged = false;
                            _pierceTarget = null;
                            _pierceTelegraphTotalTicks = Math.Max(1, PierceRepeatTelegraphTicks);
                            EnterPhase(AttackPhase.PierceTelegraph, _pierceTelegraphTotalTicks);
                            NPC.netUpdate = true;
                        }
                        else
                        {
                            EnterPhase(AttackPhase.PierceRecovery, PierceRecoveryTicks);
                        }
                    }
                    break;
                }

                // Stab variant only: target is held impaled while the sword arm raises 0→90°.
                case AttackPhase.PierceStabHold:
                {
                    LockAttackFacing();
                    NPC.velocity.X = 0f;
                    float raiseProgress = 1f - PhaseTimer / (float)PierceStabRaiseTicks;
                    if (_pierceTarget != null && _pierceTarget.active)
                        DoPierceStabHoldTick(_pierceTarget, raiseProgress);
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.PierceStabFlick, PierceStabFlickTicks);
                    break;
                }

                // Stab variant only: rotate back down and release/launch the target (OnPierceFlick fires
                // exactly once, on the phase's first tick).
                case AttackPhase.PierceStabFlick:
                {
                    LockAttackFacing();
                    int flickElapsed = PierceStabFlickTicks - PhaseTimer;
                    int releaseDelay = Math.Clamp(PierceStabFlickDelayTicks, 0,
                        Math.Max(0, PierceStabFlickTicks - 1));
                    if (_pierceTarget != null && _pierceTarget.active)
                    {
                        if (flickElapsed == 0)
                            OnPierceFlickStarted(_pierceTarget);
                        if (flickElapsed < releaseDelay)
                            DoPierceStabFlickDelayTick(_pierceTarget, flickElapsed);
                        if (flickElapsed == releaseDelay)
                            OnPierceFlick(_pierceTarget);
                    }
                    if (--PhaseTimer <= 0)
                    {
                        _pierceTarget = null;
                        EnterPhase(AttackPhase.PierceRecovery, PierceRecoveryTicks);
                    }
                    break;
                }

                // Shared recovery after either pierce variant ends — can walk, can't attack yet.
                case AttackPhase.PierceRecovery:
                    LockAttackFacing();
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _pierceCooldown = PierceCooldownAfterUse;
                        _pierceTarget = null;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Jumping Downward Slash ─────────────────────────────────────
                // Backward roll WITH real i-frames (DodgeTimer) — a genuine dodgeroll, not a cosmetic hop.
                case AttackPhase.JumpSlashDodgeback:
                {
                    if (PhaseTimer == JumpSlashDodgebackTicks)
                    {
                        int faceJ = target.Center.X < NPC.Center.X ? -1 : 1;
                        _jumpSlashDir = faceJ;
                        _attackFacingDir = faceJ;
                        NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().DodgeTimer = JumpSlashDodgebackTicks + 5;
                    }
                    // Held for the whole roll → leap → swipe → recovery chain. The leap deliberately
                    // passes OVER the player, so without this the navigator flips the sprite around
                    // at the apex and the sword comes down facing the wrong way.
                    LockAttackFacing();
                    NPC.velocity.X = -_jumpSlashDir * JumpSlashDodgebackSpeed; // away from the player
                    DoJumpSlashDodgebackTick();
                    if (--PhaseTimer <= 0)
                    {
                        _jumpSlashLaunched = false;
                        EnterPhase(AttackPhase.JumpSlashRise, JumpSlashRiseTicks);
                    }
                    break;
                }

                // Launch toward the player holding the cocked pose; breaks into the swipe once close
                // or on landing (same leap-physics shape as the ComboMotion.LeapSlam launch).
                case AttackPhase.JumpSlashRise:
                {
                    if (!_jumpSlashLaunched)
                    {
                        _jumpSlashLaunched = true;
                        // Re-aim once at launch (the roll may have crossed the player), then commit.
                        int faceJ = target.Center.X < NPC.Center.X ? -1 : 1;
                        _jumpSlashDir = faceJ;
                        _attackFacingDir = faceJ;

                        float dx = Math.Abs(target.Center.X - NPC.Center.X);
                        float rangeT = JumpSlashMaxRange > 0f
                            ? MathHelper.Clamp(dx / JumpSlashMaxRange, 0f, 1f)
                            : 1f;
                        float preferredSpeed = MathHelper.Lerp(JumpSlashLaunchForwardSpeed,
                            JumpSlashMaxForwardSpeed, rangeT);
                        float baselineAirTime = 2f * JumpSlashLaunchUpSpeed / JumpSlashGravity;
                        float airTime = Math.Max(baselineAirTime, dx / Math.Max(1f, preferredSpeed));
                        float verticalDelta = target.Center.Y - NPC.Center.Y;
                        float upSpeed = JumpSlashGravity * airTime * 0.5f - verticalDelta / airTime;
                        upSpeed = MathHelper.Clamp(upSpeed, JumpSlashLaunchUpSpeed, JumpSlashMaxUpSpeed);

                        _jumpSlashFlightSpeed = Math.Min(dx / airTime, JumpSlashMaxForwardSpeed);
                        PhaseTimer = Math.Max(PhaseTimer, (int)Math.Ceiling(airTime) + 12);
                        NPC.velocity = new Vector2(_jumpSlashDir * _jumpSlashFlightSpeed, -upSpeed);
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        NPC.velocity.X = _jumpSlashDir * _jumpSlashFlightSpeed;
                    }

                    LockAttackFacing();
                    DoJumpSlashRiseTick();

                    bool nearPlayer = NPC.Distance(target.Center) <= JumpSlashTriggerRange;
                    bool landed = PhaseTimer < JumpSlashRiseTicks && NPC.velocity.Y == 0f;
                    if (nearPlayer || landed || --PhaseTimer <= 0)
                    {
                        DoJumpSlashAttack();
                        EnterPhase(AttackPhase.JumpSlashAttack, JumpSlashAttackTicks);
                    }
                    break;
                }

                case AttackPhase.JumpSlashAttack:
                    LockAttackFacing();
                    // TryMeleeHit (called from DoJumpSlashAttack on phase entry) only ARMS the
                    // tracked blade check - TickBladeHit has to run every tick of the swing to
                    // actually test it, same as MeleeAttack/StabAttack below. Without this call the
                    // swing never connects via its real sprite sweep at all, standing player or not.
                    // An authored JumpSlashCurve disarms it after the curve's LiveTicks (the harmless settle).
                    int elapsedJumpSlashTicks = JumpSlashAttackTicks - PhaseTimer;
                    bool jumpSlashBladeLive = !JumpSlashCurve.IsSet || elapsedJumpSlashTicks <= JumpSlashCurve.LiveTicks;
                    if (jumpSlashBladeLive)
                    {
                        TickBladeHit();
                    }
                    if (--PhaseTimer <= 0)
                    {
                        // Punish the dodge: a clean whiff chains straight into Abyss Slash instead
                        // of a free recovery window, if that's off cooldown and in range.
                        _jumpSlashCooldown = JumpSlashCooldownAfterUse;
                        if (!TryDodgePunishChain(dist))
                            EnterPhase(AttackPhase.JumpSlashRecovery, JumpSlashRecoveryTicks);
                    }
                    break;

                case AttackPhase.JumpSlashRecovery:
                    LockAttackFacing();
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _jumpSlashCooldown = JumpSlashCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Forward Flip Slash ─────────────────────────────────────────
                // Airborne, genuinely i-framed (DodgeTimer refreshed every tick - a real dodgeroll,
                // not just hyper-armor), spinning continuously with a one-shot contact check.
                case AttackPhase.FlipSlashRise:
                {
                    if (PhaseTimer == FlipSlashRiseMaxTicks)
                    {
                        int faceF = target.Center.X < NPC.Center.X ? -1 : 1;
                        _flipSlashDir = faceF;
                        _attackFacingDir = faceF;
                        float dx = Math.Abs(target.Center.X - NPC.Center.X);
                        float airtime = 2f * FlipSlashLaunchUpSpeed / 0.3f;
                        float vx = MathHelper.Clamp(dx / airtime, 2f, FlipSlashLaunchForwardSpeed);
                        NPC.velocity = new Vector2(_flipSlashDir * vx, -FlipSlashLaunchUpSpeed);
                        NPC.netUpdate = true;

                        _flipSlashLaunchBottomY = NPC.Bottom.Y;
                        _flipSlashSolvedSpinSpeed = FlipSlashSpinSpeed;
                        DoFlipSlashHit();
                    }
                    else
                    {
                        NPC.velocity.X = _flipSlashDir * FlipSlashLaunchForwardSpeed;
                    }

                    // Refreshed every tick so the i-frame window always covers the whole flip
                    // regardless of exact air time.
                    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().DodgeTimer = 5;

                    LockAttackFacing();
                    DoFlipSlashRiseTick();

                    // The spin is a real sweeping blade for the whole flight. It was only ever armed
                    // before (by an overlap with NPC.Hitbox) and never ticked, so it could not hit at all.
                    TickBladeHit();

                    bool landedFlip = PhaseTimer < FlipSlashRiseMaxTicks && NPC.velocity.Y == 0f;
                    if (landedFlip || --PhaseTimer <= 0)
                    {
                        if (UseFlipSlashLandingStrike)
                        {
                            // Re-wrap the free-running spin angle next to the cocked pose, so the strike
                            // sweeps on from exactly where the blade is (no snap) and ends inside the normal
                            // rotation range that recovery lerps back from.
                            float cockedRotation = FlipSlashStrikeStartRotation;
                            _flipSlashStrikeStartRotation = cockedRotation
                                + MathHelper.WrapAngle(_weaponRotation - cockedRotation);
                            _weaponRotation = _flipSlashStrikeStartRotation;
                        }

                        OnFlipSlashLand();
                        EnterPhase(AttackPhase.FlipSlashLand, FlipSlashLandHoldTicks);
                    }
                    break;
                }

                // Legacy: the sword settles into the slam pose (rotation table below) and holds it.
                // UseFlipSlashLandingStrike: a live overhead into the ground first, then the hold.
                case AttackPhase.FlipSlashLand:
                {
                    LockAttackFacing();
                    NPC.velocity.X *= 0.5f;

                    if (UseFlipSlashLandingStrike)
                    {
                        int elapsedLand = FlipSlashLandHoldTicks - PhaseTimer;
                        if (elapsedLand <= FlipSlashStrikeLiveTicks)
                        {
                            TickBladeHit();
                        }

                        if (elapsedLand == FlipSlashStrikeEaseInTicks)
                        {
                            OnFlipSlashStrikeContact();
                        }
                    }

                    if (--PhaseTimer <= 0)
                    {
                        _flipSlashCooldown = FlipSlashCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;
                }

                // ── Abyss Slash ─────────────────────────────────────────────────
                // Deliberately does not touch NPC.velocity.X - RunMovementAI's normal pursuit
                // (already applied earlier this tick, before PuppetAttackAI runs) carries through
                // untouched, so the puppet keeps walking during the whole wind-up.
                case AttackPhase.AbyssSlashTelegraph:
                    // Re-face the player each tick of the wind-up (the puppet is still walking here,
                    // so this is where the swipe direction is chosen), then commit it for the rest
                    // of the chain: every swipe after the first fires along the SAME line, and a
                    // navigator flip mid-chain used to fling the later crescents backwards.
                    _attackFacingDir = target.Center.X < NPC.Center.X ? -1 : 1;
                    LockAttackFacing();
                    if (--PhaseTimer <= 0)
                    {
                        _abyssSlashIndex = 0;
                        DoAbyssSlashFire(0);
                        EnterPhase(AttackPhase.AbyssSlashSwipe, AbyssSlashSwipeTicks);
                    }
                    break;

                case AttackPhase.AbyssSlashSwipe:
                    LockAttackFacing();
                    if (--PhaseTimer <= 0)
                    {
                        // The delay hook may read rolled, unsynced subclass state (Artorias's variant): the
                        // server picks the next swipe or recovery and a client holds this pose until then.
                        if (IsMultiplayerClient)
                        {
                            PhaseTimer = 0;
                            break;
                        }

                        int delay = NextAbyssSlashDelay(_abyssSlashIndex);
                        if (delay < 0)
                        {
                            // Punish the dodge: if the whole swipe combo whiffed, chain straight
                            // into Jumping Downward Slash instead of a free recovery window.
                            _abyssSlashCooldown = AbyssSlashCooldownAfterUse;
                            if (!TryDodgePunishChain(dist))
                                EnterPhase(AttackPhase.AbyssSlashRecovery, AbyssSlashRecoveryTicks);
                        }
                        else
                            EnterPhase(AttackPhase.AbyssSlashPause, delay);
                    }
                    break;

                case AttackPhase.AbyssSlashPause:
                    // Each swipe is its own aimed attack, so the facing lock lasts one swipe. Re-face here,
                    // before DoAbyssSlashFire, or a player who rolled behind gets crescents out of his back.
                    _attackFacingDir = target.Center.X < NPC.Center.X ? -1 : 1;
                    LockAttackFacing();
                    if (--PhaseTimer <= 0)
                    {
                        _abyssSlashIndex++;
                        DoAbyssSlashFire(_abyssSlashIndex);
                        EnterPhase(AttackPhase.AbyssSlashSwipe, AbyssSlashSwipeTicks);
                    }
                    break;

                case AttackPhase.AbyssSlashRecovery:
                    LockAttackFacing();
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _abyssSlashCooldown = AbyssSlashCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Abyss Tendril Grab ──────────────────────────────────────────
                case AttackPhase.TendrilTelegraph:
                    // Tracks during the reach-back, then owns facing for the yank and the finishing
                    // swing - the tendril flies along this direction, so the sprite must agree.
                    _attackFacingDir = target.Center.X < NPC.Center.X ? -1 : 1;
                    LockAttackFacing();
                    SlowDown();
                    DoTendrilTelegraphTick(TendrilTelegraphTicks - PhaseTimer);
                    if (--PhaseTimer <= 0)
                    {
                        DoTendrilLaunch();
                        EnterPhase(AttackPhase.TendrilReach, TendrilReachTicks);
                    }
                    break;

                // The tendril projectile is fully self-contained (fly/yank/retract); this phase
                // just holds the arm-out pose for as long as that sequence should take.
                case AttackPhase.TendrilReach:
                    LockAttackFacing();
                    SlowDown();
                    DoTendrilReachTick();
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.TendrilSwingTelegraph, TendrilSwingArcTicks + TendrilSwingHoldTicks);
                    break;

                case AttackPhase.TendrilSwingTelegraph:
                    LockAttackFacing();
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        DoTendrilSwing();
                        EnterPhase(AttackPhase.TendrilSwing, TendrilSwingTicks);
                    }
                    break;

                case AttackPhase.TendrilSwing:
                    LockAttackFacing();
                    // DoTendrilSwing only arms the tracked blade once, on phase entry. Resolve that
                    // authored hand-to-tip sweep on every active frame just like MeleeAttack and
                    // JumpSlashAttack; without this call every tendril finisher was visual-only.
                    if (HasFireSlashVFX)
                    {
                        float tendrilSwingProgress = TendrilSwingTicks > 0
                            ? 1f - PhaseTimer / (float)TendrilSwingTicks
                            : 1f;
                        ArmFireSlashVFX(MeleeRange, tendrilSwingProgress);
                    }

                    // An authored TendrilSwingCurve disarms it after the curve's LiveTicks (the harmless settle).
                    int elapsedTendrilSwingTicks = TendrilSwingTicks - PhaseTimer;
                    bool tendrilBladeLive = !TendrilSwingCurve.IsSet || elapsedTendrilSwingTicks <= TendrilSwingCurve.LiveTicks;
                    if (tendrilBladeLive)
                    {
                        TickBladeHit();
                    }
                    if (--PhaseTimer <= 0)
                    {
                        EnterPhase(AttackPhase.TendrilRecovery, TendrilRecoveryTicks);
                    }
                    break;

                case AttackPhase.TendrilRecovery:
                    LockAttackFacing();
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _tendrilCooldown = TendrilCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Charge-up Nova ──────────────────────────────────────────────
                case AttackPhase.NovaCharge:
                {
                    SlowDown();
                    int elapsed = NovaChargeTicks - PhaseTimer;
                    DoNovaChargeTick(elapsed, NovaChargeTicks);
                    if (--PhaseTimer <= 0)
                    {
                        DoNovaBlast();
                        EnterPhase(AttackPhase.NovaBlast, NovaBlastHoldTicks);
                    }
                    break;
                }

                case AttackPhase.NovaBlast:
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.NovaRecovery, NovaRecoveryTicks);
                    break;

                case AttackPhase.NovaRecovery:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Abyss Shard ───────────────────────────────────────────────
                case AttackPhase.AbyssShardTelegraph:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _abyssShardIndex = 0;
                        DoAbyssShardFire(0);
                        EnterPhase(AttackPhase.AbyssShardFire, AbyssShardFireTicks);
                    }
                    break;

                case AttackPhase.AbyssShardFire:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        // Same as Abyss Slash: the delay hook reads subclass variant state, so the server decides.
                        if (IsMultiplayerClient)
                        {
                            PhaseTimer = 0;
                            break;
                        }

                        int delay = NextAbyssShardDelay(_abyssShardIndex);
                        if (delay < 0)
                        {
                            // Deliberately no recovery phase - the sequence just ends and control
                            // returns immediately, letting a different attack follow right away.
                            _abyssShardCooldown = AbyssShardCooldownAfterUse;
                            EnterCasualOrIdle();
                        }
                        else
                        {
                            EnterPhase(AttackPhase.AbyssShardPause, delay);
                        }
                    }
                    break;

                case AttackPhase.AbyssShardPause:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _abyssShardIndex++;
                        DoAbyssShardFire(_abyssShardIndex);
                        EnterPhase(AttackPhase.AbyssShardFire, AbyssShardFireTicks);
                    }
                    break;

                // ── Homing Volley ─────────────────────────────────────────────
                // Backward roll WITH real i-frames (DodgeTimer) - same technique as Jumping
                // Downward Slash's wind-up, but leads into an overhead chop instead of a jump.
                case AttackPhase.HomingVolleyDodgeback:
                {
                    if (PhaseTimer == HomingVolleyDodgebackTicks)
                    {
                        int faceHv = target.Center.X < NPC.Center.X ? -1 : 1;
                        _homingVolleyDir = faceHv;
                        _attackFacingDir = faceHv;
                        NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().DodgeTimer = HomingVolleyDodgebackTicks + 5;
                    }
                    LockAttackFacing();
                    NPC.velocity.X = -_homingVolleyDir * HomingVolleyDodgebackSpeed;
                    if (--PhaseTimer <= 0)
                    {
                        _homingVolleyFired = false;
                        EnterPhase(AttackPhase.HomingVolleySwingTelegraph, HomingVolleySwingTelegraphTicks);
                    }
                    break;
                }

                case AttackPhase.HomingVolleySwingTelegraph:
                    LockAttackFacing();
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        EnterPhase(AttackPhase.HomingVolleySwing, HomingVolleySwingTicks);
                    }
                    break;

                case AttackPhase.HomingVolleySwing:
                {
                    LockAttackFacing();
                    SlowDown();
                    int elapsedSwing = HomingVolleySwingTicks - PhaseTimer;
                    DoHomingVolleySwingTick(elapsedSwing, HomingVolleySwingTicks);
                    if (!_homingVolleyFired && elapsedSwing >= HomingVolleySwingTicks * HomingVolleyFireProgress)
                    {
                        _homingVolleyFired = true;
                        DoHomingVolleyFire();
                    }
                    if (--PhaseTimer <= 0)
                    {
                        _homingVolleyCooldown = HomingVolleyCooldownAfterUse;
                        EnterPhase(AttackPhase.HomingVolleyRecovery, HomingVolleyRecoveryTicks);
                    }
                    break;
                }

                case AttackPhase.HomingVolleyRecovery:
                    LockAttackFacing();
                    SlowDown();
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Sword Launch Reposition (shared) ──────────────────────────
                // Mostly-horizontal backward hop, facing the player - same leap-physics shape as
                // Jumping Downward Slash's dodgeback, but no i-frames (a spacing hop, not a dodge).
                case AttackPhase.SwordLaunchReposition:
                {
                    if (PhaseTimer == SwordLaunchRepositionTicks)
                    {
                        int faceSl = target.Center.X < NPC.Center.X ? -1 : 1;
                        _swordLaunchDir = faceSl;
                        _attackFacingDir = faceSl;
                        NPC.velocity = new Vector2(-faceSl * SwordLaunchRepositionBackSpeed, -SwordLaunchRepositionUpSpeed);
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        NPC.velocity.X = -_swordLaunchDir * SwordLaunchRepositionBackSpeed;
                    }

                    // The hop travels BACKWARDS while still facing the player - exactly the case the
                    // navigator would "correct" by spinning the sprite to match the travel direction.
                    LockAttackFacing();

                    bool landedSl = PhaseTimer < SwordLaunchRepositionTicks && NPC.velocity.Y == 0f;
                    if (landedSl || --PhaseTimer <= 0)
                    {
                        EnterPhase(_swordLaunchNextPhase, _swordLaunchNextTicks);
                    }
                    break;
                }

                // ── Boomerang Crescent ─────────────────────────────────────────
                // The raise still tracks (the crescent's line isn't chosen yet); everything from the
                // chop onward is locked so the projectile and the sprite agree.
                case AttackPhase.BoomerangSwingTelegraph:
                    _attackFacingDir = target.Center.X < NPC.Center.X ? -1 : 1;
                    LockAttackFacing();
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _boomerangFired = false;
                        EnterPhase(AttackPhase.BoomerangSwing, BoomerangSwingTicks);
                    }
                    break;

                case AttackPhase.BoomerangSwing:
                {
                    LockAttackFacing();
                    SlowDown();
                    int elapsedBoom = BoomerangSwingTicks - PhaseTimer;
                    DoBoomerangSwingTick(elapsedBoom, BoomerangSwingTicks);
                    if (!_boomerangFired && elapsedBoom >= BoomerangSwingTicks * BoomerangFireProgress)
                    {
                        _boomerangFired = true;
                        DoBoomerangFire();
                    }
                    if (--PhaseTimer <= 0)
                    {
                        _boomerangCooldown = BoomerangCooldownAfterUse;
                        EnterPhase(AttackPhase.BoomerangRecovery, BoomerangRecoveryTicks);
                    }
                    break;
                }

                case AttackPhase.BoomerangRecovery:
                    LockAttackFacing();
                    SlowDown();
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Spiral Fan ─────────────────────────────────────────────────
                case AttackPhase.SpiralFanSwingTelegraph:
                    _attackFacingDir = target.Center.X < NPC.Center.X ? -1 : 1;
                    LockAttackFacing();
                    SlowDown();
                    SetDisplayWeapon(SpiralFanWeaponItemType, swing: false);
                    if (--PhaseTimer <= 0)
                    {
                        EnterPhase(AttackPhase.SpiralFanSwing, SpiralFanSwingTicks);
                    }
                    break;

                case AttackPhase.SpiralFanSwing:
                {
                    LockAttackFacing();
                    SlowDown();
                    SetDisplayWeapon(SpiralFanWeaponItemType, swing: false);
                    DoSpiralFanSwingTick(SpiralFanSwingTicks - PhaseTimer, SpiralFanSwingTicks);
                    if (--PhaseTimer <= 0)
                    {
                        _spiralFanIndex = 0;
                        DoSpiralFanFire(0);
                        EnterPhase(AttackPhase.SpiralFanBurst, SpiralFanFireTicks);
                    }
                    break;
                }

                case AttackPhase.SpiralFanBurst:
                    LockAttackFacing();
                    SlowDown();
                    SetDisplayWeapon(SpiralFanWeaponItemType, swing: false);
                    if (--PhaseTimer <= 0)
                    {
                        // Same as Abyss Slash: the delay hook reads subclass variant state, so the server decides.
                        if (IsMultiplayerClient)
                        {
                            PhaseTimer = 0;
                            break;
                        }

                        int delaySf = NextSpiralFanDelay(_spiralFanIndex);
                        if (delaySf < 0)
                        {
                            _spiralFanCooldown = SpiralFanCooldownAfterUse;
                            EnterPhase(AttackPhase.SpiralFanRecovery, SpiralFanRecoveryTicks);
                        }
                        else
                        {
                            EnterPhase(AttackPhase.SpiralFanPause, delaySf);
                        }
                    }
                    break;

                case AttackPhase.SpiralFanPause:
                    LockAttackFacing();
                    SlowDown();
                    SetDisplayWeapon(SpiralFanWeaponItemType, swing: false);
                    if (--PhaseTimer <= 0)
                    {
                        _spiralFanIndex++;
                        DoSpiralFanFire(_spiralFanIndex);
                        EnterPhase(AttackPhase.SpiralFanBurst, SpiralFanFireTicks);
                    }
                    break;

                case AttackPhase.SpiralFanRecovery:
                    LockAttackFacing();
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        // The chain hook is a server decision; a client predicts the plain end and adopts a
                        // chained volley from the snapshot.
                        bool chained = !IsMultiplayerClient && TryContinueSpiralFanChain();
                        if (!chained)
                        {
                            EnterCasualOrIdle();
                        }
                    }
                    break;

                // ── Fire Volley chain: backward hop, facing the player ────────
                case AttackPhase.FireVolleyBackLeap:
                {
                    if (PhaseTimer == FireVolleyBackLeapTicks)
                    {
                        int faceFv = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceFv;
                        NPC.spriteDirection = faceFv;
                        _fireVolleyDir = faceFv;
                        NPC.velocity = new Vector2(-faceFv * FireVolleyBackLeapSpeed, -FireVolleyBackLeapUpSpeed);
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        NPC.velocity.X = -_fireVolleyDir * FireVolleyBackLeapSpeed;
                    }

                    bool landedFv = PhaseTimer < FireVolleyBackLeapTicks && NPC.velocity.Y == 0f;
                    bool backLeapDone = landedFv || --PhaseTimer <= 0;
                    // The landing hook picks the next volley: server only. A client holds until the snapshot.
                    if (backLeapDone && !IsMultiplayerClient)
                    {
                        OnFireVolleyRepositionLanded();
                    }
                    break;
                }

                // ── Fire Volley chain: leaping dodge-roll through the player ──
                case AttackPhase.FireVolleyDodgeThrough:
                {
                    var globalNpcFv = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
                    if (PhaseTimer == FireVolleyDodgeThroughTimeoutTicks)
                    {
                        int faceFv = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceFv;
                        NPC.spriteDirection = faceFv;
                        NPC.velocity.Y = -FireVolleyDodgeThroughUpSpeed;
                        tsorcRevampAIs.ArmQuickStep(NPC, globalNpcFv, allowForward: true);
                        NPC.netUpdate = true;
                    }
                    tsorcRevampAIs.TickQuickStep(NPC, globalNpcFv);
                    bool resolvedFv = globalNpcFv.QuickStepTimer <= 0 && globalNpcFv.QuickStepRecoveryTimer <= 0;
                    bool dodgeThroughDone = resolvedFv || --PhaseTimer <= 0;
                    // The landing hook picks the next volley: server only. A client holds until the snapshot.
                    if (dodgeThroughDone && !IsMultiplayerClient)
                    {
                        OnFireVolleyRepositionLanded();
                    }
                    break;
                }

                // ── Fire Volley chain: big arc jump over the player ───────────
                case AttackPhase.FireVolleyArcJump:
                {
                    NPC.velocity.X = _fireVolleyArcVx;
                    if (!_fireVolleyArcFired && NPC.velocity.Y >= 0f)
                    {
                        _fireVolleyArcFired = true;
                        DoFireVolleyArcFire();
                    }
                    bool landedArc = _fireVolleyArcFired && NPC.velocity.Y == 0f && PhaseTimer < 100;
                    bool arcJumpDone = landedArc || --PhaseTimer <= 0;
                    // The landing hook picks what follows: server only. A client holds until the snapshot.
                    if (arcJumpDone && !IsMultiplayerClient)
                    {
                        OnFireVolleyArcJumpLanded();
                    }
                    break;
                }

                // ── Closing distance ──────────────────────────────────────────
                // A melee combo was chosen out of reach: the mover sprints in (boosted speedMult in AI);
                // start the combo once inside MeleeEngageRange, or give up on timeout / if the player flies
                // out of combo range (re-decide → may go ranged).
                case AttackPhase.ClosingDistance:
                {
                    int faceC = target.Center.X < NPC.Center.X ? -1 : 1;
                    NPC.direction = faceC;
                    NPC.spriteDirection = faceC;

                    // Opt-in: the melee attack is already chosen, so draw the weapon for the whole run-in.
                    // Before the client early-out so every peer shows it.
                    if (ShowMeleeWeaponWhileClosingDistance)
                    {
                        SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                    }

                    // Starting the combo or giving up is the server's call; a client just runs in.
                    if (IsMultiplayerClient)
                    {
                        break;
                    }

                    if (HoldAttackSelection)
                    {
                        EnterPhase(AttackPhase.Idle, 0);
                        break;
                    }
                    bool inReach = dist <= MeleeEngageRange && NPC.velocity.Y == 0f
                                   && NPC.Center.Y - target.Center.Y < 48f;
                    if (inReach)
                    {
                        if (!TryStartMeleeCombo(dist))
                            EnterPhase(AttackPhase.Idle, 0);
                        break;
                    }
                    if (dist > ComboMaxStartRange + 80f || --PhaseTimer <= 0)
                        EnterPhase(AttackPhase.Idle, 0);
                    break;
                }

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
                        EnterPhase(AttackPhase.Healing, HealAnimationTicks);
                    }
                    break;

                // ── Healing ───────────────────────────────────────────────────
                // Stand still and channel. The heal and flask consumption happen only if the
                // entire animation completes without a poise stagger.
                case AttackPhase.Healing:
                    NPC.velocity.X *= 0.6f; // brake to a halt
                    if (--PhaseTimer <= 0)
                    {
                        CompleteEstusHeal();
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Melee combo: telegraph (step 0) ───────────────────────────
                case AttackPhase.MeleeComboTelegraph:
                    {
                    MeleeComboStep telegraphStep = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                    int telegraphTotal = GetComboTelegraphTicks(telegraphStep);
                    OnMeleeComboTelegraphTick(
                        _activeMeleeCombo,
                        telegraphStep,
                        Math.Max(0, telegraphTotal - PhaseTimer),
                        telegraphTotal);
                    // Keep turning to the player until the telegraph flash (the last TelegraphFacingCommitTicks),
                    // then commit. Rolling through early just gets tracked; only a roll after the flash ends up
                    // behind the swing. A 30-tick telegraph (the minimum) commits from its first tick as before.
                    // Same 8px dead zone as the neutral re-face, so an overlapping player can't flip it each tick.
                    {
                        float horizontalGap = target.Center.X - NPC.Center.X;
                        bool beforeFlash = PhaseTimer > TelegraphFacingCommitTicks
                            && TrackMeleeComboFacingDuringTelegraph(_activeMeleeCombo);
                        bool clearlyToOneSide = Math.Abs(horizontalGap) > NeutralRefaceDeadZone;
                        if (beforeFlash && clearlyToOneSide)
                        {
                            _comboLockedDir = Math.Sign(horizontalGap);
                        }
                    }
                    LockComboDirection();
                    // Per-move brake for aim-swing pilots (0 = keep momentum); blanket SlowDown otherwise.
                    if (SlowDownBeforeMelee)
                    {
                        if (AimSwingActive)
                        {
                            NPC.velocity.X *= (1f - _activeMeleeCombo.MoveBrake);
                        }
                        else
                        {
                            SlowDown();
                        }
                    }
                    ApplyComboTelegraphPressure(target);
                    // MeleeWeaponItemType, not FrontHandWeaponType: the latter reads FrontHandWeapon,
                    // which is built ONCE (lazily, on first access) and then cached for the NPC's whole
                    // lifetime — see DefaultFrontWeapon above. That's fine for a puppet with one fixed
                    // melee weapon, but Dread Wraith picks mace vs. glaive per combo, so the cached value
                    // is frozen to whichever one happened to be active the very first time any code read
                    // FrontHandWeapon, and every combo telegraph after that (mace OR spear) would display
                    // that same frozen weapon regardless of which combo was actually running.
                    SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                    CheckAndFireFlash(_activeMeleeCombo.InitialFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        var step0 = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                        BeginComboStepAttack(step0);
                    }
                    break;
                    }

                // ── Melee combo: active hitbox frame ──────────────────────────
                case AttackPhase.MeleeComboAttack:
                {
                    LockComboDirection();
                    var step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                    OnMeleeComboAttackTick(
                        _activeMeleeCombo,
                        step,
                        Math.Max(0, _activeComboStepTotalTicks - PhaseTimer),
                        Math.Max(1, _activeComboStepTotalTicks));
                    bool endStep;

                    if (step.Motion == ComboMotion.LeapSlam || step.Motion == ComboMotion.LeapThrust)
                    {
                        // Hold the launch velocity (SF4 ran earlier this tick and would otherwise
                        // steer X). Optional correction is limited to ascent; gravity and the locked
                        // descent preserve a committed, dodgeable landing line.
                        // An aerial dive is already solved onto the target; ascent tracking would bend it.
                        if (_comboLeapLaunched && NPC.velocity.Y < 0f && !_leapLaunchedFromAir)
                        {
                            UpdateLeapAscentTracking(target, step, 0f,
                                LeapAttackAscentTrackingStrength);
                            _comboLeapAscentTicks++;
                        }
                        NPC.velocity.X = _comboLeapVx;
                        // velocity.Y only returns to exactly 0 via a vertical collision (landing).
                        // Require a few ticks of airtime first so a launch that can't clear a low
                        // ceiling doesn't read as an instant landing.
                        bool landed = _comboLeapLaunched && NPC.velocity.Y == 0f
                            && (UseLandingTimedLeapSlam
                                ? PhaseTimer <= _activeComboStepTotalTicks - 6
                                    && Collision.TileCollision(NPC.position, Vector2.UnitY,
                                        NPC.width, NPC.height).Y < 1f
                                : PhaseTimer < 86);
                        bool landingTimedSlam = UseLandingTimedLeapSlam && step.Motion == ComboMotion.LeapSlam;

                        // In-range strike (opt-in via LeapStrikeRange): once past the apex with the
                        // target close, swing now rather than on the landing, and arm the swept blade.
                        bool pastApex = _comboLeapLaunched && NPC.velocity.Y >= 0f && !landed;

                        // Apex retarget (opt-in via LeapApexRetargetStrength): the instant the arc
                        // tips from rising to falling, re-aim the locked descent velocity at the
                        // target's position right now instead of the launch-time lead-predicted spot,
                        // so a long telegraph doesn't guarantee a walked-out-of-the-way whiff. Fires once.
                        // Apex retarget and descent gravity reshape a jump arc; an aerial dive was
                        // solved as one ballistic line, so both would throw it off the target.
                        bool jumpArc = !_leapLaunchedFromAir;

                        if (jumpArc && pastApex && !_leapApexRetargeted && step.LeapApexRetargetStrength > 0f)
                        {
                            _leapApexRetargeted = true;
                            RetargetLeapAtApex(target, step);
                        }

                        // Descent gravity boost (opt-in via LeapDescentGravityMult): speeds up only
                        // the fall from apex to landing, so a high telegraphing jump still slams down
                        // fast rather than hanging in the air for the player to simply walk away from.
                        if (jumpArc && pastApex && step.LeapDescentGravityMult > 0f && step.LeapDescentGravityMult != 1f)
                            NPC.velocity.Y += 0.3f * (step.LeapDescentGravityMult - 1f);

                        // A dive at a raised target is still rising when it arrives, so an aerial
                        // launch may strike before the apex; a jump waits for the apex as before.
                        bool strikeWindowOpen = pastApex
                            || (_leapLaunchedFromAir && _comboLeapLaunched && !landed);
                        bool canStrikeInAir = landingTimedSlam && step.LeapStrikeRange > 0f
                            && !_leapStrikeStarted && _leapSlamSwingProgress <= 0f && strikeWindowOpen;
                        if (canStrikeInAir && NPC.Distance(target.Center) <= step.LeapStrikeRange)
                        {
                            _leapStrikeStarted = true;
                            DoComboMeleeHit(step);
                            PlayMeleeSwingSound();
                        }

                        if (landingTimedSlam && _leapLaunchedFromAir && !_leapStrikeStarted)
                        {
                            // Aim the aerial slam: rotate the whole carry/downswing arc so its contact
                            // pose points from the hand at the target. Swing-space rotation maps 1:1 onto
                            // facing-space angle (both facings), so the angle gap IS the offset. Eased
                            // 0.25/tick during the carry; frozen once the strike starts (calibrated swing).
                            float contactPose = MathHelper.Lerp(LeapSlamCarryRotation, LeapSlamImpactRotation,
                                AerialSlamContactFraction);
                            Vector2 contactDirection = GetMeleeWorldDirection(contactPose, FrontHandWeapon);
                            float contactAngle = (float)Math.Atan2(contactDirection.Y, contactDirection.X * NPC.direction);
                            Vector2 handToTarget = target.Center - GetHandPosition();
                            float targetAngle = (float)Math.Atan2(handToTarget.Y, handToTarget.X * NPC.direction);
                            float desiredOffset = MathHelper.WrapAngle(targetAngle - contactAngle);
                            desiredOffset = MathHelper.Clamp(desiredOffset, -AerialSlamMaxAimRaise, AerialSlamMaxAimDrop);
                            _leapSlamAimOffset = MathHelper.Lerp(_leapSlamAimOffset, desiredOffset, 0.25f);
                        }

                        if (landingTimedSlam)
                        {
                            UpdateLeapSlamPose(landed);
                            if (_leapSlamSwingProgress > 0f)
                                OnLandingTimedLeapSlamSwingTick(step, _leapSlamSwingProgress);
                        }

                        // The predicted-landing downswing starts ~10 ticks BEFORE touchdown, so the
                        // visible strike used to play with the blade unarmed and only one static check
                        // ran at the final landing pose (telemetry: 0 armed ticks on every landing slam,
                        // hits only when standing exactly where the axe ended). Arm the swept blade the
                        // tick the downswing starts, same as an in-range air strike; the landing then
                        // adds no second hit (the endStep check below skips once _leapStrikeStarted).
                        if (landingTimedSlam && _leapSlamSwingProgress > 0f && !_leapStrikeStarted)
                        {
                            _leapStrikeStarted = true;
                            DoComboMeleeHit(step);
                        }

                        if (_leapStrikeStarted)
                            TickBladeHit();

                        endStep = (--PhaseTimer <= 0) || landed;
                        if (endStep)
                        {
                            // Deferred single-hit: arm the blade check, then resolve it the same
                            // tick against the weapon's actual landing pose — a slam/thrust still
                            // only connects if the sprite is really overlapping the target here,
                            // it just doesn't need to be checked every tick like a sweeping arc.
                            // An in-air strike already swept its hit, so the landing adds none.
                            if ((!landingTimedSlam || landed) && !_leapStrikeStarted)
                            {
                                DoComboMeleeHit(step);
                                TickBladeHit();
                            }
                            if (landingTimedSlam && landed)
                                OnLeapSlamLanded(step);
                            _comboLeapLaunched = false;
                            _leapStrikeStarted = false;
                        }
                    }
                    else if (step.Motion == ComboMotion.BackstepRaise)
                    {
                        NPC.velocity.X = _comboLeapVx;
                        bool enoughAirTime = PhaseTimer <= _activeComboStepTotalTicks - 6;
                        bool landed = enoughAirTime && _comboLeapLaunched && NPC.velocity.Y == 0f;
                        endStep = (--PhaseTimer <= 0) || landed;
                        if (endStep)
                            _comboLeapLaunched = false;
                    }
                    else if (step.Motion == ComboMotion.ApexDiveCleave)
                    {
                        if (!_apexDiveStrikeStarted && NPC.velocity.Y < 0f)
                        {
                            // Limited ascent tracking. Facing and horizontal velocity lock at apex,
                            // leaving the final descent readable and dodgeable.
                            UpdateLeapAscentTracking(target, step, 1.8f,
                                ApexDiveAscentTrackingStrength);
                            NPC.velocity.X = _comboLeapVx;
                        }
                        else
                        {
                            if (!_apexDiveStrikeStarted)
                            {
                                _apexDiveStrikeStarted = true;
                                _apexDiveFallTicks = 0;
                                _comboLeapVx = NPC.velocity.X;
                                DoComboMeleeHit(step);
                                PlayMeleeSwingSound();
                                NPC.netUpdate = true;
                            }

                            _apexDiveFallTicks++;
                            NPC.velocity.X = _comboLeapVx;
                            TickBladeHit();
                        }

                        bool landed = _apexDiveStrikeStarted && NPC.velocity.Y == 0f && _apexDiveFallTicks > 2;
                        endStep = (--PhaseTimer <= 0) || landed;
                        if (endStep)
                        {
                            _bladeArmed = false;
                            _comboLeapLaunched = false;
                            _apexDiveStrikeStarted = false;
                        }
                    }
                    else if (step.Motion == ComboMotion.RisingUppercutLeap)
                    {
                        // The uppercut itself is dangerous only while the axe is visibly sweeping.
                        // Afterward the pose remains extended overhead, but is no longer an active hit.
                        NPC.velocity.X = _comboLeapVx;
                        int elapsed = Math.Max(0, step.AttackTicks - PhaseTimer);
                        if (elapsed <= GetWeaponUseAnimation(MeleeWeaponItemType))
                            TickBladeHit();
                        else
                        {
                            _bladeArmed = false;
                            _hasPreviousBladeSample = false;
                            _backHand.HasPreviousBladeSample = false;
                        }

                        if (_comboLeapLaunched && NPC.velocity.Y >= 0f)
                            _risingUppercutFallHoldTimer++;

                        endStep = (--PhaseTimer <= 0) || _risingUppercutFallHoldTimer >= 30;
                        if (endStep)
                        {
                            _bladeArmed = false;
                            _comboLeapLaunched = false;
                            _risingUppercutFallHoldTimer = 0;
                        }
                    }
                    else if (step.Motion == ComboMotion.ThrownWeaponRetrieve)
                    {
                        bool retrieved = TickThrownWeaponRetrieve();
                        endStep = (--PhaseTimer <= 0) || retrieved;
                    }
                    else if (step.Motion == ComboMotion.ChargeChop)
                    {
                        // Run straight at the player, re-facing each tick.  No hit here — the
                        // following OverheadArc step is the chop.  Ends on reach OR the timeout
                        // (PhaseTimer = AttackTicks) so it always completes and resets to idle.
                        int faceDir = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceDir;
                        NPC.spriteDirection = faceDir;
                        _comboLockedDir = faceDir;
                        NPC.velocity.X = faceDir * (TopSpeed * ChargeAttackSpeedMult);
                        bool inReach = NPC.Distance(target.Center) <= MeleeRange * 0.9f;
                        endStep = (--PhaseTimer <= 0) || inReach;
                    }
                    else if (step.Motion == ComboMotion.LowAxeRun)
                    {
                        // The low trailing carry is a readable moving telegraph, not a hit. Sprint
                        // until the player is close enough for the rising uppercut to begin.
                        int faceDir = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceDir;
                        NPC.spriteDirection = faceDir;
                        _comboLockedDir = faceDir;
                        NPC.velocity.X = faceDir * (TopSpeed * ChargeAttackSpeedMult);
                        bool inUppercutRange = NPC.Distance(target.Center) <= MeleeRange * 1.05f;
                        endStep = (--PhaseTimer <= 0) || inUppercutRange;
                    }
                    else if (step.Motion == ComboMotion.Feint)
                    {
                        // Hold the raised bait pose without swinging; the real chop is the next step.
                        if (SlowDownBeforeMelee)
                        {
                            NPC.velocity.X *= 0.6f;
                        }
                        endStep = (--PhaseTimer <= 0);
                    }
                    else
                    {
                        // Past the step's HitWindowEnd the blade is only following through (a long
                        // Weighted ease-out), so disarm it rather than let a settling sword deal damage.
                        // Elapsed is measured before this tick's PhaseTimer decrement below.
                        float stepProgress = (_activeComboStepTotalTicks - PhaseTimer) / (float)Math.Max(1, _activeComboStepTotalTicks);
                        bool followingThrough = step.HitWindowEnd > 0f && stepProgress > step.HitWindowEnd;
                        if (followingThrough)
                        {
                            _bladeArmed = false;
                            _hasPreviousBladeSample = false;
                            _backHand.HasPreviousBladeSample = false;
                        }
                        else
                        {
                            TickBladeHit();
                        }

                        if (step.ForwardPushMult > 0f)
                            NPC.velocity.X = _comboLockedDir * (ComboForwardPushTopSpeed * step.ForwardPushMult);
                        else if (SlowDownBeforeMelee && AimSwingActive)
                            NPC.velocity.X *= (1f - _activeMeleeCombo.MoveBrake); // per-move brake (0 = keep drifting)
                        else if (SlowDownBeforeMelee)
                            NPC.velocity.X *= 0.65f;
                        endStep = (--PhaseTimer <= 0);
                    }

                    if (endStep)
                    {
                        if (step.Motion == ComboMotion.DoubleSpinSlam)
                            _weaponRotation = 1.4f;
                        if (step.Motion == ComboMotion.ThrownWeaponRetrieve)
                            FinishThrownWeaponRetrieve();
                        OnComboStepCompleted(step);

                        int nextIdx = _meleeComboStepIndex + 1;
                        _bladeArmed = false;
                        _hasPreviousBladeSample = false;
                        _backHand.HasPreviousBladeSample = false;
                        bool hasNextStep = nextIdx < _activeMeleeCombo.Steps.Length;
                        // Continuing is a server decision (it may read the server-only blade result). A client
                        // predicts the authored continuation and adopts the recovery if the server refuses.
                        bool continueCombo = hasNextStep;
                        if (hasNextStep && !IsMultiplayerClient)
                        {
                            continueCombo = ShouldContinueMeleeCombo(
                                _activeMeleeCombo.Name,
                                nextIdx,
                                target,
                                _currentComboStepHitConnected);
                        }
                        if (continueCombo)
                            EnterPhase(AttackPhase.MeleeComboPause, Math.Max(1, step.PostStepPause));
                        else
                            EnterPhase(AttackPhase.MeleeComboRecovery, ActiveComboRecoveryTicks);
                    }
                    break;
                }

                // ── Melee combo: inter-step pause ─────────────────────────────
                // Direction UNLOCKED during the pause: if the player dodgerolled through,
                // the puppet can re-face them so the next step swings the correct way.
                // The next step's lock direction is captured just before its attack fires.
                case AttackPhase.MeleeComboPause:
                    if (SlowDownBeforeMelee)
                    {
                        if (AimSwingActive)
                        {
                            NPC.velocity.X *= (1f - _activeMeleeCombo.MoveBrake);
                        }
                        else
                        {
                            SlowDown();
                        }
                    }
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
                        MeleeComboStep nextStep = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                        // Spacing-based step edits run on the server; the edited step reaches clients in the
                        // combo copy every snapshot carries.
                        if (!IsMultiplayerClient)
                        {
                            ModifyNextMeleeComboStep(
                                _activeMeleeCombo.Name,
                                _meleeComboStepIndex,
                                target,
                                ref nextStep);
                            _activeMeleeCombo.Steps[_meleeComboStepIndex] = nextStep;
                        }
                        _currentComboStepHitConnected = false;
                        BeginComboStepAttack(nextStep);
                    }
                    break;

                // ── Melee combo: final recovery ───────────────────────────────
                // Facing stays locked through recovery. It used to unlock the instant the last
                // active frame ended, which snapped a heavy greatsword's follow-through around to
                // face a player who had already rolled behind — the swing "landed" in one direction
                // and finished in the other. Re-orientation resumes at EnterCasualOrIdle.
                case AttackPhase.MeleeComboRecovery:
                    LockComboDirection();
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
            _attackFacingDir = _comboLockedDir;
            LockAttackFacing();
        }

        /// <summary>
        /// Re-assert <see cref="_attackFacingDir"/> onto the NPC, sprite included. Called every tick
        /// of a committed attack — wind-up, active frames AND recovery — because the navigator ran
        /// earlier this tick and steers toward the player unconditionally. Also refreshes the
        /// anti-bounce hold so the flip stays suppressed for a few ticks after the attack releases,
        /// instead of the sprite snapping around on the very frame the last swing ends.
        /// No-op when nothing is committed (dir 0).
        /// </summary>
        // Ranged phases always face the target. Re-asserted every tick because the mover (and its
        // 30-tick anti-flip hold) runs first and could leave the body turned away as a shot starts.
        // Same 8px dead zone as the neutral re-face, so an overlapping player can't flip it each tick.
        private void FaceTargetForRangedShot(Player target)
        {
            float horizontalGap = target.Center.X - NPC.Center.X;

            if (Math.Abs(horizontalGap) > NeutralRefaceDeadZone)
            {
                _attackFacingDir = Math.Sign(horizontalGap);
            }

            LockAttackFacing();
        }

        private void LockAttackFacing()
        {
            if (_attackFacingDir == 0)
            {
                return;
            }

            NPC.direction       = _attackFacingDir;
            NPC.spriteDirection = _attackFacingDir;
            _directionHoldTicks = Math.Max(_directionHoldTicks, 5);
        }

        private void ApplyComboTelegraphPressure(Player target)
        {
            if (ComboTelegraphAdvanceSpeedMult <= 0f || _activeMeleeCombo.RangedStartOnly)
                return;

            float signedDistance = target.Center.X - NPC.Center.X;
            int targetSide = Math.Sign(signedDistance);
            // Do not rotate or chase through the player after commitment. That crossing remains
            // the intended answer to a telegraphed swing; pressure only punishes backing away.
            if (targetSide == 0 || targetSide != _comboLockedDir
                || Math.Abs(signedDistance) <= ComboTelegraphAdvanceStopDistance)
            {
                return;
            }

            float desiredVelocity = _comboLockedDir * TopSpeed * ComboTelegraphAdvanceSpeedMult;
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVelocity, 0.22f);
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

            ModifyRangedBurst(
                useSecondary,
                ref _activeRangedItemType,
                ref _activeRangedStyle,
                ref _activeRangedFlashColor,
                ref _activeRangedTelegraphTicks,
                ref _activeRangedAttackTicks,
                ref _activeRangedRecoveryTicks);

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

            // ── Ranged burst-pattern selection (primary or secondary) ────────────
            // When a ranged burst pattern is defined the classic _rangedShotsRemaining
            // counter is replaced by a pause-array that drives the shot sequence.
            _interShotPauses     = null;
            _interShotPauseIndex = 0;
            _activeBurstPatternIndex = -1;

            int[][] patternPool = useSecondary ? SecondaryRangedBurstPatterns : PrimaryRangedBurstPatterns;
            int[] patternChances = useSecondary ? SecondaryRangedBurstChances : PrimaryRangedBurstChances;
            int[] telegraphExtras = useSecondary ? SecondaryRangedBurstTelegraphExtras : PrimaryRangedBurstTelegraphExtras;
            Color[] flashColors = useSecondary ? SecondaryRangedBurstFlashColors : PrimaryRangedBurstFlashColors;

            if (shotsOverride < 0   // override skips patterns (aerial single-shot)
                && patternPool != null
                && patternPool.Length > 0)
            {
                int patIdx = PickPatternByChance(patternChances, patternPool.Length);
                _interShotPauses = patternPool[patIdx];
                _activeBurstPatternIndex = patIdx;

                if (telegraphExtras != null && patIdx < telegraphExtras.Length)
                    _activeRangedTelegraphTicks += telegraphExtras[patIdx];

                if (flashColors != null && patIdx < flashColors.Length)
                    _activeRangedFlashColor = flashColors[patIdx];
            }

            OnRangedBurstStarted(useSecondary); // e.g. lit-fuse cue when the weapon appears in hand
        }

        private bool TryStartQueuedRangedFollowUp()
        {
            // Consuming the queue checks line of sight and range: a server decision.
            if (IsMultiplayerClient)
            {
                return false;
            }

            if (_queuedRangedFollowupSlot < 0 || _queuedRangedFollowupTicks <= 0
                || NPC.velocity.Y != 0f)
            {
                return false;
            }

            int targetIndex = _queuedRangedFollowupTarget >= 0
                ? _queuedRangedFollowupTarget
                : NPC.target;
            if (targetIndex < 0 || targetIndex >= Main.maxPlayers)
                return false;

            Player target = Main.player[targetIndex];
            if (!target.active || target.dead
                || !Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
            {
                return false;
            }

            bool useSecondary = _queuedRangedFollowupSlot == 1;
            int itemType = useSecondary ? SecondaryRangedWeaponItemType : RangedWeaponItemType;
            if (itemType < 0 || (useSecondary && !SecondaryRangedAvailable))
            {
                _queuedRangedFollowupSlot = -1;
                _queuedRangedFollowupTicks = 0;
                _queuedRangedFollowupTarget = -1;
                return false;
            }

            float maxRange = useSecondary ? SecondaryRangedRange : RangedRange;
            if (NPC.Distance(target.Center) > maxRange + 80f)
                return false;

            NPC.target = targetIndex;
            _queuedRangedFollowupSlot = -1;
            _queuedRangedFollowupTicks = 0;
            _queuedRangedFollowupTarget = -1;
            SetupRangedBurst(useSecondary, shotsOverride: 1, forceStanding: true);
            EnterPhase(AttackPhase.RangedTelegraph, _activeRangedTelegraphTicks);
            return true;
        }

        /// <summary>
        /// Lazy-init the melee combo pool and per-combo cooldown array for the active archetype.
        /// </summary>
        private void EnsureMeleeComboPool()
        {
            if (_meleeComboPool != null)
            {
                return;
            }
            _meleeComboPool = MeleeComboPoolOverride ?? WeaponArchetypeTables.GetMeleeCombos(MeleeArchetype);
            if (_meleeComboPool != null)
                _meleeComboCooldowns = new int[_meleeComboPool.Length];
        }

        private void EnsureRangedComboPool()
        {
            if (_rangedComboPool != null)
            {
                return;
            }
            _rangedComboPool = WeaponArchetypeTables.GetRangedCombos(RangedArchetype);
            if (_rangedComboPool != null)
                _rangedComboCooldowns = new int[_rangedComboPool.Length];
        }

        /// <summary>
        /// Try to start a melee combo from the active archetype's pool.  Selection is
        /// range-aware (close/mid/far bands) and HP-aware (heavy combos weighted up as HP drops).
        /// Returns true if a combo started; false if no eligible combo (cooldowns/weights).
        /// </summary>
        protected bool TryStartMeleeCombo(float dist, bool rangedStartOnly = false, bool airborneStart = false)
        {
            // Combo selection is server-authoritative; a client adopts the chosen combo, with its customized
            // steps, from the snapshot. Returning false lets any caller fall through safely.
            if (IsMultiplayerClient)
            {
                return false;
            }

            // Ground combos never start from wing flight: the flight controller owns velocity and facing,
            // so the swing plays from a hovering body with no aim. Only AirborneStartOnly combos may.
            bool inWingFlight = _flight != null && _flight.IsAirborne;
            if (inWingFlight && !airborneStart)
            {
                return false;
            }

            EnsureMeleeComboPool();
            if (_meleeComboPool == null || _meleeComboPool.Length == 0)
            {
                return false;
            }

            // Filter pool to combos whose cooldown is ready; rebuild weights array.
            float hpFrac = (float)NPC.life / NPC.lifeMax;
            float closeMax = MeleeRange + 30f;
            float midMax   = StabRange + 60f;

            // Walk the picker but with cooldown filter: temporarily zero the BaseWeight
            // of any combo on cooldown.  We use a stack array of effective weights.
            int total = 0;
            int[] effective = new int[_meleeComboPool.Length];
            ComboRangeBand band = ComboRangeBand.Far;

            if (dist <= closeMax)
            {
                band = ComboRangeBand.Close;
            }
            else if (dist <= midMax)
            {
                band = ComboRangeBand.Mid;
            }

            // Heavier combos get weighted up as the puppet loses health.
            float heavyMult = 1.0f;

            if (hpFrac <= 0.33f)
            {
                heavyMult = 2.5f;
            }
            else if (hpFrac <= 0.66f)
            {
                heavyMult = 1.5f;
            }


            for (int i = 0; i < _meleeComboPool.Length; i++)
            {
                if (_meleeComboCooldowns[i] > 0)
                {
                    effective[i] = 0;
                    continue;
                }
                if (!CanSelectMeleeCombo(_meleeComboPool[i], dist, hpFrac))
                {
                    effective[i] = 0;
                    continue;
                }
                if (_meleeComboPool[i].RangedStartOnly != rangedStartOnly)
                {
                    effective[i] = 0;
                    continue;
                }
                if (_meleeComboPool[i].AirborneStartOnly != airborneStart)
                {
                    effective[i] = 0;
                    continue;
                }
                float w = _meleeComboPool[i].BaseWeight;
                w *= (_meleeComboPool[i].Preferred == band
                      || _meleeComboPool[i].Preferred == ComboRangeBand.Any) ? 2.0f : 0.4f;
                if (_meleeComboPool[i].HeavyCommit)
                {
                    w *= heavyMult;
                }
                effective[i] = (int)w;
                total += effective[i];
            }
            if (total <= 0)
            {
                return false;
            }

            // Reactive first: let a boss pick from the player's live state (dodgeroll direction,
            // launched, flanking).  Only honoured if it names a ready combo; else weighted roll.
            int chosen = ReactiveComboIndex(dist, band, effective);
            if (chosen < 0 || chosen >= _meleeComboPool.Length || effective[chosen] <= 0)
            {
                int roll = Main.rand.Next(total);
                int cumulative = 0;
                chosen = -1;
                for (int i = 0; i < _meleeComboPool.Length; i++)
                {
                    cumulative += effective[i];
                    if (effective[i] > 0 && roll < cumulative)
                    {
                        chosen = i;
                        break;
                    }
                }
            }
            if (chosen < 0)
            {
                return false;
            }

            _activeMeleeComboIndex = chosen;
            _activeMeleeCombo      = _meleeComboPool[chosen];
            if (_activeMeleeCombo.Steps != null)
                _activeMeleeCombo.Steps = (MeleeComboStep[])_activeMeleeCombo.Steps.Clone();
            CustomizeMeleeCombo(ref _activeMeleeCombo, hpFrac);
            OnMeleeComboStarted(_activeMeleeCombo);
            _meleeComboStepIndex   = 0;
            _lastAttackHitConnected = false;
            _currentComboStepHitConnected = false;

            if (_activeMeleeCombo.RuntimeV2Clip != null)
            {
                StartAttackRuntimeV2(_activeMeleeCombo.RuntimeV2Clip);
                return true;
            }

            Player target = Main.player[NPC.target];
            _comboLockedDir = GetMeleeComboStartFacing(_activeMeleeCombo, target) < 0 ? -1 : 1;
            NPC.direction = _comboLockedDir;
            NPC.spriteDirection = _comboLockedDir;
            ArmSwingVariation(NPC.HasValidTarget ? NPC.Center.Y - Main.player[NPC.target].Center.Y : 0f);
            // Minimum 30 ticks (0.5 s) so the wind-up arc has visible time to play.
            // Lighter combos that have raw values below 22 (×1.35 → ~30) get floored here.
            int telegraphTicks = GetComboTelegraphTicks(_activeMeleeCombo.Steps[0]);
            EnterPhase(AttackPhase.MeleeComboTelegraph, telegraphTicks);
            return true;
        }

        private void StartAttackRuntimeV2(PuppetAttackClip clip)
        {
            Player target = Main.player[NPC.target];
            _comboLockedDir = target.Center.X < NPC.Center.X ? -1 : 1;
            NPC.direction = _comboLockedDir;
            NPC.spriteDirection = _comboLockedDir;

            _attackRuntimeV2.Start(clip, _comboLockedDir);
            _runtimeHasPreviousBladeSample = false;
            _bladeArmed = false;
            SetDisplayWeapon(FrontHandWeaponType, swing: false);
            _weaponAnimMax = clip.ActiveTicks;
            _weaponAnim = clip.ActiveTicks;
            EnterPhase(AttackPhase.MeleeComboTelegraph, clip.WindupTicks);
            _weaponVisible = true;

            if (Main.netMode == NetmodeID.Server)
                NPC.netUpdate = true;
        }

        private void TickAttackRuntimeV2(Player target)
        {
            PuppetAttackStage stageBeforeAdvance = _attackRuntimeV2.Stage;
            LockComboDirection();
            _weaponVisible = true;

            if (SlowDownBeforeMelee && _attackRuntimeV2.Stage != PuppetAttackStage.Recovery)
                NPC.velocity.X *= 1f - _activeMeleeCombo.MoveBrake;

            // A V2 clip owns its own bounded correction and aim-lock contract. Do not make that
            // contract depend on the legacy archetype opt-in used by the old combo renderer.
            _attackRuntimeV2.UpdateAim(NPC.Center, target.Center);
            _weaponRotation = _attackRuntimeV2.SampleRotation();
            PhaseTimer = _attackRuntimeV2.TicksRemaining;

            switch (_attackRuntimeV2.Stage)
            {
                case PuppetAttackStage.Windup:
                    Phase = AttackPhase.MeleeComboTelegraph;
                    OnMeleeComboTelegraphTick(
                        _activeMeleeCombo,
                        _activeMeleeCombo.Steps[0],
                        _attackRuntimeV2.StageTick,
                        _attackRuntimeV2.Clip.WindupTicks);
                    ApplyComboTelegraphPressure(target);
                    CheckAndFireFlash(_activeMeleeCombo.InitialFlashColor);
                    _weaponAnim = _weaponAnimMax;
                    _runtimeHasPreviousBladeSample = false;
                    break;
                case PuppetAttackStage.Active:
                    Phase = AttackPhase.MeleeComboAttack;
                    _weaponAnimMax = _attackRuntimeV2.Clip.ActiveTicks;
                    _weaponAnim = _attackRuntimeV2.TicksRemaining;
                    MeleeComboStep activeStep = _activeMeleeCombo.Steps[0];
                    OnMeleeComboAttackTick(
                        _activeMeleeCombo,
                        activeStep,
                        _attackRuntimeV2.StageTick,
                        _attackRuntimeV2.Clip.ActiveTicks);
                    if (activeStep.ForwardPushMult > 0f)
                        NPC.velocity.X = _comboLockedDir * TopSpeed * activeStep.ForwardPushMult;
                    TickRuntimeV2BladeHit();
                    break;
                case PuppetAttackStage.Recovery:
                    Phase = AttackPhase.MeleeComboRecovery;
                    _weaponAnim = 0;
                    _runtimeHasPreviousBladeSample = false;
                    break;
            }

            bool crossedBoundary = _attackRuntimeV2.Advance(out bool completed);
            if (!crossedBoundary)
                return;

            if (completed)
            {
                if (_meleeComboCooldowns != null
                    && _activeMeleeComboIndex >= 0
                    && _activeMeleeComboIndex < _meleeComboCooldowns.Length)
                {
                    _meleeComboCooldowns[_activeMeleeComboIndex] = _activeMeleeCombo.CooldownAfterUse;
                }

                CancelAttackRuntimeV2(clearCombo: true);
                EnterCasualOrIdle();
            }
            else if (stageBeforeAdvance == PuppetAttackStage.Windup)
            {
                EnterPhase(AttackPhase.MeleeComboAttack, _attackRuntimeV2.Clip.ActiveTicks);
                SetDisplayWeapon(FrontHandWeaponType, swing: true);
                _weaponAnimMax = _attackRuntimeV2.Clip.ActiveTicks;
                _weaponAnim = _weaponAnimMax;
                PlayMeleeSwingSound();
                _runtimeHasPreviousBladeSample = false;
            }
            else
            {
                EnterPhase(AttackPhase.MeleeComboRecovery, _attackRuntimeV2.Clip.RecoveryTicks);
                _bladeArmed = false;
                _runtimeHasPreviousBladeSample = false;
            }

            if (Main.netMode == NetmodeID.Server)
                NPC.netUpdate = true;
        }

        private void TickRuntimeV2BladeHit()
        {
            MeleeComboStep step = _activeMeleeCombo.Steps[0];
            float reach = ComboReachBase * 0.7f * step.ReachMult;
            Vector2 currentOrigin = GetHandPosition();
            Vector2 currentTip = currentOrigin + GetWeaponWorldDirection() * reach;

            if (!_runtimeHasPreviousBladeSample)
            {
                _runtimePreviousBladeOrigin = currentOrigin;
                _runtimePreviousBladeTip = currentTip;
                _runtimeHasPreviousBladeSample = true;
            }

            if (_attackRuntimeV2.HitWindowOpen && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float bladeWidth = FrontHandWeapon.BladeWidth;
                float earlyOutRange = reach + bladeWidth + 40f;
                if (HasSpectralOverlay)
                    earlyOutRange += Vector2.Distance(NPC.Center, currentOrigin);

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (!player.active || player.dead || _attackRuntimeV2.HasHitPlayer(i))
                        continue;
                    if (Vector2.DistanceSquared(player.Center, NPC.Center) > earlyOutRange * earlyOutRange)
                        continue;

                    bool intersects = MeleeBladeCollision.SweptSegmentIntersectsRect(
                        _runtimePreviousBladeOrigin,
                        _runtimePreviousBladeTip,
                        currentOrigin,
                        currentTip,
                        bladeWidth,
                        player.getRect(),
                        out Vector2 hitOrigin,
                        out Vector2 hitTip);

                    if (!intersects || !_attackRuntimeV2.TryRecordHit(i))
                        continue;

                    Vector2 hitPoint = MeleeBladeCollision.ClosestPointOnSegment(hitOrigin, hitTip, player.Center);
                    SpawnMeleeHitbox(
                        hitPoint,
                        bladeWidth,
                        (int)(MeleeDamage * step.DamageMult),
                        3f);
                    _lastAttackHitConnected = true;
                    _currentComboStepHitConnected = true;
                    OnBladeHit(player);
                }
            }

            _runtimePreviousBladeOrigin = currentOrigin;
            _runtimePreviousBladeTip = currentTip;
        }

        private void CancelAttackRuntimeV2(bool clearCombo)
        {
            if (!_attackRuntimeV2.Active && _attackRuntimeV2.Clip == null)
                return;

            _attackRuntimeV2.Cancel();
            _runtimeHasPreviousBladeSample = false;
            _bladeArmed = false;
            _weaponVisible = false;
            if (clearCombo)
                _activeMeleeComboIndex = -1;
            if (Main.netMode == NetmodeID.Server)
                NPC.netUpdate = true;
        }

        private int GetComboTelegraphTicks(MeleeComboStep step)
            => Math.Max(MinComboTelegraphTicks, (int)(step.TelegraphTicks * ComboTelegraphMultiplier));

        /// <summary>
        /// Spawn the damage hitbox for the current combo step.  Default applies the step's
        /// DamageMult and ReachMult on top of the puppet's MeleeRange × 0.7 base reach.
        /// Subclasses can override for per-step VFX/sounds without losing the scaling.
        /// </summary>
        protected virtual void DoComboMeleeHit(MeleeComboStep step)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            float reachBase = ComboReachBase;
            int baseDamage = MeleeDamage;

            // Dual wield: a back-hand-only step strikes with the back weapon's own reach and damage when it
            // sets them (negative = inherit the puppet's values). A Both step keeps the front weapon's numbers.
            bool backHandOnly = DualWieldActive && step.Hand == ComboHand.Back;
            if (backHandOnly && BackHandWeapon.ReachBase >= 0f)
            {
                reachBase = BackHandWeapon.ReachBase;
            }
            if (backHandOnly && BackHandWeapon.Damage >= 0)
            {
                baseDamage = BackHandWeapon.Damage;
            }

            float reach = reachBase * 0.7f * step.ReachMult;
            int   dmg   = (int)(baseDamage * step.DamageMult);
            if (dmg <= 0)
                return;
            // Arms the tracked blade check (see TickBladeHit) instead of hitting immediately —
            // the step now only connects on the tick(s) its weapon sprite actually sweeps over
            // the target, matching TryMeleeHit's one-shot swings.
            ArmBladeHit(reach, dmg, knockback: 3f);
        }

        /// <summary>
        /// Launches a LeapSlam: face the player, drop gravity back on, and fling up-and-forward.
        /// The arc closes distance; the slam hit fires when the puppet lands (handled in the
        /// MeleeComboAttack phase).
        /// </summary>
        /// <summary>Below this horizontal gap, a LeapSlam jumps high and drops straight onto the
        /// player instead of lunging forward (avoids overshooting at close range).</summary>
        protected virtual float LeapMinLungeDistance => 80f;

        /// <summary>Aerial leap (a LeapSlam started in wing flight): straight-line speed, px/tick, used to
        /// size the dive's airtime. Clamped to AerialLeapMin/MaxDiveTicks so a short dive still reads and a
        /// long one doesn't float.</summary>
        protected virtual float AerialLeapDiveSpeed => 9f;
        private const float AerialLeapMinDiveTicks = 16f;
        private const float AerialLeapMaxDiveTicks = 40f;
        // Aerial slam aim: this fraction of the carry→impact sweep is the "contact" pose that gets pointed
        // at the target. Offset clamps (rad) stop the arm folding through the torso for targets far above
        // (negative = raise the arc) or straight below (positive = drop it).
        private const float AerialSlamContactFraction = 0.65f;
        private const float AerialSlamMaxAimRaise = 1.0f;
        private const float AerialSlamMaxAimDrop = 0.8f;

        /// <summary>Horizontal distance (px) a leap-based attack (LeapSlam, ApexDiveCleave) aims to
        /// land SHORT of the target's exact position, rather than landing squarely on top of them.
        /// 0 (default) preserves the original "land right on them" aim — the right call for a leap
        /// that IS the whole engagement (Studded's Leaping Cleave/Seismic Pursuit). A puppet whose
        /// leap is meant to read as "close most of the gap, but still arrive as a separate body"
        /// (Owl Father) can raise this so landing leaves a visible gap instead of overlapping the
        /// player, while staying well inside the step's own swing reach so the slam still connects.</summary>
        protected virtual float LeapLandingStandoff => 0f;

        private float PredictedLeapTargetX(Player target)
        {
            float lead = MathHelper.Clamp(
                target.velocity.X * LeapAttackTargetLeadTicks,
                -96f,
                96f);
            return target.Center.X + lead;
        }

        private void UpdateLeapAscentTracking(
            Player target, MeleeComboStep step, float extraLaunchSpeed, float trackingStrength)
        {
            if (target == null || trackingStrength <= 0f)
                return;

            float heightMult = step.LeapHeightMult > 0f ? step.LeapHeightMult : 1f;
            float forwardMult = step.LeapForwardSpeedMult > 0f ? step.LeapForwardSpeedMult : 1f;
            float launchSpeed = (LeapAttackUpSpeed + extraLaunchSpeed) * heightMult;
            // For a flat-ground ballistic arc, (launchSpeed - currentVy) / gravity is the
            // remaining time back to launch height. It stays useful on stepped terrain while
            // avoiding the old fixed 24-tick correction that accelerated distant dives past targets.
            float remainingAirTicks = Math.Max(12f, (launchSpeed - NPC.velocity.Y) / 0.3f);
            float targetX = PredictedLeapTargetX(target);
            float signedGap = targetX - NPC.Center.X;
            int trackingDirection = signedGap < 0f ? -1 : 1;
            float travelDistance = Math.Max(0f, Math.Abs(signedGap) - LeapLandingStandoff);
            float maxForward = LeapAttackForwardSpeed * forwardMult;
            float desiredSpeed = MathHelper.Clamp(
                travelDistance / remainingAirTicks,
                0f,
                maxForward);
            float desiredVelocity = trackingDirection * desiredSpeed;

            _comboLeapVx = MathHelper.Lerp(_comboLeapVx, desiredVelocity,
                MathHelper.Clamp(trackingStrength, 0f, 1f));
            if (Math.Abs(signedGap) > 24f)
            {
                _comboLockedDir = trackingDirection;
                NPC.direction = trackingDirection;
                NPC.spriteDirection = trackingDirection;
            }
        }

        /// <summary>LeapSlam apex retarget (opt-in via <see cref="MeleeComboStep.LeapApexRetargetStrength"/>):
        /// re-aims the locked horizontal velocity at the target's position AT THE APEX, using the
        /// descent's actual remaining airtime (shorter than the ascent whenever LeapDescentGravityMult
        /// speeds the fall up), instead of the launch-time lead-predicted aim. Called once, the tick
        /// the arc crosses from rising to falling.</summary>
        private void RetargetLeapAtApex(Player target, MeleeComboStep step)
        {
            float forwardMult = step.LeapForwardSpeedMult > 0f ? step.LeapForwardSpeedMult : 1f;
            float gravityMult = step.LeapDescentGravityMult > 0f ? step.LeapDescentGravityMult : 1f;
            // Ascent and (boosted-gravity) descent both start/end at zero vertical speed, so their
            // durations relate by sqrt(gravityMult) — see BeginLeapAttack's airtime derivation.
            float remainingAirTicks = Math.Max(6f,
                _comboLeapAscentTicks / (float)Math.Sqrt(gravityMult));

            float signedGap = target.Center.X - NPC.Center.X;
            int trackingDirection = signedGap < 0f ? -1 : 1;
            float travelDistance = Math.Max(0f, Math.Abs(signedGap) - LeapLandingStandoff);
            float maxForward = LeapAttackForwardSpeed * forwardMult;
            float desiredSpeed = MathHelper.Clamp(
                travelDistance / remainingAirTicks,
                0f,
                maxForward);
            float desiredVelocity = trackingDirection * desiredSpeed;

            _comboLeapVx = MathHelper.Lerp(_comboLeapVx, desiredVelocity,
                MathHelper.Clamp(step.LeapApexRetargetStrength, 0f, 1f));
            if (Math.Abs(signedGap) > 24f)
            {
                _comboLockedDir = trackingDirection;
                NPC.direction = trackingDirection;
                NPC.spriteDirection = trackingDirection;
            }
        }

        private void BeginLeapAttack(MeleeComboStep step)
        {
            _leapSlamSwingProgress = 0f;
            _leapStrikeStarted = false;
            _leapApexRetargeted = false;
            _comboLeapAscentTicks = 0;
            Player target = Main.player[NPC.target];
            float targetX = PredictedLeapTargetX(target);
            int dir = targetX < NPC.Center.X ? -1 : 1;
            _comboLockedDir = dir;
            NPC.direction = dir;
            NPC.spriteDirection = dir;
            NPC.noGravity = false;
            _leapSlamAimOffset = 0f;
            _leapLaunchedFromAir = _flight != null && _flight.IsAirborne;

            if (_leapLaunchedFromAir)
            {
                // Started in wing flight (an AirborneStartOnly combo): drop out of flight and dive
                // straight at the target in any direction instead of jumping. Airtime comes from a
                // straight-line dive speed; normal NPC gravity (0.3 px/t²) is then solved into the
                // launch so the arc ends on the aim point, a standoff short of the target.
                _flight.EndFlightNow();

                Vector2 aimPoint = new Vector2(targetX - dir * LeapLandingStandoff, target.Center.Y);
                Vector2 diveGap = aimPoint - NPC.Center;
                float diveDistance = diveGap.Length();
                float diveTicks = MathHelper.Clamp(diveDistance / AerialLeapDiveSpeed,
                    AerialLeapMinDiveTicks, AerialLeapMaxDiveTicks);
                float gravityDrop = 0.5f * 0.3f * diveTicks * diveTicks;
                float diveVx = diveGap.X / diveTicks;
                float diveVy = (diveGap.Y - gravityDrop) / diveTicks;

                _comboLeapVx = diveVx;
                NPC.velocity = new Vector2(diveVx, diveVy);
                _comboLeapLaunched = true;
                return;
            }

            float heightMult = step.LeapHeightMult > 0f ? step.LeapHeightMult : 1f;
            float forwardMult = step.LeapForwardSpeedMult > 0f ? step.LeapForwardSpeedMult : 1f;
            float dx = Math.Abs(targetX - NPC.Center.X);
            if (dx < LeapMinLungeDistance)
            {
                // Close: solve the small horizontal gap against the complete higher arc. This can
                // reach zero when already aligned, preventing the old fixed 1.5px/tick overshoot.
                float launchSpeed = (LeapAttackUpSpeed + 2f) * heightMult;
                float airtime = 2f * launchSpeed / 0.3f;
                float travelDistance = Math.Max(0f, dx - LeapLandingStandoff);
                float vx = Math.Min(LeapAttackMinimumForwardSpeed,
                    travelDistance / Math.Max(1f, airtime));
                _comboLeapVx = dir * vx;
                NPC.velocity = new Vector2(_comboLeapVx, -launchSpeed);
            }
            else
            {
                // Far: arc toward the player, horizontal speed sized to land near them (roughly
                // dx / airtime, minus the standoff) and capped so it never overshoots.
                float travelDistance = Math.Max(0f, dx - LeapLandingStandoff);
                float launchSpeed = LeapAttackUpSpeed * heightMult;
                float airtime = 2f * launchSpeed / 0.3f; // ticks aloft ≈ 2·Vy/g
                float vx = MathHelper.Clamp(travelDistance / airtime,
                    LeapAttackMinimumForwardSpeed, LeapAttackForwardSpeed * forwardMult);
                _comboLeapVx = dir * vx;
                NPC.velocity = new Vector2(_comboLeapVx, -launchSpeed);
            }
            _comboLeapLaunched = true;
        }

        private void BeginBackstepRaise()
        {
            Player target = Main.player[NPC.target];
            int towardTarget = target.Center.X < NPC.Center.X ? -1 : 1;
            _comboLockedDir = towardTarget;
            NPC.direction = towardTarget;
            NPC.spriteDirection = towardTarget;
            NPC.noGravity = false;
            _comboLeapVx = -towardTarget * Math.Max(2.8f, TopSpeed * 1.05f);
            NPC.velocity = new Vector2(_comboLeapVx, -4.8f);
            _comboLeapLaunched = true;
            NPC.netUpdate = true;
        }

        private void BeginApexDiveCleave(MeleeComboStep step)
        {
            Player target = Main.player[NPC.target];
            float targetX = PredictedLeapTargetX(target);
            int towardTarget = targetX < NPC.Center.X ? -1 : 1;
            _comboLockedDir = towardTarget;
            NPC.direction = towardTarget;
            NPC.spriteDirection = towardTarget;
            NPC.noGravity = false;

            float heightMult = step.LeapHeightMult > 0f ? step.LeapHeightMult : 1f;
            float forwardMult = step.LeapForwardSpeedMult > 0f ? step.LeapForwardSpeedMult : 1f;
            float maxForward = LeapAttackForwardSpeed * forwardMult;
            float dx = Math.Abs(targetX - NPC.Center.X);
            float travelDistance = Math.Max(0f, dx - LeapLandingStandoff);
            float launchSpeed = (LeapAttackUpSpeed + 1.8f) * heightMult;
            float airtime = 2f * launchSpeed / 0.3f;
            float forwardSpeed = MathHelper.Clamp(travelDistance / Math.Max(1f, airtime),
                LeapAttackMinimumForwardSpeed, maxForward);
            _comboLeapVx = towardTarget * forwardSpeed;
            NPC.velocity = new Vector2(_comboLeapVx, -launchSpeed);
            _comboLeapLaunched = true;
            _apexDiveFallTicks = 0;
            _apexDiveStrikeStarted = false;
            NPC.netUpdate = true;
        }

        private void BeginComboStepAttack(MeleeComboStep step)
        {
            SetDisplayWeapon(FrontHandWeaponType, swing: true);
            if (IsSwingingMotion(step.Motion))
                PlayMeleeSwingSound();

            _bladeArmed = false;
            if (step.Motion == ComboMotion.LeapSlam || step.Motion == ComboMotion.LeapThrust)
            {
                BeginLeapAttack(step);
            }
            else if (step.Motion == ComboMotion.BackstepRaise)
            {
                BeginBackstepRaise();
            }
            else if (step.Motion == ComboMotion.ApexDiveCleave)
            {
                BeginApexDiveCleave(step);
            }
            else if (step.Motion == ComboMotion.RisingUppercutLeap)
            {
                BeginRisingUppercutLeap();
                DoComboMeleeHit(step);
            }
            else if (step.Motion == ComboMotion.ThrownWeaponRetrieve)
            {
                BeginThrownWeaponRetrieve();
            }
            else if (step.Motion != ComboMotion.ChargeChop && step.Motion != ComboMotion.Feint
                && step.Motion != ComboMotion.LowAxeRun && step.Motion != ComboMotion.BackstepRaise
                && step.Motion != ComboMotion.ApexDiveCleave)
            {
                DoComboMeleeHit(step);
            }

            _activeComboStepTotalTicks = BeginComboAttackTicks(step);
            EnterPhase(AttackPhase.MeleeComboAttack, _activeComboStepTotalTicks);
        }

        private void BeginRisingUppercutLeap()
        {
            Player target = Main.player[NPC.target];
            int direction = target.Center.X < NPC.Center.X ? -1 : 1;
            _comboLockedDir = direction;
            NPC.direction = direction;
            NPC.spriteDirection = direction;
            NPC.noGravity = false;
            float dx = Math.Abs(target.Center.X - NPC.Center.X);
            float airtime = 2f * LeapAttackUpSpeed / 0.3f;
            float forwardSpeed = MathHelper.Clamp(
                dx / airtime,
                TopSpeed * 1.1f,
                LeapAttackForwardSpeed);
            _comboLeapVx = direction * forwardSpeed;
            NPC.velocity = new Vector2(_comboLeapVx, -LeapAttackUpSpeed);
            _comboLeapLaunched = true;
            _risingUppercutFallHoldTimer = 0;
            NPC.netUpdate = true;
        }

        private void BeginThrownWeaponRetrieve()
        {
            _thrownComboWeaponIndex = Main.netMode == NetmodeID.MultiplayerClient
                ? -1
                : SpawnThrownComboWeapon();
            _thrownWeaponStage = 0;
            _thrownWeaponReadTimer = 0;
            _thrownWeaponLeapVelocityX = 0f;
            _thrownWeaponWasSeen = _thrownComboWeaponIndex >= 0;
            SetDisplayWeapon(-1, swing: false);
            NPC.netUpdate = true;
        }

        private Projectile FindThrownComboWeapon()
        {
            if (_thrownComboWeaponIndex >= 0 && _thrownComboWeaponIndex < Main.maxProjectiles)
            {
                Projectile known = Main.projectile[_thrownComboWeaponIndex];
                if (known.active && known.type == ThrownComboWeaponProjectileType)
                    return known;
            }

            if (ThrownComboWeaponProjectileType <= 0)
                return null;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];
                if (candidate.active && candidate.type == ThrownComboWeaponProjectileType
                    && (int)candidate.ai[1] == NPC.whoAmI)
                {
                    _thrownComboWeaponIndex = i;
                    return candidate;
                }
            }
            return null;
        }

        private bool TickThrownWeaponRetrieve()
        {
            SetDisplayWeapon(-1, swing: false);
            Projectile weapon = FindThrownComboWeapon();
            if (weapon == null)
            {
                NPC.velocity.X *= 0.8f;
                // Clients may need a few ticks to receive the server-spawned projectile. Once it
                // has been observed, disappearance means it was destroyed and the combo should recover.
                return _thrownWeaponWasSeen || PhaseTimer <= 30;
            }

            _thrownWeaponWasSeen = true;
            bool embedded = weapon.ai[0] == 1f;
            if (!embedded)
            {
                NPC.velocity.X *= 0.8f;
                return false;
            }

            if (_thrownWeaponStage == 0)
            {
                NPC.velocity.X *= 0.65f;
                if (++_thrownWeaponReadTimer < ThrownWeaponReadTicks)
                    return false;

                int launchDirection = weapon.Center.X < NPC.Center.X ? -1 : 1;
                float dx = Math.Abs(weapon.Center.X - NPC.Center.X);
                // Size the launch for the full jump arc, not just its rising half. The old /26
                // estimate arrived at the axe near the apex, then the unconditional X lock below
                // carried the puppet hundreds of pixels past it before landing.
                float leapVelocity = MathHelper.Clamp(dx / 52f, 2f, ThrownWeaponRetrieveLeapMaxSpeed);
                _thrownWeaponLeapVelocityX = launchDirection * leapVelocity;
                _comboLockedDir = launchDirection;
                NPC.direction = launchDirection;
                NPC.spriteDirection = launchDirection;
                NPC.noGravity = false;
                NPC.velocity = new Vector2(_thrownWeaponLeapVelocityX, -ThrownWeaponRetrieveLeapUpSpeed);
                _thrownWeaponStage = 1;
                NPC.netUpdate = true;
                return false;
            }

            float signedDx = weapon.Center.X - NPC.Center.X;
            float absDx = Math.Abs(signedDx);
            bool closeEnough = NPC.Distance(weapon.Center) <= 44f;
            bool landedNear = NPC.velocity.Y == 0f && absDx <= 64f;
            if (closeEnough || landedNear)
                return true;

            // Home toward the embedded axe and start braking before crossing it. Reasserting the
            // original launch speed every tick made even a small miss turn into a long run away.
            int retrieveDirection = signedDx < 0f ? -1 : 1;
            float desiredSpeed = MathHelper.Clamp(absDx / 28f, 1.25f, ThrownWeaponRetrieveLeapMaxSpeed);
            float desiredVelocityX = retrieveDirection * desiredSpeed;
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredVelocityX, absDx < 96f ? 0.38f : 0.22f);
            _thrownWeaponLeapVelocityX = NPC.velocity.X;
            _comboLockedDir = retrieveDirection;
            NPC.direction = retrieveDirection;
            NPC.spriteDirection = retrieveDirection;
            if (absDx < 56f)
                NPC.velocity.X *= 0.65f;

            return false;
        }

        private void FinishThrownWeaponRetrieve()
        {
            Projectile weapon = FindThrownComboWeapon();
            if (weapon != null && Main.netMode != NetmodeID.MultiplayerClient)
                weapon.Kill();

            _thrownComboWeaponIndex = -1;
            _thrownWeaponStage = 0;
            _thrownWeaponReadTimer = 0;
            _thrownWeaponLeapVelocityX = 0f;
            _thrownWeaponWasSeen = false;
            SetDisplayWeapon(FrontHandWeaponType, swing: false);
            NPC.netUpdate = true;
        }

        /// <summary>Launches a big jump arc over the player, facing them. Call once, then
        /// EnterPhase(AttackPhase.FireVolleyArcJump, someTicks); the phase's own case body fires
        /// <see cref="DoFireVolleyArcFire"/> at the apex and <see cref="OnFireVolleyArcJumpLanded"/>
        /// on landing.</summary>
        protected void BeginFireVolleyArcJump()
        {
            Player target = Main.player[NPC.target];
            int dir = target.Center.X < NPC.Center.X ? -1 : 1;
            NPC.direction = dir;
            NPC.spriteDirection = dir;
            NPC.noGravity = false;
            _fireVolleyArcVx = dir * FireVolleyArcJumpForwardSpeed;
            NPC.velocity = new Vector2(_fireVolleyArcVx, -FireVolleyArcJumpUpSpeed);
            _fireVolleyArcFired = false;
            NPC.netUpdate = true;
        }

        /// <summary>Motions that actually swing the weapon (so they get a swing whoosh + arc VFX).
        /// Feint (bait hold), ChargeChop/LowAxeRun (run-ins), and ThrownWeaponRetrieve
        /// (release/retrieval) don't begin a melee swing, so they're excluded.</summary>
        private static bool IsSwingingMotion(ComboMotion m)
            => m != ComboMotion.Feint && m != ComboMotion.ChargeChop
            && m != ComboMotion.LowAxeRun && m != ComboMotion.ThrownWeaponRetrieve
            && m != ComboMotion.BackstepRaise && m != ComboMotion.ApexDiveCleave
            && m != ComboMotion.FlailBrace;

        /// <summary>
        /// Drives the bounded reactive shield action each AI tick (no-op without a shield): raise the
        /// guard from the reactive-block timer, plant + face the player, then apply a rearm cooldown.
        /// The block timer itself ticks down in GlobalNPC.PostAI (mover-agnostic);
        /// damage / poise reduction is applied in ModifyHitBy* + the poise system via ShieldGuarding.
        /// Call AFTER the movement AI so the plant overrides pursuit velocity.
        /// </summary>
        private void UpdateShield()
        {
            if (!HasShield)
            {
                return;
            }
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // A carried shield remains visible, but reactive guards are opt-in. Their neutral hold
            // can otherwise be refreshed into an effective stun lock without ever breaking poise.
            if (!AllowReactiveDefense)
            {
                _shielding = false;
                globalNPC.ShieldGuarding = false;
                return;
            }

            Player target = Main.player[NPC.target];
            int dirToPlayer = target.Center.X < NPC.Center.X ? -1 : 1;
            bool neutralPhase = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll;
            bool guardPhase = Phase == AttackPhase.ShieldGuard;
            bool grounded = NPC.velocity.Y == 0f;

            if (!guardPhase)
            {
                _shielding = false;
                globalNPC.ShieldGuarding = false;
                _shieldWasGuarding = false;
                _shieldMeleeExtended = false;

                // A reaction is only legal from true neutral. A hit during an authored attack must
                // never interrupt it or leave behind a delayed guard that fires after recovery.
                if (!neutralPhase || !grounded || _shieldGuardCooldown > 0)
                {
                    globalNPC.ReactiveBlockTimer = 0;
                    return;
                }

                // Guard selection is server-authoritative; clients receive the guard snapshot in
                // ReceiveExtraAI instead of making a second random roll.
                if (globalNPC.ReactiveBlockTimer <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    tsorcRevampAIs.TryPreemptiveBlock(NPC, globalNPC, ShieldGuardTicksRanged);
                if (globalNPC.ReactiveBlockTimer <= 0)
                    return;

                _shieldLockedDir = dirToPlayer;
                _shieldWasGuarding = true;
                EnterPhase(AttackPhase.ShieldGuard, globalNPC.ReactiveBlockTimer);
                NPC.netUpdate = true;
            }

            // A jump/ledge fall or timer expiry cleanly ends the action. UpdateShield runs before
            // PuppetAttackAI, so the puppet can immediately choose its next attack on this tick.
            if (!grounded || globalNPC.ReactiveBlockTimer <= 0)
            {
                FinishShieldGuard(globalNPC);
                return;
            }

            _shielding = true;
            globalNPC.ShieldGuarding = true;
            PhaseTimer = globalNPC.ReactiveBlockTimer;

            // Rising edge: capture the facing the guard went up with.
            if (!_shieldWasGuarding)
            {
                _shieldLockedDir   = dirToPlayer;
                _shieldWasGuarding = true;
            }

            bool inMelee = NPC.Distance(target.Center) <= MeleeRange + 8f;
            if (inMelee)
            {
                // Commit the guard in melee: bump the hold to the melee minimum ONCE, LOCK facing so a
                // dodgeroll-through exposes the back for a backstab, and plant.
                if (!_shieldMeleeExtended)
                {
                    globalNPC.ReactiveBlockTimer = Math.Max(globalNPC.ReactiveBlockTimer, ShieldGuardTicksMelee);
                    _shieldMeleeExtended = true;
                }
                NPC.direction       = _shieldLockedDir;
                NPC.spriteDirection = _shieldLockedDir;
                _directionHoldTicks = Math.Max(_directionHoldTicks, 5);
                NPC.velocity.X *= 0.5f; // plant
            }
            else
            {
                // Out of melee range: track the player and advance slowly behind the shield.
                _shieldLockedDir    = dirToPlayer;
                NPC.direction       = dirToPlayer;
                NPC.spriteDirection = dirToPlayer;
                NPC.velocity.X      = dirToPlayer * ShieldAdvanceSpeed;
            }
        }

        private void FinishShieldGuard(tsorcRevampGlobalNPC globalNPC)
        {
            bool wasGuarding = Phase == AttackPhase.ShieldGuard || _shielding;
            globalNPC.ReactiveBlockTimer = 0;
            globalNPC.ShieldGuarding = false;
            _shielding = false;
            _shieldWasGuarding = false;
            _shieldMeleeExtended = false;
            _shieldGuardCooldown = Math.Max(_shieldGuardCooldown, ShieldGuardCooldownTicks);
            if (Phase == AttackPhase.ShieldGuard)
                EnterPhase(AttackPhase.Idle, 0);
            if (wasGuarding)
                NPC.netUpdate = true;
        }

        /// <summary>Plays a throwing-release sound, rotating across Item7/18/19 so a rapid volley of
        /// thrown things (stars, flasks, caltrops, bombs) doesn't repeat the exact same clip.</summary>
        protected void PlayThrowSound()
        {
            if (Main.dedServ)
            {
                return;
            }
            SoundStyle s = Main.rand.Next(3) switch
            {
                0 => SoundID.Item7,
                1 => SoundID.Item18,
                _ => SoundID.Item19,
            };
            SoundEngine.PlaySound(s with { Volume = 0.5f, PitchVariance = 0.2f }, NPC.Center);
        }

        /// <summary>Fired once when a ranged burst's telegraph begins (the weapon first appears in
        /// hand).  <paramref name="secondary"/> = the secondary ranged weapon.  Override to play a
        /// per-weapon "winding up" cue — e.g. a lit fuse on a smoke bomb.</summary>
        protected virtual void OnRangedBurstStarted(bool secondary) { }

        /// <summary>Plays the held weapon's own swing sound (the axe's UseSound) at a swing's start.</summary>
        protected void PlayMeleeSwingSound()
        {
            if (Main.dedServ)
            {
                return;
            }
            Item w = GetCachedWeaponItem(MeleeWeaponItemType);
            SoundStyle snd = w?.UseSound ?? SoundID.Item1;
            SoundEngine.PlaySound(snd with { Volume = 0.55f, PitchVariance = 0.3f }, NPC.Center);
        }

        /// <summary>
        /// Subtle blade-trail VFX: a few faint smoke wisps near the weapon tip, emitted only while
        /// the blade is actually sweeping (gated on per-tick rotation speed) so it reads as a swing
        /// without cluttering the telegraph or idle hold.
        /// </summary>
        private void SpawnSwingVFX(float rotDelta)
        {
            if (Main.dedServ || !IsWeaponVisiblePhase || IsLeapSlamCarry)
            {
                return;
            }
            if (Math.Abs(rotDelta) < 0.07f)
            {
                return;  // only during a real sweep, not a slow lerp
            }
            if (!Main.rand.NextBool(2))
            {
                return;  // sparse → subtle
            }
            Vector2 outward = _attackRuntimeV2.Active
                ? GetWeaponWorldDirection()
                : new Vector2(NPC.direction, 0f).RotatedBy(_weaponRotation);
            Vector2 tip     = GetHandPosition() + outward * 20f;
            Dust dust = Dust.NewDustPerfect(tip, DustID.Smoke,
                outward.RotatedBy(MathHelper.PiOver2 * Math.Sign(rotDelta)) * 0.5f,
                200, Color.LightGray, 0.55f);
            dust.noGravity = true;
        }

        /// <summary>
        /// Publishes this puppet's attack state to the poise/hyper-armor system each AI tick.
        /// AttackTelegraphing = windup (still interruptible); AttackCommitted = post-flash swing
        /// (hyper-armor: zero knockback, no poise build).  HyperArmor combos additionally hold the
        /// commit through inter-step pauses, so once they start swinging they can't be staggered
        /// until recovery — only the opening windup leaves a punish window.
        /// </summary>
        private void UpdateAttackCommitFlags()
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            bool telegraph =
                Phase == AttackPhase.MeleeTelegraph  || Phase == AttackPhase.StabTelegraph  ||
                Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.SpearTelegraph ||
                Phase == AttackPhase.MagicTelegraph  || Phase == AttackPhase.MeleeComboTelegraph ||
                Phase == AttackPhase.BreathTelegraph || Phase == AttackPhase.KnivesTelegraph ||
                Phase == AttackPhase.PierceTelegraph || Phase == AttackPhase.JumpSlashRise ||
                Phase == AttackPhase.AbyssSlashTelegraph ||
                Phase == AttackPhase.TendrilTelegraph || Phase == AttackPhase.TendrilReach ||
                Phase == AttackPhase.TendrilSwingTelegraph ||
                Phase == AttackPhase.AbyssShardTelegraph ||
                Phase == AttackPhase.HomingVolleySwingTelegraph ||
                Phase == AttackPhase.BoomerangSwingTelegraph ||
                Phase == AttackPhase.SpiralFanSwingTelegraph;

            bool committed =
                Phase == AttackPhase.MeleeAttack  || Phase == AttackPhase.StabAttack  ||
                Phase == AttackPhase.RangedAttack || Phase == AttackPhase.SpearAttack ||
                Phase == AttackPhase.MagicAttack  || Phase == AttackPhase.MeleeComboAttack ||
                Phase == AttackPhase.Breathing ||
                Phase == AttackPhase.KnivesThrow || Phase == AttackPhase.KnivesThrowPause ||
                // The whole active dash + (for the stab variant) the impale hold/flick must be
                // uninterruptible - staggering Artorias mid-impale would leave the target frozen
                // with nothing driving the sequence.
                Phase == AttackPhase.PierceDash || Phase == AttackPhase.PierceStabHold ||
                Phase == AttackPhase.PierceStabFlick ||
                Phase == AttackPhase.JumpSlashAttack ||
                // FlipSlashRise already has real i-frames (DodgeTimer); FlipSlashLand is a deliberate
                // static held pose that shouldn't stagger out mid-hold.
                Phase == AttackPhase.FlipSlashRise || Phase == AttackPhase.FlipSlashLand ||
                // Once the first swipe fires, the whole chain (including inter-swipe pauses) is
                // committed - only the initial wind-up is punishable.
                Phase == AttackPhase.AbyssSlashSwipe || Phase == AttackPhase.AbyssSlashPause ||
                Phase == AttackPhase.TendrilSwing ||
                // The whole nova (charge + blast) is a scripted health-threshold set-piece -
                // shouldn't be stagger-cancelled out of mid-charge.
                Phase == AttackPhase.NovaCharge || Phase == AttackPhase.NovaBlast ||
                // Once the first shard event fires, the whole sequence (including inter-event
                // pauses) is committed - only the initial wind-up is punishable.
                Phase == AttackPhase.AbyssShardFire || Phase == AttackPhase.AbyssShardPause ||
                // The overhead chop itself is committed - only the raised wind-up is punishable.
                Phase == AttackPhase.HomingVolleySwing ||
                Phase == AttackPhase.BoomerangSwing ||
                // Once the swing completes and the burst starts, the whole sequence (including
                // inter-shot pauses) is committed - only the raised wind-up is punishable.
                Phase == AttackPhase.SpiralFanSwing ||
                Phase == AttackPhase.SpiralFanBurst || Phase == AttackPhase.SpiralFanPause;

            if (_activeMeleeComboIndex >= 0 && _activeMeleeCombo.HyperArmor
                && Phase == AttackPhase.MeleeComboPause)
                committed = true;

            // Full-window hyper-armor: fold the telegraph into the committed (un-staggerable) window so
            // the whole attack — wind-up included — has hyper-armor, instead of only the post-flash swing.
            if (HyperArmorDuringTelegraph && telegraph)
                committed = true;

            globalNPC.AttackCommitted    = committed;
            globalNPC.AttackTelegraphing = telegraph && !committed;
        }

        // ── Aerial actions (default implementations) ──────────────────────────────
        /// <summary>Lets a subclass redirect where an aerial dive is aimed - e.g. a ground point near
        /// the player instead of the default lead-through-the-player waypoint, for a dive-slam variant
        /// distinct from the default pierce-through thrust. Called once, right before RequestDive.
        /// Default returns the waypoint unchanged.</summary>
        protected virtual Vector2 ModifyAerialDiveWaypoint(Vector2 defaultWaypoint) => defaultWaypoint;

        /// <summary>Fire a telegraph flash + dust burst when a dive begins.  Subclass override
        /// for thematic dive markers.  Also equips the melee weapon for the dive visual.</summary>
        protected virtual void DoAerialDiveTelegraph()
        {
            if (MeleeWeaponItemType >= 0)
                SetDisplayWeapon(FrontHandWeaponType, swing: false);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            SpawnTelegraphFlash(Color.OrangeRed);
        }

        /// <summary>Spawn the dive's damage hitbox.  Called repeatedly while in DiveAttack mode
        /// to give the dive a sustained contact-damage feel.</summary>
        protected virtual void DoAerialDiveHit()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            Vector2 attackDirection = (target.Center - NPC.Center)
                .SafeNormalize(NPC.velocity.SafeNormalize(Vector2.UnitX));
            int boxW = 72;
            int boxH = 56;
            Vector2 hitCenter = NPC.Center + attackDirection * 38f;
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), hitCenter, Vector2.Zero,
                ModContent.ProjectileType<PuppetMeleeHitbox>(),
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
        protected void EnterCasualOrIdle()
        {
            // The stroll roll is the server's. A client drops to Idle and adopts a stroll from the snapshot.
            if (IsMultiplayerClient)
            {
                EnterPhase(AttackPhase.Idle, 0);
                return;
            }

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
            if (count <= 1)
            {
                return 0;
            }
            if (chances == null || chances.Length == 0)
                return Main.rand.Next(count);

            int limit = Math.Min(chances.Length, count);
            int total = 0;
            for (int i = 0; i < limit; i++)
            {
                total += chances[i];
            }
            if (total <= 0)
            {
                return Main.rand.Next(count);
            }

            int roll = Main.rand.Next(total);
            int cumulative = 0;
            for (int i = 0; i < limit; i++)
            {
                cumulative += chances[i];
                if (roll < cumulative)
                {
                    return i;
                }
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
        /// For a sustained cast, set <see cref="_magicAttackTicksOverride"/> here and emit per-tick in
        /// <see cref="DoMagicTick"/>.
        /// </summary>
        protected virtual  void DoMagicAttack() { }

        /// <summary>Called on the SERVER immediately before the MagicTelegraph phase is entered, i.e. before
        /// <see cref="MagicTelegraphTicks"/> is read.  Default no-op.  A boss whose spells have different
        /// wind-ups rolls its next spell here and returns per-spell values from MagicTelegraphTicks /
        /// MagicRecoveryTicks / MagicCooldownAfterUse; the rolled spell reaches clients in the same snapshot
        /// as the phase, so a rolled-then-read telegraph length is consistent everywhere.</summary>
        protected virtual  void OnMagicTelegraphStarting() { }
        /// <summary>Configure an illusion created by the shared magic teleport before its first AI tick.</summary>
        public virtual void InitializeTeleportIllusion(NPC illusion) { }
        protected virtual float TeleportIllusionOpacity => 0.2f;

        /// <summary>Called every tick of the MagicAttack phase with the ticks remaining.  Default no-op;
        /// override (together with <see cref="_magicAttackTicksOverride"/>) for channeled casts such as a
        /// timed meteor rain.</summary>
        protected virtual  void DoMagicTick(int ticksRemaining) { }

        // ── Custom set-piece phase (generic scriptable channel) ──────────────────
        /// <summary>Fired once when a <see cref="AttackPhase.Custom"/> set-piece begins (from
        /// <see cref="StartCustomAttack"/>).  Emit the on-start payload / cue here.  Default no-op.</summary>
        protected virtual  void DoCustomAttack() { }

        /// <summary>Called every tick of the <see cref="AttackPhase.Custom"/> phase, with ticks remaining
        /// (last tick = 1, matching <see cref="DoMagicTick"/>).  Drive the bespoke behavior / VFX here.
        /// Default no-op.</summary>
        protected virtual  void DoCustomTick(int ticksRemaining) { }

        /// <summary>Whether the puppet brakes (<c>SlowDown</c>) each tick of the Custom phase.  Default
        /// true — most set-pieces are stationary channels; override false for a moving one.</summary>
        protected virtual  bool SlowDownDuringCustom => true;

        /// <summary>Optional fixed local weapon angle for a <see cref="AttackPhase.Custom"/> set-piece.
        /// Return <c>null</c> to keep the ordinary carried-weapon easing. This gives scripted guards and
        /// channels a real authored pose without pretending they are a damaging melee swing.</summary>
        protected virtual float? CustomWeaponRotation => null;

        /// <summary>
        /// Park the puppet in a bespoke, timed set-piece phase for <paramref name="duration"/> ticks: it
        /// holds an optional weapon pose, fires <see cref="DoCustomAttack"/> once now, runs
        /// <see cref="DoCustomTick"/> each tick, then returns to Idle/CasualStroll.  This is the clean home
        /// for behaviors that are NOT melee/ranged/magic attacks — self-buffs, encases, ally channels,
        /// death-bursts.  Call it from a subclass's <c>AI()</c> override when <see cref="Phase"/> is free
        /// (or unconditionally, to interrupt the current attack).  <paramref name="poseWeaponItemType"/>
        /// &lt; 0 shows no weapon (a bare-handed channel).
        /// </summary>
        protected void StartCustomAttack(int duration, int poseWeaponItemType = -1, bool swingPose = false)
        {
            _customPoseWeapon = poseWeaponItemType;
            _customSwingPose  = swingPose;
            if (poseWeaponItemType >= 0)
                SetDisplayWeapon(poseWeaponItemType, swing: swingPose);
            EnterPhase(AttackPhase.Custom, duration);
            DoCustomAttack();
        }

        /// <summary>Called every tick of the BreathTelegraph phase, with elapsed ticks counting up from 0.
        /// Emit the swelling mouth-ember / charge VFX here.</summary>
        protected virtual  void DoBreathWindup(int elapsed) { }

        /// <summary>Called every tick of the active Breathing phase, with the breath ticks remaining.
        /// Spawn the breath projectile(s) here (typically gated on a tick interval).</summary>
        protected virtual  void DoBreathTick(int ticksRemaining) { }

        /// <summary>Fired once when the breath stream releases (telegraph → Breathing).  Override for a
        /// flamethrower whoosh / roar cue.</summary>
        protected virtual  void OnBreathStart() { }

        /// <summary>Spawn one Cursed Knives volley (typically 3 knives).  Called at each throw frame.</summary>
        protected virtual  void DoCursedKnivesThrow() { }

        /// <summary>Pick the volley count + inter-volley gap for a Cursed Knives attack from the range to
        /// the player, then enter the telegraph.  Close = 1 volley; mid = 2 back-to-back; far+LOS = 3.</summary>
        private void StartCursedKnives(float dist, bool hasLOS)
        {
            if (dist <= CursedKnivesCloseRange)
            {
                _cursedKnivesVolleysLeft = 1;
                _cursedKnivesGap = 0;
            }
            else if (dist <= CursedKnivesMidRange)
            {
                _cursedKnivesVolleysLeft = 2;
                _cursedKnivesGap = CursedKnivesBackToBackGap;
            }
            else if (hasLOS)
            {
                _cursedKnivesVolleysLeft = 3;
                _cursedKnivesGap = CursedKnivesFarGap;
            }
            else
            {
                _cursedKnivesVolleysLeft = 1;
                _cursedKnivesGap = 0;
            }

            EnterPhase(AttackPhase.KnivesTelegraph, CursedKnivesTelegraphTicks);
        }

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
            bool wasNeutral = Phase == AttackPhase.Idle
                || Phase == AttackPhase.ClosingDistance
                || Phase == AttackPhase.CasualStroll;
            bool isTelegraph = phase == AttackPhase.MeleeTelegraph
                            || phase == AttackPhase.StabTelegraph
                            || phase == AttackPhase.RangedTelegraph
                            || phase == AttackPhase.SpearTelegraph
                            || phase == AttackPhase.MagicTelegraph
                            || phase == AttackPhase.MeleeComboTelegraph
                            || phase == AttackPhase.BreathTelegraph
                            || phase == AttackPhase.KnivesTelegraph;
            if (wasNeutral && isTelegraph)
                _lastAttackHitConnected = false;

            bool endsTrackedBlade = phase == AttackPhase.MeleeRecovery
                || phase == AttackPhase.StabRecovery
                || phase == AttackPhase.SpearRecovery
                || phase == AttackPhase.MeleeComboPause
                || phase == AttackPhase.MeleeComboRecovery
                || phase == AttackPhase.TendrilRecovery
                || phase == AttackPhase.Idle;
            if (endsTrackedBlade)
            {
                _bladeArmed = false;
                _hasPreviousBladeSample = false;
                _backHand.HasPreviousBladeSample = false;
            }

            // Dropping back to neutral releases the attack's facing commitment, so the navigator is
            // free to re-face on its own again. Every committed sequence re-captures on entry.
            if (phase == AttackPhase.Idle || phase == AttackPhase.CasualStroll
                || phase == AttackPhase.ClosingDistance)
            {
                _attackFacingDir = 0;
            }

            Phase      = phase;
            // Guarantee at least 30 ticks of telegraph so the flash always leads the attack by 30.
            PhaseTimer = isTelegraph ? Math.Max(duration, MinTelegraphTicks) : duration;
            // Each new telegraph phase gets its own flash.
            if (isTelegraph)
            {
                _flashFired = false;
            }

            // Multiplayer transition counter (see _phaseSequence). A client's first prediction past the last
            // server-confirmed state starts the stale-snapshot clock ReceiveExtraAI checks.
            if (IsMultiplayerClient && _phaseSequence == _lastAcceptedPhaseSequence)
            {
                _predictionStartTick = Main.GameUpdateCount;
            }
            _phaseSequence++;
        }

        private void SlowDown() => NPC.velocity.X *= 0.80f;

        /// <summary>
        /// Preemptive quick-step: when the player is mid-swing within melee reach, roll
        /// <see cref="PreemptiveQuickStepChance"/> to dash THROUGH them (i-frames + pass-through, can't
        /// damage them) and land past them.  The shared <see cref="tsorcRevampAIs.ArmQuickStep"/> picks the
        /// forward step when there's room.  Uses the proactive-evasion cooldown so it can't spam.
        /// </summary>
        private void TryPreemptiveQuickStep(tsorcRevampGlobalNPC gnpc, Player target, float dist)
        {
            if (NPC.velocity.Y != 0f)
            {
                return;  // grounded only
            }
            if (dist > MeleeRange + 56f)
            {
                return;  // must be in melee threat range
            }
            if (target.itemAnimation <= 0)
            {
                return;  // player must be actively swinging/using
            }
            if (Main.rand.Next(100) >= PreemptiveQuickStepChance)
            {
                return;
            }

            NPC.TargetClosest(true);
            tsorcRevampAIs.ArmQuickStep(NPC, gnpc, allowForward: true);
            gnpc.DodgeCooldown = 120; // shared proactive-evasion cooldown (~2 s)
            NPC.netUpdate = true;
            // A 16-tick step would be over before a throttled netUpdate reached clients.
            RequestNetworkSnapshot();
        }

        /// <summary>
        /// Proactive projectile dodge for SF4 puppets (the legacy BasicAI Agility scan doesn't run for
        /// them).  Scans for an incoming friendly projectile roughly aimed at this puppet; on a hit,
        /// rolls the GlobalNPC <c>Agility</c> stat and either jumps over it (with headroom) or triggers
        /// the mover-agnostic DodgeTimer i-frames.  Mirrors the RunFighterCombatTriggers dodge logic.
        /// </summary>
        private void TryEvadeIncomingProjectile(tsorcRevampGlobalNPC gnpc, Player target)
        {
            if (gnpc.Agility <= 0f)
            {
                return;  // Agility unset/zero → no dodging
            }
            float rangeSq = ProjectileEvadeRange * ProjectileEvadeRange;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || !proj.friendly || proj.damage <= 0)
                {
                    continue;
                }
                if (proj.DistanceSQ(NPC.Center) >= rangeSq)
                {
                    continue;
                }
                // Roughly aimed at us?  Compare the projectile's heading to the bearing toward this NPC.
                if (UsefulFunctions.CompareAngles(proj.velocity, UsefulFunctions.Aim(proj.Center, NPC.Center, 1f)) >= 0.35f)
                {
                    continue;
                }

                if (Main.rand.NextFloat() < gnpc.Agility)
                {
                    bool headroom = true;
                    for (int j = 1; j <= 8; j++)
                    {
                        if (UsefulFunctions.IsTileReallySolid(NPC.Center + new Vector2(0f, -16f * j)))
                        {
                            headroom = false;
                            break;
                        }
                    }
                    // Grounded + headroom → preemptive jump; otherwise an in-place i-frame roll.
                    if (headroom && NPC.velocity.Y == 0f && Main.rand.NextBool())
                        NPC.velocity.Y -= 8f;
                    else
                        gnpc.DodgeTimer = 30;

                    gnpc.DodgeCooldown = (int)(300 * (1f - gnpc.Agility));
                    NPC.netUpdate = true;
                    // The jump / i-frame roll is short: push it now rather than after the netSpam throttle.
                    RequestNetworkSnapshot();
                }
                break; // react to at most one projectile per tick
            }
        }

        // ── Damage tracking for emergency heal + reactive shield ──────────────────
        /// <summary>Burst-damage memory for the emergency heal, plus the reactive shield raise. Only a FRONT hit can snap
        /// the guard up — a backstab must not re-raise it. Called by GlobalNPC.ApplyHitReport, which runs where hits are
        /// authoritative: from the hit hook in singleplayer, from a client's ReportNPCHit on a multiplayer server.</summary>
        internal void RegisterHit(Vector2 sourceCenter, int damageDone, bool meleeHit)
        {
            _recentDamage += (float)damageDone / NPC.lifeMax;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool frontHit = Math.Sign(sourceCenter.X - NPC.Center.X) == NPC.direction;
            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll;
            bool guardReady = _shieldGuardCooldown <= 0 && globalNPC.ReactiveBlockTimer <= 0;
            bool canRaiseGuard = AllowReactiveDefense && HasShield && guardReady && neutral
                && NPC.velocity.Y == 0f && frontHit;
            if (canRaiseGuard)
            {
                tsorcRevampAIs.TryOnHitBlock(NPC, globalNPC, meleeHit, ShieldGuardTicksRanged);
            }
        }

        // ── Reactive shield: reduce FRONT damage while guarding (backstabs bypass) ──
        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (_shielding && Math.Sign(player.Center.X - NPC.Center.X) == NPC.direction)
                modifiers.FinalDamage *= ShieldDamageReduction;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (_shielding && Math.Sign(projectile.Center.X - NPC.Center.X) == NPC.direction)
                modifiers.FinalDamage *= ShieldDamageReduction;
        }

        // ── Melee hitbox helper ───────────────────────────────────────────────────
        // Arms the tracked blade check (see ArmBladeHit/TickBladeHit) with this swing's reach
        // instead of hitting immediately — the hit only actually lands on the tick(s) the weapon
        // sprite's real hand→tip line genuinely overlaps a player, matching the drawn swing arc.
        protected void TryMeleeHit(float reach = -1f)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            float r = reach < 0 ? MeleeRange * 0.7f : reach;
            ArmBladeHit(r, MeleeDamage, knockback: 3f);
        }

        /// <summary>Closes a bespoke attack's tracked-blade window without changing phase. Ordinary melee
        /// phases disarm automatically when they enter recovery; Custom phases need an explicit close so
        /// their harmless follow-through / landing tail cannot remain flagged as an active strike.</summary>
        protected void StopMeleeHit()
        {
            _bladeArmed = false;
            _hasPreviousBladeSample = false;
            _backHand.HasPreviousBladeSample = false;
            _bladeHitPlayers.Clear();
        }

        /// <summary>
        /// Arms the tracked blade check for the swing/step that's about to play: TickBladeHit will
        /// test every subsequent tick (until the phase ends) and only actually connect on the
        /// tick(s) where the weapon sprite's real swept position overlaps a player. Called by
        /// TryMeleeHit and the base DoComboMeleeHit; an override that skips this entirely (e.g.
        /// BlackNinja's flail combo, which fires a real physical projectile instead of a blade
        /// sweep) correctly leaves TickBladeHit a no-op for that swing.
        /// </summary>
        private void ArmBladeHit(float reach, int damage, float knockback)
        {
            _bladeArmed           = true;
            _activeBladeReach     = reach;
            _activeBladeDamage    = damage;
            _activeBladeKnockback = knockback;
            _hasPreviousBladeSample = false;
            _backHand.HasPreviousBladeSample = false;
            _bladeHitPlayers.Clear();
        }

        /// <summary>
        /// Per-tick "is the blade actually on the player" check. Call every tick while a melee
        /// swing/step is live (MeleeAttack / StabAttack / SpearAttack / MeleeComboAttack). Traces a
        /// capsule from the hand to the weapon tip along <see cref="GetWeaponWorldDirection"/> — the
        /// exact angle actually rendered — so a hit can only land on the tick(s) the sprite is
        /// genuinely overlapping the target, instead of for the swing's entire duration regardless
        /// of where the blade is pointing (the old static-box behavior).
        /// Dual wield: tests the capsule of every hand the current step swings with. Both hands share one
        /// hit list, so a Both step still strikes each player at most once.
        /// </summary>
        protected void TickBladeHit()
        {
            if (!_bladeArmed || _activeBladeReach <= 0f || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            ComboHand swingHand = ActiveComboSwingHand;

            if (swingHand != ComboHand.Back)
            {
                // Capsule thickness follows the equipped weapon, so a swapped-in weapon is tested at its
                // own width instead of whatever the puppet's default melee weapon happened to be.
                Vector2 origin = GetHandPosition();
                Vector2 tip = origin + GetWeaponWorldDirection() * _activeBladeReach;
                TestBladeCapsule(origin, tip, FrontHandWeapon.BladeWidth,
                    ref _hasPreviousBladeSample, ref _previousBladeOrigin, ref _previousBladeTip);
            }

            if (swingHand != ComboHand.Front)
            {
                Vector2 backOrigin = GetBackHandPosition();
                Vector2 backTip = backOrigin + GetBackWeaponWorldDirection() * _activeBladeReach;
                bool backHasPrevious = _backHand.HasPreviousBladeSample;
                Vector2 backPreviousOrigin = _backHand.PreviousBladeOrigin;
                Vector2 backPreviousTip = _backHand.PreviousBladeTip;

                TestBladeCapsule(backOrigin, backTip, BackHandWeapon.BladeWidth,
                    ref backHasPrevious, ref backPreviousOrigin, ref backPreviousTip);

                _backHand.HasPreviousBladeSample = backHasPrevious;
                _backHand.PreviousBladeOrigin = backPreviousOrigin;
                _backHand.PreviousBladeTip = backPreviousTip;
            }
        }

        // One hand's blade this tick: sweep from last tick's capsule to this one (so fast arcs can't tunnel)
        // against every player not yet hit, spawn the damage hitbox on overlap, then store this tick's capsule.
        private void TestBladeCapsule(Vector2 origin, Vector2 tip, float bladeWidth,
            ref bool hasPreviousSample, ref Vector2 previousOrigin, ref Vector2 previousTip)
        {
            float earlyOutRange = _activeBladeReach + bladeWidth + 40f;
            if (HasSpectralOverlay)
                earlyOutRange += Vector2.Distance(NPC.Center, origin);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || _bladeHitPlayers.Contains(i))
                {
                    continue;
                }
                if (Vector2.DistanceSquared(player.Center, NPC.Center) > earlyOutRange * earlyOutRange)
                {
                    continue;
                }

                Vector2 hitOrigin = origin;
                Vector2 hitTip = tip;
                bool intersects = hasPreviousSample
                    ? MeleeBladeCollision.SweptSegmentIntersectsRect(
                        previousOrigin,
                        previousTip,
                        origin,
                        tip,
                        bladeWidth,
                        player.getRect(),
                        out hitOrigin,
                        out hitTip)
                    : MeleeBladeCollision.SegmentIntersectsRect(
                        origin, tip, bladeWidth, player.getRect());
                if (!intersects)
                    continue;

                Vector2 hitPoint = MeleeBladeCollision.ClosestPointOnSegment(hitOrigin, hitTip, player.Center);
                SpawnMeleeHitbox(hitPoint, bladeWidth, _activeBladeDamage, _activeBladeKnockback);
                _bladeHitPlayers.Add(i);
                _lastAttackHitConnected = true;
                if (IsMeleeComboPhase)
                    _currentComboStepHitConnected = true;
                OnBladeHit(player);
            }

            previousOrigin = origin;
            previousTip = tip;
            hasPreviousSample = true;
        }

        /// <summary>Fires once per confirmed real blade-overlap hit (see <see cref="TickBladeHit"/>),
        /// right after the damage/knockback hitbox is spawned. Override for per-puppet on-hit
        /// effects (debuffs, extra VFX, etc.) without touching the shared hit-detection code.</summary>
        protected virtual void OnBladeHit(Player player) { }

        /// <summary>
        /// Spawn a small, momentary hostile hitbox at a confirmed blade-overlap point. Geometry is
        /// already resolved by the caller (TickBladeHit) — this is just the trigger that hands
        /// damage/knockback/immune-frame handling to vanilla's existing hostile-projectile pipeline.
        /// </summary>
        private void SpawnMeleeHitbox(Vector2 center, float size, int damage, float knockback)
        {
            int box = (int)size;
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), center, Vector2.Zero,
                ModContent.ProjectileType<PuppetMeleeHitbox>(),
                damage, knockback, Main.myPlayer, box, box);
        }

        // ── Telegraph dust ────────────────────────────────────────────────────────
        private void SpawnTelegraphDust()
        {
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(NPC.Center - new Vector2(8f), 16, 16, 89,
                    Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.5f),
                    0, default, 1.2f);
                dust.noGravity = true;
            }
        }

        // ── Telegraph flash VFX ───────────────────────────────────────────────────
        /// <summary>
        /// Fires the ring-flash VFX once the attack is within the requested lead window.
        /// Call each tick during a telegraph phase; self-gates via <see cref="_flashFired"/>.
        /// Telegraphs shorter than the requested lead fire immediately on the first call.
        /// </summary>
        private void CheckAndFireFlash(Color color, int leadTicks = 30)
        {
            if (_flashFired || PhaseTimer > Math.Max(1, leadTicks))
            {
                return;
            }
            SpawnTelegraphFlash(color);
            _flashFired = true;
        }

        /// <summary>One-shot colored flash + dust burst, usable by a subclass for its own extra
        /// telegraph beats (e.g. a second flash marking a dive variant) beyond the standard per-phase
        /// CheckAndFireFlash cadence.</summary>
        protected void SpawnTelegraphFlash(Color color)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectileDirect(
                NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<TelegraphFlash>(),
                0, 0, Main.myPlayer,
                UsefulFunctions.ColorToFloat(color));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Weapon visual
        // ─────────────────────────────────────────────────────────────────────────

        protected void SetDisplayWeapon(int itemType, bool swing)
        {
            // A hard swap to a DIFFERENT weapon without starting a new swing (e.g. a ranged burst
            // handing back to the melee weapon for recovery) must not carry over the old weapon's
            // leftover countdown: _weaponAnimMax below is recomputed for the new item, but _weaponAnim
            // itself was left counting down against the OLD item's timing. For a few ticks the new
            // weapon then renders mid-pose using a swing-progress fraction that belongs to a
            // differently-timed animation - a visible flash of a wrong-looking pose/sprite right at the
            // handoff. Same weapon re-asserting its own display (itemType unchanged) is unaffected.
            if (!swing && itemType != _heldItemType)
                _weaponAnim = 0;

            _heldItemType = itemType;
            _weaponAnimMax = GetWeaponUseAnimation(itemType);
            if (swing)
                _weaponAnim = _weaponAnimMax;
        }

        private int GetWeaponUseAnimation(int itemType)
        {
            if (itemType <= 0)
            {
                return DefaultWeaponAnimMax;
            }

            Item item = GetCachedWeaponItem(itemType);
            int useAnimation = item?.useAnimation ?? 0;
            return Math.Max(1, useAnimation > 0 ? useAnimation : DefaultWeaponAnimMax);
        }

        protected virtual int GetMeleeSwingTicks(int requestedTicks)
        {
            return requestedTicks > 0 ? requestedTicks : GetWeaponUseAnimation(MeleeWeaponItemType);
        }

        /// <summary>Share of a logical wind-up spent easing from the carry pose to the arc's far end
        /// before the raise to the attack start begins. 0.25 = the first quarter. A boss can lengthen
        /// it to hold a menacing raised pose longer (Gwyn's Wrath Flurry).</summary>
        protected virtual float LogicalWindupSettleFraction => 0.25f;

        private float LogicalSwingWindup(float oppositeEnd, float attackStart, float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            float settleFraction = MathHelper.Clamp(LogicalWindupSettleFraction, 0.05f, 0.95f);
            if (progress < settleFraction)
            {
                float settle = MathHelper.SmoothStep(0f, 1f, progress / settleFraction);
                return MathHelper.Lerp(MeleeCarryRotation, oppositeEnd, settle);
            }

            float raise = MathHelper.SmoothStep(0f, 1f,
                (progress - settleFraction) / (1f - settleFraction));
            return MathHelper.Lerp(oppositeEnd, attackStart, raise);
        }

        private void TickWeaponAnim()
        {
            if (_attackRuntimeV2.Active)
            {
                // Runtime V2 already sampled the authoritative pose in PuppetAttackAI. Keep only
                // renderer-side bookkeeping here; the legacy animation clock must not overwrite it.
                SpawnSwingVFX(_weaponRotation - _prevWeaponRotation);
                _prevWeaponRotation = _weaponRotation;
                TickHandPoses();
                return;
            }

            if (_weaponAnim > 0)
                _weaponAnim--;

            float t = _weaponAnimMax > 0 ? 1f - (float)_weaponAnim / _weaponAnimMax : 1f;

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
                        // Arm extends level forward toward target for bow draw.
                        _weaponRotation = MathHelper.Lerp(_weaponRotation, 0.0f, 0.22f);
                        break;
                    case RangedStyle.Staff:
                        _weaponRotation = MathHelper.SmoothStep(-0.18f, -0.78f, rangedT);
                        break;
                    default: // Throw
                        // Hold the hand high while lining up the throw.
                        _weaponRotation = MathHelper.Lerp(-0.75f, -0.10f, rangedT);
                        break;
                }
                DoRangedTelegraphVFX(_usingSecondaryRanged, MathHelper.Clamp(rangedT, 0f, 1f));
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
                        // Small release recoil / snap
                        _weaponRotation = MathHelper.Lerp(-0.08f, 0.05f, rangedT);
                        break;
                    case RangedStyle.Staff:
                        _weaponRotation = MathHelper.Lerp(-0.78f, -0.60f, rangedT);
                        break;
                    default: // Throw
                        // Swing the arm forward as the projectile leaves the hand.
                        _weaponRotation = MathHelper.Lerp(-0.10f, 0.45f, rangedT);
                        break;
                }
            }
            else if (Phase == AttackPhase.CrossbowBurstPause)
            {
                // Hold aim between shots at the same target angle each style's own telegraph uses
                // (was hardcoded to Crossbow's regardless of style, so a Bow burst pattern held the
                // Crossbow angle between shots instead of its own).
                float burstPauseTarget = _activeRangedStyle == RangedStyle.Bow ? 0.0f : 0.05f;
                _weaponRotation = MathHelper.Lerp(_weaponRotation, burstPauseTarget, 0.22f);
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
                if (UseAuthoredMagicCastPose)
                {
                    float eased = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(magicT, 0f, 1f));
                    _weaponRotation = MathHelper.Lerp(MagicCastStartRotation, MagicCastEndRotation, eased);
                    DoMagicTelegraphVFX(MathHelper.Clamp(magicT, 0f, 1f));
                }
                else
                {
                    // Arm rises overhead during the charge (Use1 / raised overhead pose).
                    _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.40f, 0.12f);
                }
            }
            else if (Phase == AttackPhase.MagicAttack)
            {
                if (UseAuthoredMagicCastPose)
                {
                    // Release at the completed raise angle and hold through follow-through.
                    _weaponRotation = MagicCastEndRotation;
                }
                else
                {
                    float magicT = MagicAttackTicks > 0
                        ? 1f - (float)PhaseTimer / MagicAttackTicks
                        : 1f;
                    // Thrust forward as the spell fires.
                    _weaponRotation = MathHelper.Lerp(-1.40f, 0.20f, magicT);
                }
            }
            else if (IsHoldingMagicWeaponDuringRecovery && UseAuthoredMagicCastPose)
            {
                _weaponRotation = MagicCastEndRotation;
            }
            else if (UseAuthoredBreathHandPose && Phase == AttackPhase.BreathTelegraph)
            {
                float breathT = BreathTelegraphTicks > 0
                    ? 1f - PhaseTimer / (float)BreathTelegraphTicks
                    : 1f;
                _weaponRotation = MathHelper.SmoothStep(BreathHandStartRotation, BreathHandEndRotation,
                    MathHelper.Clamp(breathT, 0f, 1f));
            }
            else if (UseAuthoredBreathHandPose && Phase == AttackPhase.Breathing)
            {
                _weaponRotation = BreathHandEndRotation;
            }
            else if (UseAuthoredBreathHandPose && Phase == AttackPhase.BreathRecovery)
            {
                float recoveryT = BreathRecoveryTicks > 0
                    ? 1f - PhaseTimer / (float)BreathRecoveryTicks
                    : 1f;
                _weaponRotation = MathHelper.SmoothStep(BreathHandEndRotation, HoldRotation,
                    MathHelper.Clamp(recoveryT, 0f, 1f));
            }
            else if (Phase == AttackPhase.PierceTelegraph)
            {
                // Same "cocked, arm extended toward the player" pose as JoustDash's telegraph.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.22f);
            }
            else if (Phase == AttackPhase.PierceDash)
            {
                // Held straight forward, tip leading — same active-thrust angle as JoustDash.
                Player diveTarget = Main.player[NPC.target];
                Vector2 aim = diveTarget.Center - NPC.Center;
                if (Math.Abs(aim.X) > 2f)
                    NPC.direction = NPC.spriteDirection = aim.X < 0f ? -1 : 1;
                float worldAngle = aim.SafeNormalize(new Vector2(NPC.direction, 0f)).ToRotation();
                float targetRotation = NPC.direction == 1
                    ? worldAngle + MathHelper.PiOver4 - MeleeWeaponRotationOffset
                    : MathHelper.Pi + MathHelper.PiOver4 - worldAngle + MeleeWeaponRotationOffset;
                _weaponRotation = _weaponRotation.AngleLerp(targetRotation, 0.45f);
            }
            else if (Phase == AttackPhase.PierceStabHold)
            {
                // Raise 0→90°: from straight-forward (PiOver4, "3 o'clock") up to overhead-vertical
                // (-PiOver4, "12 o'clock") as the impaled target is lifted. The raise itself plays out
                // over PierceStabRaiseAnimTicks (a quick snap by default the same as the whole hold,
                // but a subclass can shorten it) - once elapsed, the pose just HOLDS at vertical for
                // the rest of PierceStabRaiseTicks instead of continuing to visibly rotate.
                int elapsedTicks = PierceStabRaiseTicks - PhaseTimer;
                float raiseT = PierceStabRaiseAnimTicks > 0
                    ? MathHelper.Clamp(elapsedTicks / (float)PierceStabRaiseAnimTicks, 0f, 1f)
                    : 1f;
                _weaponRotation = MathHelper.Lerp(MathHelper.PiOver4, -MathHelper.PiOver4, raiseT);
            }
            else if (Phase == AttackPhase.PierceStabFlick)
            {
                // Hold the raised pose through an optional delayed blast, then snap back down and
                // past horizontal in sync with the actual release.
                int elapsedTicks = PierceStabFlickTicks - PhaseTimer;
                int releaseDelay = Math.Clamp(PierceStabFlickDelayTicks, 0,
                    Math.Max(0, PierceStabFlickTicks - 1));
                int animationTicks = Math.Max(1, PierceStabFlickTicks - releaseDelay);
                float flickT = MathHelper.Clamp(
                    (elapsedTicks - releaseDelay) / (float)animationTicks, 0f, 1f);
                _weaponRotation = MathHelper.Lerp(-MathHelper.PiOver4, MathHelper.PiOver2, flickT);
            }
            else if (Phase == AttackPhase.JumpSlashDodgeback)
            {
                // No special pose during the backward roll — ease to the normal carried hold.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, HoldRotation, 0.15f);
            }
            else if (Phase == AttackPhase.JumpSlashRise)
            {
                // Cocked "up and back" — 15° past straight-up toward the rear (12 o'clock is -45°;
                // 15° further back is -60°).
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.ToRadians(-60f), 0.30f);
            }
            else if (Phase == AttackPhase.JumpSlashAttack)
            {
                // The half-circle downward swipe: -60° (cocked) sweeping through vertical/forward to
                // JumpSlashEndRotation (+55° by default: past horizontal, angled down-forward, a ~115° arc).
                // Smooth-eased rather than linear, or a Weighted strike when the puppet authors JumpSlashCurve.
                _weaponRotation = BespokeSwingRotation(MathHelper.ToRadians(-60f), JumpSlashEndRotation,
                    JumpSlashAttackTicks, JumpSlashCurve, SwingEaseStyle.Smooth);
            }
            else if (Phase == AttackPhase.FlipSlashRise)
            {
                // Continuous spin while airborne (same free-running style as ComboMotion.Spin) —
                // no fixed start/end, just a steady rotation for as long as the flip lasts.
                float spinSpeed = FlipSlashSpinSpeed;

                if (UseFlipSlashLandingStrike)
                {
                    // Phase-lock: re-solve the speed each tick so the blade reaches the cocked strike pose
                    // just as the feet fall back to launch height. Ballistic fall time (gravity 0.3, +Y down)
                    // t = (-vy + sqrt(vy^2 + 2*g*h)) / g, h = px above launch. The whole-turn count is the one
                    // keeping speed nearest FlipSlashSpinSpeed. Under 3 ticks out, keep the last solve.
                    const float FlipGravity = 0.3f;
                    float heightAboveLaunch = Math.Max(0f, _flipSlashLaunchBottomY - NPC.Bottom.Y);
                    float fallDiscriminant = NPC.velocity.Y * NPC.velocity.Y + 2f * FlipGravity * heightAboveLaunch;
                    float ticksToLand = (-NPC.velocity.Y + (float)Math.Sqrt(fallDiscriminant)) / FlipGravity;

                    if (ticksToLand > 3f)
                    {
                        float forwardToCocked = (FlipSlashStrikeStartRotation - _weaponRotation) % MathHelper.TwoPi;
                        if (forwardToCocked < 0f)
                        {
                            forwardToCocked += MathHelper.TwoPi;
                        }

                        float authoredTurns = (FlipSlashSpinSpeed * ticksToLand - forwardToCocked) / MathHelper.TwoPi;
                        float wholeTurns = Math.Max(0f, (float)Math.Round(authoredTurns));
                        _flipSlashSolvedSpinSpeed = (forwardToCocked + wholeTurns * MathHelper.TwoPi) / ticksToLand;
                    }

                    spinSpeed = _flipSlashSolvedSpinSpeed;
                }

                _weaponRotation += spinSpeed;
                if (_weaponRotation > MathHelper.TwoPi)
                {
                    _weaponRotation -= MathHelper.TwoPi;
                }
            }
            else if (Phase == AttackPhase.FlipSlashLand && UseFlipSlashLandingStrike)
            {
                // Weighted overhead into the ground from the touchdown angle; ApplyWeighted clamps elapsed,
                // so once the strike's ticks are spent it simply holds the end pose for the rest of the phase.
                int elapsedStrike = FlipSlashLandHoldTicks - PhaseTimer;
                int strikeTicks = FlipSlashStrikeEaseInTicks + FlipSlashStrikeEaseOutTicks;
                _weaponRotation = SwingEase.ApplyWeighted(_flipSlashStrikeStartRotation, FlipSlashStrikeEndRotation,
                    elapsedStrike, strikeTicks, FlipSlashStrikeEaseInTicks, FlipSlashStrikeEaseOutTicks,
                    FlipSlashStrikeEaseOutDecay);
            }
            else if (Phase == AttackPhase.FlipSlashLand)
            {
                // Settles into the landing slam pose (195° clockwise from straight-up, i.e. past
                // 6 o'clock toward 7) over the first few ticks, then holds it for the rest of the
                // phase. It used to snap there in one frame from wherever the free spin happened to
                // be when the feet touched down, which read as a dropped frame. AngleLerp takes the
                // short way round so the blade never unwinds backwards to reach the pose.
                const int FlipSlashLandSettleTicks = 6;
                float slamPose = MathHelper.ToRadians(195f - 45f);
                int elapsedLand = FlipSlashLandHoldTicks - PhaseTimer;
                float settleT = MathHelper.Clamp(elapsedLand / (float)FlipSlashLandSettleTicks, 0f, 1f);

                _weaponRotation = _weaponRotation.AngleLerp(slamPose, MathHelper.SmoothStep(0f, 1f, settleT));
            }
            else if (Phase == AttackPhase.AbyssSlashTelegraph)
            {
                // Underhand, facing down (180°) arcing 170° to the held "sword post" pose (10°, blade
                // near-vertical, grip low) — held there once the arc portion of the timer runs out.
                int totalAbyssTicks = AbyssSlashArcTicks + AbyssSlashHoldTicks;
                int elapsedAbyss    = totalAbyssTicks - PhaseTimer;
                if (elapsedAbyss < AbyssSlashArcTicks)
                {
                    // SmoothStep, not a raw Lerp: the raise eases out of the low pose and settles
                    // into the held post instead of travelling at one speed and stopping dead.
                    float armT = AbyssSlashArcTicks > 0 ? elapsedAbyss / (float)AbyssSlashArcTicks : 1f;
                    _weaponRotation = MathHelper.SmoothStep(MathHelper.ToRadians(180f - 45f), MathHelper.ToRadians(10f - 45f), armT);
                }
                else
                {
                    _weaponRotation = MathHelper.ToRadians(10f - 45f);
                }
            }
            else if (Phase == AttackPhase.AbyssSlashSwipe)
            {
                // Quick release swing: from the held "post" pose back down through to a forward-down
                // follow-through, matching where the projectile fires from. Snap-eased — most of the
                // arc is spent in the first third, so the release reads as a whip-crack off the hold
                // rather than a uniform sweep, then decelerates into the follow-through.
                // An authored AbyssSlashSwipeCurve replaces the Snap with a Weighted strike on real ticks.
                _weaponRotation = BespokeSwingRotation(MathHelper.ToRadians(10f - 45f), MathHelper.ToRadians(170f - 45f),
                    AbyssSlashSwipeTicks, AbyssSlashSwipeCurve, SwingEaseStyle.Snap);
            }
            else if (Phase == AttackPhase.TendrilTelegraph || Phase == AttackPhase.TendrilReach)
            {
                // Arm reaches out toward the target - same "cocked, extended forward" pose as
                // Piercing Dash's telegraph.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.20f);
            }
            else if (Phase == AttackPhase.TendrilSwingTelegraph)
            {
                // The arm returns and winds up the finishing swing: same 180°→10° arc as Abyss
                // Slash's wind-up, held at 10° once the arc portion completes.
                int totalTendrilTicks = TendrilSwingArcTicks + TendrilSwingHoldTicks;
                int elapsedTendril    = totalTendrilTicks - PhaseTimer;
                if (elapsedTendril < TendrilSwingArcTicks)
                {
                    float armT = TendrilSwingArcTicks > 0 ? elapsedTendril / (float)TendrilSwingArcTicks : 1f;
                    _weaponRotation = MathHelper.SmoothStep(MathHelper.ToRadians(180f - 45f), MathHelper.ToRadians(10f - 45f), armT);
                }
                else
                {
                    _weaponRotation = MathHelper.ToRadians(10f - 45f);
                }
            }
            else if (Phase == AttackPhase.TendrilSwing)
            {
                // Same 10°→170° release as Abyss Slash's swipe - the "standard" underhand swing shape,
                // sharing its Snap easing so the two read as the same motion.
                // TendrilSwingCurve / TendrilSwingEndRotation let a puppet retime and widen it.
                _weaponRotation = BespokeSwingRotation(MathHelper.ToRadians(10f - 45f), TendrilSwingEndRotation,
                    TendrilSwingTicks, TendrilSwingCurve, SwingEaseStyle.Snap);
            }
            else if (Phase == AttackPhase.HomingVolleySwingTelegraph)
            {
                // Raise the sword fully overhead, cocked back further than Jump Slash's cocked pose -
                // a bigger, more deliberate wind-up for the volley's release chop.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.ToRadians(-100f), 0.25f);
            }
            else if (Phase == AttackPhase.HomingVolleySwing)
            {
                // Full overhead chop: cocked back down through vertical to a down-forward
                // follow-through - the volley fires partway through this arc. Smooth-eased so the
                // heavy blade builds speed out of the raise and settles at the end; the fire point
                // is a progress fraction of the same clock, so it still lands mid-arc.
                _weaponRotation = BespokeSwingRotation(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f),
                    HomingVolleySwingTicks, HomingVolleySwingCurve, SwingEaseStyle.Smooth);
            }
            else if (Phase == AttackPhase.SpiralFanSwingTelegraph && UseAuthoredSpiralFanCastPose)
            {
                float fanT = SpiralFanSwingTelegraphTicks > 0 ? 1f - PhaseTimer / (float)SpiralFanSwingTelegraphTicks : 1f;
                _weaponRotation = MathHelper.SmoothStep(SpiralFanCastStartRotation, SpiralFanCastEndRotation, fanT);
            }
            else if (Phase == AttackPhase.SpiralFanSwing && UseAuthoredSpiralFanCastPose)
            {
                _weaponRotation = SpiralFanCastEndRotation;
            }
            else if ((Phase == AttackPhase.SpiralFanBurst || Phase == AttackPhase.SpiralFanPause) && UseAuthoredSpiralFanCastPose)
            {
                _weaponRotation = SpiralFanCastEndRotation;
            }
            else if (Phase == AttackPhase.BoomerangSwingTelegraph || Phase == AttackPhase.SpiralFanSwingTelegraph)
            {
                // Same overhead cocked-back wind-up as Homing Volley - all three "sword launch"
                // attacks share one visual identity for the raise.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.ToRadians(-100f), 0.25f);
            }
            else if (Phase == AttackPhase.BoomerangSwing)
            {
                // Same overhead chop shape (and easing) as Homing Volley - the crescent(s) fire
                // partway through.
                _weaponRotation = BespokeSwingRotation(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f),
                    BoomerangSwingTicks, BoomerangSwingCurve, SwingEaseStyle.Smooth);
            }
            else if (Phase == AttackPhase.SpiralFanSwing)
            {
                // Same overhead chop shape (and easing) again, but purely a visual wind-up here - the
                // burst that follows (SpiralFanBurst/Pause) carries the actual firing sequence.
                _weaponRotation = BespokeSwingRotation(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f),
                    SpiralFanSwingTicks, SpiralFanSwingCurve, SwingEaseStyle.Smooth);
            }
            else if (Phase == AttackPhase.SpiralFanBurst || Phase == AttackPhase.SpiralFanPause)
            {
                // Hold the follow-through pose for the whole burst rather than resetting to neutral
                // between shots.
                _weaponRotation = MathHelper.ToRadians(70f);
            }
            else if (Phase == AttackPhase.FireVolleyBackLeap || Phase == AttackPhase.FireVolleyDodgeThrough)
            {
                // Just carry the axe through the reposition move — no swing here.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, HoldRotation, 0.15f);
            }
            else if (Phase == AttackPhase.FireVolleyArcJump)
            {
                // Same shape as LeapSlam: raise overhead on the way up, chop down through the apex.
                if (NPC.velocity.Y < 0f)
                {
                    _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.3f, 0.20f);
                }
                else
                {
                    _weaponRotation = MathHelper.Lerp(_weaponRotation, 1.3f, 0.18f);
                }
            }
            else if (Phase == AttackPhase.KnivesTelegraph || Phase == AttackPhase.KnivesThrowPause)
            {
                // Hold the knife high, ready to throw (same read as the throwing-star telegraph).
                _weaponRotation = MathHelper.Lerp(_weaponRotation, -0.75f, 0.20f);
            }
            else if (Phase == AttackPhase.KnivesThrow)
            {
                // Swing the arm forward as the knives leave the hand.
                float kt = CursedKnivesThrowTicks > 0 ? 1f - (float)PhaseTimer / CursedKnivesThrowTicks : 1f;
                _weaponRotation = MathHelper.Lerp(-0.75f, 0.45f, kt);
            }
            else if (Phase == AttackPhase.MeleeTelegraph || Phase == AttackPhase.MeleeAttack)
            {
                // Same OverheadArc shape as the combo system's OverheadArc case, so the plain
                // one-shot swing (DoMeleeAttack/TryMeleeHit, used outside the combo system) gets
                // the same easing/flip/aim-bias opt-ins instead of only combos getting them.
                float a0 = -1.3f - OverheadWindupOvershoot, a1 = 1.0f;
                ModifyMeleeArcEndpoints(ComboMotion.OverheadArc, ref a0, ref a1);
                if (UseAlternateFlip && _comboSwingFlipped)
                {
                    (a0, a1) = (a1, a0);
                }
                if (UseAimAdaptiveArc || AimSwingActive)
                {
                    a0 += _comboAimBias;
                    a1 += _comboAimBias;
                }

                if (Phase == AttackPhase.MeleeTelegraph)
                {
                    if (UseLogicalMeleeTelegraphs)
                    {
                        float telegraphT = 1f - PhaseTimer / (float)Math.Max(1, MeleeTelegraphTicks);
                        _weaponRotation = LogicalSwingWindup(a1, a0, telegraphT);
                    }
                    else
                    {
                        // Wind-up: weapon rises from hold angle to the top of the arc quickly.
                        _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.30f);
                    }
                }
                else if (MeleeAttackCurve.IsSet)
                {
                    // Authored Weighted downswing on the phase's own ticks. MeleeAttackTicks should equal the
                    // curve's TotalTicks, so it still ends with the weapon's use animation.
                    int elapsedSwingTicks = GetMeleeSwingTicks(MeleeAttackTicks) - PhaseTimer;
                    _weaponRotation = MeleeAttackCurve.Apply(a0, a1, elapsedSwingTicks);
                }
                else
                {
                    // Downswing: full broadsword arc, raised behind head → slash down-forward
                    // t runs 0->1 over the held item useAnimation window (reset when swing begins)
                    _weaponRotation = SwingEase.Apply(a0, a1, t, UseSwingEasing || AimSwingActive);
                }
            }
            else if (Phase == AttackPhase.MeleeComboTelegraph
                  || Phase == AttackPhase.MeleeComboAttack
                  || Phase == AttackPhase.MeleeComboPause)
            {
                // Drive rotation from the current step's ComboMotion.
                var step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                bool inTel = Phase == AttackPhase.MeleeComboTelegraph;
                bool inPause = Phase == AttackPhase.MeleeComboPause;
                float comboTelegraphT = inTel
                    ? 1f - PhaseTimer / (float)Math.Max(1, GetComboTelegraphTicks(step))
                    : 1f;

                // Applies the alternating-flip / aim-adaptive-bias opt-ins (both default off) to a
                // motion's fixed (start, end) arc endpoints. Telegraph/pause poses below are all
                // expressed as a Lerp fraction of these same endpoints (not separate hardcoded
                // angles), so flip/bias stay consistent across wind-up -> strike with no snap.
                // Base angles come from WeaponArchetypeTables.SwingArcEndpoints so the offline preview
                // harness swings the same numbers as the game; only the per-puppet transforms below
                // stay here, because they depend on live instance state.
                (float, float) Endpoints(ComboMotion motion)
                {
                    (float a0, float a1) = WeaponArchetypeTables.SwingArcEndpoints(
                        motion, OverheadWindupOvershoot);
                    ModifyMeleeArcEndpoints(motion, ref a0, ref a1);
                    if (UseAlternateFlip && _comboSwingFlipped)
                    {
                        (a0, a1) = (a1, a0);
                    }
                    if (UseAimAdaptiveArc || AimSwingActive)
                    {
                        a0 += _comboAimBias;
                        a1 += _comboAimBias;
                    }
                    return (a0, a1);
                }

                // Handoff smoothing (aim-swing pilots): during a pause, ease toward where the NEXT
                // step's arc STARTS rather than re-raising to the outgoing motion's apex — so e.g.
                // Down-Up's overhead flows straight into the rising cut instead of snapping ~2 rad.
                bool handoffEased = false;
                if (inPause && MeleeComboInterStepLingerTicks > 0)
                {
                    int pauseTotal = Math.Max(1, step.PostStepPause);
                    int elapsedPause = Math.Max(0, pauseTotal - PhaseTimer);
                    int lingerTicks = Math.Min(MeleeComboInterStepLingerTicks, pauseTotal);
                    if (elapsedPause < lingerTicks)
                    {
                        // Preserve the exact last damaging-frame pose. Collision is already disarmed
                        // by the combo executor, so this communicates weight without extending danger.
                        handoffEased = true;
                    }
                    else if (_meleeComboStepIndex + 1 < _activeMeleeCombo.Steps.Length)
                    {
                        float target = ComboStepStartRotation(_activeMeleeCombo.Steps[_meleeComboStepIndex + 1]);
                        // A step whose rotation accumulates past a single turn (Spin) can leave
                        // _weaponRotation many radians from `target` in raw value space even though
                        // the SHORT way around is small. Re-express it as the equivalent angle nearest
                        // `target` first, or the handoff below sweeps most of a full circle instead of
                        // the true gap (this is what previously made Spin's own recovery rewind).
                        _weaponRotation = target + MathHelper.WrapAngle(_weaponRotation - target);
                        int transitionTicks = Math.Max(1, pauseTotal - lingerTicks);
                        float transition = MathHelper.Clamp(
                            (elapsedPause - lingerTicks + 1f) / transitionTicks, 0f, 1f);
                        _weaponRotation = MathHelper.SmoothStep(_weaponRotation, target, transition);
                        handoffEased = true;
                    }
                }
                else if (inPause && AimSwingActive
                    && _meleeComboStepIndex + 1 < _activeMeleeCombo.Steps.Length)
                {
                    float target = ComboStepStartRotation(_activeMeleeCombo.Steps[_meleeComboStepIndex + 1]);
                    _weaponRotation = target + MathHelper.WrapAngle(_weaponRotation - target);
                    _weaponRotation = MathHelper.Lerp(_weaponRotation, target, 0.22f);
                    handoffEased = true;
                }

                if (!handoffEased)
                switch (step.Motion)
                {
                    case ComboMotion.OverheadArc:
                    {
                        var (a0, a1) = Endpoints(ComboMotion.OverheadArc);

                        if (inTel)
                        {
                            _weaponRotation = UseLogicalMeleeTelegraphs
                                ? LogicalSwingWindup(a1, a0, comboTelegraphT)
                                : MathHelper.Lerp(_weaponRotation, a0, 0.30f);
                        }
                        else if (inPause)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.Lerp(a0, a1, 0.130f), 0.20f);
                        }
                        else
                        {
                            _weaponRotation = ApplySwingEase(a0, a1, t, step);
                        }
                        break;
                    }
                    case ComboMotion.UnderhandArc:
                    {
                        // Rising cut: dipped low-forward (+1.0) → up-FORWARD (-1.0).  Ending at
                        // -1.3 would map to body row 1, whose hand offset sits BEHIND the head
                        // (X=-8) — making the swing finish over the shoulder.  -1.0 keeps it in
                        // row 2 (hand up-forward, X=+4) for a clean rising slash.
                        var (a0, a1) = Endpoints(ComboMotion.UnderhandArc);

                        if (inTel)
                        {
                            _weaponRotation = UseLogicalMeleeTelegraphs
                                ? LogicalSwingWindup(a1, a0, comboTelegraphT)
                                : MathHelper.Lerp(_weaponRotation, a0, 0.30f);
                        }
                        else if (inPause)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.Lerp(a0, a1, 0.150f), 0.20f);
                        }
                        else
                        {
                            _weaponRotation = ApplySwingEase(a0, a1, t, step);
                        }
                        break;
                    }
                    case ComboMotion.HorizontalSweep:
                    {
                        // Flat side-to-side: arm extends, weapon held near horizontal
                        var (a0, a1) = Endpoints(ComboMotion.HorizontalSweep);
                        if (inTel)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.25f);
                        }
                        else if (inPause)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.Lerp(a0, a1, 0.700f), 0.18f);
                        }
                        else
                        {
                            _weaponRotation = ApplySwingEase(a0, a1, t, step);
                        }
                        break;
                    }
                    case ComboMotion.VerticalChop:
                    {
                        // Straight overhead → straight down (hammer)
                        var (a0, a1) = Endpoints(ComboMotion.VerticalChop);
                        if (inTel)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.32f);
                        }
                        else if (inPause)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.Lerp(a0, a1, 0.119f), 0.18f);
                        }
                        else
                        {
                            _weaponRotation = ApplySwingEase(a0, a1, t, step);
                        }
                        break;
                    }
                    case ComboMotion.Thrust:
                        if (inTel)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.20f);
                        }
                        else if (inPause)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2 * 0.8f, 0.18f);
                        }
                        else
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.42f);
                        }
                        break;
                    case ComboMotion.JoustDash:
                    {
                        // Same architecture as the arc swings above (Endpoints + LogicalSwingWindup +
                        // ApplySwingEase), just a much smaller sweep — couch the lance back, then
                        // extend to level. Used to be a bare exponential Lerp-toward-target every
                        // tick, which never actually arrives (each tick only closes part of the
                        // REMAINING distance), reading as "creeps, slows down, then just kind of
                        // stops" instead of one continuous motion.
                        var (a0, a1) = Endpoints(ComboMotion.JoustDash);

                        if (inTel)
                        {
                            _weaponRotation = UseLogicalMeleeTelegraphs
                                ? LogicalSwingWindup(a1, a0, comboTelegraphT)
                                : MathHelper.Lerp(_weaponRotation, a0, 0.18f);
                        }
                        else if (inPause)
                        {
                            // Single-step combo today — nothing to hand off to, just hold the landed pose.
                            _weaponRotation = a1;
                        }
                        else
                        {
                            _weaponRotation = ApplySwingEase(a0, a1, t, step);
                        }
                        break;
                    }
                    case ComboMotion.Spin:
                    {
                        // Continuous rotation; 1 full revolution per held item useAnimation-ish window.
                        const float peakSpinSpeed = 0.28f;
                        float spinSpeedMult = 1f;

                        if (UseEasedSpin)
                        {
                            if (inTel)
                            {
                                // Wind-up: ramp 0 -> full speed across the telegraph, so the windmill
                                // visibly spins UP into the strike instead of already spinning at full
                                // speed on the very first telegraph frame.
                                spinSpeedMult = MathHelper.SmoothStep(0f, 1f, comboTelegraphT);
                            }
                            else
                            {
                                // Attack: hold full speed, then ease DOWN over the step's own
                                // EaseOutTicks (reused here as "ticks to ramp down") so the strike
                                // visibly slows into its recovery instead of snapping to a dead stop.
                                int easeOutTicks = Math.Max(1, step.EaseOutTicks > 0 ? step.EaseOutTicks : 10);
                                if (PhaseTimer < easeOutTicks)
                                {
                                    spinSpeedMult = MathHelper.SmoothStep(0f, 1f, PhaseTimer / (float)easeOutTicks);
                                }
                            }
                        }

                        _weaponRotation += peakSpinSpeed * spinSpeedMult;
                        if (_weaponRotation > MathHelper.TwoPi)
                        {
                            _weaponRotation -= MathHelper.TwoPi;
                        }
                        break;
                    }
                    case ComboMotion.FlailBrace:
                        // The hand is the chain anchor. Ease it forward during the tell, then hold it
                        // still while the ball-and-chain projectile owns every visible orbit and lash.
                        if (inTel)
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.20f);
                        else
                            _weaponRotation = MathHelper.PiOver4;
                        break;
                    case ComboMotion.IaidoDraw:
                    {
                        var (a0, a1) = Endpoints(ComboMotion.IaidoDraw);
                        if (inTel)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.15f);  // weapon held low/behind
                        }
                        else
                        {
                            _weaponRotation = ApplySwingEase(a0, a1, t, step);  // fast snap forward
                        }
                        break;
                    }
                    case ComboMotion.GroundSlam:
                    {
                        var (a0, a1) = Endpoints(ComboMotion.GroundSlam);
                        if (inTel)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.25f);
                        }
                        else
                        {
                            _weaponRotation = ApplySwingEase(a0, a1, t, step);
                        }
                        break;
                    }
                    case ComboMotion.LeapSlam:
                    {
                        if (UseLandingTimedLeapSlam)
                        {
                            if (inTel)
                                _weaponRotation = LogicalSwingWindup(LeapSlamImpactRotation,
                                    LeapSlamCarryRotation, comboTelegraphT);
                            // Active pose is sampled before collision in PuppetAttackAI. Pauses
                            // preserve the planted axe until the shared combo handoff begins.
                            break;
                        }
                        // Wind up overhead, then carry the axe up-and-FORWARD (toward the player,
                        // ~1 o'clock facing right / ~11 facing left) through the airborne arc, and
                        // slam down hard as it lands. Same Endpoints + ApplySwingEase architecture
                        // as JoustDash below: one continuous arc across the whole leap, instead of
                        // three separate Lerp-toward-target chains keyed off velocity.Y's sign that
                        // each converged early and then sat frozen mid-air for the rest of the leap.
                        var (a0, a1) = Endpoints(ComboMotion.LeapSlam);

                        if (inTel)
                        {
                            _weaponRotation = UseLogicalMeleeTelegraphs
                                ? LogicalSwingWindup(a1, a0, comboTelegraphT)
                                : MathHelper.Lerp(_weaponRotation, a0, 0.30f);
                        }
                        else
                        {
                            _weaponRotation = ApplySwingEase(a0, a1, t, step);
                        }
                        break;
                    }
                    case ComboMotion.LeapThrust:
                    {
                        // Telegraph: dip the spear low-forward, cocked for the upcoming poke. Attack:
                        // level out to the thrust angle and hold it through the whole airborne arc for
                        // the landing contact. Same fix as LeapSlam above — was three Lerp-toward-target
                        // chains keyed off velocity.Y that never actually arrived anywhere.
                        var (a0, a1) = Endpoints(ComboMotion.LeapThrust);

                        if (inTel)
                        {
                            _weaponRotation = UseLogicalMeleeTelegraphs
                                ? LogicalSwingWindup(a1, a0, comboTelegraphT)
                                : MathHelper.Lerp(_weaponRotation, a0, 0.22f);
                        }
                        else
                        {
                            _weaponRotation = ApplySwingEase(a0, a1, t, step);
                        }
                        break;
                    }
                    case ComboMotion.ChargeChop:
                        // Carry the axe cocked back/up while charging; the chop is the next step.
                        _weaponRotation = MathHelper.Lerp(_weaponRotation, -0.95f, 0.20f);
                        break;
                    case ComboMotion.Feint:
                    {
                        // Raise exactly like a real overhead attack, then hold the apex as bait.
                        var (a0, a1) = Endpoints(ComboMotion.Feint);
                        _weaponRotation = inTel && UseLogicalMeleeTelegraphs
                            ? LogicalSwingWindup(a1, a0, comboTelegraphT)
                            : MathHelper.Lerp(_weaponRotation, a0, 0.30f);
                        break;
                    }
                    case ComboMotion.DoubleSpinSlam:
                    {
                        var (a0, a1) = Endpoints(ComboMotion.DoubleSpinSlam);
                        if (inTel)
                        {
                            _weaponRotation = UseLogicalMeleeTelegraphs
                                ? LogicalSwingWindup(a1, a0, comboTelegraphT)
                                : MathHelper.Lerp(_weaponRotation, a0, 0.30f);
                        }
                        else if (inPause)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, a1, 0.30f);
                        }
                        else
                        {
                            const float circlePortion = 0.78f;
                            float twoCircleEnd = a0 + MathHelper.TwoPi * 2f;
                            float groundEnd = a1 + MathHelper.TwoPi * 2f;
                            if (t < circlePortion)
                                _weaponRotation = MathHelper.Lerp(a0, twoCircleEnd, t / circlePortion);
                            else
                                _weaponRotation = MathHelper.Lerp(twoCircleEnd, groundEnd,
                                    MathHelper.SmoothStep(0f, 1f, (t - circlePortion) / (1f - circlePortion)));
                        }
                        break;
                    }
                    case ComboMotion.ThrownWeaponRetrieve:
                        if (inTel)
                        {
                            // One harmless 360-degree wind-up makes the throw unmistakable.
                            float circle = MathHelper.SmoothStep(0f, 1f, comboTelegraphT);
                            _weaponRotation = HoldRotation + MathHelper.TwoPi * circle;
                        }
                        else
                        {
                            // Bare throwing/retrieval arm follows through toward the embedded axe.
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, 0.45f, 0.20f);
                        }
                        break;
                    case ComboMotion.LowAxeRun:
                    {
                        // Arm down and slightly back; the corrected axe blade points behind and
                        // down at an angle while the legs keep their normal running animation.
                        const float carry = 1.9f;
                        _weaponRotation = inTel
                            ? MathHelper.SmoothStep(HoldRotation, carry, comboTelegraphT)
                            : MathHelper.Lerp(_weaponRotation, carry, 0.35f);
                        break;
                    }
                    case ComboMotion.RisingUppercutLeap:
                    {
                        // Sweep from the exact running carry into a high, extended finish during
                        // the first weapon-animation window, then freeze there through the ascent
                        // and the explicitly timed 30-frame falling hold.
                        const float carry = 1.9f;
                        const float highFinish = -1.0f;
                        int elapsed = Math.Max(0, step.AttackTicks - PhaseTimer);
                        float rise = MathHelper.Clamp(
                            elapsed / (float)Math.Max(1, GetWeaponUseAnimation(MeleeWeaponItemType)), 0f, 1f);
                        _weaponRotation = MathHelper.Lerp(carry, highFinish,
                            MathHelper.SmoothStep(0f, 1f, rise));
                        break;
                    }
                    case ComboMotion.BackstepRaise:
                    {
                        // Show the entire defensive read: axe starts low, then rises overhead during
                        // the retreat and remains there for the following re-entry chop.
                        if (inTel)
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, 1.0f, 0.24f);
                        else
                        {
                            float elapsed = Math.Max(0, _activeComboStepTotalTicks - PhaseTimer);
                            float raise = MathHelper.Clamp(elapsed / 22f, 0f, 1f);
                            _weaponRotation = MathHelper.SmoothStep(1.0f, -1.3f, raise);
                        }
                        break;
                    }
                    case ComboMotion.ApexDiveCleave:
                    {
                        if (inTel)
                        {
                            _weaponRotation = LogicalSwingWindup(1.0f, -1.3f, comboTelegraphT);
                        }
                        else if (!_apexDiveStrikeStarted)
                        {
                            _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.05f, 0.24f);
                        }
                        else
                        {
                            float diveSwing = MathHelper.Clamp(_apexDiveFallTicks / 20f, 0f, 1f);
                            _weaponRotation = SwingEase.Apply(
                                -1.05f,
                                1.25f,
                                diveSwing,
                                SwingEaseStyle.Whip);
                        }
                        break;
                    }
                }
            }
            else if (Phase == AttackPhase.Custom && CustomWeaponRotation.HasValue)
            {
                _weaponRotation = CustomWeaponRotation.Value;
            }
            else if (IsWeaponRecoveryPhase && IsHoldingMeleeRecoveryFollowThrough())
            {
                // The last active-frame rotation is intentionally left untouched. Damage has
                // already ended; this is only the heavy sword settling into its final pose.
            }
            else if (_flight != null && _flight.IsDiving && MeleeWeaponItemType >= 0)
            {
                // Dive thrust pose: arm and shortsword track the live target in any direction.
                // The Use3 body frame supplies the straight front arm while this angle keeps the blade aligned.
                Player diveTarget = Main.player[NPC.target];
                Vector2 aim = diveTarget.Center - NPC.Center;
                if (Math.Abs(aim.X) > 2f)
                    NPC.direction = NPC.spriteDirection = aim.X < 0f ? -1 : 1;
                float worldAngle = aim.SafeNormalize(new Vector2(NPC.direction, 0f)).ToRotation();
                float targetRotation = NPC.direction == 1
                    ? worldAngle + MathHelper.PiOver4 - MeleeWeaponRotationOffset
                    : MathHelper.Pi + MathHelper.PiOver4 - worldAngle + MeleeWeaponRotationOffset;
                _weaponRotation = _weaponRotation.AngleLerp(targetRotation, 0.45f);
            }
            else
            {
                // Idle / walking / jumping / recovery:
                // Ease the weapon back to the natural hold angle so it always looks carried.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MeleeCarryRotation, 0.10f);
            }

            if (UseCompositeArmForAdditionalPhase)
                ModifyAdditionalPhaseWeaponRotation(ref _weaponRotation);

            UpdateSpearGrip(t);
            SpawnSwingVFX(_weaponRotation - _prevWeaponRotation);
            _prevWeaponRotation = _weaponRotation;
            TickHandPoses();
        }

        /// <summary>
        /// Slides the spear grip along the shaft so the weapon extends/retracts like the player's spear.
        /// _spearGrip 0 = gripped at the head (compact), 1 = gripped at the base (head thrust far forward).
        /// A poke pulses to the base and back (extend → retract); spins/swings hold a FIXED grip instead —
        /// a real polearm doesn't telescope mid-arc, so a moving grip during a swing read as the head
        /// sliding in and out rather than one rigid shaft turning. No-op unless <see cref="DrawWeaponAsSpear"/>.
        /// <paramref name="swingClockT"/> is TickWeaponAnim's own 0→1 swing progress (only meaningful
        /// during MeleeComboAttack); used instead of a separate PhaseTimer-based hump when
        /// <see cref="UseAuthoredSpearGrip"/> opts in, so the visible extension settles on the same
        /// clock as the eased rotation instead of drifting out of sync with it.
        /// </summary>
        private void UpdateSpearGrip(float swingClockT)
        {
            if (!DrawWeaponAsSpear)
            {
                return;
            }

            float target = 0.5f; // idle: gripped in the middle
            float ease = 0.25f;

            switch (Phase)
            {
                case AttackPhase.SpearTelegraph:
                    target = 0.4f;
                    ease = 0.22f; // pull the head back a touch, ready to thrust
                    break;
                case AttackPhase.SpearAttack:
                {
                    float p = SpearAttackTicks > 0 ? 1f - (float)PhaseTimer / SpearAttackTicks : 1f;
                    target = MathHelper.Lerp(0.45f, 0.97f, (float)Math.Sin(p * Math.PI)); // extend then retract
                    ease = 0.55f;
                    break;
                }
                case AttackPhase.MeleeComboTelegraph:
                    target = 0.45f;
                    ease = 0.22f;
                    break;
                case AttackPhase.MeleeComboPause:
                    target = 0.5f;
                    ease = 0.25f;
                    break;
                case AttackPhase.RangedTelegraph:
                case AttackPhase.RangedAttack:
                case AttackPhase.CrossbowBurstPause:
                    // Ranged/throw poses that still use the spear texture (primary ranged casting
                    // off the same weapon) previously fell through to the neutral 0.5 grip, which
                    // puts the draw origin at the shaft's midpoint — the plain butt then sticks out
                    // exactly as far as the tip on the OPPOSITE side, through the body, reading as
                    // "the spear is pointing the wrong way." Pull the grip toward the base (like
                    // SpearAttack's extended pose) so the tip clearly leads toward the aim direction
                    // and the butt tucks in near the hand instead.
                    target = 0.85f;
                    ease = 0.25f;
                    break;
                case AttackPhase.MeleeComboAttack:
                {
                    ComboMotion motion = _activeMeleeCombo.Steps[_meleeComboStepIndex].Motion;
                    if (motion == ComboMotion.Thrust || motion == ComboMotion.JoustDash || motion == ComboMotion.LeapThrust)
                    {
                        float p = UseAuthoredSpearGrip
                            ? MathHelper.Clamp(swingClockT, 0f, 1f)
                            : (_weaponAnimMax > 0 ? 1f - (float)_weaponAnim / _weaponAnimMax : 1f);
                        target = MathHelper.Lerp(0.45f, 0.97f, (float)Math.Sin(p * Math.PI)); // poke extend/retract
                        ease = 0.55f;
                    }
                    else if (motion == ComboMotion.Spin)
                    {
                        target = 0.9f;
                        ease = 0.3f; // held out near the base so the spear sweeps wide
                    }
                    else
                    {
                        // Overhead / sweep / chop: hold a FIXED grip near the base for the whole arc.
                        // A real pole doesn't telescope mid-swing — sliding 0.6->0.85 through the arc
                        // (the old behavior) read as the head extending and retracting mid-cut.
                        target = 0.85f;
                        ease = 0.3f;
                    }
                    break;
                }
                default:
                    target = 0.5f; // idle / walking / ranged / etc.
                    break;
            }

            _spearGrip = MathHelper.Lerp(_spearGrip, target, ease);
        }

        /// <summary>
        /// Diagnostic: append one line per weapon-visible swing frame to
        /// <c>tsorcRevamp-puppet-swing.log</c>. Captures the values that drive both the manual
        /// (legacy 4-frame) and composite-arm swing so misaligned per-motion swings can be read
        /// directly: phase + combo motion, the swing progress <c>t</c>, <c>_weaponRotation</c> and
        /// the per-weapon draw offset, the resolved body row + hand offset (legacy path), and the
        /// composite-arm rotation/hand when the experiment is active.
        /// </summary>
        private void LogSwingFrame()
        {
            try
            {
                string sep = System.IO.Path.DirectorySeparatorChar.ToString();
                string dir = Main.SavePath + sep + "Logs";
                System.IO.Directory.CreateDirectory(dir);
                string path = dir + sep + "tsorcRevamp-puppet-swing.log";

                string motion = "-";
                if (IsMeleeComboPhase && _activeMeleeComboIndex >= 0 && _activeMeleeCombo.Steps != null
                    && _meleeComboStepIndex >= 0 && _meleeComboStepIndex < _activeMeleeCombo.Steps.Length)
                {
                    var step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                    motion = $"{step.Motion}#{_meleeComboStepIndex}";
                }

                // Swing progress 0..1: the V2 runtime reports it directly; the legacy path derives it
                // from the countdown timer, and a zero max means "no swing in flight" -> finished.
                float t = 1f;

                if (_attackRuntimeV2.Active)
                {
                    t = _attackRuntimeV2.StageProgress;
                }
                else if (_weaponAnimMax > 0)
                {
                    t = 1f - (float)_weaponAnim / _weaponAnimMax;
                }

                int bodyRow = _puppet != null ? _puppet.bodyFrame.Y / FrameHeight : -1;
                Vector2 handOff = _puppet != null ? (GetHandPosition() - NPC.Center) : Vector2.Zero;
                bool composite = CompositeArmActive;
                float drawRot = GetMeleeDrawRotation();

                string line = $"[{System.DateTime.Now:HH:mm:ss.fff}] {NPC.TypeName}#{NPC.whoAmI}"
                    + $" phase={Phase} motion={motion}"
                    + $" dir={NPC.direction} t={t:F2} anim={_weaponAnim}/{_weaponAnimMax}"
                    + $" weaponRot={_weaponRotation:F3} drawRot={drawRot:F3} offset={MeleeWeaponRotationOffset:F3}"
                    + $" bodyRow={bodyRow} handOff=({handOff.X:F1},{handOff.Y:F1})"
                    + $" composite={composite} compRot={(composite ? CompositeArmRotation : 0f):F3}";

                System.IO.File.AppendAllText(path, line + System.Environment.NewLine);
            }
            catch { }
        }

        /// <summary>
        /// Maps a weapon-draw angle to a Use1-Use4 body row via the same pitch formula
        /// <c>(1 - sin(angle)) / 2</c> used throughout this file (and in BroadswordRework's
        /// MeleeAnimation.cs, where it was calibrated).  Takes rotation/direction explicitly
        /// (rather than reading <see cref="_weaponRotation"/>/NPC.direction directly) so it can
        /// also pose a one-off echo/duplicate swinging independently of the puppet's own state.
        /// </summary>
        private int BodyRowFromWeaponRotation(float weaponRotation, int direction)
            => BodyRowFromWeaponRotation(weaponRotation, direction, MeleeWeaponRotationOffset);

        /// <summary>
        /// Which Use1-Use4 body row matches a weapon angle, so the torso pose tracks the blade
        /// instead of snapping between four fixed frames. Pitch is 1 = straight up, 0 = straight down.
        ///
        /// Public and static so the offline body renderer
        /// (.agents/tools/SwingPreview) poses the torso from the same thresholds the game uses
        /// — a copied set would drift and quietly preview a different pose than ships.
        /// </summary>
        public static int BodyRowFromWeaponRotation(float weaponRotation, int direction, float meleeWeaponRotationOffset)
        {
            float visualAngle = weaponRotation + meleeWeaponRotationOffset * direction;
            float pitch = (1f - (float)Math.Sin(visualAngle)) / 2f;
            if (pitch > 0.95f)
            {
                return 1;
            }
            if (pitch > 0.70f)
            {
                return 2;
            }
            if (pitch > 0.30f)
            {
                return 3;
            }
            return 4;
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
            Vector2 hand = GetUnscaledHandPosition();
            float visualScale = Math.Max(1, ArmorTemplateScale)
                * (HasSpectralOverlay ? SpectralOverlayScale : 1f);
            return visualScale != 1f
                ? NPC.Bottom + (hand - NPC.Bottom) * visualScale
                : hand;
        }

        private Vector2 GetUnscaledHandPosition()
        {
            if (_puppet == null)
                return PuppetVisualCenter;

            // ── Composite-arm experiment ───────────────────────────────────────────
            // When the new path is active the front arm is a continuously-rotated composite
            // arm, so the authoritative hand is whatever vanilla reports for that rotation —
            // not the 4-row offset table.  (This is the call the old comment flagged as
            // unreliable on a puppet; the experiment exists to find out whether the puppet is
            // sufficiently initialised here.  If it returns garbage, fall back to the table.)
            if (CompositeArmActive)
            {
                Vector2 composite = _puppet.GetFrontHandPosition(CompositeArmStretch, CompositeArmRotation);
                if (composite != Vector2.Zero)
                    return composite;
            }

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

            // Mounting draws the rider raised above NPC.Center, but every offset above is measured from
            // NPC.Center — without this correction the held weapon, the flail anchor and the melee hitbox
            // all stay down at the mount's feet.
            float mountedRise = 0f;

            if (IsMounted)
            {
                mountedRise = MountedHandOffsetY;
            }

            return PuppetVisualCenter + new Vector2(offset.X * NPC.direction, offset.Y + mountedRise);
        }

        /// <summary>World-space front-hand position of the puppet — the anchor a subclass fires casts from
        /// (e.g. a staff muzzle). Add <c>aimDirection * reach</c> to reach the weapon tip.</summary>
        protected Vector2 PuppetHandPosition => GetHandPosition();

        /// <summary>Dual wield: world-space back-hand position and back-weapon direction, for subclass VFX
        /// anchored to the off-hand blade.</summary>
        protected Vector2 PuppetBackHandPosition => GetBackHandPosition();
        protected Vector2 PuppetBackWeaponDirection => GetBackWeaponWorldDirection();

        /// <summary>World-space bow grip for <see cref="UseBowStringDrawPose"/>: the outstretched back hand.
        /// Arrows should leave from here — the front hand is on the string, back near the cheek.</summary>
        protected Vector2 PuppetBowGripPosition
        {
            get
            {
                Vector2 grip = GetUnscaledBowGripPosition();
                float visualScale = Math.Max(1, ArmorTemplateScale)
                    * (HasSpectralOverlay ? SpectralOverlayScale : 1f);

                if (visualScale == 1f)
                {
                    return grip;
                }

                return NPC.Bottom + (grip - NPC.Bottom) * visualScale;
            }
        }

        // Pure function of facing — no per-frame pose state — so the server can fire from it too.
        private Vector2 GetUnscaledBowGripPosition()
        {
            if (_puppet == null)
            {
                return PuppetVisualCenter;
            }

            return _puppet.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, BowHoldArmRotation);
        }

        private Vector2 GetBackHandPosition()
        {
            Vector2 hand = GetUnscaledBackHandPosition();
            float visualScale = Math.Max(1, ArmorTemplateScale)
                * (HasSpectralOverlay ? SpectralOverlayScale : 1f);
            return visualScale != 1f
                ? NPC.Bottom + (hand - NPC.Bottom) * visualScale
                : hand;
        }

        // Vanilla's back composite hand for the back arm's pose. Falls back to just behind the front hand if the
        // puppet player isn't built yet or vanilla reports nothing.
        private Vector2 GetUnscaledBackHandPosition()
        {
            if (_puppet == null)
            {
                return PuppetVisualCenter;
            }

            Vector2 composite = _puppet.GetBackHandPosition(CompositeArmStretch, BackCompositeArmRotation);
            if (composite != Vector2.Zero)
            {
                return composite;
            }

            return GetUnscaledHandPosition() + new Vector2(-6f * NPC.direction, 0f);
        }

        /// <summary>World-space point along the currently drawn melee weapon's blade.</summary>
        protected Vector2 PuppetWeaponTipPosition(float reach) => GetHandPosition() + GetWeaponWorldDirection() * reach;

        /// <summary>World-space direction of the visible weapon, exposed so subclass VFX can use
        /// the same authored pose as drawing and blade collision.</summary>
        protected Vector2 PuppetWeaponDirection => GetWeaponWorldDirection();

        /// <summary>Current armed blade reach, with the normal special-swing reach as fallback.</summary>
        protected float PuppetActiveBladeReach => _activeBladeReach > 0f ? _activeBladeReach : MeleeRange * 0.7f;

        /// <summary>Normalized progress through the current ordinary weapon animation.</summary>
        protected float PuppetWeaponAnimationProgress => MathHelper.Clamp(
            _weaponAnimMax > 0 ? 1f - (float)_weaponAnim / _weaponAnimMax : 1f, 0f, 1f);

        /// <summary>
        /// World-space unit direction the currently-drawn melee weapon sprite points, matching
        /// <see cref="DrawWeaponToLayer"/>'s actual render rotation exactly — including the
        /// per-weapon <see cref="MeleeWeaponRotationOffset"/> / <see cref="SpearDrawRotationOffset"/>
        /// corrections and the <see cref="BladeFlipActive"/> mirror — so it's the single source of
        /// truth for "where the sprite actually is," shared by decorative tip-position code
        /// (<see cref="GetSpearTipWorldPosition"/>) and real hit detection (<see cref="TickBladeHit"/>)
        /// rather than two approximations that could drift apart. Assumes the sprite is calibrated so
        /// the tip sits at -45° when _weaponRotation=0, dir=1, no flip (the standard broadsword
        /// convention used across all puppets).
        /// </summary>
        private Vector2 GetWeaponWorldDirection()
        {
            if (_heldItemType == MagicWeaponItemType)
            {
                return GetDiagonalSpriteWorldDirection(_weaponRotation + MagicWeaponRotationOffset, BladeFlipActive);
            }
            if (DrawWeaponAsSpear)
            {
                return GetDiagonalSpriteWorldDirection(_weaponRotation + SpearDrawRotationOffset, BladeFlipActive);
            }
            // A bow's rotation is a plain aim angle (see heldBowLike in DrawWeaponToLayer, which draws
            // it at _weaponRotation * NPC.direction with no diagonal blade-tip offset) — falling through
            // to the melee branch below would run it through the broadsword ±45° convention instead,
            // producing a direction unrelated to how the bow is actually drawn. Matters for
            // ResolveTwoHandedBackArmPose, which aims the rear-hand IK target off this vector.
            if (_heldItemType == _activeRangedItemType && _activeRangedStyle == RangedStyle.Bow)
            {
                float bowRotation = _weaponRotation * NPC.direction;
                return new Vector2((float)Math.Cos(bowRotation), (float)Math.Sin(bowRotation));
            }
            return GetMeleeWorldDirection(FrontHandPoseRotation, FrontHandWeapon);
        }

        /// <summary>World direction of the dual-wield back weapon — the back-hand twin of
        /// <see cref="GetWeaponWorldDirection"/>, used by its draw and its blade capsule.</summary>
        private Vector2 GetBackWeaponWorldDirection() => GetMeleeWorldDirection(BackHandPoseRotation, BackHandWeapon);

        // A melee weapon's world direction for one hand's pose. Shared by both hands so drawing and hit
        // detection can never disagree about where either blade points.
        private Vector2 GetMeleeWorldDirection(float handRotation, PuppetWeapon weapon)
        {
            bool bladeFlip = BladeFlipActiveFor(weapon);

            if (!MirrorMeleeSwingRotationByFacing)
            {
                return GetDiagonalSpriteWorldDirection(handRotation + weapon.RotationOffset * NPC.direction, bladeFlip);
            }

            float drawRotation = GetMeleeDrawRotation(handRotation, weapon);
            // Blade's natural rest angle, mirrored by facing; the flip swaps which side it hangs.
            // Base angle is MeleeNaturalRestAngleDeg (45° = standard broadsword convention, the
            // default every puppet except Dread Wraith actually uses); dir=-1 mirrors it about the
            // vertical axis (180 - base) rather than hardcoding a second unrelated magic number.
            float baseDeg = MeleeNaturalRestAngleDeg;
            float naturalDeg;

            if (NPC.direction == 1)
            {
                naturalDeg = bladeFlip ? baseDeg : -baseDeg;
            }
            else
            {
                naturalDeg = bladeFlip ? (180f - baseDeg) : -(180f - baseDeg);
            }

            float actualAngle = MathHelper.ToRadians(naturalDeg + MathHelper.ToDegrees(drawRotation));
            return new Vector2((float)Math.Cos(actualAngle), (float)Math.Sin(actualAngle));
        }

        // Tip direction of a sprite on the broadsword diagonal for the legacy (non-mirrored) draw path.
        private Vector2 GetDiagonalSpriteWorldDirection(float drawRotation, bool bladeFlip)
        {
            // FlipVertically mirrors the source rect across its local horizontal centerline before
            // rotation is applied, so the pre-rotation tip angle reflects to the opposite side.
            float correctedNaturalDeg = bladeFlip ? 45f : -45f;
            float rotDeg = MathHelper.ToDegrees(drawRotation);
            float angleDeg = NPC.direction == 1
                ? correctedNaturalDeg + rotDeg
                : 180f - (correctedNaturalDeg + rotDeg); // mirror across vertical axis (FlipHorizontally)
            float rad = MathHelper.ToRadians(angleDeg);
            return new Vector2((float)Math.Cos(rad), (float)Math.Sin(rad));
        }

        /// <summary>World-space position of the spear's TIP, derived from the current weapon-draw
        /// rotation.  Decorative use only (telegraph VFX placement) — not pixel-exact, but tracks the
        /// visible tip as the weapon swings/thrusts/aims.</summary>
        protected Vector2 GetSpearTipWorldPosition(float reachPx = 90f)
            => GetHandPosition() + GetWeaponWorldDirection() * reachPx;

        // ── Debug: weapon-rotation log (DebugMode config only) ──────────────────────
        // Dumps everything relevant to diagnosing hand placement / spear rotation bugs:
        // Phase, raw _weaponRotation, the final drawRotation actually passed to DrawData,
        // holdingSpearNow/heldRangedLike routing, held item type, spear grip, and positions.
        // Throttled per-NPC so a telegraph doesn't spam thousands of near-identical lines.
        private int _lastWeaponDebugLogTick = -9999;
        private void LogWeaponDebug()
        {
            int now = (int)Main.GameUpdateCount;
            if (now - _lastWeaponDebugLogTick < 6)
            {
                return;  // ~10/sec — enough resolution, not spam
            }
            _lastWeaponDebugLogTick = now;
            try
            {
                string sep = Path.DirectorySeparatorChar.ToString();
                string dir = Main.SavePath + sep + "Logs";
                Directory.CreateDirectory(dir);
                string path = dir + sep + "tsorcRevamp-puppet-weapon.log";
                tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
                // For combo phases, "phase=MeleeComboAttack" alone doesn't say WHICH named combo/motion
                // is playing (Charged Chop vs Forward Thrust vs Leaping Lunge, etc.) — every combo shares
                // the same phase enum, so without this the log can't tell them apart.
                string comboTag = "";
                bool inCombo = Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                            || Phase == AttackPhase.MeleeComboPause    || Phase == AttackPhase.MeleeComboRecovery;
                if (inCombo && _activeMeleeComboIndex >= 0 && _activeMeleeCombo.Steps != null && _meleeComboStepIndex < _activeMeleeCombo.Steps.Length)
                    comboTag = $" combo=\"{_activeMeleeCombo.Name}\" motion={_activeMeleeCombo.Steps[_meleeComboStepIndex].Motion}"
                             + $" step={_meleeComboStepIndex}/{_activeMeleeCombo.Steps.Length}";
                string line = $"[{DateTime.Now:HH:mm:ss}] {NPC.TypeName}#{NPC.whoAmI}"
                    + $" phase={Phase}{comboTag} phaseTimer={PhaseTimer}"
                    + $" heldItem={_heldItemType} holdingSpear={DebugHoldingSpearNow} rangedLike={DebugHeldRangedLike}"
                    + $" dir={DebugDirection} spearGrip={DebugSpearGrip:F2}"
                    + $" weaponRotDeg={DebugWeaponRotationDeg:F1} drawRotDeg={DebugDrawRotationDeg:F1}"
                    + $" hand=({DebugHandPos.X:F0},{DebugHandPos.Y:F0}) origin=({DebugOrigin.X:F0},{DebugOrigin.Y:F0})"
                    + $" npcPos=({NPC.Center.X:F0},{NPC.Center.Y:F0}) airborne={_flight?.IsAirborne ?? false}"
                    + $" flightMode={(_flight != null ? _flight.Mode.ToString() : "n/a")}"
                    + $" reactiveDefense={AllowReactiveDefense} shield={_shielding} guardTimer={globalNPC.ReactiveBlockTimer} guardCooldown={_shieldGuardCooldown}"
                    + $" evasion={globalNPC.InEvasion}/{globalNPC.CurrentEvasion} dodge={globalNPC.DodgeTimer}"
                    + $" stagger={globalNPC.StaggerTimer} poise={globalNPC.Poise:F1}/{globalNPC.EffectivePoiseMax:F1}"
                    + $" attackFlags={globalNPC.AttackTelegraphing}/{globalNPC.AttackCommitted}";
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { /* best-effort debug log; never let logging break gameplay */ }
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
            if (itemType == SecondaryRangedWeaponItemType)
            {
                if (_cachedSecondaryRangedType != itemType)
                {
                    _secondaryRangedItemCache = new Item();
                    _secondaryRangedItemCache.SetDefaults(itemType);
                    _secondaryRangedItemCache.noUseGraphic = true;
                    _cachedSecondaryRangedType = itemType;
                }
                return _secondaryRangedItemCache;
            }
            if (itemType == MagicWeaponItemType)
            {
                if (_cachedMagicType != itemType)
                {
                    _magicItemCache = new Item();
                    _magicItemCache.SetDefaults(itemType);
                    _magicItemCache.noUseGraphic = true;
                    _cachedMagicType = itemType;
                }
                return _magicItemCache;
            }
            return new Item(); // air
        }

        private void InitPuppet()
        {
            _puppet = new Player();
            _puppet.active  = true;
            _puppet.whoAmI  = 0;
            _puppet.gravDir = 1f;
            _puppet.Male    = PuppetIsMale;
            _puppet.skinColor = PuppetSkinColor;
            _puppet.eyeColor = PuppetEyeColor;
            _puppet.skinVariant = PuppetSkinVariant;

            ApplyPuppetArmor();
            SetPuppetDye(3, HasWings ? WingsDyeItemType : 0);

            // Wings — populated only when HasWings, so wing layer renders behind the body
            if (HasWings && WingsAccessoryItemType > 0)
            {
                _wingsItemCache = new Item();
                _wingsItemCache.SetDefaults(WingsAccessoryItemType);
                _cachedWingsType = WingsAccessoryItemType;
                _puppet.armor[3] = _wingsItemCache.Clone();
                _puppet.wings = _wingsItemCache.wingSlot;
                _puppet.wingTimeMax = 200; // arbitrary > 0 so wing layer treats it as a real wing
            }

            int[] accessoryItems = AccessoryItemTypes;
            int[] accessoryDyes = AccessoryDyeItemTypes;
            for (int i = 0; i < accessoryItems.Length && i + 4 < _puppet.armor.Length; i++)
            {
                int slot = i + 4;
                Item accessory = new Item();
                accessory.SetDefaults(accessoryItems[i]);
                _puppet.armor[slot] = accessory;
                SetPuppetDye(slot, i < accessoryDyes.Length ? accessoryDyes[i] : 0);
                _puppet.UpdateVisibleAccessory(slot, accessory);
                ApplyAccessoryDye(accessory, _puppet.dye[slot].dye);
            }

            // Resolve the wings dye item into the shader ID DrawPlayer reads (head/body/legs are
            // resolved inside ApplyPuppetArmor itself, since that also runs standalone later).
            _puppet.cWings = _puppet.dye[3].dye;

            // Pre-set default weapon so it shows from the first frame
            _heldItemType = MeleeWeaponItemType >= 0 ? MeleeWeaponItemType : RangedWeaponItemType;
        }

        /// <summary>Equips Head/Body/LegsArmorItemType (+ their dyes) onto the puppet and resolves
        /// the implied-robe-legs case. Called once from InitPuppet, and again from SyncPuppet
        /// whenever a subclass's item types change mid-fight (e.g. Owl Father's health-phase armor
        /// swap) — see the _cachedHeadArmorItemType family below.</summary>
        private void ApplyPuppetArmor()
        {
            _puppet.armor[0] = new Item();
            _puppet.armor[0].SetDefaults(HeadArmorItemType);
            _puppet.armor[1] = new Item();
            _puppet.armor[1].SetDefaults(BodyArmorItemType);
            _puppet.armor[2] = new Item();
            _puppet.armor[2].SetDefaults(LegsArmorItemType);

            // These equip-slot indices are what Terraria's player renderer reads. Phase-specific
            // oversized art is substituted only after this normal composite has been assembled.
            _puppet.head = _puppet.armor[0].headSlot;
            _puppet.body = _puppet.armor[1].bodySlot;
            _puppet.legs = _puppet.armor[2].legSlot;

            SetPuppetDye(0, HeadArmorDyeItemType);
            SetPuppetDye(1, BodyArmorDyeItemType);
            SetPuppetDye(2, LegsArmorDyeItemType);

            // Robe bodies (cultist robes, mage robes...) get their floor-length skirt from an implied leg slot
            // that vanilla's PlayerFrame assigns through Player.SetMatch — which puppets never run. Apply it only
            // when no leg armor is set, so a puppet that picked explicit legs keeps them. SetMatch returns -1 for
            // bodies with no implied legs; wearsRobe switches the draw to the Robe layer instead of Leggings.
            if (_puppet.legs <= 0)
            {
                bool wearsRobe = false;
                Player.SetMatchRequest robeRequest = new Player.SetMatchRequest
                {
                    Head = _puppet.head,
                    Body = _puppet.body,
                    Legs = _puppet.legs,
                    Male = _puppet.Male,
                    ArmorSlotRequested = 1,
                };
                int impliedLegs = Player.SetMatch(robeRequest, ref wearsRobe);
                if (impliedLegs != -1)
                {
                    _puppet.legs = impliedLegs;
                }
                _puppet.wearsRobe = wearsRobe;
            }

            // Resolve the vanilla dye items into the shader IDs consumed by DrawPlayer. Doing this
            // directly avoids running accessory/mod-player update hooks on the synthetic puppet.
            _puppet.cHead = _puppet.dye[0].dye;
            _puppet.cBody = _puppet.dye[1].dye;
            _puppet.cLegs = _puppet.dye[2].dye;

            _cachedHeadArmorItemType = HeadArmorItemType;
            _cachedBodyArmorItemType = BodyArmorItemType;
            _cachedLegsArmorItemType = LegsArmorItemType;
        }

        private void SetPuppetDye(int slot, int dyeItemType)
        {
            _puppet.dye[slot] = new Item();
            if (dyeItemType > 0)
            {
                _puppet.dye[slot].SetDefaults(dyeItemType);
            }
        }

        private void ApplyAccessoryDye(Item accessory, int shader)
        {
            if (accessory.shoeSlot > 0)
            {
                _puppet.cShoe = shader;
            }
            if (accessory.shieldSlot > 0)
            {
                _puppet.cShield = shader;
                _puppet.cShieldFallback = shader;
            }
            if (accessory.neckSlot > 0)
            {
                _puppet.cNeck = shader;
            }
            if (accessory.type == ItemID.AngelHalo)
            {
                _puppet.cAngelHalo = shader;
            }
        }

        private void SyncPuppet()
        {
            _puppet.position  = PuppetVisualPosition;
            _puppet.velocity  = NPC.velocity;
            _puppet.direction = NPC.direction;
            _puppet.width     = PuppetVisualWidth;
            _puppet.height    = PuppetVisualHeight;
            _puppet.gravDir   = 1f;

            // Hot-swap support: InitPuppet only ever runs once (the first PreDraw, long before any
            // health-phase flag flips), so a subclass whose Head/Body/LegsArmorItemType depends on
            // mutable state (Owl Father's phase-two Ancient-set swap) would otherwise stay equipped
            // in whatever it wore at spawn forever. Re-apply whenever any of the three actually change.
            if (HeadArmorItemType != _cachedHeadArmorItemType
                || BodyArmorItemType != _cachedBodyArmorItemType
                || LegsArmorItemType != _cachedLegsArmorItemType)
            {
                ApplyPuppetArmor();
            }

            // Dye hot-swap, same reason as the armor above: a subclass's dyes may depend on phase (Owl
            // Father's phase-two Burning Hades) while its armor items stay the same. Compared by item
            // type, so an unchanged frame re-creates nothing. An empty slot's Item has type 0.
            int wingsDyeItemType = HasWings ? WingsDyeItemType : 0;
            bool dyesChanged = _puppet.dye[0].type != HeadArmorDyeItemType
                || _puppet.dye[1].type != BodyArmorDyeItemType
                || _puppet.dye[2].type != LegsArmorDyeItemType
                || _puppet.dye[3].type != wingsDyeItemType;

            if (dyesChanged)
            {
                SetPuppetDye(0, HeadArmorDyeItemType);
                SetPuppetDye(1, BodyArmorDyeItemType);
                SetPuppetDye(2, LegsArmorDyeItemType);
                SetPuppetDye(3, wingsDyeItemType);
                _puppet.cHead = _puppet.dye[0].dye;
                _puppet.cBody = _puppet.dye[1].dye;
                _puppet.cLegs = _puppet.dye[2].dye;
                _puppet.cWings = _puppet.dye[3].dye;
            }

            // Mount visual is DERIVED from the gameplay flag every frame rather than driven by mount/dismount
            // events. A client that learns about the dismount via synced state (not by running CheckDead
            // itself) still stops drawing the mount, with no event plumbing to miss.
            if (HasMount && MountType > 0)
            {
                _puppet.mount ??= new Mount();

                if (IsMounted && !_puppet.mount.Active)
                {
                    _puppet.mount.SetMount(MountType, _puppet);
                }
                else if (!IsMounted && _puppet.mount.Active)
                {
                    _puppet.mount.Dismount(_puppet);
                }
            }

            // Mount animation: the puppet never runs Player.Update, so the mount's frame counter has to be
            // advanced by hand. GetIntendedGroundedFrame picks standing vs running from the velocity we
            // just copied across, so the gallop speeds up and settles on its own.
            if (_puppet.mount != null && _puppet.mount.Active)
            {
                // Vanilla grows a mounted player's hitbox by the mount's heightBoost and raises its
                // position to match. We overwrite position/height from the NPC every tick, which undoes
                // that — so reapply it here or the mount's own offset math works from a rider ~20px too
                // short, sinking the whole assembly into the ground. Feet are pinned to NPC.Bottom so
                // the MOUNT stands on the ground rather than the rider.
                _puppet.height = NPC.height + _puppet.mount.HeightBoost;
                _puppet.position.Y = NPC.Bottom.Y - _puppet.height;

                _puppet.mount.UpdateFrame(_puppet, _puppet.mount.GetIntendedGroundedFrame(_puppet), NPC.velocity);
            }

            // Wing flap state: drive vanilla wing draw layer's animation.
            // controlJump=true + wingTime>0 makes the wing layer pick the flap frames;
            // false + wingTime=0 makes it pick the glide/closed frame.
            if (HasWings && _flight != null)
            {
                // Hot-swap support: (re)build the cached wing item whenever the subclass's
                // WingsAccessoryItemType changes (e.g. Gwyn's Angel → Flame wings below 30% HP).
                if (_cachedWingsType != WingsAccessoryItemType && WingsAccessoryItemType > 0)
                {
                    _wingsItemCache = new Item();
                    _wingsItemCache.SetDefaults(WingsAccessoryItemType);
                    _cachedWingsType = WingsAccessoryItemType;
                    _puppet.wingTimeMax = 200;
                }

                bool airborne = _flight.IsAirborne;
                _puppet.wings = (airborne || ShowWingsWhenGrounded) ? (_wingsItemCache?.wingSlot ?? 0) : 0;
                _puppet.wingTime = airborne ? _puppet.wingTimeMax : 0;
                _puppet.controlJump = _flight.WingsActiveThisTick;
                // During idle hover drift the wings spread open (glide pose = frame 1) rather
                // than folding shut.  Active flapping and non-hover flight use the normal
                // animated phase so the wing-beat loop runs correctly.
                bool gliding = airborne && !_flight.WingsActiveThisTick
                    && (_flight.Mode == FlightMode.Hover || _flight.Mode == FlightMode.Strafe);
                // Reset to frame zero after landing. Otherwise the last open flight frame remains
                // cached while walking, making grounded puppets look as though they are still gliding.
                if (!airborne)
                {
                    _puppet.wingFrame = 0;
                }
                else if (gliding)
                {
                    _puppet.wingFrame = 1;
                }
                else
                {
                    _puppet.wingFrame = (int)(_flight.WingAnimPhase * 4f) % 4;
                }
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
            _puppet.itemAnimationMax = _weaponAnimMax;
            _puppet.selectedItem     = 0;

            if (inAttackPhase)
            {
                // Put weapon in hand so the arm extends to the correct Use1–Use4 pose.
                // noUseGraphic=true prevents DrawPlayer from rendering the sprite itself —
                // PuppetWeaponDrawLayer handles that at the correct layer depth.
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

            if (BowStringDrawPoseActive)
            {
                // Archer pose: back arm holds the bow out level, front arm works the string.
                // Replaces the two-handed IK / dual-wield back arm below for the bow phases only.
                TickBowStringDrawPose();
                _puppet.SetCompositeArmFront(true, _bowStringHandStretch, _bowStringHandRotation);
                _puppet.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, BowHoldArmRotation);
                return;
            }

            // Set AFTER SyncFrames so the continuously-authored arms override the matching pieces
            // of the selected body frame. The front hand remains authoritative for weapon draw and
            // collision. An opt-in rear arm is visual-only and solves toward a second hilt grip.
            if (CompositeArmActive)
                _puppet.SetCompositeArmFront(true, CompositeArmStretch, CompositeArmRotation);
            else
                _puppet.SetCompositeArmFront(false, Player.CompositeArmStretchAmount.Full, 0f);

            if (TwoHandedCompositeArmActive)
            {
                ResolveTwoHandedBackArmPose();
                _puppet.SetCompositeArmBack(true, _twoHandedBackStretch, _twoHandedBackRotation);
            }
            else if (BackHandWeaponVisible)
            {
                // Dual wield: the back arm holds its own weapon, posed from its own blended rotation.
                _puppet.SetCompositeArmBack(true, CompositeArmStretch, BackCompositeArmRotation);
            }
            else
            {
                _puppet.SetCompositeArmBack(false, Player.CompositeArmStretchAmount.Full, 0f);
                _twoHandedBackStretch = Player.CompositeArmStretchAmount.ThreeQuarters;
                _twoHandedBackRotation = 0f;
                _twoHandedBackHandWorld = Vector2.Zero;
                _twoHandedBackGripTargetWorld = Vector2.Zero;
                _twoHandedBackGripError = 0f;
            }
        }

        private void SyncFrames()
        {
            bool onGround = NPC.velocity.Y == 0f;
            bool moving   = Math.Abs(NPC.velocity.X) > 0.15f;

            // Always advance walk counter when moving on ground — legs animate
            // even during attacks so the puppet doesn't glide with frozen feet.
            // Guard against Main.gamePaused so the animation freezes with every other NPC.
            if (onGround && moving && !Main.gamePaused)
            {
                _frameCounter += Math.Abs(NPC.velocity.X) * 0.55f
                    * Math.Max(0f, WalkAnimationSpeedMultiplier);
                if (_frameCounter >= 14f)
                {
                    _frameCounter = 0f;
                }
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

            if (IsHoldingMagicWeaponDuringRecovery)
            {
                bodyRow = BodyRowFromWeaponRotation(_weaponRotation, NPC.direction);
            }
            else if (Phase == AttackPhase.Healing)
            {
                // Arm raised to drink — Use2 matches the "arm held up" pose
                bodyRow = 2;
            }
            else if (isMeleeSwing)
            {
                bodyRow = BodyRowFromWeaponRotation(_weaponRotation, NPC.direction);
            }
            else if (Phase == AttackPhase.MeleeRecovery && MeleeRecoveryLingerTicks > 0)
            {
                bodyRow = BodyRowFromWeaponRotation(_weaponRotation, NPC.direction);
            }
            else if (Phase == AttackPhase.StabTelegraph)
            {
                bodyRow = 4; // Use4 — arm dipped down to match sword dip telegraph
            }
            else if (Phase == AttackPhase.StabAttack)
            {
                bodyRow = 3; // Use3 — arm level/forward for the horizontal thrust
            }
            else if (Phase == AttackPhase.StabRecovery)
            {
                // Keep the arm tracking _weaponRotation as it eases back to HoldRotation instead
                // of snapping straight to Idle/Walk while the weapon is still mid-lerp.
                bodyRow = BodyRowFromWeaponRotation(_weaponRotation, NPC.direction);
            }
            else if (Phase == AttackPhase.RangedTelegraph)
            {
                // Body row depends on animation style so the arm matches the weapon arc.
                bodyRow = _activeRangedStyle switch
                {
                    RangedStyle.Crossbow => 3, // Use3 — arm level/forward for horizontal aim
                    RangedStyle.Bow      => 3, // Use3 â€” level arm extended forward for bow aim
                    RangedStyle.Staff    => 2, // Use2 — raised casting arm
                    _                    => 2, // Throw default — raised arm
                };
            }
            else if (Phase == AttackPhase.RangedAttack)
            {
                bodyRow = _activeRangedStyle switch
                {
                    RangedStyle.Crossbow => 3, // Use3 — arm stays level, small click jolt
                    RangedStyle.Bow      => 3, // Use3 — arm snaps forward at release
                    RangedStyle.Staff    => 2, // Use2 — retain the raised staff through release
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
            else if (Phase == AttackPhase.KnivesTelegraph || Phase == AttackPhase.KnivesThrowPause)
            {
                bodyRow = 2; // Use2 — arm raised, knife held ready
            }
            else if (Phase == AttackPhase.KnivesThrow)
            {
                bodyRow = 3; // Use3 — arm forward at release
            }
            else if (Phase == AttackPhase.TendrilTelegraph || Phase == AttackPhase.TendrilReach)
            {
                // The grab is a bare, forward-reaching hand. Keeping Use3 here prevents an
                // ordinary walk frame from fighting the hand position while the tendril is live.
                bodyRow = 3;
            }
            else if (Phase == AttackPhase.TendrilSwingTelegraph || Phase == AttackPhase.TendrilSwing)
            {
                bodyRow = BodyRowFromWeaponRotation(_weaponRotation, NPC.direction);
            }
            else if (Phase == AttackPhase.HomingVolleyDodgeback
                  || Phase == AttackPhase.HomingVolleySwingTelegraph
                  || Phase == AttackPhase.HomingVolleySwing)
            {
                if (UseRaisedHomingVolleyHoldoutPose)
                {
                    if (Phase == AttackPhase.HomingVolleySwing)
                    {
                        float release = HomingVolleySwingTicks > 0
                            ? 1f - PhaseTimer / (float)HomingVolleySwingTicks
                            : 1f;
                        bodyRow = 1 + Math.Min(3, (int)(MathHelper.Clamp(release, 0f, 0.999f) * 4f));
                    }
                    else
                    {
                        bodyRow = 1; // player Sword of Gwyn's held-spear Use1 pose
                    }
                }
                else
                {
                    bodyRow = BodyRowFromWeaponRotation(_weaponRotation, NPC.direction);
                }
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
                    case ComboMotion.UnderhandArc:
                    case ComboMotion.HorizontalSweep:
                    case ComboMotion.LeapSlam:
                    case ComboMotion.ChargeChop:
                    case ComboMotion.Feint:
                    case ComboMotion.DoubleSpinSlam:
                    case ComboMotion.ThrownWeaponRetrieve:
                    case ComboMotion.LowAxeRun:
                    case ComboMotion.RisingUppercutLeap:
                    case ComboMotion.BackstepRaise:
                    case ComboMotion.ApexDiveCleave:
                        bodyRow = BodyRowFromWeaponRotation(_weaponRotation, NPC.direction);
                        break;
                    case ComboMotion.Thrust:
                    case ComboMotion.JoustDash:
                    case ComboMotion.LeapThrust:
                        bodyRow = inTel ? 4 : 3;
                        break;
                    case ComboMotion.Spin:
                        // Rotate body row to suggest motion; cycle Use1→Use3 by rotation phase
                        bodyRow = 1 + ((int)(_weaponRotation / MathHelper.PiOver2) & 3);
                        if (bodyRow > 4)
                        {
                            bodyRow = 4;
                        }
                        break;
                    case ComboMotion.FlailBrace:
                        bodyRow = 3;
                        break;
                    case ComboMotion.IaidoDraw:
                        bodyRow = inTel ? 4 : 3;
                        break;
                    default:
                        bodyRow = 3;
                        break;
                }
            }
            else if (Phase == AttackPhase.MeleeComboRecovery)
            {
                // Follow-through: keep the arm tracking the weapon as it eases back to the hold
                // angle instead of snapping to Idle/Walk mid-settle (same fix as StabRecovery).
                bodyRow = BodyRowFromWeaponRotation(_weaponRotation, NPC.direction);
            }
            else if (_flight != null && _flight.IsDiving && MeleeWeaponItemType >= 0)
            {
                // Dive: arm forward, weapon thrusting toward the dive target.
                bodyRow = 3; // Use3 — arm level/forward
            }
            else if (!onGround)
            {
                // Wing flight is a controlled hover, not a jump: arms hang at rest (Idle row) while the
                // legs keep the airborne frame. Only a real jump or fall raises the arms.
                bool wingFlight = _flight != null && _flight.IsAirborne;
                bodyRow = 5; // Jump

                if (wingFlight)
                {
                    bodyRow = 0; // Idle — arms down
                }
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

            // ── Mounted pose ──────────────────────────────────────────────────────
            // Match what vanilla does to a mounted player (Player.PlayerFrame): pin the legs to row 6 —
            // the one-leg sitting pose — with the counter frozen so they DON'T walk while the mount
            // moves, and drop the body into the mount's own reins-holding frame so the front arm is held
            // out. The body override is skipped mid-attack: the Use rows still have to drive swings and
            // casts from the saddle.
            if (IsMounted && _puppet.mount != null && _puppet.mount.Active)
            {
                legRow = 6;

                bool neutralPose = bodyRow == 0 || bodyRow == 5 || bodyRow >= 6;

                if (neutralPose)
                {
                    bodyRow = _puppet.mount.BodyFrame;
                }
            }

            _puppet.bodyFrame = new Rectangle(0, FrameHeight * bodyRow, 40, FrameHeight);
            _puppet.legFrame  = new Rectangle(0, FrameHeight * legRow,  40, FrameHeight);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Draw
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws a deterministic, fully equipped portrait pose for
        /// <see cref="PuppetSpriteExporterSystem"/>. This deliberately uses the same
        /// synthetic <see cref="Player"/>, dye shaders, accessories, wings, shield layer, and custom
        /// held-weapon layer as normal puppet rendering instead of rebuilding those layers offline.
        /// </summary>
        /// <returns><see langword="false"/> only when a weapon pose was requested but this puppet
        /// has no configured melee, primary ranged, secondary ranged, or magic weapon.</returns>
        internal bool DrawExportPose(Vector2 worldPosition, bool withPrimaryWeapon)
        {
            if (_puppet == null)
                InitPuppet();

            NPC.active = true;
            NPC.position = worldPosition;
            NPC.velocity = Vector2.Zero;
            NPC.direction = 1;
            NPC.spriteDirection = 1;

            if (HasWings)
                _flight ??= new EnemyFlightController(FlightConfig);

            // First configured weapon wins, in preference order; magic is the fallback.
            int primaryWeapon = MagicWeaponItemType;

            if (MeleeWeaponItemType > 0)
            {
                primaryWeapon = MeleeWeaponItemType;
            }
            else if (RangedWeaponItemType > 0)
            {
                primaryWeapon = RangedWeaponItemType;
            }
            else if (SecondaryRangedWeaponItemType > 0)
            {
                primaryWeapon = SecondaryRangedWeaponItemType;
            }


            if (withPrimaryWeapon && primaryWeapon <= 0)
                return false;

            if (withPrimaryWeapon)
            {
                bool usingMelee = MeleeWeaponItemType > 0;
                if (usingMelee)
                {
                    Phase = AttackPhase.MeleeTelegraph;
                    PhaseTimer = Math.Max(1, MeleeTelegraphTicks);
                    _weaponRotation = HoldRotation;
                }
                else if (primaryWeapon == MagicWeaponItemType)
                {
                    Phase = AttackPhase.MagicTelegraph;
                    PhaseTimer = Math.Max(1, MagicTelegraphTicks);
                    _weaponRotation = 0f;
                }
                else
                {
                    bool usingSecondary = primaryWeapon == SecondaryRangedWeaponItemType;
                    _activeRangedItemType = primaryWeapon;
                    _activeRangedStyle = usingSecondary ? SecondaryRangedAnimStyle : RangedAnimStyle;
                    _activeRangedTelegraphTicks = Math.Max(1,
                        usingSecondary ? SecondaryRangedTelegraphTicks : RangedTelegraphTicks);
                    Phase = AttackPhase.RangedTelegraph;
                    PhaseTimer = _activeRangedTelegraphTicks;
                    _weaponRotation = 0f;
                }

                SetDisplayWeapon(primaryWeapon, swing: false);
                _weaponVisible = true;
                _forceExportHeldWeapon = true;
            }
            else
            {
                Phase = AttackPhase.Idle;
                PhaseTimer = 0;
                _heldItemType = -1;
                _weaponAnim = 0;
                _weaponRotation = 0f;
                _weaponVisible = false;
                _forceExportHeldWeapon = false;
            }

            SyncPuppet();

            // Export every configured wing set even when that puppet normally folds/hides it while
            // grounded. Frame zero is the stable closed/idle frame used by the normal wing layer.
            if (HasWings && _wingsItemCache != null)
            {
                _puppet.wings = _wingsItemCache.wingSlot;
                _puppet.wingFrame = 0;
                _puppet.wingTime = 0;
                _puppet.controlJump = false;
            }

            // Display-doll rendering removes world-light tint while preserving armor/accessory dyes.
            _puppet.isDisplayDollOrInanimate = true;
            _puppet.socialIgnoreLight = true;
            _layerDrawColor = Color.White;

            DrawingPuppetFor = this;
            try
            {
                Main.PlayerRenderer.DrawPlayer(Main.Camera, _puppet, PuppetDrawPosition, 0f, Vector2.Zero, 0f, PuppetDrawScale);
            }
            finally
            {
                DrawingPuppetFor = null;
                _forceExportHeldWeapon = false;
            }

            return true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (_puppet == null)
                InitPuppet();

            SyncPuppet();

            // Store draw color so PuppetWeaponDrawLayer can read it during the pipeline below.
            bool teleportIllusion = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().IsTeleportIllusion;
            _layerDrawColor = teleportIllusion ? drawColor * TeleportIllusionOpacity : drawColor;

            // PuppetWeaponDrawLayer is registered at AfterParent(HeldItem) in tModLoader's pipeline.
            // By setting DrawingPuppetFor = this for exactly the duration of DrawPlayer, that layer
            // wakes up and calls DrawWeaponToLayer — inserting the weapon sprite after body/legs
            // but before the front arm, which gives the correct "hand gripping the sword" look.
            DrawingPuppetFor = this;

            // Dash afterimages: sparse translucent echoes at cached NPC.oldPos[] positions, drawn
            // BEFORE the real puppet so the solid sprite always renders on top.  Uses vanilla's own
            // fractal-afterimage fields (see AfterimageTicks doc) — no bespoke alpha-blend plumbing.
            if (AfterimageTicks > 0 && !HasSpectralOverlay)
            {
                _puppet.isFirstFractalAfterImage = true;
                int sampleLimit = Math.Min(NPC.oldPos.Length, Math.Max(0, AfterimageSampleLimit));
                int sampleStep = Math.Max(1, AfterimageSampleStep);
                for (int k = 0; k < sampleLimit; k += sampleStep)
                {
                    if (NPC.oldPos[k] == Vector2.Zero)
                    {
                        continue;
                    }
                    _puppet.firstFractalAfterImageOpacity = AfterimageOpacity * (1f - (float)k / NPC.oldPos.Length);

                    // oldPos stores raw NPC.position, but a mounted puppet is DRAWN raised by the mount's
                    // height boost. Without the same raise the echoes trail below the goat and sink into
                    // the ground, instead of only lagging behind it.
                    Vector2 echoPosition = NPC.oldPos[k] + (PuppetDrawPosition - NPC.position);

                    Main.PlayerRenderer.DrawPlayer(Main.Camera, _puppet, echoPosition, 0f, Vector2.Zero, 0f, PuppetDrawScale);
                }
                _puppet.isFirstFractalAfterImage = false;
            }

            bool previousFractal = _puppet.isFirstFractalAfterImage;
            float previousOpacity = _puppet.firstFractalAfterImageOpacity;
            if (teleportIllusion)
            {
                _puppet.isFirstFractalAfterImage = true;
                _puppet.firstFractalAfterImageOpacity = TeleportIllusionOpacity;
            }

            Main.PlayerRenderer.DrawPlayer(Main.Camera, _puppet, PuppetDrawPosition, 0f, Vector2.Zero, 0f, PuppetDrawScale);
            _puppet.isFirstFractalAfterImage = previousFractal;
            _puppet.firstFractalAfterImageOpacity = previousOpacity;
            DrawingPuppetFor = null;

            return false;
        }

        /// <summary>
        /// Called by <see cref="PuppetWeaponDrawLayer"/> during the <c>DrawPlayer</c> pipeline.
        /// Adds the weapon sprite to <paramref name="drawInfo"/>'s draw-data cache at the correct
        /// layer depth — after body/legs but before the front arm — so the hand appears to grip it.
        /// </summary>
        /// <summary>
        /// Draws the off-hand shield sprite on the puppet (no-op without a shield).  Held lowered at
        /// the back hip in neutral, raised in front of the chest while guarding.  Called by
        /// <see cref="PuppetWeaponDrawLayer"/> just before the weapon so the front arm/weapon layer
        /// over it.  Override <see cref="ShieldItemType"/> to pick the shield sprite.
        /// </summary>
        internal void DrawShieldToLayer(ref PlayerDrawSet drawInfo)
        {
            if (!HasShield || _puppet == null
                || Phase == AttackPhase.Healing || Phase == AttackPhase.FleeToHeal)
                return;

            if (!_shieldDrawTexLoaded)
            {
                _shieldDrawTexLoaded = true;
                string path = ShieldDrawTexturePath;
                if (!string.IsNullOrEmpty(path) && ModContent.HasAsset(path))
                    _shieldDrawTex = ModContent.Request<Texture2D>(path,
                        ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            if (_shieldDrawTex == null)
                return;

            // The strip shares the player body's 20-frame layout: index it by the current puppet
            // body row so the shield pose tracks the animation, and draw it body-aligned so it
            // overlays the body 1:1 (each frame already positions the shield for that pose).
            const int shieldFrames = 20;
            int row    = Math.Clamp(_puppet.bodyFrame.Y / FrameHeight, 0, shieldFrames - 1);
            int frameH = _shieldDrawTex.Height / shieldFrames;
            Rectangle src = new Rectangle(0, row * frameH, _shieldDrawTex.Width, frameH);

            Vector2 origin  = new Vector2(_shieldDrawTex.Width / 2f, frameH / 2f);
            Vector2 drawPos = new Vector2(
                NPC.position.X + NPC.width / 2f - _shieldDrawTex.Width / 2f,
                NPC.position.Y + NPC.height - frameH + 4f) - Main.screenPosition + origin;
            // The equip strip supplies the carried position. During the authored guard, lift it
            // toward the chest and push it toward the attacker so the defensive state reads clearly.
            if (_shielding)
                drawPos += new Vector2(NPC.direction * 5f, -5f);
            SpriteEffects spriteFx = NPC.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            drawInfo.DrawDataCache.Add(new DrawData(
                _shieldDrawTex, drawPos, src, _layerDrawColor, 0f, origin, NPC.scale, spriteFx, 0));
        }

        private static Texture2D _slashVFXTex;
        private static bool _slashVFXTexLoadAttempted;

        // ── Generic shader-based slash trail (opt-in alternative to HasSlashVFX above) ─────────
        // A DynamicTrail ribbon (see PuppetSwordSlashTrail) driven by this puppet's own live hand
        // position / blade direction / reach, instead of the flat Slash.png sprite strip below.
        // Artorias pioneered this exact pattern (ArtoriasSwordTrail.fx) before moving to the shared
        // procedural VoidSlashVFX shader (see Artorias.DrawSwordSlashVFX); PuppetSwordSlashTrail
        // remains the reference implementation of THIS ribbon system for any puppet that opts in
        // with just a spawned projectile + three colors, no boss-specific wiring required.
        // The two systems are mutually exclusive per puppet in practice, but nothing enforces that —
        // a puppet could run both if it wanted a fallback during A/B testing.
        protected virtual bool HasSlashTrailVFX => false;
        protected virtual Color SlashTrailDarkColor => new Color(10, 2, 24);
        protected virtual Color SlashTrailCenterColor => new Color(104, 34, 180);
        protected virtual Color SlashTrailEdgeColor => new Color(232, 66, 198);

        private int _meleeSlashTrailSequence;
        private bool _meleeSlashTrailWasActive;

        /// <summary>Same swing-phase union <see cref="DrawSlashToLayer"/> uses for the sprite VFX —
        /// the shader trail should be live in every phase that already draws a swinging blade.</summary>
        private bool IsMeleeSlashActive =>
            Phase == AttackPhase.MeleeAttack || Phase == AttackPhase.StabAttack
            || Phase == AttackPhase.SpearAttack || Phase == AttackPhase.MeleeComboAttack
            || Phase == AttackPhase.JumpSlashAttack || Phase == AttackPhase.FlipSlashLand
            || Phase == AttackPhase.FlipSlashRise
            || Phase == AttackPhase.AbyssSlashSwipe || Phase == AttackPhase.TendrilSwing
            || Phase == AttackPhase.HomingVolleySwing || Phase == AttackPhase.BoomerangSwing
            || Phase == AttackPhase.SpiralFanSwing;

        /// <summary>Bumped once per fresh swing so <see cref="PuppetSwordSlashTrail"/>
        /// knows to reset its ribbon history instead of interpolating across the gap between two
        /// unrelated swings (e.g. two different combo steps).</summary>
        private void UpdateMeleeSlashTrailSequence()
        {
            bool active = IsMeleeSlashActive;
            if (active && !_meleeSlashTrailWasActive)
                _meleeSlashTrailSequence++;
            _meleeSlashTrailWasActive = active;
        }

        /// <summary>Live pose + palette for the generic shader-based slash trail. Same accessors
        /// <see cref="DrawSlashToLayer"/>'s sprite VFX already reads, just handed to the trail
        /// projectile instead of a DrawData call. Returns false while no swing is live; the caller
        /// should fade its trail history out rather than snapping it away.</summary>
        internal bool TryGetMeleeSlashTrailPose(out Vector2 pivot, out Vector2 direction,
            out float reach, out float progress, out int sequence,
            out Color darkColor, out Color centerColor, out Color edgeColor, bool backHand = false)
        {
            pivot = PuppetHandPosition;
            direction = PuppetWeaponDirection.SafeNormalize(new Vector2(NPC.direction, 0f));
            // Dual wield: an effect tracking the back weapon reads the back hand's pivot and blade instead.
            if (backHand && DualWieldActive)
            {
                pivot = PuppetBackHandPosition;
                direction = PuppetBackWeaponDirection.SafeNormalize(new Vector2(NPC.direction, 0f));
            }
            reach = Math.Max(MeleeRange * 0.7f, PuppetActiveBladeReach);
            if (Phase == AttackPhase.MeleeComboAttack && _activeMeleeComboIndex >= 0
                && _activeMeleeCombo.Steps != null && _meleeComboStepIndex >= 0
                && _meleeComboStepIndex < _activeMeleeCombo.Steps.Length)
                reach = ComboReachBase * 0.7f * _activeMeleeCombo.Steps[_meleeComboStepIndex].ReachMult;
            // Landing-timed leap slams: Phase can already advance to MeleeComboPause/Recovery in the
            // same Update tick that _leapSlamSwingProgress reaches 1 (both happen once landing is
            // detected), so gate on the motion + progress directly instead of Phase == MeleeComboAttack.
            // Without this, anything sampling this pose (the crescent sprite, the fire-slash shader)
            // never observes progress hit 1 - it freezes one tick early on whatever partial arc it had.
            bool leapSlamPoseLive = UseLandingTimedLeapSlam
                && ActiveMeleeComboMotion == ComboMotion.LeapSlam
                && _leapSlamSwingProgress > 0f;

            progress = PuppetWeaponAnimationProgress;
            if (leapSlamPoseLive)
            {
                progress = MathHelper.Clamp(_leapSlamSwingProgress, 0f, 1f);
            }
            else if (Phase == AttackPhase.MeleeComboAttack)
            {
                if (UseLandingTimedLeapSlam && ActiveMeleeComboMotion == ComboMotion.LeapSlam)
                {
                    progress = MathHelper.Clamp(_leapSlamSwingProgress, 0f, 1f);
                }
                else if (_attackRuntimeV2.Active)
                {
                    progress = _attackRuntimeV2.StageProgress;
                }
                else if (_activeMeleeCombo.Steps != null
                    && _meleeComboStepIndex >= 0
                    && _meleeComboStepIndex < _activeMeleeCombo.Steps.Length)
                {
                    MeleeComboStep step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                    int elapsed = Math.Max(0, _activeComboStepTotalTicks - PhaseTimer);
                    progress = step.Motion == ComboMotion.RisingUppercutLeap
                        ? elapsed / (float)Math.Max(1, GetWeaponUseAnimation(MeleeWeaponItemType))
                        : elapsed / (float)Math.Max(1, _activeComboStepTotalTicks);
                    progress = MathHelper.Clamp(progress, 0f, 1f);
                }
            }
            else if (IsMeleeSlashActive)
            {
                progress = Phase switch
                {
                    AttackPhase.JumpSlashAttack => 1f - PhaseTimer / (float)JumpSlashAttackTicks,
                    AttackPhase.FlipSlashLand => 1f - PhaseTimer / (float)FlipSlashLandHoldTicks,
                    AttackPhase.AbyssSlashSwipe => 1f - PhaseTimer / (float)AbyssSlashSwipeTicks,
                    AttackPhase.TendrilSwing => 1f - PhaseTimer / (float)TendrilSwingTicks,
                    AttackPhase.HomingVolleySwing => 1f - PhaseTimer / (float)HomingVolleySwingTicks,
                    AttackPhase.BoomerangSwing => 1f - PhaseTimer / (float)BoomerangSwingTicks,
                    AttackPhase.SpiralFanSwing => 1f - PhaseTimer / (float)SpiralFanSwingTicks,
                    _ => progress,
                };

                // The flip's spin has no start or end, so its crescent holds sheet frame 1 (a full
                // crescent; 0.3 * 4 frames). The landing strike's clock is its own ticks, not the hold.
                if (Phase == AttackPhase.FlipSlashRise)
                {
                    progress = 0.3f;
                }
                else if (Phase == AttackPhase.FlipSlashLand && UseFlipSlashLandingStrike)
                {
                    int strikeTicks = FlipSlashStrikeEaseInTicks + FlipSlashStrikeEaseOutTicks;
                    progress = (FlipSlashLandHoldTicks - PhaseTimer) / (float)Math.Max(1, strikeTicks);
                }

                progress = MathHelper.Clamp(progress, 0f, 1f);
            }
            sequence = _meleeSlashTrailSequence;
            darkColor = SlashTrailDarkColor;
            centerColor = SlashTrailCenterColor;
            edgeColor = SlashTrailEdgeColor;
            return (IsMeleeSlashActive || leapSlamPoseLive) && !IsLeapSlamCarry;
        }

        private bool IsLeapSlamCarry => UseLandingTimedLeapSlam
            && Phase == AttackPhase.MeleeComboAttack
            && ActiveMeleeComboMotion == ComboMotion.LeapSlam
            && _leapSlamSwingProgress <= 0f;

        // ── Generic shader-lit fire slash (opt-in third alternative alongside HasSlashVFX /
        // HasSlashTrailVFX above) ───────────────────────────────────────────────────────────────
        // A single procedural quad (Effects/GwynCinderTrail.fx, technique FireSlashArc) drawn
        // directly in PostDraw — punchier than the ribbon trail and not tied to a spawned
        // projectile. Gwyn originated the shape and the palette naming; this lifts it generically
        // onto PuppetNPC so any puppet can opt in with three colors and a couple of Arm calls.
        //
        // Unlike the sprite/ribbon systems, this one does not dim itself as the swing plays out —
        // it holds full strength for as long as the caller keeps calling ArmFireSlashVFX (every tick
        // the real hitbox is live) and only starts fading once those calls stop, over a short
        // FireSlashFadeoutTicks window. That split is what makes "how long it stays bright" track
        // the actual damage window instead of an arbitrary in-shader progress curve, and it fades
        // out cleanly instead of freezing on a stale pose/size when a caller skips a tick (e.g. a
        // non-damaging carry motion that deliberately never arms it).
        protected virtual bool HasFireSlashVFX => false;
        protected virtual Color FireSlashCinderColor => new Color(64, 8, 2);
        protected virtual Color FireSlashFlameColor => new Color(255, 116, 14);
        protected virtual Color FireSlashCoreColor => new Color(255, 236, 172);
        protected virtual float FireSlashOpacity => 0.95f;
        protected virtual int FireSlashFadeoutTicks => 10;

        // Quad proportions the FireSlashArc technique's geometry is authored against: width/height
        // are multiples of the live blade reach, and the quad is centred `offsetMult * reach` ahead
        // of the hand so the shader's local arc apex lands on the weapon tip. A greatsword and an
        // axe read as different sizes at the same "reach" number, so subclasses can retune these
        // instead of every puppet fighting over one shared magic number.
        protected virtual float FireSlashQuadWidthMult => 1.5f;
        protected virtual float FireSlashQuadHeightMult => 1.9f;
        protected virtual float FireSlashQuadOffsetMult => 0.325f;
        /// <summary>Small sprite-rig calibration after the reach-scaled anchor, measured along the blade.</summary>
        protected virtual float FireSlashForwardOffsetPixels => 0f;
        /// <summary>Small screen/world-axis calibration after the blade anchor, for unusual weapon art.</summary>
        protected virtual Vector2 FireSlashWorldOffset => Vector2.Zero;
        /// <summary>Which facing vertically mirrors the procedural arc in its local blade space.
        /// Most fire-sword users inherit Gwyn's right-facing sweep; asymmetric axes can reverse it
        /// without changing their weapon direction, hitbox, or hand anchor.</summary>
        protected virtual bool FireSlashSweepFlippedWhenFacingRight => true;

        private int _fireSlashTicksSinceLive = int.MaxValue;
        private float _fireSlashLiveReach;
        private float _fireSlashLiveSweep;
        private Vector2 _fireSlashLivePosition;
        private float _fireSlashLiveRotation;
        private bool _fireSlashLiveFlipped;

        /// <summary>Call every tick the real melee hitbox is live (from <see cref="OnMeleeComboAttackTick"/>
        /// or <see cref="DoMeleeAttack"/>) to keep the fire slash at full strength and tracking the
        /// live blade. <paramref name="sweepProgress"/> is this swing's 0-to-1 position, used only to
        /// shape the shader's internal reveal — stop calling this the instant the swing should no
        /// longer read as "hitting" and the quad fades out on its own over
        /// <see cref="FireSlashFadeoutTicks"/> ticks rather than vanishing or freezing stale.</summary>
        protected void ArmFireSlashVFX(float reach, float sweepProgress)
        {
            _fireSlashTicksSinceLive = 0;
            _fireSlashLiveReach = Math.Max(24f, reach);
            _fireSlashLiveSweep = MathHelper.Clamp(sweepProgress, 0f, 1f);

            Vector2 hand = PuppetHandPosition;
            Vector2 direction = PuppetWeaponDirection.SafeNormalize(new Vector2(NPC.direction, 0f));
            _fireSlashLivePosition = hand
                + direction * (_fireSlashLiveReach * FireSlashQuadOffsetMult + FireSlashForwardOffsetPixels)
                + FireSlashWorldOffset;
            _fireSlashLiveRotation = direction.ToRotation();
            // Mirrors the shader's local Y, which mirrors the sweep with the puppet's facing.
            _fireSlashLiveFlipped = FireSlashSweepFlippedWhenFacingRight
                ? NPC.direction > 0
                : NPC.direction < 0;
        }

        /// <summary>Ages the post-swing fadeout by one tick. Runs every tick regardless of
        /// <see cref="HasFireSlashVFX"/> — a puppet that never opts in just never re-arms it, so
        /// this only ever counts up to the cap and sits there.</summary>
        private void UpdateFireSlashFade()
        {
            if (_fireSlashTicksSinceLive <= FireSlashFadeoutTicks)
                _fireSlashTicksSinceLive++;
        }

        private static Effect _fireSlashEffect;
        private static Texture2D _fireSlashShapeNoise;
        private static Texture2D _fireSlashDetailNoise;
        private const float FireSlashPixelBlockSize = 2f;

        /// <summary>Draws the fire slash quad while live or fading. The arc's shape comes from the
        /// shader rather than a sprite (see FireSlashArcPixel for why), so all this does is size,
        /// place, and light a plain noise quad.</summary>
        private void DrawFireSlashVFX()
        {
            if (!HasFireSlashVFX || Main.dedServ
                || _fireSlashTicksSinceLive > FireSlashFadeoutTicks || _fireSlashLiveReach <= 0f)
                return;

            _fireSlashEffect ??= ModContent.Request<Effect>(
                "tsorcRevamp/Effects/GwynCinderTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            _fireSlashShapeNoise ??= ModContent.Request<Texture2D>(
                "tsorcRevamp/Textures/Noise/Turbulence_06-512x512", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            _fireSlashDetailNoise ??= ModContent.Request<Texture2D>(
                "tsorcRevamp/Textures/Noise/Turbulence_07-512x512", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            Texture2D noiseTexture = _fireSlashShapeNoise;
            int quadWidth = Math.Max(2, (int)(_fireSlashLiveReach * FireSlashQuadWidthMult));
            int quadHeight = Math.Max(2, (int)(_fireSlashLiveReach * FireSlashQuadHeightMult));
            Rectangle source = new Rectangle(0, 0, quadWidth, quadHeight);
            Vector2 quadSize = source.Size();

            Vector2 position = _fireSlashLivePosition - Main.screenPosition;
            SpriteEffects spriteEffects = _fireSlashLiveFlipped ? SpriteEffects.FlipVertically : SpriteEffects.None;

            // Full strength through the entire live window (ticksSinceLive stays 0 as long as the
            // caller keeps re-arming every tick); the FireSlashFadeoutTicks after that ease it to
            // zero instead of popping to nothing the instant the caller stops.
            float opacity = FireSlashOpacity
                * (1f - _fireSlashTicksSinceLive / (float)Math.Max(1, FireSlashFadeoutTicks));

            Vector2 pixelBlocks = quadSize / FireSlashPixelBlockSize;
            Vector4 pixelGrid = new Vector4(pixelBlocks.X, pixelBlocks.Y, 1f / pixelBlocks.X, 1f / pixelBlocks.Y);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = _fireSlashDetailNoise;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                _fireSlashEffect.CurrentTechnique = _fireSlashEffect.Techniques["FireSlashArc"];
                _fireSlashEffect.Parameters["CinderColor"].SetValue(FireSlashCinderColor.ToVector3());
                _fireSlashEffect.Parameters["FlameColor"].SetValue(FireSlashFlameColor.ToVector3());
                _fireSlashEffect.Parameters["CoreColor"].SetValue(FireSlashCoreColor.ToVector3());
                _fireSlashEffect.Parameters["Opacity"].SetValue(opacity);
                _fireSlashEffect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                _fireSlashEffect.Parameters["Progress"].SetValue(_fireSlashLiveSweep);
                _fireSlashEffect.Parameters["DrawSize"].SetValue(quadSize);
                _fireSlashEffect.Parameters["CoordScale"].SetValue(noiseTexture.Size() / quadSize);
                _fireSlashEffect.Parameters["PixelGrid"].SetValue(pixelGrid);
                _fireSlashEffect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(noiseTexture, position, source, Color.White, _fireSlashLiveRotation,
                    quadSize * 0.5f, 1f, spriteEffects, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            DrawFireSlashVFX();
            DrawFlailsAbovePuppet(drawColor);
        }

        /// <summary>Finishes a puppet-owned ball-and-chain after the complete player composite.
        /// Projectile PreDraw is earlier than a PuppetNPC's player renderer, which previously let
        /// torso, armor and the front arm cover the chain. This final pass deliberately puts the
        /// full prop over the puppet; the hand anchor still makes it visibly originate from the hand.</summary>
        private void DrawFlailsAbovePuppet(Color drawColor)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active
                    && projectile.ModProjectile is EnemyFlailProjectileBase flail
                    && flail.IsOwnedByPuppet(NPC))
                {
                    flail.DrawAbovePuppet(drawColor);
                }
            }
        }

        // A composed puppet cannot use DrawUnblockableWeaponAura directly because its body is many
        // PlayerDrawSet layers rather than one texture. This applies the same eight-direction red
        // silhouette pattern to the completed draw cache, then restores the ordinary body on top.
        // Default-off so subclasses can opt in for a specific telegraph phase.
        protected virtual bool HasUnblockableBodyAura => false;
        protected virtual float UnblockableBodyAuraScale => 1.1f;
        protected virtual float UnblockableBodyAuraOpacity => 0.5f;
        private readonly List<DrawData> _unblockableBodyDrawCache = new List<DrawData>();

        private void TransformUnblockableBodyAura(ref PlayerDrawSet drawInfo)
        {
            Vector2 anchor = NPC.Center - Main.screenPosition;
            float scale = Math.Max(1f, UnblockableBodyAuraScale);
            float opacity = MathHelper.Clamp(UnblockableBodyAuraOpacity, 0f, 1f);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
            float outlineRadius = MathHelper.Lerp(1.6f, 2.8f, pulse) * scale;
            // Opacity describes the finished eight-copy silhouette, not every individual copy.
            // Applying it to each layer made 50% alpha accumulate to virtually solid red wherever
            // the displaced body sprites overlapped.
            float layerOpacity = opacity <= 0f
                ? 0f
                : 1f - (float)Math.Pow(1f - opacity, 1f / AttackTelegraphDraw.GlowDirections.Length);
            Color auraColor = new Color(255, 4, 2) * layerOpacity;

            _unblockableBodyDrawCache.Clear();
            _unblockableBodyDrawCache.AddRange(drawInfo.DrawDataCache);
            drawInfo.DrawDataCache.Clear();

            foreach (Vector2 direction in AttackTelegraphDraw.GlowDirections)
            {
                Vector2 offset = direction * outlineRadius;
                foreach (DrawData original in _unblockableBodyDrawCache)
                {
                    DrawData aura = original;
                    aura.position = anchor + (original.position - anchor) * scale + offset;
                    aura.scale *= scale;
                    aura.color = auraColor * (original.color.A / 255f);
                    aura.shader = 0;
                    drawInfo.DrawDataCache.Add(aura);
                }
            }

            // The normal puppet is last, so the warning remains a silhouette behind the armor
            // rather than tinting or washing out Gwyn himself.
            drawInfo.DrawDataCache.AddRange(_unblockableBodyDrawCache);
        }

        // The finished player draw cache becomes the spectral body. This preserves every armor
        // layer and the hand/weapon attachment without a second solid small puppet underneath.
        protected virtual bool HasSpectralOverlay => false;
        /// <summary>Keep armor/wing dye shaders on the enlarged spectral body (halo and trail copies stay
        /// flat). Off by default: ghost-style overlays are meant to read as a tinted silhouette.</summary>
        protected virtual bool SpectralKeepsArmorDyes => false;
        protected virtual float SpectralOverlayScale => 3f;
        protected virtual Color SpectralOverlayColor => new Color(100, 200, 255);
        protected virtual float SpectralCoreTintStrength => 0.75f;
        protected virtual float SpectralCoreOpacity => 1f;
        protected virtual Color SpectralHaloColor => new Color(40, 140, 255);
        protected virtual int SpectralHaloCopyCount => 12;
        protected virtual float SpectralHaloRadius => 20f;
        protected virtual float SpectralHaloOpacity => 0.45f;
        protected virtual float SpectralHaloScale => 1.05f;
        protected virtual bool SpectralHaloFollowsCoreOpacity => true;
        protected virtual float SpectralTrailOpacity => 0.45f;
        /// <summary>Suppress halo and motion-history copies below the puppet's current feet.</summary>
        protected virtual bool SpectralExcludeDownwardCopies => false;
        /// <summary>Gate for the radial halo ring and the motion-history trail specifically — NOT
        /// the enlarged solid body itself, which always draws while <see cref="HasSpectralOverlay"/>
        /// is on. Lets a puppet keep its permanent size-up while reserving the busier ghost-copy
        /// visual for a specific moment (Owl Father: leap attacks only).</summary>
        protected virtual bool ShowSpectralGhostCopies => true;

        // Optional shader drawn as an extra copy of the oversized armor pieces. A subclass may opt
        // its melee weapon into the same mask; every solid sprite and all existing VFX remain intact.
        protected virtual int LargeArmorMaskShaderId => 0;
        protected virtual bool IncludeMeleeWeaponInLargeArmorMask => false;

        // Optional raw equip-sheet replacements for an oversized puppet template. The normal
        // player compositor still supplies its familiar 40x56 frame coordinates; the transform
        // below swaps only the matching armor layer, then expands those coordinates into the
        // authored sheet's larger cells. This deliberately does not depend on a synthetic
        // EquipLoader slot, so a phase may change template after the player draw cache exists.
        protected virtual string LargeHeadArmorTemplateTexture => null;
        protected virtual string LargeBodyArmorTemplateTexture => null;
        protected virtual string LargeLegsArmorTemplateTexture => null;
        private Texture2D _largeHeadArmorTemplateTexture;
        private Texture2D _largeBodyArmorTemplateTexture;
        private Texture2D _largeLegsArmorTemplateTexture;

        private Vector2[] _spectralOldPositions;
        private readonly List<DrawData> _spectralDrawCache = new List<DrawData>();

        private void UpdateSpectralHistory()
        {
            if (Main.dedServ || !HasSpectralOverlay)
                return;
            if (_spectralOldPositions == null)
            {
                _spectralOldPositions = new Vector2[18];
                Array.Fill(_spectralOldPositions, NPC.position);
            }
            for (int i = _spectralOldPositions.Length - 1; i > 0; i--)
                _spectralOldPositions[i] = _spectralOldPositions[i - 1];
            _spectralOldPositions[0] = NPC.position;
        }

        /// <summary>Hydra's shield treatment: 12 radial blue silhouettes, 18-position motion
        /// history, then a blue-tinted core. Called after armor/weapon layers have composed.</summary>
        internal void TransformSpectralDrawData(ref PlayerDrawSet drawInfo)
        {
            if (drawInfo.drawPlayer != _puppet)
                return;

            TransformLargeArmorTemplate(ref drawInfo);

            if (HasUnblockableBodyAura)
            {
                TransformUnblockableBodyAura(ref drawInfo);
                return;
            }

            if (!HasSpectralOverlay)
                return;

            Vector2 feet = NPC.Bottom - Main.screenPosition;
            _spectralDrawCache.Clear();
            foreach (DrawData original in drawInfo.DrawDataCache)
            {
                DrawData data = original;
                data.position = feet + (data.position - feet) * SpectralOverlayScale;
                data.scale *= SpectralOverlayScale;
                data.color = Color.Lerp(data.color,
                    SpectralOverlayColor * (data.color.A / 255f), SpectralCoreTintStrength)
                    * SpectralCoreOpacity;

                // Stripped by default: a ghost-style overlay (Artorias's phantom) is a flat tinted
                // silhouette. A giant form that is still "the real body" keeps its armor dyes.
                if (!SpectralKeepsArmorDyes)
                {
                    data.shader = 0;
                }

                _spectralDrawCache.Add(data);
            }
            drawInfo.DrawDataCache.Clear();

            if (ShowSpectralGhostCopies)
            {
                Color haloColor = SpectralHaloColor * SpectralHaloOpacity;
                for (int i = 0; i < SpectralHaloCopyCount; i++)
                {
                    Vector2 offset = new Vector2(SpectralHaloRadius, 0f)
                        .RotatedBy(MathHelper.TwoPi * i / SpectralHaloCopyCount);
                    if (SpectralExcludeDownwardCopies && offset.Y > 0.01f)
                        continue;
                    foreach (DrawData core in _spectralDrawCache)
                    {
                        DrawData halo = core;
                        halo.shader = 0; // flat-colour copy even when the core keeps its dyes
                        halo.position = feet + (core.position - feet) * SpectralHaloScale + offset;
                        halo.scale *= SpectralHaloScale;
                        halo.color = SpectralHaloFollowsCoreOpacity
                            ? haloColor * (core.color.A / 255f)
                            : haloColor;
                        drawInfo.DrawDataCache.Add(halo);
                    }
                }
                if (_spectralOldPositions != null)
                {
                    for (int k = _spectralOldPositions.Length - 1; k >= 1; k--)
                    {
                        Vector2 offset = _spectralOldPositions[k] - NPC.position;
                        // Avoid stacking stationary copies or spanning an instantaneous teleport.
                        if (offset.LengthSquared() < 1f || offset.LengthSquared() > 320f * 320f)
                            continue;
                        if (SpectralExcludeDownwardCopies && offset.Y > 1f)
                            continue;
                        Color trailColor = SpectralHaloColor
                            * ((_spectralOldPositions.Length - k) / (float)_spectralOldPositions.Length
                                * SpectralTrailOpacity);
                        foreach (DrawData core in _spectralDrawCache)
                        {
                            DrawData trail = core;
                            trail.shader = 0; // flat-colour copy even when the core keeps its dyes
                            trail.position += offset;
                            trail.color = trailColor * (core.color.A / 255f);
                            drawInfo.DrawDataCache.Add(trail);
                        }
                    }
                }
            }
            drawInfo.DrawDataCache.AddRange(_spectralDrawCache);
            AddLargeArmorMaskOverlay(ref drawInfo);
        }

        /// <summary>Adds a final shader copy of the authored large armor cells and, when requested,
        /// the currently held melee weapon. Because the shader samples the same texture/source
        /// rectangle as each solid sprite, alpha is an exact mask; skin, halo, trails and attack VFX
        /// are never included.</summary>
        private void AddLargeArmorMaskOverlay(ref PlayerDrawSet drawInfo)
        {
            int shaderId = LargeArmorMaskShaderId;
            if (shaderId <= 0)
                return;

            Texture2D meleeWeaponTexture = null;
            if (IncludeMeleeWeaponInLargeArmorMask && MeleeWeaponItemType > 0)
            {
                if (!string.IsNullOrEmpty(MeleeDrawTexturePath)
                    && (MeleeDrawTexturePath.StartsWith("Terraria/")
                        || ModContent.HasAsset(MeleeDrawTexturePath)))
                {
                    meleeWeaponTexture = ModContent.Request<Texture2D>(MeleeDrawTexturePath,
                        ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }
                else
                {
                    Main.instance.LoadItem(MeleeWeaponItemType);
                    meleeWeaponTexture = TextureAssets.Item[MeleeWeaponItemType]?.Value;
                }
            }

            // Matches either an oversized template texture (ArmorTemplateScale > 1, substituted in
            // TransformLargeArmorTemplate) or the puppet's own normal-size equipped textures
            // (ArmorTemplateScale == 1, enlarged instead via SpectralOverlayScale) — a puppet's
            // current phase determines which of these is actually what's sitting in the draw cache.
            Texture2D headTexture = _puppet.head > 0 && _puppet.head < TextureAssets.ArmorHead.Length
                ? TextureAssets.ArmorHead[_puppet.head]?.Value
                : null;
            Texture2D bodyTexture = _puppet.body > 0 && _puppet.body < TextureAssets.ArmorBody.Length
                ? TextureAssets.ArmorBody[_puppet.body]?.Value
                : null;
            Texture2D bodyCompositeTexture = _puppet.body > 0
                && _puppet.body < TextureAssets.ArmorBodyComposite.Length
                ? TextureAssets.ArmorBodyComposite[_puppet.body]?.Value
                : null;
            Texture2D armTexture = _puppet.body > 0 && _puppet.body < TextureAssets.ArmorArm.Length
                ? TextureAssets.ArmorArm[_puppet.body]?.Value
                : null;
            Texture2D legsTexture = _puppet.legs > 0 && _puppet.legs < TextureAssets.ArmorLeg.Length
                ? TextureAssets.ArmorLeg[_puppet.legs]?.Value
                : null;

            foreach (DrawData core in _spectralDrawCache)
            {
                bool isLargeArmor = core.texture == _largeHeadArmorTemplateTexture
                    || core.texture == _largeBodyArmorTemplateTexture
                    || core.texture == _largeLegsArmorTemplateTexture
                    || core.texture == headTexture
                    || core.texture == bodyTexture
                    || core.texture == bodyCompositeTexture
                    || core.texture == armTexture
                    || core.texture == legsTexture;
                bool isMeleeWeapon = meleeWeaponTexture != null && core.texture == meleeWeaponTexture;
                if (!isLargeArmor && !isMeleeWeapon)
                    continue;

                DrawData solarOverlay = core;
                solarOverlay.color = Color.White;
                solarOverlay.shader = shaderId;
                drawInfo.DrawDataCache.Add(solarOverlay);
            }
        }

        /// <summary>Adapts an oversized armor template to Terraria's fixed 40x56 player-frame
        /// pipeline. Vanilla first composes the usual small frames; this substitutes the matching
        /// larger source rectangles while applying the same feet-anchored geometry transform to the
        /// rest of the rig. Weapon, arm anchors, VFX, and animation therefore stay aligned without
        /// teaching every vanilla PlayerDrawLayer about a new cell size.</summary>
        private void TransformLargeArmorTemplate(ref PlayerDrawSet drawInfo)
        {
            int templateScale = Math.Max(1, ArmorTemplateScale);
            if (templateScale == 1 || Main.dedServ || _puppet == null)
            {
                return;
            }

            Texture2D headTexture = _puppet.head > 0 && _puppet.head < TextureAssets.ArmorHead.Length
                ? TextureAssets.ArmorHead[_puppet.head]?.Value
                : null;
            Texture2D bodyTexture = _puppet.body > 0 && _puppet.body < TextureAssets.ArmorBody.Length
                ? TextureAssets.ArmorBody[_puppet.body]?.Value
                : null;
            Texture2D bodyCompositeTexture = _puppet.body > 0
                && _puppet.body < TextureAssets.ArmorBodyComposite.Length
                ? TextureAssets.ArmorBodyComposite[_puppet.body]?.Value
                : null;
            Texture2D armTexture = _puppet.body > 0 && _puppet.body < TextureAssets.ArmorArm.Length
                ? TextureAssets.ArmorArm[_puppet.body]?.Value
                : null;
            Texture2D legsTexture = _puppet.legs > 0 && _puppet.legs < TextureAssets.ArmorLeg.Length
                ? TextureAssets.ArmorLeg[_puppet.legs]?.Value
                : null;

            if (headTexture == null && bodyTexture == null && bodyCompositeTexture == null
                && armTexture == null && legsTexture == null)
            {
                return;
            }

            _largeHeadArmorTemplateTexture ??= RequestLargeArmorTemplateTexture(LargeHeadArmorTemplateTexture);
            _largeBodyArmorTemplateTexture ??= RequestLargeArmorTemplateTexture(LargeBodyArmorTemplateTexture);
            _largeLegsArmorTemplateTexture ??= RequestLargeArmorTemplateTexture(LargeLegsArmorTemplateTexture);

            Vector2 feet = NPC.Bottom - Main.screenPosition;
            for (int i = 0; i < drawInfo.DrawDataCache.Count; i++)
            {
                DrawData data = drawInfo.DrawDataCache[i];
                Texture2D replacementTexture = headTexture != null && data.texture == headTexture
                    ? _largeHeadArmorTemplateTexture
                    : (bodyTexture != null && data.texture == bodyTexture)
                        || (bodyCompositeTexture != null && data.texture == bodyCompositeTexture)
                        || (armTexture != null && data.texture == armTexture)
                            ? _largeBodyArmorTemplateTexture
                            : legsTexture != null && data.texture == legsTexture
                                ? _largeLegsArmorTemplateTexture
                                : null;
                bool isLargeArmorPiece = replacementTexture != null;

                data.position = feet + (data.position - feet) * templateScale;

                if (isLargeArmorPiece && data.sourceRect.HasValue)
                {
                    data.texture = replacementTexture;
                    Rectangle source = data.sourceRect.Value;
                    data.sourceRect = new Rectangle(
                        source.X * templateScale,
                        source.Y * templateScale,
                        source.Width * templateScale,
                        source.Height * templateScale);
                    data.origin *= templateScale;
                }
                else
                {
                    // Weapons and any puppet-owned draw data have ordinary-size source art, so
                    // retain the same visual proportion to the new body by scaling their DrawData.
                    data.scale *= templateScale;
                }

                drawInfo.DrawDataCache[i] = data;
            }
        }

        private static Texture2D RequestLargeArmorTemplateTexture(string path)
        {
            return string.IsNullOrEmpty(path) || !ModContent.HasAsset(path)
                ? null
                : ModContent.Request<Texture2D>(path, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        /// <summary>
        /// Draws an arc-shaped slash swoosh along the same angle/reach <see cref="TickBladeHit"/>
        /// uses for real hit detection (<see cref="GetWeaponWorldDirection"/> + the active blade
        /// reach) — an honest preview of the actual hitbox rather than disconnected decoration.
        /// Reuses the generic Slash.png frame-strip asset from the player-weapon BroadswordRework
        /// system (same visual language as the real player slash VFX). No-op unless
        /// <see cref="HasSlashVFX"/> and a trackable swing is actually live.
        /// </summary>
        internal void DrawSlashToLayer(ref PlayerDrawSet drawInfo)
        {
            if (!HasSlashVFX || SuppressSlashVFXForCurrentPhase || IsLeapSlamCarry)
                return;

            bool standardSwing = Phase == AttackPhase.MeleeAttack || Phase == AttackPhase.StabAttack
                || Phase == AttackPhase.SpearAttack || Phase == AttackPhase.MeleeComboAttack;
            bool specialSwing = Phase == AttackPhase.JumpSlashAttack || Phase == AttackPhase.FlipSlashLand
                || Phase == AttackPhase.AbyssSlashSwipe || Phase == AttackPhase.TendrilSwing
                || Phase == AttackPhase.HomingVolleySwing || Phase == AttackPhase.BoomerangSwing
                || Phase == AttackPhase.SpiralFanSwing;
            if (!standardSwing && !specialSwing)
                return;

            bool visualComboSlash = Phase == AttackPhase.MeleeComboAttack
                && _activeMeleeCombo.Steps != null
                && _meleeComboStepIndex >= 0
                && _meleeComboStepIndex < _activeMeleeCombo.Steps.Length
                && (IsArcSwingMotion(_activeMeleeCombo.Steps[_meleeComboStepIndex].Motion)
                    || _activeMeleeCombo.Steps[_meleeComboStepIndex].Motion == ComboMotion.LeapSlam
                    || _activeMeleeCombo.Steps[_meleeComboStepIndex].Motion == ComboMotion.RisingUppercutLeap
                    || _activeMeleeCombo.Steps[_meleeComboStepIndex].Motion == ComboMotion.Spin
                    || _activeMeleeCombo.Steps[_meleeComboStepIndex].Motion == ComboMotion.ApexDiveCleave);
            bool runtimeV2Slash = _attackRuntimeV2.Active
                && _attackRuntimeV2.Stage == PuppetAttackStage.Active;
            bool needsComboVisualFallback = visualComboSlash && (!_bladeArmed || _activeBladeReach <= 0f);
            if (standardSwing && (!_bladeArmed || _activeBladeReach <= 0f)
                && !needsComboVisualFallback && !runtimeV2Slash)
                return;

            if (!_slashVFXTexLoadAttempted)
            {
                _slashVFXTexLoadAttempted = true;
                const string path = "tsorcRevamp/Content/Items/Weapons/Melee/Broadswords/BroadswordRework/Common/Melee/Slash";
                if (ModContent.HasAsset(path))
                    _slashVFXTex = ModContent.Request<Texture2D>(path, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            if (_slashVFXTex == null)
                return;

            float t;
            float reach = _activeBladeReach;
            if (runtimeV2Slash)
            {
                MeleeComboStep step = _activeMeleeCombo.Steps[0];
                reach = ComboReachBase * 0.7f * step.ReachMult;
                t = _attackRuntimeV2.StageProgress;
            }
            else if (needsComboVisualFallback)
            {
                MeleeComboStep step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                reach = ComboReachBase * 0.7f * step.ReachMult;
                if (step.Motion == ComboMotion.RisingUppercutLeap)
                {
                    int elapsed = Math.Max(0, step.AttackTicks - PhaseTimer);
                    t = elapsed / (float)Math.Max(1, GetWeaponUseAnimation(MeleeWeaponItemType));
                }
                else
                {
                    float speedMult = step.SwingSpeedMult > 0f ? step.SwingSpeedMult : 1f;
                    int swingTicks = UseAuthoredComboSwingClock && IsArcSwingMotion(step.Motion)
                        ? Math.Max(6, (int)Math.Round(step.AttackTicks / speedMult))
                        : Math.Max(1, GetMeleeSwingTicks(step.AttackTicks));
                    t = 1f - PhaseTimer / (float)swingTicks;
                }
            }
            else if (specialSwing)
            {
                reach = MeleeRange * 0.7f;
                t = Phase switch
                {
                    AttackPhase.JumpSlashAttack => 1f - PhaseTimer / (float)JumpSlashAttackTicks,
                    AttackPhase.FlipSlashLand => 1f - PhaseTimer / (float)FlipSlashLandHoldTicks,
                    AttackPhase.AbyssSlashSwipe => 1f - PhaseTimer / (float)AbyssSlashSwipeTicks,
                    AttackPhase.TendrilSwing => 1f - PhaseTimer / (float)TendrilSwingTicks,
                    AttackPhase.HomingVolleySwing => 1f - PhaseTimer / (float)HomingVolleySwingTicks,
                    AttackPhase.BoomerangSwing => 1f - PhaseTimer / (float)BoomerangSwingTicks,
                    AttackPhase.SpiralFanSwing => 1f - PhaseTimer / (float)SpiralFanSwingTicks,
                    _ => 1f,
                };
            }
            else
            {
                t = _weaponAnimMax > 0 ? 1f - (float)_weaponAnim / _weaponAnimMax : 1f;
            }
            t = MathHelper.Clamp(t, 0f, 1f);

            var frame = new SpriteFrame(1, 3) { CurrentRow = (byte)Math.Min(2, (int)(t * 3f)) };

            Vector2 direction = GetWeaponWorldDirection();
            if (ActiveComboSwingHand == ComboHand.Back)
            {
                direction = GetBackWeaponWorldDirection();
            }
            float rotation = direction.ToRotation();
            // Match BroadswordRework's player layer: the arc is centered on the wielder, while
            // rotation and scale communicate the live blade direction and reach.
            Vector2 position = NPC.Center + direction * 2f;
            Rectangle sourceRectangle = frame.GetSourceRectangle(_slashVFXTex);
            Vector2 origin = sourceRectangle.Size() * 0.5f;
            float scale = reach / 30f * SlashVFXScale;
            bool flippedSwing = UseAlternateFlip && _comboSwingFlipped;
            SpriteEffects spriteFx = (NPC.direction > 0) ^ flippedSwing ? SpriteEffects.FlipVertically : SpriteEffects.None;

            float maxAlpha = SlashVFXOpacity;
            var alphaGradient = new Gradient<float>(
                (0.00f, 0f),
                (0.25f, maxAlpha),
                (0.75f, maxAlpha),
                (1.00f, 0f)
            );
            Color color = Lighting.GetColor(position.ToTileCoordinates()).MultiplyRGB(SlashVFXColor) * alphaGradient.GetValue(t);

            drawInfo.DrawDataCache.Add(new DrawData(
                _slashVFXTex, position - Main.screenPosition, sourceRectangle, color, rotation, origin, scale, spriteFx, 0));
        }

        /// <summary>
        /// Dual wield: draws the back-hand weapon, called from <see cref="PuppetBackWeaponDrawLayer"/> so it
        /// renders over the back arm but under the torso. Handle-anchored at the back composite hand with the
        /// same grip-mirroring and blade-flip rules as the front melee branch of <see cref="DrawWeaponToLayer"/>,
        /// reading everything off the back hand's own <see cref="PuppetWeapon"/>.
        /// </summary>
        internal void DrawBackHandWeaponToLayer(ref PlayerDrawSet drawInfo)
        {
            if (!BackHandWeaponVisible)
            {
                return;
            }

            PuppetWeapon weapon = BackHandWeapon;
            if (weapon.HideHeldSprite)
            {
                return;
            }

            Main.instance.LoadItem(weapon.ItemType);
            Texture2D texture = TextureAssets.Item[weapon.ItemType]?.Value;
            if (texture == null)
            {
                return;
            }

            // Origin is in pre-flip texture space, so mirror the grip for FlipHorizontally (and FlipVertically).
            float handleX = texture.Width * weapon.HandleNorm.X;
            if (NPC.direction != 1)
            {
                handleX = texture.Width * (1f - weapon.HandleNorm.X);
            }
            Vector2 origin = new Vector2(handleX, texture.Height * weapon.HandleNorm.Y);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (NPC.direction == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            if (BladeFlipActiveFor(weapon))
            {
                spriteEffects |= SpriteEffects.FlipVertically;
                origin.Y = texture.Height - origin.Y;
            }

            float drawRotation = GetMeleeDrawRotation(BackHandPoseRotation, weapon);
            Vector2 drawPosition = GetUnscaledBackHandPosition() - Main.screenPosition;

            drawInfo.DrawDataCache.Add(new DrawData(
                texture,
                drawPosition,
                null,
                _layerDrawColor,
                drawRotation,
                origin,
                NPC.scale * weapon.DrawScale,
                spriteEffects,
                0));
        }

        internal void DrawWeaponToLayer(ref PlayerDrawSet drawInfo)
        {
            // Healing: draw the estus flask instead of the combat weapon.
            if (Phase == AttackPhase.Healing)
            {
                DrawEstusFlaskToLayer(ref drawInfo);
                return;
            }

            if (DrawSpecialHeldWeapon(ref drawInfo))
                return;

            if (Phase == AttackPhase.FleeToHeal || !_weaponVisible || _heldItemType <= 0)
                return;

            // Flails (and similar) render their own projectile visual (ball + chain), so the held
            // item icon shouldn't be drawn in the hand at all.
            // Read the hide flag off the weapon actually in hand, not the puppet: a hand that swapped
            // to a flail/whip must hide its icon even when the puppet's default weapon is a sword.
            if (FrontHandWeapon.HideHeldSprite && _heldItemType == FrontHandWeaponType && !_forceExportHeldWeapon)
                return;

            // Only draw as spear when actually holding the polearm — not the magic staff, secondary ranged,
            // or cursed knives (those are separate weapons that use normal centred draw).
            bool holdingSpearNow = DrawWeaponAsSpear
                && _heldItemType != MagicWeaponItemType
                && _heldItemType != SecondaryRangedWeaponItemType
                && (CursedKnivesWeaponItemType < 0 || _heldItemType != CursedKnivesWeaponItemType);

            // When drawing as a spear, prefer the holdout-projectile texture (the full shaft+head sprite)
            // over the item icon (which is just a small inventory tile).
            Texture2D tex;
            // ModContent.HasAsset only answers for MOD assets, so a vanilla holdout path
            // ("Terraria/Images/Projectile_N") used to fail this check and silently fall back to the small
            // item icon. Vanilla assets are always loadable, so accept them directly.
            bool spearTextureUsable = SpearDrawTexturePath != null
                && (SpearDrawTexturePath.StartsWith("Terraria/") || ModContent.HasAsset(SpearDrawTexturePath));

            // Same "prefer the full weapon art over the tiny inventory icon" idea as the spear branch
            // above, for a swing that isn't drawn as a spear. Only applies while actually holding the
            // melee weapon — a magic/ranged/cursed-knives swap in the same hand keeps its own icon.
            bool meleeTextureUsable = MeleeDrawTexturePath != null
                && _heldItemType == MeleeWeaponItemType
                && (MeleeDrawTexturePath.StartsWith("Terraria/") || ModContent.HasAsset(MeleeDrawTexturePath));

            string rangedDrawTexturePath = GetHeldRangedDrawTexturePath(_heldItemType);
            bool rangedTextureUsable = rangedDrawTexturePath != null
                && _heldItemType == _activeRangedItemType
                && (rangedDrawTexturePath.StartsWith("Terraria/") || ModContent.HasAsset(rangedDrawTexturePath));

            if (holdingSpearNow && spearTextureUsable)
            {
                tex = ModContent.Request<Texture2D>(SpearDrawTexturePath,
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            else if (!holdingSpearNow && meleeTextureUsable)
            {
                tex = ModContent.Request<Texture2D>(MeleeDrawTexturePath,
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            else if (rangedTextureUsable)
            {
                tex = ModContent.Request<Texture2D>(rangedDrawTexturePath,
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            else
            {
                // Vanilla item textures are lazy-loaded; ensure they're in memory before drawing.
                Main.instance.LoadItem(_heldItemType);
                var texAsset = TextureAssets.Item[_heldItemType];
                if (texAsset?.Value == null)
                {
                    return;
                }
                tex = texAsset.Value;
            }
            // A weapon that doubles as the melee weapon (e.g. a spear that also casts the primary-ranged
            // projectile) draws as MELEE — handle-anchored at full melee scale — not as a centred thrown item.
            bool heldRangedLike = (_heldItemType == RangedWeaponItemType && RangedWeaponItemType != MeleeWeaponItemType)
                               || _heldItemType == SecondaryRangedWeaponItemType
                               || _heldItemType == MagicWeaponItemType;
            bool heldCrossbowLike = _heldItemType == _activeRangedItemType
                                 && _activeRangedStyle == RangedStyle.Crossbow;
            bool heldBowLike = _heldItemType == _activeRangedItemType
                            && _activeRangedStyle == RangedStyle.Bow;
            bool flipHeldRangedHorizontally = heldRangedLike
                && ShouldFlipHeldRangedSpriteHorizontally(_heldItemType);
            // Melee scale comes from the equipped weapon so a per-attack swap rescales with it;
            // ranged/crossbow keep their own held-item scaling.
            float scale = FrontHandWeapon.DrawScale;

            if (heldCrossbowLike)
            {
                scale = 0.8f;
            }
            else if (heldRangedLike)
            {
                scale = GetHeldRangedDrawScale(_heldItemType);
            }
            // The spectral transform scales the complete draw cache once, including this hand.
            Vector2 drawPos = GetUnscaledHandPosition() - Main.screenPosition;
            bool bowDrawPose = heldBowLike && BowStringDrawPoseActive;
            if (bowDrawPose)
            {
                // Archer pose: the bow lives in the outstretched back hand; the front hand is on the string.
                drawPos = GetUnscaledBowGripPosition() - Main.screenPosition;
            }
            if (heldCrossbowLike)
            {
                Item heldItem = GetCachedWeaponItem(_heldItemType);
                Vector2? holdoutOffset = heldItem.ModItem?.HoldoutOffset();
                if (holdoutOffset.HasValue)
                {
                    drawPos += new Vector2(holdoutOffset.Value.X * NPC.direction, holdoutOffset.Value.Y) * NPC.scale;
                }
            }
            if (heldRangedLike)
            {
                Vector2 rangedOffset = GetHeldRangedDrawOffset(_heldItemType);
                if (rangedOffset != Vector2.Zero)
                {
                    drawPos += new Vector2(rangedOffset.X * NPC.direction, rangedOffset.Y) * NPC.scale;
                }
            }

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
            // Only the bow draw pose crops the sprite (to remove its baked-in string); null = whole texture.
            Rectangle? sourceRect = null;
            Rectangle bowStringTexels = Rectangle.Empty;
            if (bowDrawPose)
            {
                bowStringTexels = GetBowStringTexels(_heldItemType);
            }
            if (heldCrossbowLike)
            {
                float hx = flipHeldRangedHorizontally ? tex.Width * 0.78f : tex.Width * 0.22f;
                origin = new Vector2(hx, tex.Height * 0.58f);
            }
            else if (heldBowLike)
            {
                // Bow is held at its authored grip. Mirror the origin in texture space when the
                // sprite flips so the same physical grip pixel remains pinned to the hand.
                Vector2 gripNorm = GetHeldRangedGripNorm(_heldItemType);
                float hx = flipHeldRangedHorizontally
                    ? tex.Width * (1f - gripNorm.X)
                    : tex.Width * gripNorm.X;
                origin = new Vector2(hx, tex.Height * gripNorm.Y);

                if (bowStringTexels != Rectangle.Empty)
                {
                    // Crop off the string columns (left edge) — live lines to the hand replace them.
                    // Origin is measured inside the source rect, so the unflipped case shifts left by
                    // the cropped width; the flipped case (width − gripX) already comes out the same.
                    int stringWidth = bowStringTexels.Right;
                    sourceRect = new Rectangle(stringWidth, 0, tex.Width - stringWidth, tex.Height);

                    if (!flipHeldRangedHorizontally)
                    {
                        origin.X -= stringWidth;
                    }
                }
            }
            else if (heldRangedLike)
            {
                if (_heldItemType == MagicWeaponItemType)
                {
                    // Staves grip lower on the shaft (not centred) so the hand holds near the base.
                    Vector2 magicGrip = MagicGripNorm;
                    float mgx = flipHeldRangedHorizontally
                        ? tex.Width * (1f - magicGrip.X)
                        : tex.Width * magicGrip.X;
                    origin = new Vector2(mgx, tex.Height * magicGrip.Y);
                }
                else
                {
                    origin = tex.Bounds.Center.ToVector2(); // symmetric throwing items - keep centred
                }
            }
            else if (holdingSpearNow)
            {
                // Grip slides along the shaft (head→base) so the spear extends/retracts.
                Vector2 gripNorm = Vector2.Lerp(SpearHeadNorm, SpearBaseNorm, _spearGrip);
                float gx = NPC.direction == 1 ? tex.Width * gripNorm.X : tex.Width * (1f - gripNorm.X);
                origin = new Vector2(gx, tex.Height * gripNorm.Y);
            }
            else
            {
                // Grip point travels with the weapon — a swapped mace grips nothing like a sword.
                Vector2 handleNorm = FrontHandWeapon.HandleNorm;
                float   handleX = NPC.direction == 1
                    ? tex.Width * handleNorm.X
                    : tex.Width * (1f - handleNorm.X);
                origin = new Vector2(handleX, tex.Height * handleNorm.Y);
            }

            SpriteEffects spriteFx = (heldRangedLike
                    ? flipHeldRangedHorizontally
                    : NPC.direction == -1)
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            // Mirror single-bladed melee weapons (axes) for motions that swing opposite the
            // OverheadArc convention (e.g. UnderhandArc) so the blade edge leads instead of trails.
            // See BladeFlipActive / GetWeaponWorldDirection for the matching hit-detection math.
            if (!heldRangedLike && !holdingSpearNow && BladeFlipActive)
            {
                spriteFx |= SpriteEffects.FlipVertically;

                // origin.X above is already mirrored for FlipHorizontally because SpriteBatch always
                // measures origin in PRE-flip texture space — the same rule applies to Y here. Without
                // this, FlipVertically mirrors the sprite but the pivot stays pinned to the unflipped
                // handle position, which after the flip is actually the BLADE end: the axe visibly
                // pivots/holds from the wrong end during UnderhandArc (e.g. Up-Down Reversal's second
                // step). Mirroring origin.Y keeps the handle pixel at the hand for both flip states,
                // same fix as the FlipHorizontally case just on the other axis.
                origin.Y = tex.Height - origin.Y;
            }

            // Per-weapon angular correction for melee sprites whose blade/head diagonal doesn't
            // match the broadsword convention (handle lower-left → blade upper-right).  Applied to
            // the drawn sprite only — the arm pose and swing arc still run off raw _weaponRotation.
            //
            // heldBowLike gets the same *NPC.direction mirror GetMeleeDrawRotation() applies for
            // melee: SpriteBatch's rotation parameter is a plain angle that does NOT auto-flip with
            // SpriteEffects.FlipHorizontally, so a bow's un-mirrored _weaponRotation was tilting the
            // same physical direction whether the sprite itself was flipped or not, reading as a
            // small held-close/twisted pose on the flipped (left-facing) side only.
            float drawRotation = heldBowLike ? _weaponRotation * NPC.direction : _weaponRotation;
            if (bowDrawPose)
            {
                // Archer pose: the bow also tilts with the ±45° aim, matching the bow arm.
                drawRotation = (_weaponRotation + BowAimAngle) * NPC.direction;
            }
            // Spears use their own SpearDrawRotationOffset to correct for the sprite's natural
            // orientation — MeleeWeaponRotationOffset is a sword-only fine-tune and would double
            // up with (and fight) that correction, so it's excluded here.
            if (!heldRangedLike && !holdingSpearNow)
                drawRotation = GetMeleeDrawRotation();
            if (_heldItemType == MagicWeaponItemType)
                drawRotation = (_weaponRotation + MagicWeaponRotationOffset)
                    * (MirrorMagicWeaponRotationByFacing ? NPC.direction : 1);
            if (holdingSpearNow)
                drawRotation += SpearDrawRotationOffset; // draw-only correction, direction-neutral (FlipH handles facing)

            drawInfo.DrawDataCache.Add(new DrawData(
                tex,
                drawPos,
                sourceRect,
                _layerDrawColor,
                drawRotation,
                origin,
                NPC.scale * scale,
                spriteFx,
                0));

            if (bowStringTexels != Rectangle.Empty)
            {
                // Live bowstring: two lines from the limb tips to the string point. Tips are texel
                // offsets from the grip, mirrored for a flipped sprite and turned with the bow.
                float texelScale = NPC.scale * scale;
                Vector2 gripTexel = new Vector2(tex.Width, tex.Height) * GetHeldRangedGripNorm(_heldItemType);
                float stringTexelX = bowStringTexels.X + bowStringTexels.Width * 0.5f;
                Vector2 topOffset = (new Vector2(stringTexelX, bowStringTexels.Top) - gripTexel) * texelScale;
                Vector2 bottomOffset = (new Vector2(stringTexelX, bowStringTexels.Bottom) - gripTexel) * texelScale;

                if (flipHeldRangedHorizontally)
                {
                    topOffset.X = -topOffset.X;
                    bottomOffset.X = -bottomOffset.X;
                }

                Vector2 topTip = drawPos + topOffset.RotatedBy(drawRotation);
                Vector2 bottomTip = drawPos + bottomOffset.RotatedBy(drawRotation);
                Vector2 restPoint = (topTip + bottomTip) * 0.5f;
                Vector2 stringPoint = restPoint;

                if (_bowStringHeld)
                {
                    stringPoint = _puppet.GetFrontHandPosition(_bowStringHandStretch, _bowStringHandRotation)
                        - Main.screenPosition;

                    // Never bow the string outward past rest — early in the pull the hand is still a
                    // few px in front of it.
                    float handAheadOfRest = (stringPoint.X - restPoint.X) * NPC.direction;
                    if (handAheadOfRest > 0f)
                    {
                        stringPoint.X = restPoint.X;
                    }
                }

                // Colour sampled from the sprite's own string (46, 26, 17), lit like the bow.
                Color stringColor = new Color(46, 26, 17).MultiplyRGBA(_layerDrawColor);
                float stringThickness = bowStringTexels.Width * texelScale;
                Texture2D pixel = TextureAssets.MagicPixel.Value;

                foreach (Vector2 tip in new[] { topTip, bottomTip })
                {
                    Vector2 span = stringPoint - tip;
                    drawInfo.DrawDataCache.Add(new DrawData(
                        pixel,
                        tip,
                        new Rectangle(0, 0, 1, 1),
                        stringColor,
                        span.ToRotation(),
                        new Vector2(0f, 0.5f),
                        new Vector2(span.Length(), stringThickness),
                        SpriteEffects.None,
                        0));
                }
            }

            // ── Debug snapshot + log (DebugMode only) ───────────────────────────────
            DebugWeaponRotationDeg = MathHelper.ToDegrees(_weaponRotation);
            DebugDrawRotationDeg   = MathHelper.ToDegrees(drawRotation);
            DebugHoldingSpearNow   = holdingSpearNow;
            DebugHeldRangedLike    = heldRangedLike;
            DebugHeldItemType      = _heldItemType;
            DebugSpearGrip         = _spearGrip;
            DebugHandPos           = drawPos + Main.screenPosition;
            DebugOrigin            = origin;
            DebugDirection         = NPC.direction;

            if (SwingDebugLog && PuppetAttackTelemetry.IsActive(NPC.whoAmI))
            {
                Vector2 handWorld = GetHandPosition();
                Vector2 weaponDirection = GetWeaponWorldDirection();
                float drawScale = NPC.scale * scale * (HasSpectralOverlay ? SpectralOverlayScale : 1f);
                float visualReach = MaxCornerDistance(origin, tex.Width, tex.Height) * drawScale;
                Vector2 visualTip = handWorld + weaponDirection * visualReach;
                float collisionReach = _bladeArmed ? _activeBladeReach : 0f;
                Vector2 collisionTip = handWorld + weaponDirection * collisionReach;
                float armWorldDeg = float.NaN;
                float armWeaponErrorDeg = 0f;
                if (CompositeArmActive)
                {
                    float armPoseRotation = _weaponRotation + CompositeArmRotationOffset + MeleeCompositeArmRotationOffset;
                    float armWorldRad = NPC.direction == 1
                        ? armPoseRotation
                        : MathHelper.Pi - armPoseRotation;
                    armWorldDeg = MathHelper.ToDegrees(armWorldRad);
                    float weaponWorldRad = (float)Math.Atan2(weaponDirection.Y, weaponDirection.X);
                    float armWeaponSeparationDeg = MathHelper.ToDegrees(Math.Abs(
                        MathHelper.WrapAngle(weaponWorldRad - armWorldRad)));
                    // The expected grip separation includes both Terraria's diagonal item texture
                    // convention and this weapon's calibrated draw/arm offsets. Compare against
                    // that pose-specific baseline so a correct non-zero wrist angle is not flagged.
                    float naturalItemAngle = BladeFlipActive && !PreserveShaftDirectionOnBladeFlip
                        ? MathHelper.PiOver4 : -MathHelper.PiOver4;
                    float expectedSeparationDeg = MathHelper.ToDegrees(Math.Abs(MathHelper.WrapAngle(
                        naturalItemAngle + MeleeWeaponRotationOffset - CompositeArmRotationOffset
                        - MeleeCompositeArmRotationOffset)));
                    armWeaponErrorDeg = Math.Abs(armWeaponSeparationDeg - expectedSeparationDeg);
                }

                PuppetAttackTelemetry.RecordRender(new PuppetAttackRenderSample
                {
                    NpcId = NPC.whoAmI,
                    Tick = (long)Main.GameUpdateCount,
                    Phase = Phase.ToString(),
                    PhaseTimer = PhaseTimer,
                    Motion = TelemetryMotionName,
                    Direction = NPC.direction,
                    SpriteDirection = NPC.spriteDirection,
                    LockedDirection = _comboLockedDir,
                    NpcCenter = NPC.Center,
                    HandWorld = handWorld,
                    VisualTipWorld = visualTip,
                    CollisionTipWorld = collisionTip,
                    WeaponDirection = weaponDirection,
                    DrawOrigin = origin,
                    TextureWidth = tex.Width,
                    TextureHeight = tex.Height,
                    DrawScale = drawScale,
                    VisualReach = visualReach,
                    CollisionReach = collisionReach,
                    RawWeaponRotationDeg = MathHelper.ToDegrees(_weaponRotation),
                    DrawWeaponRotationDeg = MathHelper.ToDegrees(drawRotation),
                    CompositeArmRotationDeg = CompositeArmActive
                        ? MathHelper.ToDegrees(CompositeArmRotation)
                        : float.NaN,
                    BackCompositeArmRotationDeg = TwoHandedCompositeArmActive
                        ? MathHelper.ToDegrees(_twoHandedBackRotation)
                        : float.NaN,
                    ArmWorldRotationDeg = armWorldDeg,
                    ArmWeaponErrorDeg = armWeaponErrorDeg,
                    CompositeArmActive = CompositeArmActive,
                    CompositeStretch = CompositeArmStretch.ToString(),
                    BackCompositeArmActive = TwoHandedCompositeArmActive,
                    BackCompositeStretch = _twoHandedBackStretch.ToString(),
                    BackHandWorld = _twoHandedBackHandWorld,
                    BackGripTargetWorld = _twoHandedBackGripTargetWorld,
                    BackGripError = _twoHandedBackGripError,
                    BladeArmed = _bladeArmed,
                    SpriteEffects = spriteFx.ToString(),
                    BladeFlipActive = BladeFlipActive,
                });
            }

            if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
                LogWeaponDebug();

            // Lit-fuse: red sparks off the top of an in-hand bomb-like ranged item.
            if (heldRangedLike && HeldRangedFuseSparks(_heldItemType) && !Main.dedServ && Main.rand.NextBool(3))
            {
                Vector2 topWorld = GetHandPosition() - new Vector2(0f, tex.Height * NPC.scale * scale * 0.5f);
                Dust spark = Dust.NewDustPerfect(topWorld, DustID.RedTorch,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1.2f, -0.3f)),
                    0, default, Main.rand.NextFloat(0.6f, 1.1f));
                spark.noGravity = true;
            }
        }

        private static float MaxCornerDistance(Vector2 origin, int width, int height)
        {
            float d0 = Vector2.Distance(origin, Vector2.Zero);
            float d1 = Vector2.Distance(origin, new Vector2(width, 0f));
            float d2 = Vector2.Distance(origin, new Vector2(0f, height));
            float d3 = Vector2.Distance(origin, new Vector2(width, height));
            return Math.Max(Math.Max(d0, d1), Math.Max(d2, d3));
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
            int frame = 2;

            if (_estusCharges >= (int)(EstusChargesMax * 0.6f))
            {
                frame = 0;
            }
            else if (_estusCharges >= (int)(EstusChargesMax * 0.3f))
            {
                frame = 1;
            }

            Rectangle src = new Rectangle(0, frame * frameHeight, tex.Width, frameHeight);

            // Anchor to the same unscaled animated hand as every other held item. The completed
            // draw cache is scaled around the feet later for spectral puppets, so starting from
            // NPC.Center here would apply a resized-hitbox offset before that visual transform.
            Vector2 handWorld = GetUnscaledHandPosition()
                + new Vector2(6f * NPC.direction, -6f); // small nudge toward the mouth
            Vector2 drawPos   = handWorld - Main.screenPosition;
            Vector2 origin    = new Vector2(src.Width / 2f, src.Height / 2f);

            // Rotate the flask so it looks like it's tipped toward the mouth.
            // -π/2 (= −90°) points the sprite's "top" to the right for a right-facing puppet.
            float rotation = MathHelper.PiOver2 * -NPC.direction;

            SpriteEffects spriteFx = NPC.direction == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            drawInfo.DrawDataCache.Add(new DrawData(tex, drawPos, src,
                _layerDrawColor * 0.85f, rotation, origin, 0.75f, spriteFx, 0));
        }

        /// <summary>
        /// Normalised (0–1) handle position within the melee-weapon texture.
        /// Default (0.10, 0.85) anchors near the lower-left corner — correct for most
        /// Terraria broadsword / katana sprites whose grip occupies that region.
        /// Override in a subclass to fine-tune the grip point for a specific weapon.
        /// </summary>
        protected virtual Vector2 MeleeHandleNorm => new Vector2(0.10f, 0.85f);

        // ── Per-weapon melee draw tuning ────────────────────────────────────────────
        /// <summary>Draw scale for the melee weapon sprite.  Default 1f (full size); override per
        /// puppet if a particular sprite should read larger or smaller.</summary>
        protected virtual float MeleeWeaponDrawScale => 1f;

        // ── Spear-style draw (grip slides along the shaft = extend/retract like the player's spear) ──
        /// <summary>When true, the melee weapon is drawn as a SPEAR: the grip point slides along the shaft
        /// (<see cref="_spearGrip"/>) so pokes thrust the head forward and retract, and swings/spins hold it
        /// out for reach — instead of the static handle-anchored sword draw.</summary>
        protected virtual bool DrawWeaponAsSpear => false;
        /// <summary>Normalized texture coords of the spear's HEAD tip (the pointy end).  Tune to the sprite.</summary>
        protected virtual Vector2 SpearHeadNorm => new Vector2(0.82f, 0.18f);
        /// <summary>Normalized texture coords of the spear's BASE (butt of the shaft).  Tune to the sprite.</summary>
        protected virtual Vector2 SpearBaseNorm => new Vector2(0.12f, 0.88f);
        /// <summary>When <see cref="DrawWeaponAsSpear"/> is true, use this texture instead of the item icon.
        /// The item sprite is a small inventory icon; the spear's holdout-projectile sprite is the full
        /// shaft-and-head weapon the player sees.  Override to point at the projectile texture path.
        /// Null (default) falls back to the item sprite.</summary>
        protected virtual string SpearDrawTexturePath => null;
        /// <summary>Same idea as <see cref="SpearDrawTexturePath"/> but for the normal (non-spear) melee
        /// draw branch — a swing that should show the full weapon art instead of the small item icon.
        /// Null (default) falls back to the item sprite.</summary>
        protected virtual string MeleeDrawTexturePath => null;
        /// <summary>Extra draw-only rotation applied after <see cref="MeleeWeaponRotationOffset"/> when drawing the spear.
        /// Use this to correct a sprite whose tip isn't in the standard orientation without affecting the arm-pose rows
        /// (which read <see cref="MeleeWeaponRotationOffset"/> via <see cref="BodyRowFromWeaponRotation"/>).
        /// Direction-neutral — SpriteBatch FlipHorizontally already handles left/right facing.</summary>
        protected virtual float SpearDrawRotationOffset => 0f;
        /// <summary>When true, a spear's thrust extend/retract (<see cref="_spearGrip"/>) is timed off
        /// the SAME 0→1 swing clock that drives <c>_weaponRotation</c> for the step (see
        /// <see cref="UpdateSpearGrip"/>), instead of its own separate PhaseTimer-based hump. Default
        /// false preserves every existing <see cref="DrawWeaponAsSpear"/> puppet's grip timing exactly.
        /// Needed for a puppet whose thrust steps run on <see cref="AuthoredClockCoversJoustDash"/>,
        /// so the visible extension and the eased rotation settle together instead of drifting apart.</summary>
        protected virtual bool UseAuthoredSpearGrip => false;

        /// <summary>Normalized grip point for a held MAGIC staff (where the hand holds it).  Default centred;
        /// override lower (larger Y) so a tall staff is gripped near its base.</summary>
        protected virtual Vector2 MagicGripNorm => new Vector2(0.5f, 0.5f);

        /// <summary>When true, the held melee item icon is NOT drawn in the hand — for weapons whose
        /// visual is a separate projectile (e.g. a flail's ball + chain).  Default false.</summary>
        protected virtual bool HideHeldMeleeSprite => false;

        /// <summary>Draw scale for a held ranged/thrown/magic item.  Default 1f (full size); override
        /// per item type for sprites that should read smaller (e.g. tiny throwing stars).</summary>
        /// <summary>Optional custom sprite for a ranged item held by the puppet. The gameplay item
        /// remains unchanged; this only replaces its inventory icon in the puppet draw layer.</summary>
        protected virtual string GetHeldRangedDrawTexturePath(int itemType) => null;
        protected virtual float GetHeldRangedDrawScale(int itemType) => 1f;
        /// <summary>Normalized grip pixel for a held bow sprite before horizontal mirroring.</summary>
        protected virtual Vector2 GetHeldRangedGripNorm(int itemType) => new Vector2(0.25f, 0.5f);
        /// <summary>Extra world-space nudge for a held ranged sprite, same idea as the crossbow's
        /// item-level HoldoutOffset but puppet-controlled. X is mirrored by facing (positive = away
        /// from the body); Y is not. Default none.</summary>
        protected virtual Vector2 GetHeldRangedDrawOffset(int itemType) => Vector2.Zero;
        /// <summary>Whether this ranged sprite needs a horizontal flip for its current facing.
        /// Override for custom art authored facing left instead of the usual right.</summary>
        protected virtual bool ShouldFlipHeldRangedSpriteHorizontally(int itemType) => NPC.direction == -1;

        /// <summary>When true for the given held ranged item type, red "lit fuse" sparks are emitted
        /// off the top of it while it's in hand (e.g. a smoke bomb).  Default false.</summary>
        protected virtual bool HeldRangedFuseSparks(int itemType) => false;

        /// <summary>Per-weapon angular correction (radians) added to the drawn melee sprite ONLY
        /// (it does not change the arm pose or the swing arc).  Use it when a weapon texture's
        /// diagonal doesn't follow the broadsword convention this system assumes
        /// (handle lower-left → blade upper-right) — e.g. an axe whose head sits on the
        /// opposite diagonal and would otherwise appear to swing backwards.</summary>
        protected virtual float MeleeWeaponRotationOffset => 0f;

        /// <summary>Opt-in for continuously driven melee weapons whose raw swing arc must mirror with
        /// facing, just like the composite arm does. Kept off by default while legacy puppets retain
        /// their established draw tuning.</summary>
        protected virtual bool MirrorMeleeSwingRotationByFacing => false;

        /// <summary>Only read when <see cref="MirrorMeleeSwingRotationByFacing"/> is true. The blade's
        /// rest angle (degrees) at _weaponRotation=0, dir=1, no flip — 45° is the standard broadsword
        /// convention (handle lower-left, blade upper-right) every current adopter of the mirrored path
        /// actually uses. A weapon sprite calibrated to a DIFFERENT convention (e.g. a glaive drawn along
        /// the anti-diagonal) needs this overridden, or GetWeaponWorldDirection() — which both hit
        /// detection and any cosmetic effect anchored to the blade tip rely on — silently points the
        /// "logical" blade somewhere other than where the sprite is actually drawn.</summary>
        protected virtual float MeleeNaturalRestAngleDeg => 45f;

        // Offset comes off the equipped weapon, not the puppet, so swapping to a sprite on the
        // opposite diagonal (a mace head vs a sword tip) corrects itself.
        private float GetMeleeDrawRotation() => GetMeleeDrawRotation(FrontHandPoseRotation, FrontHandWeapon);

        // Per-hand form: the back hand passes its own pose and weapon.
        private float GetMeleeDrawRotation(float handRotation, PuppetWeapon weapon)
        {
            if (!MirrorMeleeSwingRotationByFacing)
            {
                return handRotation + weapon.RotationOffset * NPC.direction;
            }

            float shaftCorrection = 0f;
            if (BladeFlipActiveFor(weapon) && PreserveShaftDirectionOnBladeFlip)
            {
                shaftCorrection = MathHelper.ToRadians(2f * MeleeNaturalRestAngleDeg);
            }
            return (handRotation + weapon.RotationOffset - shaftCorrection) * NPC.direction;
        }

        /// <summary>Reflect the cutting edge across the diagonal shaft without turning that shaft.</summary>
        protected virtual bool PreserveShaftDirectionOnBladeFlip => false;

        // ── Blade-leads-the-swing flip ──────────────────────────────────────────────
        /// <summary>True for weapons whose head reads asymmetrically (a single cutting edge, e.g.
        /// an axe) so it matters which way the sprite faces mid-swing. The fixed rigid rotation this
        /// system draws with is calibrated for downward/forward arcs (<see cref="ComboMotion.OverheadArc"/>);
        /// motions that swing the other way (<see cref="ComboMotion.UnderhandArc"/>) put the blade
        /// trailing instead of leading unless the sprite is mirrored for those frames (see
        /// <see cref="BladeFlipActive"/>). Defaults to on for the whole Axe archetype — a
        /// double-bladed/symmetric axe wouldn't be harmed by the mirror either, so there's no need
        /// to special-case per weapon. Override false for a weapon where the mirror looks wrong.</summary>
        protected virtual bool MeleeWeaponIsSingleBladed => MeleeArchetype == WeaponArchetype.Axe;

        /// <summary>Runtime master kill-switch for the blade-flip mirror. Toggle in-game with
        /// <c>/swingarm flip</c> for instant A/B without a rebuild.</summary>
        internal static bool BladeFlipMasterEnable = true;

        /// <summary>Which combo motions need the mirror to keep the blade leading the swing instead
        /// of trailing. Only <see cref="ComboMotion.UnderhandArc"/> reverses direction relative to
        /// the OverheadArc convention this system is calibrated for; extend here if other motions
        /// turn out to need it too.</summary>
        private static bool BladeFlipsForMotion(ComboMotion motion) => motion == ComboMotion.UnderhandArc;

        /// <summary>True when the current combo step's motion should mirror the weapon sprite this
        /// frame. Only meaningful during combo phases — the plain one-shot MeleeAttack/MeleeTelegraph
        /// path (outside the combo system) always uses OverheadArc's shape, so it never flips.</summary>
        private bool BladeFlipActive => BladeFlipActiveFor(FrontHandWeapon);

        private bool BladeFlipActiveFor(PuppetWeapon weapon)
        {
            // Asymmetry is a property of the weapon in hand, so a swap to a symmetric one stops flipping.
            if (!weapon.SingleBladed || !BladeFlipMasterEnable)
            {
                return false;
            }
            if (!IsMeleeComboPhase || _activeMeleeComboIndex < 0 || _activeMeleeCombo.Steps == null
                || _meleeComboStepIndex < 0 || _meleeComboStepIndex >= _activeMeleeCombo.Steps.Length)
            {
                return false;
            }
            return BladeFlipsForMotion(_activeMeleeCombo.Steps[_meleeComboStepIndex].Motion);
        }

        // ── Aim-centered swing (full 360° player-style aim) ─────────────────────────
        /// <summary>When true (and <see cref="AimSwingMasterEnable"/> is on), this puppet's swing
        /// arc reorients toward the actual direction to the player — up, level, or down — exactly
        /// like the BroadswordRework player swing centers its arc on the cursor. Turns on the aim
        /// bias + easing for the swing motions without needing each motion's endpoints re-tuned by
        /// hand. Defaults on for the whole Axe archetype (the first pilot); the arc endpoints are
        /// still the same OverheadArc/UnderhandArc shapes, just rotated by the aim pitch.</summary>
        protected virtual bool UseAimCenteredSwing => MeleeArchetype == WeaponArchetype.Axe;

        /// <summary>Runtime master kill-switch for the aim-centered swing. Toggle in-game with
        /// <c>/swingarm aim</c> for instant A/B — off reverts the axe to its fixed legacy arc.</summary>
        internal static bool AimSwingMasterEnable = true;

        /// <summary>Resolved per-frame: aim-centered swing is live for this puppet right now.</summary>
        private bool AimSwingActive => UseAimCenteredSwing && AimSwingMasterEnable;

        /// <summary>Uses each combo step's authored attack duration as the complete 0-to-1 swing
        /// clock. Aim-centered puppets need this automatically; other puppets can opt in without
        /// inheriting target-pitch steering.</summary>
        protected virtual bool UseAuthoredComboSwingClock => AimSwingActive;

        /// <summary>Clamp on the aim pitch. The base overhead arc already starts near the raised
        /// limit (-1.3 rad ≈ -74°), and the composite arm inverts past vertical, so a big upward aim
        /// bias drove the arm to a broken -112° pose. Kept moderate (~34°) so the arc still visibly
        /// reorients toward the player without over-rotating the arm. (The 4-frame arm path tolerates
        /// far more, since it snaps extremes to Use1 — a fuller aim range is safe once composite is off.)</summary>
        private const float MaxAimPitch = 0.6f;

        /// <summary>When true, <see cref="ComboMotion.JoustDash"/> also gets the authored 0→1 swing
        /// clock (<c>AttackTicks / SwingSpeedMult</c>) instead of sweeping over the held weapon's
        /// <c>useAnimation</c>. Default false preserves every existing JoustDash tuning exactly (Gwyn's
        /// Wrath Flurry, Artorias, Dread Wraith's Rotted Fork charge). A puppet whose combo AttackTicks
        /// is SHORTER than its weapon's useAnimation — a fast thrust authored tighter than the item's
        /// swing time — should opt in, or the step is cut off mid-arc (see puppet-swing-tuning's
        /// "reaches t=X before step ends" trap). Deliberately excludes LeapThrust: that motion's
        /// AttackTicks is usually much LONGER than useAnimation on purpose (extend quickly, then hold
        /// the pose through the rest of the airborne travel) — resizing its clock to the full
        /// AttackTicks stretches the extension across the whole leap instead, reading as far slower.</summary>
        protected virtual bool AuthoredClockCoversJoustDash => false;

        /// <summary>Motions whose visual is a continuous a0→a1 sweep driven by <see cref="SwingEase"/>
        /// over the swing window (so per-step swing-speed / easing applies). Hold/charge/leap motions
        /// are excluded — their pose is a fixed lerp, not a timed sweep.</summary>
        private bool IsArcSwingMotion(ComboMotion m) =>
            m == ComboMotion.OverheadArc || m == ComboMotion.UnderhandArc ||
            m == ComboMotion.HorizontalSweep || m == ComboMotion.VerticalChop ||
            m == ComboMotion.GroundSlam || m == ComboMotion.IaidoDraw ||
            m == ComboMotion.DoubleSpinSlam ||
            (AuthoredClockCoversJoustDash && m == ComboMotion.JoustDash);

        /// <summary>Enters a combo step's attack: for aim-swing pilots, arc motions play over
        /// <c>AttackTicks / SwingSpeedMult</c> (decoupled from the weapon's useAnimation, so swings
        /// can be faster and vary per step) with the swing counter reset to fill exactly that window.
        /// Returns the attack-phase length. Non-pilots / non-arc motions keep the old
        /// <see cref="GetMeleeSwingTicks"/> behavior.</summary>
        private int BeginComboAttackTicks(MeleeComboStep step)
        {
            int ticks = GetMeleeSwingTicks(step.AttackTicks);
            if (UseAuthoredComboSwingClock && IsArcSwingMotion(step.Motion))
            {
                float mult = step.SwingSpeedMult > 0f ? step.SwingSpeedMult : 1f;
                ticks = Math.Max(6, (int)Math.Round(step.AttackTicks / mult));
                _weaponAnimMax = ticks;
            }
            _weaponAnim = _weaponAnimMax; // restart the sweep so t runs 0→1 across this step
            return ticks;
        }

        /// <summary>Eases the arc through the step's chosen <see cref="SwingEaseStyle"/> when
        /// <see cref="UseAuthoredComboSwingClock"/> is on, else the legacy on/off easing. Every arc
        /// motion plus JoustDash/LeapSlam/LeapThrust swings through here. Trapezoidal is a special case:
        /// it needs the step's raw tick budget (its accel/decel/hold phases are absolute tick counts,
        /// not fractions of the swing), so it's honored regardless of UseAuthoredComboSwingClock.</summary>
        private float ApplySwingEase(float a0, float a1, float t, MeleeComboStep step)
        {
            if (step.Ease == SwingEaseStyle.Trapezoidal)
            {
                int totalTicks = Math.Max(1, step.AttackTicks);
                int elapsedTicks = (int)Math.Round(t * totalTicks);
                return SwingEase.ApplyTrapezoidal(a0, a1, elapsedTicks, totalTicks);
            }

            // Weighted is authored in ticks, so like Trapezoidal it maps t back onto AttackTicks. It
            // still needs the authored clock: without it t runs over the weapon's useAnimation.
            if (step.Ease == SwingEaseStyle.Weighted && UseAuthoredComboSwingClock)
            {
                int totalTicks = Math.Max(1, step.AttackTicks);
                return SwingEase.ApplyWeighted(a0, a1, t * totalTicks, totalTicks,
                    step.EaseInTicks, step.EaseOutTicks, step.EaseOutDecay);
            }

            return UseAuthoredComboSwingClock ? SwingEase.Apply(a0, a1, t, step.Ease)
                              : SwingEase.Apply(a0, a1, t, UseSwingEasing);
        }

        /// <summary>Blade angle for a bespoke (non-combo) swing phase <paramref name="phaseTicks"/> long. An authored
        /// Weighted <paramref name="curve"/> plays on real elapsed ticks; unset falls back to the phase's legacy
        /// ease over the same 0..1 progress it always used, so puppets that author nothing are unchanged.</summary>
        private float BespokeSwingRotation(float start, float end, int phaseTicks, WeightedSwing curve, SwingEaseStyle legacyEase)
        {
            if (curve.IsSet)
            {
                int elapsedTicks = phaseTicks - PhaseTimer;
                return curve.Apply(start, end, elapsedTicks);
            }

            float progress = 1f;
            if (phaseTicks > 0)
            {
                progress = 1f - (float)PhaseTimer / phaseTicks;
            }

            return SwingEase.Apply(start, end, progress, legacyEase);
        }

        /// <summary>The start angle a step's arc begins from, with the same flip / aim-bias transforms
        /// the live swing applies — so an inter-step pause can ease toward where the NEXT step actually
        /// starts (a continuous handoff) instead of re-raising to the outgoing step's apex and snapping.</summary>
        private float ComboStepStartRotation(MeleeComboStep step)
        {
            if (UseLandingTimedLeapSlam && step.Motion == ComboMotion.LeapSlam)
                return LeapSlamCarryRotation;
            (float a0, float a1) = step.Motion switch
            {
                ComboMotion.OverheadArc     => (-1.3f - OverheadWindupOvershoot, 1.0f),
                ComboMotion.UnderhandArc    => (1.0f, -1.0f),
                ComboMotion.HorizontalSweep => (-0.4f, 0.6f),
                ComboMotion.VerticalChop    => (-1.55f - OverheadWindupOvershoot, 1.4f),
                ComboMotion.GroundSlam      => (-1.55f - OverheadWindupOvershoot, 1.5f),
                ComboMotion.IaidoDraw       => (1.2f, -0.5f),
                ComboMotion.Feint           => (-1.3f, -1.3f),
                ComboMotion.ChargeChop      => (-0.95f, -0.95f),
                ComboMotion.DoubleSpinSlam  => (-1.3f - OverheadWindupOvershoot, 1.4f),
                ComboMotion.ThrownWeaponRetrieve => (HoldRotation, HoldRotation),
                ComboMotion.LeapSlam        => (-1.45f - OverheadWindupOvershoot, 1.4f),
                ComboMotion.LeapThrust      => (MathHelper.PiOver2 * 0.8f, MathHelper.PiOver4),
                // Missing until Cursed Dragon's Skewer String became the first combo to chain two
                // JoustDash steps: without an entry here, the fallback (HoldRotation) sent the
                // inter-step pause easing toward the wrong angle, then the next thrust's real start
                // (PiOver2) opened with a ~107 degree snap. Matches WeaponArchetypeTables.SwingArcEndpoints.
                ComboMotion.JoustDash       => (MathHelper.PiOver2, MathHelper.PiOver4),
                ComboMotion.LowAxeRun       => (1.9f, 1.9f),
                ComboMotion.RisingUppercutLeap => (1.9f, -1.0f),
                ComboMotion.BackstepRaise   => (1.0f, -1.3f),
                ComboMotion.ApexDiveCleave  => (-1.3f, 1.25f),
                _                           => (HoldRotation, HoldRotation),
            };
            ModifyMeleeArcEndpoints(step.Motion, ref a0, ref a1);
            if (UseAlternateFlip && _comboSwingFlipped)
            {
                (a0, a1) = (a1, a0);
            }
            bool fixedCarryMotion = step.Motion == ComboMotion.LowAxeRun
                || step.Motion == ComboMotion.RisingUppercutLeap;
            if (!fixedCarryMotion && (UseAimAdaptiveArc || AimSwingActive))
            {
                a0 += _comboAimBias;
                a1 += _comboAimBias;
            }
            return a0;
        }

        /// <summary>
        /// True while a recovery phase should keep the weapon parked at the pose the swing finished
        /// on, before the idle handler eases it back to the carried hold. Covers the bespoke sword
        /// recoveries as well as the shared melee/combo ones: those all used to drop straight into
        /// the 0.10-per-tick ease toward HoldRotation on the recovery's very first frame, which cut
        /// the follow-through off and made a greatsword read as weightless. Opt-in per puppet via
        /// <see cref="MeleeRecoveryLingerTicks"/> (0 disables, which is the default).
        /// </summary>
        private bool IsHoldingMeleeRecoveryFollowThrough()
        {
            if (MeleeRecoveryLingerTicks <= 0)
            {
                return false;
            }

            int recoveryTicks;

            switch (Phase)
            {
                case AttackPhase.MeleeComboRecovery:
                    recoveryTicks = ActiveComboRecoveryTicks;
                    break;
                case AttackPhase.JumpSlashRecovery:
                    recoveryTicks = JumpSlashRecoveryTicks;
                    break;
                case AttackPhase.AbyssSlashRecovery:
                    recoveryTicks = AbyssSlashRecoveryTicks;
                    break;
                case AttackPhase.TendrilRecovery:
                    recoveryTicks = TendrilRecoveryTicks;
                    break;
                case AttackPhase.HomingVolleyRecovery:
                    recoveryTicks = HomingVolleyRecoveryTicks;
                    break;
                case AttackPhase.BoomerangRecovery:
                    recoveryTicks = BoomerangRecoveryTicks;
                    break;
                case AttackPhase.StabRecovery:
                    recoveryTicks = StabRecoveryTicks;
                    break;
                case AttackPhase.SpearRecovery:
                    recoveryTicks = SpearRecoveryTicks;
                    break;
                case AttackPhase.PierceRecovery:
                    recoveryTicks = PierceRecoveryTicks;
                    break;
                case AttackPhase.SpiralFanRecovery:
                    recoveryTicks = SpiralFanRecoveryTicks;
                    break;
                default:
                    recoveryTicks = MeleeRecoveryTicks;
                    break;
            }

            int lingerTicks = Math.Min(MeleeRecoveryLingerTicks, Math.Max(1, recoveryTicks));
            return PhaseTimer > Math.Max(0, recoveryTicks - lingerTicks);
        }

        // ── Composite-arm swing experiment (opt-in, single-enemy safe A/B) ──────────
        /// <summary>EXPERIMENTAL.  When true (and <see cref="CompositeArmSwingMasterEnable"/> is on),
        /// this puppet's melee swings drive the puppet's vanilla composite FRONT arm so the arm
        /// rotates continuously with the blade — a genuine player-style swing — instead of the
        /// 4-frame Use1–Use4 approximation.  Opt-in per subclass so it can be tested on one enemy
        /// before any wider rollout.  See the notes in <c>EnemySpriteRenderer</c> for why the
        /// project previously avoided composite arms (rotation-convention mismatch + unreliable
        /// <c>GetFrontHandPosition</c> on a puppet) — both are what this experiment validates.</summary>
        protected virtual bool UseCompositeArmSwing => false;

        /// <summary>Allows a subclass to keep the continuously-authored front arm active during
        /// bespoke weapon phases that are not part of the shared melee/combo phase list.</summary>
        protected virtual bool UseCompositeArmForAdditionalPhase => false;

        /// <summary>Allows a bespoke additional phase to replace the normal weapon-space rotation
        /// before it drives the composite arm. Default is a no-op.</summary>
        protected virtual void ModifyAdditionalPhaseWeaponRotation(ref float weaponRotation) { }

        /// <summary>Opt-in great-weapon pose. The front arm continues to own the weapon anchor and
        /// collision; the back arm follows the same authored swing and reaches for a second hilt
        /// point. This is intentionally independent of vanilla golf/use-style animation.</summary>
        protected virtual bool UseTwoHandedCompositeSwing => false;

        /// <summary>Distance from the authoritative front-hand anchor toward the blade/crossguard
        /// where the rear hand tries to grip. Kept small because Terraria composite arms are short.</summary>
        protected virtual float TwoHandedBackGripOffset => 4f;

        /// <summary>Maximum angular separation between the arms. The bound prevents the inverse-
        /// kinematics solution from folding through the torso when the desired grip crosses a shoulder.</summary>
        protected virtual float TwoHandedBackArmMaxSeparation => MathHelper.ToRadians(70f);

        /// <summary>Pixel-equivalent hysteresis cost for changing the rear elbow stretch frame.</summary>
        protected virtual float TwoHandedBackStretchSwitchPenalty => 2f;

        /// <summary>Opt-in archer pose for a <see cref="RangedStyle.Bow"/> weapon: the back arm holds the
        /// bow out at its grip while the front arm reaches for the string, draws it to the cheek,
        /// trembles at full draw and lets go on the shot. Needs <see cref="UseCompositeArmSwing"/> and the
        /// bow phases in <see cref="UseCompositeArmForAdditionalPhase"/>.</summary>
        protected virtual bool UseBowStringDrawPose => false;

        /// <summary>Texel rectangle of a bow sprite's string, in the unflipped (facing-right) texture.
        /// Must start at column 0. Cut out of the drawn sprite and replaced by two live lines to the
        /// string hand while <see cref="UseBowStringDrawPose"/> is on. Empty = draw the sprite as-is.</summary>
        protected virtual Rectangle GetBowStringTexels(int itemType) => Rectangle.Empty;

        /// <summary>Runtime master kill-switch for the composite-arm experiment.  Lets you flip the
        /// new arm path off globally (e.g. from a debug command) for instant A/B without a rebuild.</summary>
        internal static bool CompositeArmSwingMasterEnable = true;

        /// <summary>Tunable: radians added to <c>_weaponRotation</c> before it drives the composite arm.
        /// Static so it can be nudged live while comparing against the legacy path.</summary>
        internal static float CompositeArmRotationOffset = 0f;

        /// <summary>Local wrist calibration, added to the global debug arm adjustment.</summary>
        protected virtual float MeleeCompositeArmRotationOffset => 0f;

        /// <summary>Tunable: how far the composite front arm extends from the shoulder.</summary>
        internal static Player.CompositeArmStretchAmount CompositeArmStretch = Player.CompositeArmStretchAmount.Full;

        /// <summary>Automatic structured attack telemetry master switch. World load enables it and
        /// starts a timestamped JSONL session; <c>/swingarm log</c> remains a manual override.</summary>
        internal static bool SwingDebugLog = true;

        /// <summary>Phases whose <c>_weaponRotation</c> represents an actual swinging-arm motion
        /// (as opposed to a held-aim pose).  Only these drive the composite arm experiment.</summary>
        private bool IsMeleeSwingPosePhase => !WeaponSheathed && (
            Phase == AttackPhase.MeleeTelegraph || Phase == AttackPhase.MeleeAttack ||
            Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack ||
            Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery ||
            Phase == AttackPhase.TendrilSwingTelegraph || Phase == AttackPhase.TendrilSwing ||
            Phase == AttackPhase.BoomerangSwingTelegraph || Phase == AttackPhase.BoomerangSwing ||
            // A weapon shown during the run-in is held on the composite arm too. Otherwise the arm plays the walk
            // cycle while the sprite sits at the static 4-row hand offset, and the two visibly drift apart.
            (Phase == AttackPhase.ClosingDistance && ShowMeleeWeaponWhileClosingDistance) ||
            UseCompositeArmForAdditionalPhase);

        /// <summary>Receives the current ball position from a projectile-owned flail. Most patterns
        /// use a deliberately bounded shoulder motion. A specifically authored projectile path can
        /// instead pass its direct facing-right arm angle; this also lets full-orbit attacks retain an
        /// unwrapped 360° value without <see cref="MathHelper.WrapAngle(float)"/> reversing at the seam.</summary>
        internal void UpdateFlailProjectileArmPose(Vector2 hand, Vector2 ball,
            bool useAuthoredPose = false, float authoredArmFacingRight = 0f)
        {
            if (useAuthoredPose)
            {
                _flailArmPoseRotation = authoredArmFacingRight * NPC.direction;
                _flailArmPoseActive = true;
                _flailArmPoseTick = (long)Main.GameUpdateCount;
                return;
            }

            Vector2 local = (ball - hand) * NPC.direction;
            if (local.LengthSquared() < 1f)
                return;

            float angle = local.ToRotation();
            float desiredFacingRight = -1.23f
                + 0.72f * (float)Math.Sin(angle)
                - 0.14f * (float)Math.Cos(angle);
            float desired = desiredFacingRight * NPC.direction;

            if (!_flailArmPoseActive || _flailArmPoseTick < Main.GameUpdateCount - 1)
            {
                _flailArmPoseRotation = (FrontHandPoseRotation - MathHelper.PiOver2
                    + CompositeArmRotationOffset + MeleeCompositeArmRotationOffset) * NPC.direction;
            }

            _flailArmPoseRotation = MathHelper.WrapAngle(_flailArmPoseRotation
                + MathHelper.WrapAngle(desired - _flailArmPoseRotation) * 0.28f);
            _flailArmPoseActive = true;
            _flailArmPoseTick = (long)Main.GameUpdateCount;
        }

        private bool HasRecentFlailArmPose =>
            _flailArmPoseActive && (long)Main.GameUpdateCount - _flailArmPoseTick <= 1;

        /// <summary>True when the composite-arm swing path should be active this frame.</summary>
        private bool CompositeArmActive =>
            CompositeArmSwingMasterEnable && IsMeleeSwingPosePhase
            && (UseCompositeArmSwing || HasRecentFlailArmPose);

        private bool TwoHandedCompositeArmActive =>
            CompositeArmActive && UseTwoHandedCompositeSwing;

        /// <summary>Rotation handed to the composite arm / <c>GetFrontHandPosition</c>.  Converts our
        /// weapon-swing convention (0 = broadsword hold diagonal, −1.3 = raised overhead, +1.0 =
        /// swung down-forward) into vanilla's composite-arm space, where 0 = arm hanging straight
        /// DOWN and −π/2 = arm level FORWARD — see decompiled <c>Player.GetFrontHandPosition</c>:
        /// hand direction = (cos(rot+π/2), sin(rot+π/2)).  The two spaces differ by exactly −π/2;
        /// passing the weapon angle raw (the old behavior) trailed the arm 90° behind the swing,
        /// which read as "arm pointing backwards / hand behind the NPC".  Mirrored by facing —
        /// vanilla callers pre-negate for direction −1 the same way (e.g. useStyle 9).</summary>
        private float CompositeArmRotation => HasRecentFlailArmPose
            ? _flailArmPoseRotation
            : (FrontHandPoseRotation - MathHelper.PiOver2 + CompositeArmRotationOffset
                + MeleeCompositeArmRotationOffset) * NPC.direction;

        /// <summary>Same weapon-space → composite-arm conversion as <see cref="CompositeArmRotation"/>, for the
        /// dual-wield back arm.</summary>
        private float BackCompositeArmRotation =>
            (BackHandPoseRotation - MathHelper.PiOver2 + CompositeArmRotationOffset
                + MeleeCompositeArmRotationOffset) * NPC.direction;

        /// <summary>
        /// A small one-segment inverse-kinematics solve for the visual rear arm. Terraria exposes
        /// exact front/back hand endpoints but only four discrete elbow stretches, so each stretch
        /// is evaluated against the desired hilt point. A switch penalty avoids elbow-frame chatter,
        /// and the separation bound keeps the arm on a continuous, readable path through the swing.
        /// </summary>
        private void ResolveTwoHandedBackArmPose()
        {
            Vector2 frontHand = _puppet.GetFrontHandPosition(CompositeArmStretch, CompositeArmRotation);
            _twoHandedBackGripTargetWorld = frontHand
                + GetWeaponWorldDirection() * TwoHandedBackGripOffset;

            float maxSeparation = Math.Max(0f, TwoHandedBackArmMaxSeparation);
            float switchPenaltySq = TwoHandedBackStretchSwitchPenalty * TwoHandedBackStretchSwitchPenalty;
            float bestScore = float.MaxValue;
            Player.CompositeArmStretchAmount bestStretch = _twoHandedBackStretch;
            float bestRotation = CompositeArmRotation;
            Vector2 bestHand = frontHand;

            // Evaluate the current elbow first so exact ties preserve continuity, then cover all
            // four vanilla composite-arm stretch frames. The duplicate current entry is harmless.
            EvaluateTwoHandedBackCandidate(_twoHandedBackStretch,
                maxSeparation, switchPenaltySq, ref bestScore, ref bestStretch, ref bestRotation, ref bestHand);
            EvaluateTwoHandedBackCandidate(Player.CompositeArmStretchAmount.ThreeQuarters,
                maxSeparation, switchPenaltySq, ref bestScore, ref bestStretch, ref bestRotation, ref bestHand);
            EvaluateTwoHandedBackCandidate(Player.CompositeArmStretchAmount.Quarter,
                maxSeparation, switchPenaltySq, ref bestScore, ref bestStretch, ref bestRotation, ref bestHand);
            EvaluateTwoHandedBackCandidate(Player.CompositeArmStretchAmount.Full,
                maxSeparation, switchPenaltySq, ref bestScore, ref bestStretch, ref bestRotation, ref bestHand);
            EvaluateTwoHandedBackCandidate(Player.CompositeArmStretchAmount.None,
                maxSeparation, switchPenaltySq, ref bestScore, ref bestStretch, ref bestRotation, ref bestHand);

            _twoHandedBackStretch = bestStretch;
            _twoHandedBackRotation = bestRotation;
            _twoHandedBackHandWorld = bestHand;
            _twoHandedBackGripError = Vector2.Distance(bestHand, _twoHandedBackGripTargetWorld);
        }

        private void EvaluateTwoHandedBackCandidate(
            Player.CompositeArmStretchAmount stretch,
            float maxSeparation,
            float switchPenaltySq,
            ref float bestScore,
            ref Player.CompositeArmStretchAmount bestStretch,
            ref float bestRotation,
            ref Vector2 bestHand)
        {
            GetBackArmEllipse(stretch, out float radiusX, out float radiusY);
            Vector2 shoulder = _puppet.MountedCenter + new Vector2(6f * NPC.direction, -2f);
            Vector2 targetFromShoulder = _twoHandedBackGripTargetWorld - shoulder;
            float endpointAngle = (float)Math.Atan2(
                targetFromShoulder.Y / radiusY,
                targetFromShoulder.X / radiusX);
            float candidateRotation = endpointAngle - MathHelper.PiOver2;
            float separation = MathHelper.WrapAngle(candidateRotation - CompositeArmRotation);
            candidateRotation = CompositeArmRotation
                + MathHelper.Clamp(separation, -maxSeparation, maxSeparation);

            Vector2 candidateHand = _puppet.GetBackHandPosition(stretch, candidateRotation);
            float errorSq = Vector2.DistanceSquared(candidateHand, _twoHandedBackGripTargetWorld);
            float score = errorSq + (stretch == _twoHandedBackStretch ? 0f : switchPenaltySq);
            if (score < bestScore)
            {
                bestScore = score;
                bestStretch = stretch;
                bestRotation = candidateRotation;
                bestHand = candidateHand;
            }
        }

        private bool BowStringDrawPoseActive =>
            UseBowStringDrawPose
            && CompositeArmActive
            && _heldItemType == _activeRangedItemType
            && _activeRangedStyle == RangedStyle.Bow
            && (Phase == AttackPhase.RangedTelegraph
                || Phase == AttackPhase.RangedAttack
                || Phase == AttackPhase.CrossbowBurstPause);

        // Bow aim, facing-space radians (positive = down): body → target angle, clamped to ±45°. A pure
        // function of positions, so the server's arrow origin and every client's pose agree without sync.
        private const float BowMaxAimAngle = MathHelper.PiOver4;

        private float BowAimAngle
        {
            get
            {
                if (!NPC.HasValidTarget)
                {
                    return 0f;
                }

                Vector2 toTarget = Main.player[NPC.target].Center - NPC.Center;
                float facingAngle = (float)Math.Atan2(toTarget.Y, toTarget.X * NPC.direction);
                return MathHelper.Clamp(facingAngle, -BowMaxAimAngle, BowMaxAimAngle);
            }
        }

        // Bow arm straight out along the aim. Composite space: -π/2 = level forward and adding the aim
        // tilts the hand down (positive) or up; mirrored by facing like every other composite rotation.
        private float BowHoldArmRotation => (-MathHelper.PiOver2 + BowAimAngle) * NPC.direction;

        // String-hand cycle, as fractions of each telegraph / inter-shot pause: reach out to the string
        // until BowReachEnd, draw it back until BowPullEnd, then hold at full draw until the shot.
        private const float BowReachEnd = 0.25f;
        private const float BowPullEnd = 0.65f;
        // Radians the string arm is raised from level-forward at full draw. Arcs the hand up over the
        // shoulder to the cheek (~13px behind the resting string) instead of down through the legs.
        private const float BowPulledAngle = 2.2f;
        // Extra backward flick of the empty hand right after release.
        private const float BowReleaseFlickAngle = 0.25f;
        // Full-draw tremble amplitude. At Quarter stretch (6px arm) ±0.2 rad is a ~1px shake.
        private const float BowTrembleAngle = 0.2f;

        /// <summary>
        /// Poses the string hand for <see cref="UseBowStringDrawPose"/>. RangedTelegraph and
        /// CrossbowBurstPause both run reach → pull → trembling hold, so the release lands exactly on
        /// the shot (fired when those phases end). RangedAttack is the release: string snaps home and
        /// the empty hand flicks back, then the next pause reaches for the string again.
        /// </summary>
        private void TickBowStringDrawPose()
        {
            // Phase progress needs the phase's full length. A phase change, or the same phase
            // re-entered with a larger timer, restarts the clock at the current PhaseTimer.
            bool phaseRestarted = Phase != _bowPoseTrackedPhase || PhaseTimer > _bowPoseLastTimer;

            if (phaseRestarted)
            {
                _bowPoseTrackedPhase = Phase;
                _bowPosePhaseTicks = Math.Max(1, PhaseTimer);
            }

            _bowPoseLastTimer = PhaseTimer;

            float progress = 1f - PhaseTimer / (float)_bowPosePhaseTicks;
            progress = MathHelper.Clamp(progress, 0f, 1f);

            float raisedAngle;

            if (Phase == AttackPhase.RangedAttack)
            {
                // Release: over the first third of the phase the empty hand flicks a bit further back.
                float flick = MathHelper.SmoothStep(0f, 1f, Math.Min(1f, progress * 3f));
                raisedAngle = BowPulledAngle + BowReleaseFlickAngle * flick;
                _bowStringHeld = false;
            }
            else if (progress < BowReachEnd)
            {
                // Reach: from the cheek back out to the string, as if grabbing string and arrow.
                float reach = MathHelper.SmoothStep(0f, 1f, progress / BowReachEnd);
                raisedAngle = MathHelper.Lerp(BowPulledAngle, 0f, reach);
                _bowStringHeld = false;
            }
            else if (progress < BowPullEnd)
            {
                float pull = MathHelper.SmoothStep(0f, 1f, (progress - BowReachEnd) / (BowPullEnd - BowReachEnd));
                raisedAngle = MathHelper.Lerp(0f, BowPulledAngle, pull);
                _bowStringHeld = true;
            }
            else
            {
                // Full draw: ~3-tick sine tremble from the global clock, identical on every peer.
                float tremble = (float)Math.Sin(Main.GameUpdateCount * 2.1f) * BowTrembleAngle;
                raisedAngle = BowPulledAngle + tremble;
                _bowStringHeld = true;
            }

            // Arm shortens as it comes back. Thresholds split the arc so each stretch frame covers a
            // similar stretch of hand travel (Full at the string, Quarter at the cheek).
            Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Quarter;

            if (raisedAngle < 0.5f)
            {
                stretch = Player.CompositeArmStretchAmount.Full;
            }
            else if (raisedAngle < 1.2f)
            {
                stretch = Player.CompositeArmStretchAmount.ThreeQuarters;
            }

            _bowStringHandStretch = stretch;
            // The whole reach/draw arc turns with the bow, so the string hand stays on the bow's axis.
            _bowStringHandRotation = (-MathHelper.PiOver2 - raisedAngle + BowAimAngle) * NPC.direction;
        }

        private static void GetBackArmEllipse(
            Player.CompositeArmStretchAmount stretch,
            out float radiusX,
            out float radiusY)
        {
            (radiusX, radiusY) = stretch switch
            {
                Player.CompositeArmStretchAmount.Full => (10f, 12f),
                Player.CompositeArmStretchAmount.ThreeQuarters => (8f, 10f),
                Player.CompositeArmStretchAmount.Quarter => (6f, 8f),
                _ => (4f, 6f),
            };
        }
    }
}
