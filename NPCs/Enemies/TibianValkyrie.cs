using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Defensive.Shields;
using tsorcRevamp.Content.Items.Armor.Magic;
using tsorcRevamp.Content.Items.Armor.TibianValkyrieSet;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Spears;
using tsorcRevamp.Content.Items.Weapons.Throwing;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>
    /// Tibian Valkyrie: a gilded spear-and-axe valkyrie who presses forward. Gilded Dual-Bladed Axe in the back hand,
    /// Gilded Iron Spear in the front hand. Never kites: with nothing in range she advances.
    ///
    /// Kit (one server-side AttackBag; every action is one Custom-phase clock):
    ///   Valkyrie's Waltz   axe chop -> spear thrust -> axe return -> spear finisher (on hit or close only)
    ///   Gilded Cross       spear thrust with a delayed axe cut (on hit or reachable only)
    ///   Charging Thrust    distance-solved spear dash that stops 64px past the target (gap-closer)
    ///   Skyfall Cleave     axe leap slam, swept in the air once past the apex (gap-closer)
    ///   Gilded Javelin     throws the held spear, then a 24t re-arm beat with the spear fading back in
    /// Both hands are posed by ResolvePose on one clock (front = spear via CustomWeaponRotation + spear grip,
    /// back = axe via CustomBackWeaponRotation). The shared combo engine has one swing clock and one off-hand carry
    /// pose for both hands, so it cannot hold the idle weapon at its own end pose or thrust one hand while the other
    /// arcs; hits are tested here per hand with the base blade-hitbox projectile.
    /// Multiplayer: picks, locks, the Waltz finisher decision, dash/leap/javelin solutions and landing are server
    /// decisions synced in Send/ReceiveExtraAI after the base payload; the re-arm counter is synced so every client
    /// hides and shows the spear together. Projectiles and hitboxes spawn on the server; dust and sound on clients.
    /// </summary>
    public class TibianValkyrie : PuppetNPC, IHumanoidMeleeHitEffects
    {
        // ── Damage: real Expert damage before defense, converted once by EnemyDamage.Projectile ──────────────
        // Columns: before Eye of Cthulhu / pre-Hardmode / Hardmode / Super Hardmode.
        static readonly int[] AxeCutExpert = { 32, 48, 96, 168 };
        static readonly int[] SpearThrustExpert = { 28, 40, 80, 140 };
        static readonly int[] ChargeExpert = { 36, 56, 112, 196 };
        static readonly int[] SkyfallExpert = { 40, 60, 120, 210 };
        static readonly int[] JavelinExpert = { 28, 40, 80, 140 };
        // Skyfall's Hardmode floor shock deals half the slam.
        const float ShockDamageFraction = 0.5f;

        // ── Spear geometry, measured from the 104x104 staged art: head (1.5, 1.5) -> butt (102.5, 102.5) ─────
        // Grip fractions run from the head toward the butt (the base spear-grip convention). Draw scale 0.75:
        // carry 0.65 = hand 35% up from the butt, 69.6px hand->tip; extended 0.82 = 87.8px; drawn back 0.52 = 55.7px.
        public const float SpearDrawScale = 0.75f;
        public static readonly Vector2 SpearHeadTexel = new Vector2(1.5f, 1.5f);
        public static readonly Vector2 SpearButtTexel = new Vector2(102.5f, 102.5f);
        public static readonly float SpearShaftTexels = (SpearButtTexel - SpearHeadTexel).Length();
        public const float CarryGrip = 0.65f;
        public const float ExtendedGrip = 0.82f;
        public const float CockedGrip = 0.58f;
        public const float DrawnGrip = 0.52f;
        public const float ThrowGrip = 0.35f;
        // Steel head: the first 33 texels from the point = 25px at draw scale. Thrusts connect with the head only.
        const float SpearHeadLength = 25f;
        const float SpearBladeWidth = 12f;

        // ── Axe geometry, measured from the 80x80 staged art: butt (0.7, 78.3), head top (71.7, 7.3) ──────────
        // Grip 25% up the haft (18.5, 60.5). Draw scale 0.85: grip -> head corner 75 texels = 64px. The capsule runs
        // from 24px out (the upper haft) to 64px (the head), so only the head and top of the haft cut.
        public const float AxeDrawScale = 0.85f;
        public static readonly Vector2 AxeHandleNorm = new Vector2(18.5f / 80f, 60.5f / 80f);
        const float AxeReach = 64f;
        const float AxeHaftSkip = 24f;
        const float AxeBladeWidth = 24f;

        // ── Swing-space poses: world angle = rotation - 45 degrees facing right; level = pi/4 ─────────────────
        public const float SpearCarry = -0.30f;
        public const float AxeCarry = -1.20f;
        public const float LevelRotation = MathHelper.PiOver4;
        public const float SpearGuard = 0.30f;
        public const float SpearBrace = 1.30f;
        public const float AxeCocked = -1.62f;
        public const float AxeLow = 2.22f;
        public const float AxeRiseEnd = -1.40f;
        public const float SkyfallImpact = 2.10f;
        public const float JavelinCockLift = 0.60f;
        public const float JavelinFollowDrop = 0.70f;
        public const float JavelinReachBack = -1.50f;
        const float ThrustPitchLimit = 0.35f;
        const float JavelinPitchLimit = 0.9f;

        static readonly Color Gilded = new Color(236, 196, 92);

        public enum Move : byte { None, Waltz, Cross, Charge, Skyfall, Javelin }

        // ── Valkyrie's Waltz (ticks from the action's start) ──────────────────────────────────────────────────
        // Every beat is eased, pushes in, and the next beat's wind-up starts as the previous one finishes.
        // Step 1 AXE   Poses -1.62 -> 2.22 (envelope 220 deg; live ~176). Tell 36t: axe raised over 18t, spear to guard,
        //              glint on the axe head for the last 12t. Weighted 10/0/45 k7 -> peak 220 / (10/3 + 45 * 0.999 / 7)
        //              = 22.6 deg/t. Live while speed >= 30% of peak: [5.5, 17.7) of the step = t42-53 (12t).
        //              Push-in 48px shaped like the blade. Ends t91 (near-still tail).
        // Step 2 SPEAR Wind-up (draw back) t91-98 as the chop's tail settles; strike t99: 6t cubic ease-in extension,
        //              4t hold, 12t decaying retract. Live t99-108 (extension + hold, 10t), 45t after step 1's ends.
        //              Push-in 40px, fast start, gone by the hold's end.
        // Step 3 AXE   Wind-up t118-135 over the thrust's retract: the low axe cocks 0.45 rad further back, then settles
        //              on 2.22. Rising return 2.22 -> -1.40 (envelope 207; live ~166), Weighted 8/0/40 k7 -> peak 207 /
        //              (8/3 + 40 * 0.999 / 7) = 24.7 deg/t from t136. Live t141-150 (10t), 32t after the thrust's ends.
        //              Push-in 48px with the swing.
        // Continuation at t91/t118: preceding contact earns the next beat; a miss requires its own reach + live push.
        // Finisher     Decided at t184, BEFORE re-facing: the preceding axe connected, or the next thrust can reach
        //              on the committed side. Otherwise straight to the 40t recovery.
        // Step 4 SPEAR Wind-up t184-191, strike t192, live t192-201 (10t), 41t after step 3's ends. Push-in 48px.
        // Recovery 40  Spear retracts to carry over the first 20t, axe returns to the shoulder over the last 20t.
        // Counter      Live windows 10-12t with >= 32t gaps: 3-4 separate rolls standing still, or disengage beyond
        //              the next weapon's reach + live push after a miss. 254t (~4.2s) full, shorter on a failed follow-up.
        public const int WaltzTell = 36;
        public const int WaltzStep1Ticks = 55;
        public const int WaltzThrust2Draw = 91;
        public const int WaltzThrust2Strike = 99;
        public const int ThrustDrawTicks = 8;
        public const int ThrustStrikeTicks = 22;
        public const int WaltzStep3Windup = 118;
        public const int WaltzStep3Start = 136;
        public const int WaltzStep3Ticks = 48;
        public const int WaltzStep3End = 184;
        public const int WaltzFinisherStrike = 192;
        public const int WaltzStep4End = 214;
        public const int WaltzRecovery = 40;
        public const int WaltzTotal = 254;
        public const float WaltzStep1StepIn = 48f;
        public const float WaltzThrustStepIn = 40f;
        public const float WaltzStep3StepIn = 48f;
        public const float WaltzFinisherStepIn = 48f;
        // Axe wind-up anticipation: cock this far against the coming swing (65% of the wind-up), then settle on its start.
        const float AxeWindupAnticipation = 0.45f;

        // ── Gilded Cross ──────────────────────────────────────────────────────────────────────────────────────
        // Two beats that read as a combo, strikes 44t apart (start to start) so the live windows sit a full 40t apart:
        // a player hit on the thrust's last live tick is out of post-hit immunity by the axe's first one.
        // Tell 40t: spear levelled high and drawn back, axe on the shoulder behind it so the shafts form an X; gold glint
        //              at the crossing for the last 12t. Aim locks at t30.
        // Beat 1 SPEAR strike t40: 6t cubic ease-in extension, 4t hold, 12t decaying retract. Live t40-49 (10t).
        //              Push-in 40px, fast start, gone by t50.
        // Beat 2 AXE   Wind-up t66-83 over the spear's retract (cocks up-back to -1.65, settles on -1.20; glint last 6t).
        //              Strike t84: -1.20 -> 2.20 (envelope 195 deg, live ~156: a compact second beat by design),
        //              Weighted 8/0/36 k7 -> peak 195 / (8/3 + 36 * 0.999 / 7) = 25.0 deg/t. Live t89-98 (10t), 39t after
        //              the thrust's ends: each beat rolls separately. Push-in 48px with the swing.
        // Continuation at t66: thrust contact earns the axe cut; a miss requires the axe's reach + live push.
        // Open:        axe tail to t128, then 40t recovery. Total 168t.
        public const int CrossTell = 40;
        public const int CrossAxeWindup = 66;
        public const int CrossAxeStart = 84;
        public const int CrossAxeTicks = 44;
        public const int CrossRecoveryStart = 128;
        public const int CrossTotal = 168;
        public const float CrossAxeFrom = -1.20f;
        public const float CrossAxeTo = 2.20f;
        public const float CrossThrustStepIn = 40f;
        public const float CrossAxeStepIn = 48f;

        // ── Charging Thrust ───────────────────────────────────────────────────────────────────────────────────
        // Tell 32t: spear levelled and drawn far back, axe raised for balance, dust kicks at her feet for the last 10t.
        // Lock at t24 (facing, pitch). At release (t32) the server solves the lane: target X + 64px past it, cut short
        // at the first wall, missing floor or drop deeper than 3 tiles.
        // Dash 20t: speed eases in 4t, cruises 6t, decays 10t (k3); peak = distance / 10.5 (304px -> 29px/t). The
        // spear is live for the whole dash (20t < 22: a roll covers it; a tell-time sidestep onto a platform escapes).
        // Recovery 40t: spear stays extended 14t while she slides to a stop, then retracts. Hyper-armor: dash only.
        public const int ChargeTell = 32;
        public const int ChargeLock = 24;
        public const int ChargeDashTicks = 20;
        public const int ChargeTotal = 92;
        const float ChargeOvershoot = 64f;
        const float ChargeMinimumTravel = 48f;
        const float ChargeMaxDrop = 48f;

        // ── Skyfall Cleave ────────────────────────────────────────────────────────────────────────────────────
        // Tell 40t: axe raised overhead, spear braced across the body, glint on the axe head for the last 12t.
        // Leap at t40: vy -9.5, vx solved for the flat-ground airtime (63t) to land 24px past the target, capped at
        // 6px/t (378px), so the 320px gate is always reachable on flat ground. The strike starts once past the apex
        // with the target within 72px, or on landing: -1.62 -> 2.10, Weighted 6/0/24 k5 -> 30t, live [3, 12) = 9t.
        // One hit per leap (no second hit on landing). Hardmode+: a 6t floor shock 48px each side, jumpable.
        // Landing beat 14t with the axe buried, then 36t slow walk. Hyper-armor: takeoff to the end of the live window.
        public const int SkyfallTell = 40;
        public const int SkyfallLock = 34;
        public const int SkyfallStrikeTicks = 30;
        const float SkyfallLaunchSpeed = 9.5f;
        const float SkyfallGravity = 0.3f;
        const float SkyfallMaxForward = 6f;
        const float SkyfallOvershoot = 24f;
        const float SkyfallStrikeRange = 72f;
        const int SkyfallMaxAirTicks = 90;
        const int SkyfallLandingBeat = 14;
        const int SkyfallSlowWalk = 36;
        const float ShockHalfWidth = 48f;

        // ── Gilded Javelin ────────────────────────────────────────────────────────────────────────────────────
        // Tell 30t: spear lifted 0.6 rad above the aim with the hand slid up toward the head (the shaft drawn back over
        // the shoulder); gold glint at the point for the last 12t; aim tracks until t22, then locks for 8t.
        // Release t30: the held spear IS the projectile - it leaves from the drawn tip position, the hand prop is hidden
        // the same tick. Up to 10px/t, gravity 0.12, leading the target through the lock delay and flight.
        // Follow-through 12t, then the 24t re-arm: the arm reaches back over the shoulder and a fresh spear fades in.
        // Spear moves are gated until it is back. Total 76t.
        public const int JavelinTell = 30;
        public const int JavelinLock = 22;
        public const int JavelinFollowThrough = 12;
        public const int JavelinRearm = 24;
        public const int JavelinTotal = 76;

        // ── Synced state (SendExtraAI / ReceiveExtraAI, in this order) ─────────────────────────────────────────
        bool _phaseTwo;
        Move _move;
        int _actionTotal;
        bool _abortRecovery;
        float _abortSpear;
        float _abortGrip;
        float _abortAxe;
        int _lockedTarget = -1;
        int _lockFacing = 1;
        float _pitch;
        bool _waltzFinisher = true;
        float _dashDistance;
        float _leapVelocityX;
        int _strikeStart = -1;
        int _landTick = -1;
        Vector2 _javelinVelocity;
        Vector2 _javelinTargetPosition;
        Vector2 _javelinTargetVelocity;
        int _spearMissingTicks;
        int _restTicks;

        // ── Server-only selection state (never synced; clients follow the chosen action) ───────────────────────
        AttackBag<Move> _bag;
        readonly Dictionary<Move, int> _cooldowns = new Dictionary<Move, int>();
        Move _lastMove;
        int _age;
        int _neutralTicks;
        int _finisherWaitTicks;
        bool _beatHit;
        bool _previousBeatHit;

        // ── Per-tick, recomputed on every machine in DoCustomTick; hit tests run on the server ──────────────────
        // A hit window is one live strike; its id changes between strikes so each strike hits each player once.
        int _hitWindow;
        int _lastHitWindow;
        bool _spearLive;
        bool _axeLive;
        int _spearExpert;
        int _axeExpert;
        readonly HashSet<int> _hitPlayers = new HashSet<int>();
        bool _spearHasPrevious;
        Vector2 _spearPreviousOrigin;
        Vector2 _spearPreviousTip;
        bool _axeHasPrevious;
        Vector2 _axePreviousOrigin;
        Vector2 _axePreviousTip;
        bool _drawingSpearFade;
        bool _pushing;

        bool Server => Main.netMode != NetmodeID.MultiplayerClient;
        bool Neutral => Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
        int CustomElapsed => _actionTotal - PhaseTimer;
        bool SpearInHand => _spearMissingTicks == 0;

        static readonly PuppetWeapon AxeWeapon = new PuppetWeapon(
            ModContent.ItemType<EnemyGildedDualBladedAxe>(),
            handleNorm: AxeHandleNorm,
            drawScale: AxeDrawScale,
            bladeWidth: AxeBladeWidth,
            archetype: WeaponArchetype.Axe,
            reachBase: AxeReach / 0.7f,
            singleBladed: false);

        // ── Puppet configuration ────────────────────────────────────────────────────────────────────────────
        protected override string InvaderTitle => "Tibian Valkyrie";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override int CasualStrollChance => 0;
        protected override int EstusChargesMax => 0;
        protected override int HeadArmorItemType => ModContent.ItemType<TibianValkyrieHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<TibianValkyrieBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<TibianValkyrieGreaves>();
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyGildedIronSpear>();
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => EnemyDamage.Projectile(SpearThrustExpert[DamageTier()]);
        protected override int RangedDamage => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override bool HoldNeutralWeaponOnCompositeArm => true;
        protected override bool ShowMeleeWeaponWhileClosingDistance => true;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Halberd;
        protected override MeleeCombo[] MeleeComboPoolOverride => InertPool;
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction) => false;
        protected override void DoMeleeAttack() { }
        protected override void DoRangedAttack() { }

        // The combo engine is never used (HoldAttackSelection, nothing selectable); the pool only satisfies the base.
        static readonly MeleeCombo[] InertPool =
        {
            new MeleeCombo
            {
                Name = "Inert", BaseWeight = 0, Preferred = ComboRangeBand.Any,
                Steps = new[] { new MeleeComboStep { Motion = ComboMotion.OverheadArc, AttackTicks = 1, DamageMult = 0f, ReachMult = 1f, SwingSpeedMult = 1f } },
            },
        };

        // Spear on the spear draw path, so thrusts slide the grip along the shaft. The art's -135 degree shaft is turned
        // onto the -45 degree swing-space convention by +90 degrees.
        protected override bool DrawWeaponAsSpear => true;
        protected override bool MirrorSpearRotationByFacing => true;
        protected override string SpearDrawTexturePath => EnemyGildedIronSpear.ArtPath;
        protected override Vector2 SpearTextureSize => new Vector2(104f, 104f);
        protected override Vector2 SpearHeadNorm => SpearHeadTexel / 104f;
        protected override Vector2 SpearBaseNorm => SpearButtTexel / 104f;
        protected override float SpearDrawRotationOffset => MathHelper.PiOver2;
        protected override float MeleeWeaponDrawScale => SpearDrawScale;
        protected override float MeleeBladeWidth => SpearBladeWidth;
        protected override float ComboReachBase => 100f;
        protected override float MeleeRange => 96f;
        protected override float MeleeEngageRange => 96f;

        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom;
        // The axe rests over the back shoulder whenever she isn't attacking with it.
        protected override float OffHandCarryRotation => AxeCarry;
        // Hand changes are authored per tick by ResolvePose, so the engine's crossfade must not lag behind them.
        protected override int DualWieldHandBlendTicks => 1;
        protected override Color SlashVFXColor => Gilded;
        protected override float SlashVFXOpacity => 0.35f;
        protected override bool SuppressSlashVFXForCurrentPhase => Phase == AttackPhase.Custom;

        protected override float TopSpeed => 1.55f;
        protected override float Acceleration => 0.05f;
        protected override float PuppetJumpPower => 9f;
        protected override float PuppetJumpBoost => 5f;

        bool ActionOwnsMovement
        {
            get
            {
                if (Phase != AttackPhase.Custom || _abortRecovery)
                {
                    return false;
                }

                int elapsed = CustomElapsed;
                switch (_move)
                {
                    case Move.Waltz:
                    case Move.Cross:
                        return _pushing;
                    case Move.Charge:
                        return elapsed >= ChargeTell;
                    case Move.Skyfall:
                        return elapsed >= SkyfallTell && _landTick < 0;
                    default:
                        return false;
                }
            }
        }

        protected override bool SlowDownDuringCustom => !ActionOwnsMovement;

        // Hyper-armor: the Charging Thrust dash, and Skyfall from takeoff to the end of its live window.
        protected override bool CustomAttackCommitted
        {
            get
            {
                if (Phase != AttackPhase.Custom || _abortRecovery)
                {
                    return false;
                }

                int elapsed = CustomElapsed;
                if (_move == Move.Charge)
                {
                    return elapsed >= ChargeTell && elapsed < ChargeTell + ChargeDashTicks;
                }

                if (_move == Move.Skyfall)
                {
                    bool airborne = elapsed >= SkyfallTell && _strikeStart < 0;
                    bool striking = _strikeStart >= 0 && elapsed < _strikeStart + 12;
                    return airborne || striking;
                }

                return false;
            }
        }

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (_move == Move.None)
                {
                    return null;
                }

                ResolvePose(out float spear, out _, out _);
                return spear;
            }
        }

        protected override float? CustomBackWeaponRotation
        {
            get
            {
                if (Phase != AttackPhase.Custom || _move == Move.None)
                {
                    return null;
                }

                ResolvePose(out _, out _, out float axe);
                return axe;
            }
        }

        protected override float? OverrideSpearGrip(float swingClockT)
        {
            if (Phase != AttackPhase.Custom || _move == Move.None)
            {
                return CarryGrip;
            }

            ResolvePose(out _, out float grip, out _);
            return grip;
        }

        static float Smooth(float from, float to, float elapsed, float duration)
        {
            float progress = MathHelper.Clamp(elapsed / duration, 0f, 1f);
            return MathHelper.SmoothStep(from, to, progress);
        }

        /// <summary>Spear thrust, ticks from the start of its wind-up: draw back 8t (guard -> aim, grip pulled in), then
        /// the strike: 6t cubic ease-in extension (the point accelerates into full reach), 4t hold, 12t decaying retract
        /// (k5). Live = extension + hold. Shared by both Waltz thrusts and the Cross thrust.</summary>
        public static void ThrustPose(int elapsed, float aim, out float rotation, out float grip)
        {
            rotation = aim;

            if (elapsed < ThrustDrawTicks)
            {
                rotation = Smooth(SpearGuard, aim, elapsed, ThrustDrawTicks);
                grip = Smooth(CockedGrip, DrawnGrip, elapsed, ThrustDrawTicks);
                return;
            }

            int strike = elapsed - ThrustDrawTicks;

            if (strike < 6)
            {
                float extension = (strike + 1) / 6f;
                float eased = extension * extension * extension;
                grip = MathHelper.Lerp(DrawnGrip, ExtendedGrip, eased);
                return;
            }

            if (strike < 10)
            {
                grip = ExtendedGrip;
                return;
            }

            float retract = MathHelper.Clamp((strike - 10) / 12f, 0f, 1f);
            float settled = (1f - (float)Math.Exp(-5f * retract)) / (1f - (float)Math.Exp(-5f));
            grip = MathHelper.Lerp(ExtendedGrip, CarryGrip, settled);
        }

        /// <summary>Axe wind-up into a swing's start pose: from where the axe is, cock against the coming swing by
        /// AxeWindupAnticipation over the first 65%, then settle exactly on the start (zero speed into the strike).
        /// <paramref name="swingSign"/> is the sign of the swing's travel.</summary>
        static float AxeWindup(float from, float start, float swingSign, float elapsed, float duration)
        {
            float cocked = start - swingSign * AxeWindupAnticipation;
            float cockTicks = duration * 0.65f;

            if (elapsed < cockTicks)
            {
                return Smooth(from, cocked, elapsed, cockTicks);
            }

            return Smooth(cocked, start, elapsed - cockTicks, duration - cockTicks);
        }

        /// <summary>
        /// The single source of every Custom pose: spear rotation and grip (front hand) and axe rotation (back hand)
        /// for the current action and tick. Read by CustomWeaponRotation, CustomBackWeaponRotation and
        /// OverrideSpearGrip, so the drawn weapons and their hit capsules can never disagree. Aim = _pitch, tracked on
        /// every machine until the action's lock and then the server's synced value. Public for SwingPreview.
        /// </summary>
        public void ResolvePose(out float spear, out float grip, out float axe)
        {
            int elapsed = CustomElapsed;
            spear = SpearCarry;
            grip = CarryGrip;
            axe = AxeCarry;

            if (_abortRecovery)
            {
                // Hold 6t, then return over up to 24t so a low axe does not rewind faster than its strike.
                // Short aborts reserve two final carry ticks; recovery duration and damage timing stay unchanged.
                float settleTicks = _actionTotal - 8f;
                settleTicks = MathHelper.Clamp(settleTicks, 1f, 24f);
                float easeBack = MathHelper.Clamp((elapsed - 6) / settleTicks, 0f, 1f);
                spear = MathHelper.SmoothStep(_abortSpear, SpearCarry, easeBack);
                grip = MathHelper.SmoothStep(_abortGrip, CarryGrip, easeBack);
                axe = MathHelper.SmoothStep(_abortAxe, AxeCarry, easeBack);
                return;
            }

            float aim = LevelRotation + _pitch;

            switch (_move)
            {
                case Move.Waltz:
                {
                    int recoveryStart = WaltzStep3End;
                    if (_waltzFinisher)
                    {
                        recoveryStart = WaltzStep4End;
                    }

                    // Axe: raise, overhead chop, park low through the thrust, wind-up, rising return, hold, shoulder.
                    if (elapsed < WaltzTell)
                    {
                        axe = Smooth(AxeCarry, AxeCocked, elapsed, 18f);
                    }
                    else if (elapsed < WaltzTell + WaltzStep1Ticks)
                    {
                        axe = SwingEase.ApplyWeighted(AxeCocked, AxeLow, elapsed - WaltzTell, WaltzStep1Ticks, 10, 45, 7f);
                    }
                    else if (elapsed < WaltzStep3Windup)
                    {
                        axe = AxeLow;
                    }
                    else if (elapsed < WaltzStep3Start)
                    {
                        axe = AxeWindup(AxeLow, AxeLow, -1f, elapsed - WaltzStep3Windup, WaltzStep3Start - WaltzStep3Windup);
                    }
                    else if (elapsed < WaltzStep3End)
                    {
                        axe = SwingEase.ApplyWeighted(AxeLow, AxeRiseEnd, elapsed - WaltzStep3Start, WaltzStep3Ticks, 8, 40, 7f);
                    }
                    else
                    {
                        axe = AxeRiseEnd;
                    }

                    if (elapsed >= recoveryStart + 20)
                    {
                        axe = Smooth(AxeRiseEnd, AxeCarry, elapsed - recoveryStart - 20, 20f);
                    }

                    // Spear: to guard in the tell, thrust, back to guard, (finisher thrust), carry.
                    int thrustTwoEnd = WaltzThrust2Strike + ThrustStrikeTicks;
                    int finisherDraw = WaltzFinisherStrike - ThrustDrawTicks;
                    if (elapsed < WaltzThrust2Draw)
                    {
                        spear = Smooth(SpearCarry, SpearGuard, elapsed, 18f);
                        grip = Smooth(CarryGrip, CockedGrip, elapsed, 18f);
                    }
                    else if (elapsed < thrustTwoEnd)
                    {
                        ThrustPose(elapsed - WaltzThrust2Draw, aim, out spear, out grip);
                    }
                    else if (elapsed < finisherDraw || !_waltzFinisher)
                    {
                        int sinceThrust = elapsed - thrustTwoEnd;
                        spear = Smooth(aim, SpearGuard, sinceThrust, 12f);
                        grip = Smooth(CarryGrip, CockedGrip, sinceThrust, 12f);
                    }
                    else
                    {
                        ThrustPose(elapsed - finisherDraw, aim, out spear, out grip);
                    }

                    if (elapsed >= recoveryStart)
                    {
                        float spearFrom = SpearGuard;
                        float gripFrom = CockedGrip;
                        if (_waltzFinisher)
                        {
                            spearFrom = aim;
                            gripFrom = CarryGrip;
                        }

                        spear = Smooth(spearFrom, SpearCarry, elapsed - recoveryStart, 20f);
                        grip = Smooth(gripFrom, CarryGrip, elapsed - recoveryStart, 20f);
                    }

                    return;
                }

                case Move.Cross:
                {
                    float crossAim = aim - 0.15f;
                    int thrustEnd = CrossTell + ThrustStrikeTicks;

                    // Spear: cocked high and drawn back through the tell, strike at t40, then to guard for the axe beat.
                    if (elapsed < CrossTell)
                    {
                        spear = Smooth(SpearCarry, crossAim, elapsed, 24f);
                        grip = Smooth(CarryGrip, DrawnGrip, elapsed, 24f);
                    }
                    else if (elapsed < thrustEnd)
                    {
                        // The tell already ends in the drawn pose, so the strike enters ThrustPose past its draw.
                        ThrustPose(elapsed - CrossTell + ThrustDrawTicks, crossAim, out spear, out grip);
                    }
                    else
                    {
                        spear = Smooth(crossAim, SpearGuard, elapsed - thrustEnd, 12f);
                        grip = Smooth(CarryGrip, CockedGrip, elapsed - thrustEnd, 12f);
                    }

                    // Axe: rests on the shoulder (its strike start), winds up over the retract, cuts at t84.
                    axe = CrossAxeFrom;
                    if (elapsed >= CrossAxeWindup && elapsed < CrossAxeStart)
                    {
                        axe = AxeWindup(CrossAxeFrom, CrossAxeFrom, 1f, elapsed - CrossAxeWindup, CrossAxeStart - CrossAxeWindup);
                    }
                    else if (elapsed >= CrossAxeStart)
                    {
                        axe = SwingEase.ApplyWeighted(CrossAxeFrom, CrossAxeTo, elapsed - CrossAxeStart, CrossAxeTicks, 8, 36, 7f);
                    }

                    if (elapsed >= CrossRecoveryStart)
                    {
                        spear = Smooth(SpearGuard, SpearCarry, elapsed - CrossRecoveryStart, 24f);
                        grip = Smooth(CockedGrip, CarryGrip, elapsed - CrossRecoveryStart, 24f);
                        axe = Smooth(CrossAxeTo, AxeCarry, elapsed - CrossRecoveryStart, 24f);
                    }

                    return;
                }

                case Move.Charge:
                {
                    if (elapsed < ChargeTell)
                    {
                        spear = Smooth(SpearCarry, aim, elapsed, 16f);
                        grip = Smooth(CarryGrip, DrawnGrip, elapsed, 16f);
                        axe = Smooth(AxeCarry, AxeCocked, elapsed, 16f);
                        return;
                    }

                    int dash = elapsed - ChargeTell;
                    spear = aim;
                    axe = AxeCocked;
                    grip = Smooth(DrawnGrip, ExtendedGrip, dash, 4f);

                    int recovery = dash - ChargeDashTicks;
                    if (recovery >= 0)
                    {
                        // Spear held out for 14t while she slides to a stop, then retracts and drops to carry.
                        grip = Smooth(ExtendedGrip, CarryGrip, recovery - 14, 10f);
                        spear = Smooth(aim, SpearCarry, recovery - 14, 16f);
                        axe = Smooth(AxeCocked, AxeCarry, recovery, 20f);
                    }

                    return;
                }

                case Move.Skyfall:
                {
                    spear = Smooth(SpearCarry, SpearBrace, elapsed, 22f);
                    grip = Smooth(CarryGrip, CockedGrip, elapsed, 22f);
                    axe = Smooth(AxeCarry, AxeCocked, elapsed, 22f);

                    if (_strikeStart >= 0 && elapsed >= _strikeStart)
                    {
                        axe = SwingEase.ApplyWeighted(AxeCocked, SkyfallImpact, elapsed - _strikeStart, SkyfallStrikeTicks, 6, 24, 5f);
                    }

                    int recoveryBegin = SkyfallRecoveryBegin();
                    if (recoveryBegin >= 0 && elapsed >= recoveryBegin)
                    {
                        spear = Smooth(SpearBrace, SpearCarry, elapsed - recoveryBegin, 24f);
                        grip = Smooth(CockedGrip, CarryGrip, elapsed - recoveryBegin, 24f);
                        axe = Smooth(SkyfallImpact, AxeCarry, elapsed - recoveryBegin, 24f);
                    }

                    return;
                }

                case Move.Javelin:
                {
                    float cock = aim - JavelinCockLift;
                    int release = JavelinTell;
                    int rearmStart = JavelinTell + JavelinFollowThrough;
                    int settleStart = rearmStart + JavelinRearm;

                    if (elapsed < release)
                    {
                        spear = Smooth(SpearCarry, cock, elapsed, 14f);
                        grip = Smooth(CarryGrip, ThrowGrip, elapsed, 14f);
                    }
                    else if (elapsed < rearmStart)
                    {
                        // The empty hand whips through to follow the throw.
                        spear = Smooth(cock, aim + JavelinFollowDrop, elapsed - release, 6f);
                    }
                    else if (elapsed < settleStart)
                    {
                        // Reach up over the shoulder (14t); the fresh spear fades in there, point up.
                        spear = Smooth(aim + JavelinFollowDrop, JavelinReachBack, elapsed - rearmStart, 14f);
                    }
                    else
                    {
                        spear = Smooth(JavelinReachBack, SpearCarry, elapsed - settleStart, 9f);
                    }

                    return;
                }
            }
        }

        /// <summary>Skyfall's recovery start: the later of the landing beat's end and the strike's end. -1 while
        /// still airborne.</summary>
        int SkyfallRecoveryBegin()
        {
            if (_landTick < 0)
            {
                return -1;
            }

            int landingBeatEnd = _landTick + SkyfallLandingBeat;
            int strikeEnd = _strikeStart + SkyfallStrikeTicks;
            return Math.Max(landingBeatEnd, strikeEnd);
        }

        /// <summary>Speed (px/t) at <paramref name="elapsed"/> of a move that covers <paramref name="distance"/> with a
        /// swing-shaped profile: (t/in)^2 ramp, constant cruise, then e^(-k p) decay. Peak is solved so the integral
        /// equals the distance: peak * (in/3 + cruise + out * (1 - e^-k) / k) = distance.</summary>
        public static float ShapedSpeed(float distance, float elapsed, int easeIn, int cruise, int easeOut, float decay)
        {
            float integral = easeIn / 3f + cruise + easeOut * (1f - (float)Math.Exp(-decay)) / decay;
            float peak = distance / integral;
            float sample = elapsed + 0.5f;

            if (sample < easeIn)
            {
                float rampProgress = sample / easeIn;
                return peak * rampProgress * rampProgress;
            }

            if (sample < easeIn + cruise)
            {
                return peak;
            }

            float decayProgress = MathHelper.Clamp((sample - easeIn - cruise) / easeOut, 0f, 1f);
            return peak * (float)Math.Exp(-decay * decayProgress);
        }

        /// <summary>Current aim pitch at the target (positive = below), clamped. Every machine can compute it from the
        /// synced target, so tell poses match before the lock snapshot arrives.</summary>
        float LivePitch(float limit)
        {
            int targetIndex = _lockedTarget;
            if (targetIndex < 0)
            {
                targetIndex = NPC.target;
            }

            if (targetIndex < 0 || targetIndex >= Main.maxPlayers || !Main.player[targetIndex].active)
            {
                return 0f;
            }

            Vector2 gap = Main.player[targetIndex].Center - NPC.Center;
            float pitch = (float)Math.Atan2(gap.Y, Math.Abs(gap.X));
            return MathHelper.Clamp(pitch, -limit, limit);
        }

        /// <summary>Index into the damage tables: before Eye of Cthulhu / pre-Hardmode / Hardmode / Super Hardmode.</summary>
        static int DamageTier()
        {
            if (tsorcRevampWorld.SuperHardMode)
            {
                return 3;
            }

            if (Main.hardMode)
            {
                return 2;
            }

            if (NPC.downedBoss1)
            {
                return 1;
            }

            return 0;
        }

        // ── Per-tick AI ─────────────────────────────────────────────────────────────────────────────────────
        public override void AI()
        {
            _age++;

            if (BackHandWeapon.IsEmpty)
            {
                EquipWeapon(PuppetHandSlot.Back, AxeWeapon);
            }

            // Every machine counts the re-arm down from the synced value, so the spear reappears on the same tick.
            if (_spearMissingTicks > 0)
            {
                _spearMissingTicks--;
            }

            if (_restTicks > 0)
            {
                _restTicks--;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (Server)
            {
                ServerPreTick(globalNPC);
            }

            base.AI();

            // Blade hits sample AFTER base.AI, so the capsules use this tick's visible pose.
            if (Server)
            {
                TickWeaponHits();

                if (Neutral && _move != Move.None)
                {
                    CompleteAction();
                }
            }

            globalNPC.AttackCommitted = Phase == AttackPhase.Custom && CustomAttackCommitted;
            globalNPC.AttackTelegraphing = !Neutral;
            DebugAttackLabel = CurrentLabel();
        }

        /// <summary>Server, before base.AI: cooldowns, half-health latch, target loss and the next pick.</summary>
        void ServerPreTick(tsorcRevampGlobalNPC globalNPC)
        {
            foreach (Move move in new[] { Move.Waltz, Move.Cross, Move.Charge, Move.Skyfall, Move.Javelin })
            {
                if (_cooldowns.TryGetValue(move, out int remaining) && remaining > 0)
                {
                    _cooldowns[move] = remaining - 1;
                }
            }

            if (!NPC.HasValidTarget)
            {
                NPC.TargetClosest();
            }

            // Below half health latches once: two more gap-closer cards (in place of the legacy enrage speed spike).
            if (!_phaseTwo && NPC.life < NPC.lifeMax * 0.5f)
            {
                _phaseTwo = true;
                _bag?.Clear();
                Trace("phase-two", "latched", false);
                Snapshot();
            }

            if (Neutral)
            {
                _neutralTicks++;
            }
            else
            {
                _neutralTicks = 0;
            }

            if (Phase == AttackPhase.Custom && _move != Move.None && !_abortRecovery)
            {
                bool targetLost = _lockedTarget < 0 || _lockedTarget >= Main.maxPlayers
                    || !Main.player[_lockedTarget].active || Main.player[_lockedTarget].dead;

                if (targetLost)
                {
                    Trace("target-lost", "abort", false);
                    AbortIntoRecovery(30);
                }
            }

            bool grounded = NPC.velocity.Y == 0f;
            bool ready = Neutral && NPC.HasValidTarget && grounded && _restTicks == 0
                && globalNPC.StaggerTimer == 0 && _age >= 30 && !InFinisherPunishWindow
                && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height);

            if (ready)
            {
                SelectMove(globalNPC, Main.player[NPC.target]);
            }
        }

        void Snapshot()
        {
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        void Trace(string kind, string gate, bool finisher)
        {
            if (!Server || !ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                return;
            }

            Mod.Logger.Info("VALKYRIE pick=" + _move + " kind=" + kind + " gate=" + gate + " finisher=" + finisher
                + " npc=" + NPC.whoAmI + " tick=" + Main.GameUpdateCount + " phase=" + Phase + " elapsed=" + CustomElapsed
                + " phaseTwo=" + _phaseTwo + " life=" + NPC.life);
        }

        string CurrentLabel()
        {
            if (Phase == AttackPhase.Custom && _move != Move.None)
            {
                string stage = "t" + CustomElapsed;
                if (_abortRecovery)
                {
                    stage = "aborted";
                }

                switch (_move)
                {
                    case Move.Waltz:
                        return "Valkyrie's Waltz (" + stage + ")";
                    case Move.Cross:
                        return "Gilded Cross (" + stage + ")";
                    case Move.Charge:
                        return "Charging Thrust (" + stage + ")";
                    case Move.Skyfall:
                        return "Skyfall Cleave (" + stage + ")";
                    default:
                        return "Gilded Javelin (" + stage + ")";
                }
            }

            if (!SpearInHand)
            {
                return "Re-arming";
            }

            return null;
        }

        // ── Selection ───────────────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Server, neutral and ready. The bag deals Waltz x2, Cross, Charge, Skyfall, Javelin (+ Charge, Skyfall below
        /// half health), skipping ineligible cards without dropping them. An immediate repeat is blocked until a
        /// different move completes or 90t of neutral pass. Once 3 blocks are banked (the unblockable finisher), only
        /// Charge or Skyfall may start for up to 120t of neutral; after that the close moves resume, never unblockable.
        /// With nothing eligible she simply keeps advancing.
        /// </summary>
        void SelectMove(tsorcRevampGlobalNPC globalNPC, Player target)
        {
            float distance = NPC.Distance(target.Center);
            float verticalGap = Math.Abs(target.Center.Y - NPC.Center.Y);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            if (!lineOfSight)
            {
                return;
            }

            bool finisherReady = globalNPC.IsMaximumGuardPressureReady(NPC);
            if (finisherReady)
            {
                _finisherWaitTicks++;
            }
            else
            {
                _finisherWaitTicks = 0;
            }

            bool gapClosersOnly = finisherReady && _finisherWaitTicks < 120;

            _bag ??= new AttackBag<Move>(cards =>
            {
                cards.Add(Move.Waltz);
                cards.Add(Move.Waltz);
                cards.Add(Move.Cross);
                cards.Add(Move.Charge);
                cards.Add(Move.Skyfall);
                cards.Add(Move.Javelin);

                if (_phaseTwo)
                {
                    cards.Add(Move.Charge);
                    cards.Add(Move.Skyfall);
                }
            });

            bool Eligible(Move move)
            {
                bool repeatBlocked = move == _lastMove && _neutralTicks < 90;
                if (repeatBlocked || !CooldownReady(move))
                {
                    return false;
                }

                bool gapCloser = move == Move.Charge || move == Move.Skyfall;
                if (gapClosersOnly && !gapCloser)
                {
                    return false;
                }

                switch (move)
                {
                    case Move.Waltz:
                        return SpearInHand && distance <= 96f && verticalGap < 48f;
                    case Move.Cross:
                        return SpearInHand && distance <= 80f && verticalGap < 48f;
                    case Move.Charge:
                    {
                        if (!SpearInHand || distance < 112f || distance > 240f)
                        {
                            return false;
                        }

                        float laneDistance = SolveChargeLane(target);
                        return ChargeLaneCanReachTarget(target, laneDistance);
                    }
                    case Move.Skyfall:
                        return distance >= 160f && distance <= 320f && verticalGap < 96f && SkyfallHeadroom();
                    case Move.Javelin:
                        return SpearInHand && distance >= 240f && distance <= 560f
                            && JavelinArcClear(target, JavelinTell - JavelinLock, out _, out _);
                    default:
                        return false;
                }
            }

            if (!_bag.TryDraw(Eligible, out Move chosen))
            {
                return;
            }

            BeginMove(globalNPC, chosen, target, finisherReady);
        }

        bool CooldownReady(Move move)
        {
            return !_cooldowns.TryGetValue(move, out int remaining) || remaining <= 0;
        }

        /// <summary>Server: face and lock the target, and start the chosen action's Custom phase.</summary>
        void BeginMove(tsorcRevampGlobalNPC globalNPC, Move move, Player target, bool finisherReady)
        {
            int facing = 1;
            if (target.Center.X < NPC.Center.X)
            {
                facing = -1;
            }

            NPC.direction = facing;
            NPC.spriteDirection = facing;
            _lockFacing = facing;
            _lockedTarget = target.whoAmI;
            _move = move;
            _pitch = 0f;
            _waltzFinisher = true;
            _beatHit = false;
            _previousBeatHit = false;
            _strikeStart = -1;
            _landTick = -1;
            _dashDistance = 0f;
            _leapVelocityX = 0f;
            _abortRecovery = false;

            // Only the gap-closers may carry the unblockable finisher. The base tags whichever attack starts while
            // blocks are banked; for any other move the threshold is lifted for this one start (the base restores
            // the puppet's value next tick), so the banked blocks wait for the next Charge or Skyfall.
            bool gapCloser = move == Move.Charge || move == Move.Skyfall;
            bool carriesFinisher = finisherReady && gapCloser;
            if (!gapCloser)
            {
                globalNPC.GuardPressureFinisherBlocks = int.MaxValue;
            }

            int total = WaltzTotal;
            switch (move)
            {
                case Move.Cross:
                    total = CrossTotal;
                    break;
                case Move.Charge:
                    total = ChargeTotal;
                    break;
                case Move.Skyfall:
                    total = SkyfallTell + SkyfallMaxAirTicks + SkyfallStrikeTicks + SkyfallSlowWalk;
                    break;
                case Move.Javelin:
                    total = JavelinTotal;
                    break;
            }

            _actionTotal = total;
            StartCustomAttack(total, MeleeWeaponItemType);
            Trace("pick", "band " + (int)NPC.Distance(target.Center) + "px", carriesFinisher);
            Snapshot();
        }

        /// <summary>Server: end the current action's offense and play a harmless recovery of the given length,
        /// starting from the exact pose it stopped in.</summary>
        void AbortIntoRecovery(int recoveryTicks)
        {
            ResolvePose(out float spear, out float grip, out float axe);
            _abortSpear = spear;
            _abortGrip = grip;
            _abortAxe = axe;
            _abortRecovery = true;
            _actionTotal = recoveryTicks;
            PhaseTimer = recoveryTicks + 1;
            _hitWindow = 0;
            Snapshot();
        }

        /// <summary>Server: the action returned to neutral. Cooldowns start on full exit.</summary>
        void CompleteAction()
        {
            int cooldown = 0;
            switch (_move)
            {
                case Move.Waltz:
                    cooldown = 150;
                    break;
                case Move.Cross:
                    cooldown = 240;
                    break;
                case Move.Charge:
                    cooldown = 300;
                    break;
                case Move.Skyfall:
                    cooldown = 360;
                    break;
                case Move.Javelin:
                    cooldown = 240;
                    break;
            }

            _cooldowns[_move] = cooldown;
            // A 12t neutral planning beat on top of every authored recovery.
            _restTicks = 12;
            Trace("complete", "cooldown " + cooldown, false);
            _lastMove = _move;
            _move = Move.None;
            _abortRecovery = false;
            _lockedTarget = -1;
            _hitWindow = 0;
            Snapshot();
        }

        // ── Custom phase tick (every machine; locks, spawns and branch decisions are server only) ───────────────
        protected override void DoCustomTick(int ticksRemaining)
        {
            _hitWindow = 0;
            _spearLive = false;
            _axeLive = false;
            _pushing = false;

            if (_move == Move.None)
            {
                return;
            }

            int elapsed = _actionTotal - ticksRemaining;

            if (_abortRecovery)
            {
                NPC.velocity.X *= 0.7f;
                return;
            }

            Player target = null;
            if (_lockedTarget >= 0 && _lockedTarget < Main.maxPlayers && Main.player[_lockedTarget].active)
            {
                target = Main.player[_lockedTarget];
            }

            if (Server)
            {
                // Contact was sampled after base.AI on the preceding live tick. Keep history server-only;
                // the existing abort pose/clock and finisher payload carry each visible decision to clients.
                bool beatEnded = _move == Move.Waltz
                    && (elapsed == 54 || elapsed == WaltzThrust2Strike + 10 || elapsed == 151);
                beatEnded |= _move == Move.Cross && elapsed == CrossTell + 10;
                if (beatEnded)
                {
                    _previousBeatHit = _beatHit;
                    _beatHit = false;
                }

                if (!_previousBeatHit)
                {
                    bool unreachable = false;
                    float spearReach = SpearShaftTexels * ExtendedGrip * SpearDrawScale;
                    if (_move == Move.Waltz && elapsed == WaltzThrust2Draw)
                    {
                        unreachable = !CanReachNextBeat(target, spearReach, WaltzThrustStepIn, 1, 9, 10, 4f);
                    }
                    else if (_move == Move.Waltz && elapsed == WaltzStep3Windup)
                    {
                        unreachable = !CanReachNextBeat(target, AxeReach, WaltzStep3StepIn, 8, 40, 15, 7f);
                    }
                    else if (_move == Move.Cross && elapsed == CrossAxeWindup)
                    {
                        unreachable = !CanReachNextBeat(target, AxeReach, CrossAxeStepIn, 8, 36, 15, 7f);
                    }

                    if (unreachable)
                    {
                        Trace("whiff-recovery", "next beat out of reach", false);
                        AbortIntoRecovery(WaltzRecovery);
                        return;
                    }
                }
            }

            // Facing and aim follow the target only in each action's tracking windows (tells before their lock,
            // Waltz pauses), then hold the locked side and pitch.
            bool tracking = false;
            float pitchLimit = ThrustPitchLimit;
            switch (_move)
            {
                case Move.Waltz:
                {
                    // Re-face only during each beat's wind-up; the finisher's wind-up starts after its decision tick.
                    bool tell = elapsed < 24;
                    bool thrustWindup = elapsed >= WaltzThrust2Draw && elapsed < WaltzThrust2Strike;
                    bool axeWindup = elapsed >= WaltzStep3Windup && elapsed < WaltzStep3Start;
                    bool finisherWindup = elapsed > WaltzStep3End && elapsed < WaltzFinisherStrike;
                    tracking = tell || thrustWindup || axeWindup || finisherWindup;
                    break;
                }
                case Move.Cross:
                    tracking = elapsed < 30 || (elapsed >= CrossAxeWindup && elapsed < CrossAxeStart);
                    break;
                case Move.Charge:
                    tracking = elapsed < ChargeLock;
                    break;
                case Move.Skyfall:
                    tracking = elapsed < SkyfallLock;
                    break;
                case Move.Javelin:
                    tracking = elapsed < JavelinLock;
                    pitchLimit = JavelinPitchLimit;
                    break;
            }

            if (tracking && target != null)
            {
                if (Math.Abs(target.Center.X - NPC.Center.X) > 8f)
                {
                    _lockFacing = 1;
                    if (target.Center.X < NPC.Center.X)
                    {
                        _lockFacing = -1;
                    }
                }

                _pitch = LivePitch(pitchLimit);
            }

            NPC.direction = _lockFacing;
            NPC.spriteDirection = _lockFacing;

            switch (_move)
            {
                case Move.Waltz:
                    TickWaltz(elapsed, target);
                    break;
                case Move.Cross:
                    TickCross(elapsed);
                    break;
                case Move.Charge:
                    TickCharge(elapsed, target);
                    break;
                case Move.Skyfall:
                    TickSkyfall(elapsed, target);
                    break;
                case Move.Javelin:
                    TickJavelin(elapsed, target);
                    break;
            }
        }

        void TickWaltz(int elapsed, Player target)
        {
            int tier = DamageTier();

            if (!Main.dedServ)
            {
                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.35f }, NPC.Center);
                }

                // Gold glint on the raised axe head for the last 12t of the tell, and on the low head as step 3 winds up.
                bool tellGlint = elapsed >= WaltzTell - 12 && elapsed < WaltzTell;
                bool windupGlint = elapsed >= WaltzStep3Start - 8 && elapsed < WaltzStep3Start;
                if ((tellGlint || windupGlint) && elapsed % 2 == 0)
                {
                    Glint(PuppetBackHandPosition + PuppetBackWeaponDirection * 50f, 1);
                }

                bool swingStarts = elapsed == WaltzTell || elapsed == WaltzStep3Start;
                bool thrustStarts = elapsed == WaltzThrust2Strike || (_waltzFinisher && elapsed == WaltzFinisherStrike);
                if (swingStarts)
                {
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
                }

                if (thrustStarts)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = 0.25f }, NPC.Center);
                }
            }

            // Facing and pitch lock at each strike; the server's value is synced.
            bool strikeLock = elapsed == WaltzThrust2Strike || elapsed == WaltzStep3Start || elapsed == WaltzFinisherStrike;
            if (strikeLock && Server)
            {
                Snapshot();
            }

            // Eased push-in on every beat, shaped like that beat's own speed curve.
            int stepOne = elapsed - WaltzTell;
            int thrustTwo = elapsed - WaltzThrust2Strike;
            int stepThree = elapsed - WaltzStep3Start;
            int finisher = elapsed - WaltzFinisherStrike;
            if (stepOne >= 0 && stepOne < WaltzStep1Ticks)
            {
                PushIn(ShapedSpeed(WaltzStep1StepIn, stepOne, 10, 0, 45, 7f));
            }
            else if (thrustTwo >= 0 && thrustTwo < 10)
            {
                PushIn(ShapedSpeed(WaltzThrustStepIn, thrustTwo, 1, 0, 9, 4f));
            }
            else if (stepThree >= 0 && stepThree < WaltzStep3Ticks)
            {
                PushIn(ShapedSpeed(WaltzStep3StepIn, stepThree, 8, 0, 40, 7f));
            }
            else if (_waltzFinisher && finisher >= 0 && finisher < 10)
            {
                PushIn(ShapedSpeed(WaltzFinisherStepIn, finisher, 1, 0, 9, 4f));
            }

            // Owl Father's hit-or-reachable rule uses the preceding strike, not any hit earlier in the combo.
            // Check the thrust's own live advance before its wind-up re-faces the target.
            if (elapsed == WaltzStep3End && Server)
            {
                bool close = false;
                if (target != null)
                {
                    int targetSide = Math.Sign(target.Center.X - NPC.Center.X);
                    float spearReach = SpearShaftTexels * ExtendedGrip * SpearDrawScale;
                    close = targetSide == _lockFacing
                        && CanReachNextBeat(target, spearReach, WaltzFinisherStepIn, 1, 9, 10, 4f);
                }

                _waltzFinisher = _previousBeatHit || close;
                if (!_waltzFinisher)
                {
                    _actionTotal = WaltzStep3End + WaltzRecovery;
                    PhaseTimer = _actionTotal - elapsed;
                }

                Trace("waltz-finisher", "previous hit " + _previousBeatHit + " reachable " + close, false);
                Snapshot();
            }

            // Live windows: axe while its speed is >= 30% of peak; thrusts for extension + hold.
            if (elapsed >= 42 && elapsed < 54)
            {
                _hitWindow = 1;
                _axeLive = true;
            }
            else if (thrustTwo >= 0 && thrustTwo < 10)
            {
                _hitWindow = 2;
                _spearLive = true;
            }
            else if (elapsed >= 141 && elapsed < 151)
            {
                _hitWindow = 3;
                _axeLive = true;
            }
            else if (_waltzFinisher && finisher >= 0 && finisher < 10)
            {
                _hitWindow = 4;
                _spearLive = true;
            }

            _axeExpert = AxeCutExpert[tier];
            _spearExpert = SpearThrustExpert[tier];
        }

        void TickCross(int elapsed)
        {
            int tier = DamageTier();

            if (!Main.dedServ)
            {
                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.1f }, NPC.Center);
                }

                // Gold glint where the shafts cross for the last 12t of the tell, then on the axe head as it winds up.
                if (elapsed >= CrossTell - 12 && elapsed < CrossTell && elapsed % 2 == 0)
                {
                    Vector2 axeHead = PuppetBackHandPosition + PuppetBackWeaponDirection * 40f;
                    Vector2 crossing = Vector2.Lerp(PuppetSpearTipPosition, axeHead, 0.5f);
                    Glint(crossing, 2);
                }

                if (elapsed >= CrossAxeStart - 6 && elapsed < CrossAxeStart && elapsed % 2 == 0)
                {
                    Glint(PuppetBackHandPosition + PuppetBackWeaponDirection * 50f, 1);
                }

                if (elapsed == CrossTell)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = 0.3f }, NPC.Center);
                }

                if (elapsed == CrossAxeStart)
                {
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = 0f }, NPC.Center);
                }
            }

            if ((elapsed == 30 || elapsed == CrossAxeStart) && Server)
            {
                Snapshot();
            }

            // Two separate strikes 40t apart, each with its own eased push-in and its own hit list.
            int thrust = elapsed - CrossTell;
            int cut = elapsed - CrossAxeStart;
            if (thrust >= 0 && thrust < 10)
            {
                PushIn(ShapedSpeed(CrossThrustStepIn, thrust, 1, 0, 9, 4f));
                _hitWindow = 5;
                _spearLive = true;
            }
            else if (cut >= 0 && cut < CrossAxeTicks)
            {
                PushIn(ShapedSpeed(CrossAxeStepIn, cut, 8, 0, 36, 7f));

                if (cut >= 5 && cut < 15)
                {
                    _hitWindow = 8;
                    _axeLive = true;
                }
            }

            _axeExpert = AxeCutExpert[tier];
            _spearExpert = SpearThrustExpert[tier];
        }

        // Budget the next weapon's own blade reach plus advance delivered by its final live tick.
        // The harmless settling tail adds no reach; damage still requires the existing capsule collision.
        bool CanReachNextBeat(Player target, float reach, float stepIn, int easeIn, int easeOut, int liveEnd, float decay)
        {
            if (target == null || target.dead)
            {
                return false;
            }

            float liveAdvance = 0f;
            for (int i = 0; i < liveEnd; i++)
            {
                liveAdvance += ShapedSpeed(stepIn, i, easeIn, 0, easeOut, decay);
            }

            float distance = NPC.Distance(target.Center);
            return distance <= reach + liveAdvance;
        }

        /// <summary>Eased push-in for one tick of a beat: forward at <paramref name="speed"/>, or stopped by a wall ahead.
        /// Marks the tick as action-owned movement so the neutral walker and the brake leave it alone.</summary>
        void PushIn(float speed)
        {
            Vector2 stepAhead = NPC.position + new Vector2(_lockFacing * 6f, -2f);
            bool wallAhead = Collision.SolidCollision(stepAhead, NPC.width, NPC.height);
            NPC.velocity.X = 0f;
            if (!wallAhead)
            {
                NPC.velocity.X = _lockFacing * speed;
            }

            _pushing = true;
        }

        void TickCharge(int elapsed, Player target)
        {
            if (!Main.dedServ)
            {
                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.5f }, NPC.Center);
                }

                // Dust kicks at her feet for the last 10t of the tell.
                if (elapsed >= ChargeTell - 10 && elapsed < ChargeTell)
                {
                    Vector2 heel = NPC.Bottom + new Vector2(-_lockFacing * 6f, -2f);
                    Dust kick = Dust.NewDustPerfect(heel, DustID.Smoke, new Vector2(-_lockFacing * 1.5f, -0.8f), 100, default, 1f);
                    kick.noGravity = true;
                }

                if (elapsed == ChargeTell)
                {
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f, Pitch = 0.4f }, NPC.Center);
                }
            }

            if (elapsed == ChargeLock && Server)
            {
                Snapshot();
            }

            if (elapsed == ChargeTell && Server)
            {
                _dashDistance = 0f;
                if (target != null)
                {
                    _dashDistance = SolveChargeLane(target);
                }

                bool targetAhead = target != null && (target.Center.X - NPC.Center.X) * _lockFacing > 0f;
                if (!targetAhead || !ChargeLaneCanReachTarget(target, _dashDistance))
                {
                    Trace("charge-lane", "rejected " + (int)_dashDistance + "px", false);
                    AbortIntoRecovery(36);
                    return;
                }

                Trace("charge-lane", "travel " + (int)_dashDistance + "px", false);
                Snapshot();
            }

            int dash = elapsed - ChargeTell;
            if (dash < 0)
            {
                return;
            }

            if (dash >= ChargeDashTicks)
            {
                NPC.velocity.X *= 0.75f;
                return;
            }

            float speed = ShapedSpeed(_dashDistance, dash, 4, 6, 10, 3f);
            NPC.velocity.X = _lockFacing * speed;
            _hitWindow = 6;
            _spearLive = true;
            _spearExpert = ChargeExpert[DamageTier()];

            if (!Main.dedServ && dash % 2 == 0)
            {
                Glint(PuppetSpearTipPosition, 1);
            }

            // A wall the lane check missed (an NPC-shaped gap, a door shut since) ends the dash where it is.
            if (Server)
            {
                Vector2 nextPosition = NPC.position + new Vector2(NPC.velocity.X, -2f);
                if (Collision.SolidCollision(nextPosition, NPC.width, NPC.height))
                {
                    NPC.velocity.X = 0f;
                    Trace("charge-wall", "stopped", false);
                    AbortIntoRecovery(36);
                }
            }
        }

        /// <summary>
        /// Charging Thrust lane, server: travel from her position to 64px past the target's X, cut short at the first
        /// 8px step that is blocked at body height, has no floor within 48px (3 tiles) below, or sits more than 16px
        /// above or below the start. Feet within 32px of the target's. Returns the travel in px (0 = no lane).
        /// </summary>
        float SolveChargeLane(Player target)
        {
            float feetGap = Math.Abs(target.Bottom.Y - NPC.Bottom.Y);
            if (feetGap > 32f)
            {
                return 0f;
            }

            int side = Math.Sign(target.Center.X - NPC.Center.X);
            if (side == 0)
            {
                return 0f;
            }

            float wanted = Math.Abs(target.Center.X - NPC.Center.X) + ChargeOvershoot;
            float travel = 0f;

            for (float distance = 8f; distance <= wanted; distance += 8f)
            {
                Vector2 bodyPosition = NPC.position + new Vector2(side * distance, -2f);
                if (Collision.SolidCollision(bodyPosition, NPC.width, NPC.height))
                {
                    break;
                }

                Vector2 foot = NPC.Bottom + new Vector2(side * distance, 0f);
                bool floorFound = false;
                for (float drop = 2f; drop <= ChargeMaxDrop + 2f; drop += 8f)
                {
                    Point tilePoint = (foot + new Vector2(0f, drop)).ToTileCoordinates();
                    Tile tile = Framing.GetTileSafely(tilePoint);
                    bool solidTop = tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                    if (solidTop)
                    {
                        floorFound = true;
                        break;
                    }
                }

                if (!floorFound)
                {
                    break;
                }

                travel = distance;
            }

            return travel;
        }

        // A short valid floor segment is insufficient if the dash and extended spear still stop before the player.
        // Both selection and release use this check; a late jump or blocked lane earns a harmless recovery.
        bool ChargeLaneCanReachTarget(Player target, float laneDistance)
        {
            if (target == null || target.dead || laneDistance < ChargeMinimumTravel)
            {
                return false;
            }

            float spearReach = SpearShaftTexels * ExtendedGrip * SpearDrawScale;
            float horizontalGap = Math.Abs(target.Center.X - NPC.Center.X);
            return horizontalGap <= laneDistance + spearReach;
        }

        void TickSkyfall(int elapsed, Player target)
        {
            int tier = DamageTier();

            if (!Main.dedServ)
            {
                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.6f }, NPC.Center);
                }

                if (elapsed >= SkyfallTell - 12 && elapsed < SkyfallTell && elapsed % 2 == 0)
                {
                    Glint(PuppetBackHandPosition + PuppetBackWeaponDirection * 50f, 1);
                }
            }

            // Lock the landing 6t before takeoff: vx solved for the flat-ground airtime, 24px past the target.
            if (elapsed == SkyfallLock && Server)
            {
                _leapVelocityX = 0f;
                if (target != null)
                {
                    float airtime = 2f * SkyfallLaunchSpeed / SkyfallGravity;
                    float travel = Math.Abs(target.Center.X - NPC.Center.X) + SkyfallOvershoot;
                    float forward = Math.Min(travel / airtime, SkyfallMaxForward);
                    _leapVelocityX = _lockFacing * forward;
                }

                Trace("skyfall-lock", "vx " + _leapVelocityX.ToString("0.00"), false);
                Snapshot();
            }

            if (elapsed < SkyfallTell)
            {
                return;
            }

            if (elapsed == SkyfallTell)
            {
                NPC.velocity = new Vector2(_leapVelocityX, -SkyfallLaunchSpeed);

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.DoubleJump with { Pitch = -0.2f }, NPC.Center);
                }
            }

            int airTicks = elapsed - SkyfallTell;
            bool airborne = _landTick < 0;

            if (airborne)
            {
                NPC.velocity.X = _leapVelocityX;

                // Swing once past the apex with the target this close; the landing then adds no second hit.
                bool pastApex = airTicks > 2 && NPC.velocity.Y >= 0f;
                bool inRange = target != null && NPC.Distance(target.Center) <= SkyfallStrikeRange;
                if (Server && _strikeStart < 0 && pastApex && inRange)
                {
                    _strikeStart = elapsed;
                    Trace("skyfall-air-strike", "in range", false);
                    Snapshot();
                }

                bool landed = airTicks >= 6 && NPC.velocity.Y == 0f;
                bool timedOut = airTicks >= SkyfallMaxAirTicks;
                if (Server && landed)
                {
                    Land(elapsed, tier);
                }
                else if (Server && timedOut)
                {
                    // Falling beyond the authored airtime is not a landing: do not spawn a floor shock in mid-air.
                    Trace("skyfall-timeout", "abort airborne", false);
                    AbortIntoRecovery(WaltzRecovery);
                    return;
                }
            }
            else
            {
                NPC.velocity.X *= 0.6f;

                // Slow walk after the 14t landing beat.
                int recoveryBegin = SkyfallRecoveryBegin();
                if (recoveryBegin >= 0 && elapsed >= recoveryBegin && target != null)
                {
                    int towardTarget = Math.Sign(target.Center.X - NPC.Center.X);
                    NPC.velocity.X = towardTarget * TopSpeed * 0.4f;
                }
            }

            if (_strikeStart >= 0)
            {
                int strike = elapsed - _strikeStart;
                if (strike == 0 && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.9f, Pitch = -0.4f }, NPC.Center);
                }

                if (strike >= 3 && strike < 12)
                {
                    _hitWindow = 7;
                    _axeLive = true;
                    _axeExpert = SkyfallExpert[tier];
                }
            }
        }

        /// <summary>Server: Skyfall touched down. The strike starts now if it hasn't; the action ends after the later of
        /// the landing beat and the strike, plus the slow walk. Hardmode adds the floor shock.</summary>
        void Land(int elapsed, int tier)
        {
            _landTick = elapsed;
            if (_strikeStart < 0)
            {
                _strikeStart = elapsed;
            }

            int end = SkyfallRecoveryBegin() + SkyfallSlowWalk;
            _actionTotal = end;
            PhaseTimer = _actionTotal - elapsed;

            if (Main.hardMode)
            {
                // One 6t-live floor hitbox spanning 48px either side of her feet; a jump clears it.
                int width = (int)(ShockHalfWidth * 2f) + NPC.width;
                int shockDamage = EnemyDamage.Projectile((int)(SkyfallExpert[tier] * ShockDamageFraction));
                Vector2 center = NPC.Bottom + new Vector2(0f, -8f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), center, Vector2.Zero, ModContent.ProjectileType<PuppetMeleeHitbox>(),
                    shockDamage, 2f, Main.myPlayer, width, 16f, PuppetMeleeHitbox.EncodeHand(PuppetHandSlot.Back));
            }

            Trace("skyfall-land", "strike t" + _strikeStart, false);
            Snapshot();
        }

        void TickJavelin(int elapsed, Player target)
        {
            if (!Main.dedServ)
            {
                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = 0.1f }, NPC.Center);
                }

                // Gold glint at the point for the last 12t of the tell.
                if (elapsed >= JavelinTell - 12 && elapsed < JavelinTell && elapsed % 2 == 0)
                {
                    Glint(PuppetSpearTipPosition, 1);
                }

                if (elapsed == JavelinTell)
                {
                    SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                }

                // The fresh spear gathers gold motes as it fades into the raised hand.
                int rearm = elapsed - JavelinTell - JavelinFollowThrough;
                if (rearm >= 0 && rearm < JavelinRearm && rearm % 3 == 0)
                {
                    Glint(PuppetHandPosition, 1);
                }
            }

            if (elapsed == JavelinLock && Server)
            {
                bool clear = false;
                if (target != null)
                {
                    clear = JavelinArcClear(target, JavelinTell - JavelinLock, out _, out _javelinVelocity);
                }

                if (!clear)
                {
                    Trace("javelin-arc", "blocked", false);
                    AbortIntoRecovery(24);
                    return;
                }

                _javelinTargetVelocity = new Vector2(target.velocity.X, target.velocity.Y * 0.5f);
                _javelinTargetPosition = target.Center + _javelinTargetVelocity * (JavelinTell - JavelinLock);
                Snapshot();
            }

            if (elapsed == JavelinTell)
            {
                Vector2 launchSource = PuppetSpearTipPosition;
                if (Server)
                {
                    // Re-solve from the actual release tip, using only the target motion captured eight ticks ago.
                    // Recompute flight time for that origin without tracking a last-second dodge.
                    _javelinVelocity = TibianValkyrieJavelin.SolveLaunch(launchSource, _javelinTargetPosition,
                        _javelinTargetVelocity, 0, out Vector2 aimPoint);
                    if (!JavelinLaneClear(launchSource, aimPoint, _javelinVelocity))
                    {
                        AbortIntoRecovery(24);
                        return;
                    }
                }

                // The held spear leaves the hand: hidden 12t, then fades back in over the 24t re-arm.
                _spearMissingTicks = JavelinFollowThrough + JavelinRearm;

                if (Server)
                {
                    int damage = EnemyDamage.Projectile(JavelinExpert[DamageTier()]);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), launchSource, _javelinVelocity,
                        ModContent.ProjectileType<TibianValkyrieJavelin>(), damage, 3f, Main.myPlayer);
                    Snapshot();
                }
            }
        }

        // Selection uses a body-relative feasibility probe; the lock uses the real spear tip.
        // Lead horizontal motion fully and vertical motion halfway, so jumping does not project far above the arena.
        bool JavelinArcClear(Player target, int releaseDelay, out Vector2 aimPoint, out Vector2 velocity)
        {
            int side = Math.Sign(target.Center.X - NPC.Center.X);
            if (side == 0)
            {
                side = NPC.direction;
            }

            Vector2 start = NPC.Center + new Vector2(side * 12f, -14f);
            if (Phase == AttackPhase.Custom && _move == Move.Javelin)
            {
                start = PuppetSpearTipPosition;
            }

            Vector2 targetVelocity = new Vector2(target.velocity.X, target.velocity.Y * 0.5f);
            velocity = TibianValkyrieJavelin.SolveLaunch(start, target.Center, targetVelocity, releaseDelay, out aimPoint);
            return JavelinLaneClear(start, aimPoint, velocity);
        }

        // Sample the real 14px projectile box every 4px, including at the intercept, to reject walls and low ceilings.
        bool JavelinLaneClear(Vector2 start, Vector2 aimPoint, Vector2 velocity)
        {
            if (velocity == Vector2.Zero)
            {
                return false;
            }

            float side = Math.Sign(aimPoint.X - start.X);

            Vector2 position = start;
            Vector2 stepVelocity = velocity;
            for (int tick = 0; tick < TibianValkyrieJavelin.MaxFlightTicks; tick++)
            {
                stepVelocity.Y += TibianValkyrieJavelin.Gravity;
                Vector2 nextPosition = position + stepVelocity;
                int sampleCount = Math.Max(1, (int)Math.Ceiling(stepVelocity.Length() / 4f));
                for (int i = 1; i <= sampleCount; i++)
                {
                    Vector2 sample = Vector2.Lerp(position, nextPosition, i / (float)sampleCount);
                    if (Collision.SolidCollision(sample - new Vector2(7f), 14, 14))
                    {
                        return false;
                    }
                }

                position = nextPosition;
                bool pastTarget = side != 0f && (position.X - aimPoint.X) * side >= 0f;
                if (pastTarget)
                {
                    return true;
                }

                if (side == 0f && Vector2.DistanceSquared(position, aimPoint) <= 100f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Skyfall needs open air above for the 150px rise (9.5^2 / 0.6).</summary>
        bool SkyfallHeadroom()
        {
            for (float rise = 16f; rise <= 144f; rise += 16f)
            {
                Vector2 bodyPosition = NPC.position + new Vector2(0f, -rise);
                if (Collision.SolidCollision(bodyPosition, NPC.width, NPC.height))
                {
                    return false;
                }
            }

            return true;
        }

        static void Glint(Vector2 position, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Dust glint = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(3f, 3f), DustID.GoldFlame, Vector2.Zero, 60, default, 1.1f);
                glint.noGravity = true;
            }
        }

        // ── Blade hits (server, after base.AI) ──────────────────────────────────────────────────────────────
        /// <summary>Tests each live hand's capsule against every player not yet hit in this strike. Spear: the head
        /// only (25px back from the drawn point). Axe: 24px out from the hand to its 64px reach.</summary>
        void TickWeaponHits()
        {
            if (_hitWindow != _lastHitWindow)
            {
                _hitPlayers.Clear();
                _spearHasPrevious = false;
                _axeHasPrevious = false;
                _lastHitWindow = _hitWindow;
            }

            if (_hitWindow == 0)
            {
                return;
            }

            if (_spearLive)
            {
                Vector2 tip = PuppetSpearTipPosition;
                Vector2 shaft = PuppetWeaponDirection;
                Vector2 origin = tip - shaft * SpearHeadLength;
                TestCapsule(origin, tip, SpearBladeWidth, PuppetHandSlot.Front, _spearExpert,
                    ref _spearHasPrevious, ref _spearPreviousOrigin, ref _spearPreviousTip);
            }
            else
            {
                _spearHasPrevious = false;
            }

            if (_axeLive)
            {
                Vector2 hand = PuppetBackHandPosition;
                Vector2 haft = PuppetBackWeaponDirection;
                Vector2 origin = hand + haft * AxeHaftSkip;
                Vector2 tip = hand + haft * AxeReach;
                TestCapsule(origin, tip, AxeBladeWidth, PuppetHandSlot.Back, _axeExpert,
                    ref _axeHasPrevious, ref _axePreviousOrigin, ref _axePreviousTip);
            }
            else
            {
                _axeHasPrevious = false;
            }
        }

        // One hand's capsule this tick, swept from last tick's so a fast cut or the dash can't tunnel past a player;
        // on overlap a blade hitbox spawns at the contact point (the base's damage, block and finisher pipeline).
        void TestCapsule(Vector2 origin, Vector2 tip, float width, PuppetHandSlot hand, int expertDamage,
            ref bool hasPrevious, ref Vector2 previousOrigin, ref Vector2 previousTip)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || _hitPlayers.Contains(i))
                {
                    continue;
                }

                if (Vector2.Distance(player.Center, NPC.Center) > 400f)
                {
                    continue;
                }

                Vector2 hitOrigin = origin;
                Vector2 hitTip = tip;
                bool intersects;
                if (hasPrevious)
                {
                    intersects = MeleeBladeCollision.SweptSegmentIntersectsRect(previousOrigin, previousTip, origin, tip, width,
                        player.getRect(), out hitOrigin, out hitTip);
                }
                else
                {
                    intersects = MeleeBladeCollision.SegmentIntersectsRect(origin, tip, width, player.getRect());
                }

                if (!intersects)
                {
                    continue;
                }

                Vector2 hitPoint = MeleeBladeCollision.ClosestPointOnSegment(hitOrigin, hitTip, player.Center);
                int box = (int)width;
                int damage = EnemyDamage.Projectile(expertDamage);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), hitPoint, Vector2.Zero, ModContent.ProjectileType<PuppetMeleeHitbox>(),
                    damage, 3f, Main.myPlayer, box, box, PuppetMeleeHitbox.EncodeHand(hand));
                _hitPlayers.Add(i);

                _beatHit = true;
            }

            previousOrigin = origin;
            previousTip = tip;
            hasPrevious = true;
        }

        public void OnHumanoidMeleeHit(Player target)
        {
            // Confirmed contact only: a small gold and steel puff where the weapon landed.
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                int dustType = DustID.Silver;
                if (i % 2 == 0)
                {
                    dustType = DustID.GoldFlame;
                }

                Dust puff = Dust.NewDustPerfect(target.Center, dustType, Main.rand.NextVector2Circular(3f, 3f), 60, default, 1f);
                puff.noGravity = true;
            }
        }

        // ── Movement ────────────────────────────────────────────────────────────────────────────────────────
        // Neutral: advance via SmartFighter4 (navigating patrol around the give-up point with no target). She never
        // kites or backs off. Actions own their movement (step-ins, dash, leap); every other action tick brakes.
        protected override void RunMovementAI(float speedMult)
        {
            if (ActionOwnsMovement)
            {
                return;
            }

            if (Neutral && _restTicks == 0)
            {
                SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * speedMult, acceleration: Acceleration, doorBreakingDamage: 3, attackRange: 700f);
                return;
            }

            bool skyfallWalk = Phase == AttackPhase.Custom && _move == Move.Skyfall && _landTick >= 0;
            if (skyfallWalk)
            {
                return;
            }

            NPC.velocity.X *= 0.65f;
        }

        // ── Interruption ────────────────────────────────────────────────────────────────────────────────────
        // Only a completed poise break lands here. The interrupted action still counts as exited (cooldown starts).
        // A thrown spear stays thrown: the re-arm counter keeps running.
        public override void OnStagger(NPC npc)
        {
            if (Server && _move != Move.None)
            {
                Trace("poise-interruption", "stagger", false);
                CompleteAction();
            }

            _move = Move.None;
            _hitWindow = 0;
            _spearLive = false;
            _axeLive = false;
            base.OnStagger(npc);
        }

        // ── Drawing (client) ───────────────────────────────────────────────────────────────────────────────
        /// <summary>The held spear's re-arm fade: hidden while it is in flight, then drawn by the base spear path
        /// with its colour scaled 0 -> 1 over the 24t re-arm. The axe draws on its own layer and is unaffected.</summary>
        protected override bool DrawSpecialHeldWeapon(ref PlayerDrawSet drawInfo)
        {
            if (_drawingSpearFade || SpearInHand)
            {
                return false;
            }

            float opacity = 1f - _spearMissingTicks / (float)JavelinRearm;
            if (opacity <= 0f)
            {
                return true;
            }

            int firstEntry = drawInfo.DrawDataCache.Count;
            _drawingSpearFade = true;
            DrawWeaponToLayer(ref drawInfo, DrawHeldWeaponBehindBody);
            _drawingSpearFade = false;

            for (int i = firstEntry; i < drawInfo.DrawDataCache.Count; i++)
            {
                DrawData entry = drawInfo.DrawDataCache[i];
                entry.color *= opacity;
                drawInfo.DrawDataCache[i] = entry;
            }

            return true;
        }

        // ── Spawn, loot and death (carried over from the legacy enemy) ─────────────────────────────────────
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 90;
            NPC.damage = 22;
            NPC.scale = 1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 500;
            NPC.defense = 4;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.TibianValkyrieBanner>();

            if (Main.hardMode)
            {
                NPC.lifeMax = 260;
                NPC.damage = 40;
                NPC.defense = 12;
                NPC.value = 2500;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 700;
                NPC.damage = 70;
                NPC.defense = 30;
                NPC.value = 6000;
            }

            EquipWeapon(PuppetHandSlot.Back, AxeWeapon);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.MaxJumpPower = 9f;
            globalNPC.MaxJumpBoost = 5f;
            globalNPC.NavSearchRadius = 80;
            globalNPC.CanUseRopes = true;
            globalNPC.PatrolUsesNavigation = true;
            globalNPC.PatrolAnchorSource = PatrolAnchorSource.GiveUpLocation;
            globalNPC.PatrolRange = 24;
            globalNPC.PatrolWanderMinTiles = 8;
            globalNPC.PatrolWanderMaxTiles = 16;
            globalNPC.CanTeleport = false;
            globalNPC.TeleportMaxCharges = 0;
            globalNPC.TeleportChargesRemaining = 0;
            globalNPC.EvasiveTeleportAway = false;
            globalNPC.EvasiveQuickStep = false;
            globalNPC.EvasiveRetreatAndShoot = false;
            globalNPC.KiteRangeMin = 0f;
            globalNPC.KiteRangeMax = 0f;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            int[] armorIDs = new int[] {
                ModContent.ItemType<RedClothHat>(),
                ModContent.ItemType<RedClothTunic>(),
                ModContent.ItemType<RedClothPants>(),
            };
            npcLoot.Add(new DropMultiple(armorIDs, 30, 1, !NPC.downedBoss1));

            npcLoot.Add(ItemDropRule.Common(ItemID.Torch, 10, 10, 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ThrowingSpear>(), 1, 20, 75));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<IronShield>(), 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OldHalberd>(), 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.Diamond, 5));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DeadChicken>(), 10));
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 1);
        }

        #region Gore
        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit);

            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }

                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 3").Type, 1f);
                }
            }
        }
        #endregion

        // ── Multiplayer payload: base first, then the subclass fields in a fixed order ──────────────────────
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_phaseTwo);
            writer.Write((byte)_move);
            writer.Write(_actionTotal);
            writer.Write(_abortRecovery);
            writer.Write(_abortSpear);
            writer.Write(_abortGrip);
            writer.Write(_abortAxe);
            writer.Write(_lockedTarget);
            writer.Write(_lockFacing);
            writer.Write(_pitch);
            writer.Write(_waltzFinisher);
            writer.Write(_dashDistance);
            writer.Write(_leapVelocityX);
            writer.Write(_strikeStart);
            writer.Write(_landTick);
            writer.WriteVector2(_javelinVelocity);
            writer.WriteVector2(_javelinTargetPosition);
            writer.WriteVector2(_javelinTargetVelocity);
            writer.Write(_spearMissingTicks);
            writer.Write(_restTicks);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _phaseTwo = reader.ReadBoolean();
            _move = (Move)reader.ReadByte();
            _actionTotal = reader.ReadInt32();
            _abortRecovery = reader.ReadBoolean();
            _abortSpear = reader.ReadSingle();
            _abortGrip = reader.ReadSingle();
            _abortAxe = reader.ReadSingle();
            _lockedTarget = reader.ReadInt32();
            _lockFacing = reader.ReadInt32();
            _pitch = reader.ReadSingle();
            _waltzFinisher = reader.ReadBoolean();
            _dashDistance = reader.ReadSingle();
            _leapVelocityX = reader.ReadSingle();
            _strikeStart = reader.ReadInt32();
            _landTick = reader.ReadInt32();
            _javelinVelocity = reader.ReadVector2();
            _javelinTargetPosition = reader.ReadVector2();
            _javelinTargetVelocity = reader.ReadVector2();
            _spearMissingTicks = reader.ReadInt32();
            _restTicks = reader.ReadInt32();
        }
    }
}
