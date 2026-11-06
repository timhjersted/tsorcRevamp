using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Magic;
using tsorcRevamp.Content.Items.Armor.TibianAmazonSet;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>
    /// Tibian Amazon: a dual-knife rogue. A knife in each fist; she presses in with quick alternating eased cuts and
    /// stabs, pounces on anyone who backs off, throws both knives at range, and back-flips away from anyone who
    /// crowds her, throwing from the air.
    ///
    /// Kit (one server-side AttackBag; every action is one Custom-phase clock):
    ///   Twin Fang Flurry  front slash -> back rising slash -> front overhand stab -> scissor finisher (hit or close only)
    ///   Viper's Kiss      back-hand stab -> front descending slash
    ///   Shadow Pounce     distance-solved leap, both knives driven down once past the apex or on landing
    ///   Twin Toss         throws the front knife, then the back knife 14t later, then draws two fresh ones
    ///   Backflip Toss     reactive (crowded 45t, or hit twice in 60t): hops back and throws from the apex; outside the bag
    /// Both hands are posed by ResolvePose on one clock (front via CustomWeaponRotation, back via
    /// CustomBackWeaponRotation); hits are tested here per hand with the base blade-hitbox projectile.
    /// Multiplayer: picks, locks, the finisher decision, leap/hop solutions, landing and throws are server decisions
    /// synced in Send/ReceiveExtraAI after the base payload; the two knife-away counters are synced so every client hides
    /// and re-shows each knife together. Projectiles and hitboxes spawn on the server; dust and sound on clients.
    /// </summary>
    public class TibianAmazon : PuppetNPC, IHumanoidMeleeHitEffects
    {
        // ── Damage: real Expert damage before defense, converted once by EnemyDamage.Projectile ──────────────
        // Columns: before Eye of Cthulhu / pre-Hardmode / Hardmode / Super Hardmode. Knife = the legacy raw knife x4.
        static readonly int[] SlashExpert = { 28, 36, 90, 160 };
        static readonly int[] StabExpert = { 24, 32, 80, 140 };
        static readonly int[] PounceExpert = { 32, 44, 110, 190 };
        static readonly int[] KnifeExpert = { 24, 32, 80, 140 };

        // ── Knife geometry, measured from the 18x34 point-up art: tip (8.5, 0), grip centre (9, 28.5) ─────────
        // Draw scale 1.1: grip -> tip 28.5 texels = 31px. The capsule runs from 4px out (the guard) to the tip.
        // The art stands vertical, so each weapon turns it +45 degrees onto the swing-space diagonal and the puppet's
        // natural rest angle is 90 degrees: the drawn knife and its hit direction both land where a broadsword would.
        public const float KnifeDrawScale = 1.1f;
        public static readonly Vector2 KnifeHandleNorm = new Vector2(9f / 18f, 28.5f / 34f);
        public const float KnifeRotationOffset = MathHelper.PiOver4;
        const float KnifeReach = 31f;
        const float KnifeGuardSkip = 4f;
        const float KnifeBladeWidth = 12f;

        // ── Swing-space poses: world angle = rotation - 45 degrees facing right; level = pi/4 ─────────────────
        public const float FrontCarry = 0.40f;
        public const float BackCarry = 0.95f;
        public const float LevelRotation = MathHelper.PiOver4;
        public const float FrontHigh = -1.55f;
        public const float FrontLow = 2.10f;
        public const float FrontRiseEnd = -1.40f;
        public const float BackHigh = -1.30f;
        public const float BackLow = 2.25f;
        public const float BackRiseEnd = -1.25f;
        public const float BackScissorEnd = 2.35f;
        public const float PounceFrontEnd = 2.20f;
        public const float BackGuard = 1.40f;
        // Stab: drawn up-back this far above the aim, then driven down onto it.
        public const float StabDrawLift = 1.10f;
        public const float StabSettleDrop = 0.35f;
        // Throw: knife cocked over the shoulder this far above the aim; the empty hand follows through below it.
        public const float ThrowCockLift = 2.00f;
        public const float ThrowFollowDrop = 0.60f;
        // Re-arm: the empty hand reaches down to the belt (pointing down and back) where the fresh knife is drawn.
        public const float BeltReach = 2.70f;
        // Wind-up anticipation: cock this far against the coming swing (65% of the wind-up), then settle on its start.
        const float WindupAnticipation = 0.40f;
        const float StabPitchLimit = 0.35f;
        const float ThrowPitchLimit = 0.9f;

        static readonly Color Steel = new Color(214, 222, 232);

        public enum Move : byte { None, Flurry, Viper, Pounce, Toss, Backflip }

        // ── Twin Fang Flurry (ticks from the action's start) ──────────────────────────────────────────────────
        // Every beat is eased and pushes in; each beat's wind-up runs over the previous beat's tail. Live windows sit
        // 40t apart (last live tick to next first live tick), so a late roller is out of the post-hit immunity and the
        // 8t post-roll gap before the next one goes live, and every beat can land.
        // Tell 28t     front knife raised behind the head over 16t, back knife dropped low; steel glint on the front
        //              knife for the last 10t. Aim tracks until t20.
        // Beat 1 FRONT descending slash -1.55 -> 2.10 (envelope 209 deg, live ~170: short knife cuts by identity).
        //              Weighted 7/0/30 k7 over 37t -> peak 209 / (7/3 + 30 * 0.999 / 7) = 31.6 deg/t (28.2 per tick sampled).
        //              Live while speed >= 30% of peak, as measured: [5, 14) of the step = t33-41 (9t). Push-in 16px.
        // Beat 2 BACK  wind-up t58-75 over beat 1's tail (cocks 0.40 further down-back, settles on 2.25); rising slash
        //              2.25 -> -1.25 (200 deg) from t76, same curve, live t81-89 (9t), 40t after beat 1's. Push-in 16px.
        // Beat 3 FRONT overhand stab: draw t109-128 (the low knife rises up-back over beat 2's tail), strike t129: 6t
        //              cubic drive onto the aim, 4t hold, 12t settle. Live t129-138 (10t), 40t after beat 2's. Push-in 28px.
        // Finisher     decided at t151, BEFORE re-facing: any earlier beat connected, or the target within 72px on the
        //              committed side. Otherwise straight into the 36t recovery (187t total).
        // Beat 4 BOTH  scissor: wind-up t151-172, strike t173: front rises 2.10 -> -1.40 while back falls -1.25 -> 2.35
        //              (200 / 206 deg), Weighted 8/0/32 k7 over 40t -> ~29.5 deg/t. One shared live window t178-187
        //              (10t), 40t after beat 3's. Push-in 24px. Then 36t recovery: 249t (4.2s) total.
        // Counter      four separate rolls standing still (each live <= 10t, >= 40t apart), or back out of 72px after beat 3
        //              and the finisher never fires.
        public const int FlurryTell = 28;
        public const int SlashTicks = 37;
        public const int FlurryCutTwoWindup = 58;
        public const int FlurryCutTwo = 76;
        public const int FlurryStabDraw = 109;
        public const int FlurryStab = 129;
        public const int StabStrikeTicks = 22;
        public const int FlurryDecision = 151;
        public const int FlurryScissor = 173;
        public const int ScissorTicks = 40;
        public const int FlurryScissorEnd = 213;
        public const int FlurryRecovery = 36;
        public const int FlurryTotal = 249;
        const float FlurrySlashStepIn = 16f;
        const float FlurryStabStepIn = 28f;
        const float FlurryScissorStepIn = 24f;
        const float FlurryFinisherRange = 72f;

        // ── Viper's Kiss ──────────────────────────────────────────────────────────────────────────────────────
        // Tell 22t     back knife drawn up-back for the stab, front knife lifted to guard; glint on the back knife last 8t.
        // Beat 1 BACK  stab at t22: 6t cubic drive onto the aim, 4t hold, 12t settle. Live t22-31 (10t). Push-in 24px.
        // Beat 2 FRONT wind-up t45-66 over the stab's settle (front knife raised behind the head), descending slash
        //              -1.55 -> 2.10 from t67, Weighted 7/0/30 k7. Live t72-80 (9t), 41t after the stab's. Push-in 18px.
        // Recovery     30t from t104. Total 134t (2.2s).
        public const int ViperTell = 22;
        public const int ViperSlashWindup = 45;
        public const int ViperSlash = 67;
        public const int ViperRecoveryStart = 104;
        public const int ViperTotal = 134;
        const float ViperStabStepIn = 24f;
        const float ViperSlashStepIn = 18f;

        // ── Shadow Pounce ─────────────────────────────────────────────────────────────────────────────────────
        // Tell 30t     both knives raised overhead, steel glint on both, heel dust for the last 10t. Lock at t24.
        // Leap t30     vy -8, vx solved for the flat-ground airtime (53t) to land 16px past the target, capped at 6px/t
        //              (318px), so the 288px gate is always reachable on flat ground. Apex ~107px.
        // Strike       once past the apex with the target within 64px, or on landing: front -1.55 -> 2.20, back
        //              -1.30 -> 2.35, Weighted 5/0/22 k5 over 27t -> ~32 deg/t; one shared live window [4, 12) = 8t.
        //              One hit per leap (no second hit on landing).
        // Landing      14t crouch beat with both knives low, then a 30t slow walk. Hyper-armor: takeoff to live end.
        public const int PounceTell = 30;
        public const int PounceLock = 24;
        public const int PounceStrikeTicks = 27;
        const float PounceLaunchSpeed = 8f;
        const float PounceGravity = 0.3f;
        const float PounceMaxForward = 6f;
        const float PounceOvershoot = 16f;
        const float PounceStrikeRange = 64f;
        const int PounceMaxAirTicks = 80;
        const int PounceLandingBeat = 14;
        const int PounceSlowWalk = 30;

        // ── Twin Toss ─────────────────────────────────────────────────────────────────────────────────────────
        // Tell 26t     front knife cocked over the shoulder (aim - 2.0) over 14t, glint at its tip for the last 10t; aim
        //              tracks until t20 and locks 6t before release.
        // Release 1    t26: the front knife leaves from its drawn tip at 11px/t, straight, aimed at the target; the
        //              front hand is empty from that tick. Its arm follows through.
        // Release 2    the back knife cocks over t26-39, re-aims at the target until t34, and leaves at t40 (14t later).
        //              One roll timed to the first knife covers both: arrivals +0 / +14t, inside the 22t i-frames.
        // Re-arm       each empty hand reaches to the belt and draws a fresh knife there (front shows at t56, back at
        //              t60, with a steel glint), then returns to carry. Total 82t.
        public const int TossTell = 26;
        public const int TossFrontLock = 20;
        public const int TossBackRelease = 40;
        public const int TossBackLock = 34;
        public const int TossFrontAwayTicks = 30;
        public const int TossBackAwayTicks = 20;
        public const int TossTotal = 82;

        // ── Backflip Toss (reactive, outside the bag) ─────────────────────────────────────────────────────────
        // Trigger      server, neutral and ready: the target within 56px for 45 neutral ticks, or two confirmed hits within
        //              60t. Only when the hop is safe (clear arc, floor within 6 tiles of the landing, no lava).
        // Tell 16t     front knife cocked over the shoulder, back knife to guard; she plants.
        // Hop t16      velocity (-3.6 x facing, -6.6): airtime ~44t, ~158px back, apex ~73px at t38.
        // Throw t38    the front knife leaves from its tip at 11px/t, aim locked at t32; the hand draws a fresh knife at
        //              the belt mid-air (shows at t54). Recovery: 16t after landing, at least to t74.
        // Fairness     the knife's tell is the 22t cock from t16; it crosses a player in ~2t, rollable or side-steppable.
        public const int BackflipTell = 16;
        public const int BackflipThrow = 38;
        public const int BackflipLock = 32;
        public const int BackflipAwayTicks = 16;
        const float BackflipHopForward = 3.6f;
        const float BackflipHopUp = 6.6f;
        const float BackflipCrowdRange = 56f;
        const int BackflipCrowdTicks = 45;
        const int BackflipHitWindow = 60;
        const int BackflipMaxAirTicks = 70;

        // ── Synced state (SendExtraAI / ReceiveExtraAI, in this order) ─────────────────────────────────────────
        bool _phaseTwo;
        Move _move;
        int _actionTotal;
        bool _abortRecovery;
        float _abortFront;
        float _abortBack;
        int _lockedTarget = -1;
        int _lockFacing = 1;
        float _pitch;
        bool _flurryFinisher = true;
        float _leapVelocityX;
        int _strikeStart = -1;
        int _landTick = -1;
        int _frontKnifeAway;
        int _backKnifeAway;
        int _restTicks;

        // ── Server-only selection state (never synced; clients follow the chosen action) ───────────────────────
        AttackBag<Move> _bag;
        readonly Dictionary<Move, int> _cooldowns = new Dictionary<Move, int>();
        Move _lastMove;
        int _age;
        int _neutralTicks;
        int _finisherWaitTicks;
        bool _flurryHit;
        int _crowdTicks;
        int _lastLife = -1;
        int _lastHitAge = -1000;
        int _previousHitAge = -1000;

        // ── Per-tick, recomputed on every machine in DoCustomTick; hit tests run on the server ──────────────────
        // A hit window is one live strike; its id changes between strikes so each strike hits each player once.
        int _hitWindow;
        int _lastHitWindow;
        bool _frontLive;
        bool _backLive;
        int _strikeExpert;
        readonly HashSet<int> _hitPlayers = new HashSet<int>();
        bool _frontHasPrevious;
        Vector2 _frontPreviousOrigin;
        Vector2 _frontPreviousTip;
        bool _backHasPrevious;
        Vector2 _backPreviousOrigin;
        Vector2 _backPreviousTip;
        bool _pushing;

        bool Server => Main.netMode != NetmodeID.MultiplayerClient;
        bool Neutral => Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
        int CustomElapsed => _actionTotal - PhaseTimer;
        bool KnivesInHand => _frontKnifeAway == 0 && _backKnifeAway == 0;

        static readonly PuppetWeapon Knife = new PuppetWeapon(
            ModContent.ItemType<EnemyTibianAmazonKnife>(),
            handleNorm: KnifeHandleNorm,
            drawScale: KnifeDrawScale,
            bladeWidth: KnifeBladeWidth,
            archetype: WeaponArchetype.Dagger,
            reachBase: KnifeReach / 0.7f,
            rotationOffset: KnifeRotationOffset);

        // The same knife with its sprite hidden: swapped into a hand while that hand's knife is in flight, so the
        // dual-wield arm poses stay active with an empty fist.
        static readonly PuppetWeapon ThrownKnife = new PuppetWeapon(
            ModContent.ItemType<EnemyTibianAmazonKnife>(),
            handleNorm: KnifeHandleNorm,
            drawScale: KnifeDrawScale,
            bladeWidth: KnifeBladeWidth,
            archetype: WeaponArchetype.Dagger,
            reachBase: KnifeReach / 0.7f,
            rotationOffset: KnifeRotationOffset,
            hideHeldSprite: true);

        // ── Puppet configuration ────────────────────────────────────────────────────────────────────────────
        protected override string InvaderTitle => "Tibian Amazon";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override int CasualStrollChance => 0;
        protected override int EstusChargesMax => 0;
        protected override int HeadArmorItemType => ModContent.ItemType<TibianAmazonHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<TibianAmazonTunic>();
        protected override int LegsArmorItemType => ModContent.ItemType<TibianAmazonBoots>();
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyTibianAmazonKnife>();
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => EnemyDamage.Projectile(SlashExpert[DamageTier()]);
        protected override int RangedDamage => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override bool HoldNeutralWeaponOnCompositeArm => true;
        protected override bool ShowMeleeWeaponWhileClosingDistance => true;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Dagger;
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

        protected override Vector2 MeleeHandleNorm => KnifeHandleNorm;
        protected override float MeleeWeaponDrawScale => KnifeDrawScale;
        protected override float MeleeBladeWidth => KnifeBladeWidth;
        protected override float ComboReachBase => KnifeReach / 0.7f;
        protected override float MeleeRange => 64f;
        protected override float MeleeEngageRange => 64f;
        // Point-up art: with the per-weapon +45 degree turn, its rest angle is 90 degrees (see Knife).
        protected override float MeleeNaturalRestAngleDeg => 90f;

        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom;
        // Neutral guard: front knife forward and slightly up, back knife forward and slightly down.
        protected override float MeleeCarryRotation => FrontCarry;
        protected override float OffHandCarryRotation => BackCarry;
        // Hand changes are authored per tick by ResolvePose, so the engine's crossfade must not lag behind them.
        protected override int DualWieldHandBlendTicks => 1;
        protected override Color SlashVFXColor => Steel;
        protected override float SlashVFXOpacity => 0.3f;
        protected override bool SuppressSlashVFXForCurrentPhase => Phase == AttackPhase.Custom;

        protected override float TopSpeed => 1.8f;
        protected override float Acceleration => 0.15f;
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
                    case Move.Flurry:
                    case Move.Viper:
                        return _pushing;
                    case Move.Pounce:
                        return elapsed >= PounceTell && _landTick < 0;
                    case Move.Backflip:
                        return elapsed >= BackflipTell && _landTick < 0;
                    default:
                        return false;
                }
            }
        }

        protected override bool SlowDownDuringCustom => !ActionOwnsMovement;

        // Hyper-armor: Shadow Pounce from takeoff to the end of its live window. Nothing else; she is fragile.
        protected override bool CustomAttackCommitted
        {
            get
            {
                if (Phase != AttackPhase.Custom || _abortRecovery || _move != Move.Pounce)
                {
                    return false;
                }

                int elapsed = CustomElapsed;
                bool airborne = elapsed >= PounceTell && _strikeStart < 0;
                bool striking = _strikeStart >= 0 && elapsed < _strikeStart + 12;
                return airborne || striking;
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

                ResolvePose(out float front, out _);
                return front;
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

                ResolvePose(out _, out float back);
                return back;
            }
        }

        static float Smooth(float from, float to, float elapsed, float duration)
        {
            float progress = MathHelper.Clamp(elapsed / duration, 0f, 1f);
            return MathHelper.SmoothStep(from, to, progress);
        }

        /// <summary>Knife stab, ticks from the strike: 6t cubic drive from the drawn pose (aim - 1.10) onto the aim, 4t
        /// hold, then a 12t decaying settle that lets the point drop 0.35 below the aim. Live = drive + hold (10t).
        /// Shared by the Flurry's front stab and Viper's Kiss's back stab.</summary>
        public static float StabPose(int strike, float aim)
        {
            float drawn = aim - StabDrawLift;

            if (strike < 6)
            {
                float drive = (strike + 1) / 6f;
                float eased = drive * drive * drive;
                return MathHelper.Lerp(drawn, aim, eased);
            }

            if (strike < 10)
            {
                return aim;
            }

            float settle = MathHelper.Clamp((strike - 10) / 12f, 0f, 1f);
            float settled = (1f - (float)Math.Exp(-5f * settle)) / (1f - (float)Math.Exp(-5f));
            return MathHelper.Lerp(aim, aim + StabSettleDrop, settled);
        }

        /// <summary>Wind-up into a swing's start pose: from where the knife is, cock against the coming swing by
        /// WindupAnticipation over the first 65%, then settle exactly on the start (zero speed into the strike).
        /// <paramref name="swingSign"/> is the sign of the swing's travel (+1 descending, -1 rising).</summary>
        static float Windup(float from, float start, float swingSign, float elapsed, float duration)
        {
            float cocked = start - swingSign * WindupAnticipation;
            float cockTicks = duration * 0.65f;

            if (elapsed < cockTicks)
            {
                return Smooth(from, cocked, elapsed, cockTicks);
            }

            return Smooth(cocked, start, elapsed - cockTicks, duration - cockTicks);
        }

        /// <summary>
        /// The single source of every Custom pose: front and back knife rotations for the current action and tick. Read
        /// by CustomWeaponRotation and CustomBackWeaponRotation, so the drawn knives and their hit capsules can never
        /// disagree. Aim = _pitch, tracked on every machine until each lock and then the server's synced value.
        /// Public for SwingPreview.
        /// </summary>
        public void ResolvePose(out float front, out float back)
        {
            int elapsed = CustomElapsed;
            front = FrontCarry;
            back = BackCarry;

            if (_abortRecovery)
            {
                // Hold the pose the action stopped in for 6t, then ease both hands to carry over 12t.
                float easeBack = MathHelper.Clamp((elapsed - 6) / 12f, 0f, 1f);
                front = MathHelper.SmoothStep(_abortFront, FrontCarry, easeBack);
                back = MathHelper.SmoothStep(_abortBack, BackCarry, easeBack);
                return;
            }

            float aim = LevelRotation + _pitch;

            switch (_move)
            {
                case Move.Flurry:
                {
                    int recoveryStart = FlurryDecision;
                    if (_flurryFinisher)
                    {
                        recoveryStart = FlurryScissorEnd;
                    }

                    // Front: raise, descending slash, low through beat 2, draw up-back, stab, (rise for the scissor).
                    if (elapsed < FlurryTell)
                    {
                        front = Smooth(FrontCarry, FrontHigh, elapsed, 16f);
                    }
                    else if (elapsed < FlurryTell + SlashTicks)
                    {
                        front = SwingEase.ApplyWeighted(FrontHigh, FrontLow, elapsed - FlurryTell, SlashTicks, 7, 30, 7f);
                    }
                    else if (elapsed < FlurryStabDraw)
                    {
                        front = FrontLow;
                    }
                    else if (elapsed < FlurryStab)
                    {
                        front = Smooth(FrontLow, aim - StabDrawLift, elapsed - FlurryStabDraw, FlurryStab - FlurryStabDraw);
                    }
                    else if (elapsed < FlurryDecision || !_flurryFinisher)
                    {
                        front = StabPose(elapsed - FlurryStab, aim);
                    }
                    else if (elapsed < FlurryScissor)
                    {
                        float stabEnd = aim + StabSettleDrop;
                        front = Windup(stabEnd, FrontLow, -1f, elapsed - FlurryDecision, FlurryScissor - FlurryDecision);
                    }
                    else
                    {
                        front = SwingEase.ApplyWeighted(FrontLow, FrontRiseEnd, elapsed - FlurryScissor, ScissorTicks, 8, 32, 7f);
                    }

                    // Back: low guard, wind-up, rising slash, held high, (falling scissor cut).
                    if (elapsed < FlurryCutTwoWindup)
                    {
                        back = Smooth(BackCarry, BackLow, elapsed, 16f);
                    }
                    else if (elapsed < FlurryCutTwo)
                    {
                        back = Windup(BackLow, BackLow, -1f, elapsed - FlurryCutTwoWindup, FlurryCutTwo - FlurryCutTwoWindup);
                    }
                    else if (elapsed < FlurryCutTwo + SlashTicks)
                    {
                        back = SwingEase.ApplyWeighted(BackLow, BackRiseEnd, elapsed - FlurryCutTwo, SlashTicks, 7, 30, 7f);
                    }
                    else if (elapsed < FlurryDecision || !_flurryFinisher)
                    {
                        back = BackRiseEnd;
                    }
                    else if (elapsed < FlurryScissor)
                    {
                        back = Windup(BackRiseEnd, BackRiseEnd, 1f, elapsed - FlurryDecision, FlurryScissor - FlurryDecision);
                    }
                    else
                    {
                        back = SwingEase.ApplyWeighted(BackRiseEnd, BackScissorEnd, elapsed - FlurryScissor, ScissorTicks, 8, 32, 7f);
                    }

                    if (elapsed >= recoveryStart)
                    {
                        float frontFrom = aim + StabSettleDrop;
                        float backFrom = BackRiseEnd;
                        if (_flurryFinisher)
                        {
                            frontFrom = FrontRiseEnd;
                            backFrom = BackScissorEnd;
                        }

                        front = Smooth(frontFrom, FrontCarry, elapsed - recoveryStart, 24f);
                        back = Smooth(backFrom, BackCarry, elapsed - recoveryStart, 24f);
                    }

                    return;
                }

                case Move.Viper:
                {
                    int backStabEnd = ViperTell + StabStrikeTicks;

                    // Back: drawn up-back through the tell, stab at t22, settle, then back to carry.
                    if (elapsed < ViperTell)
                    {
                        back = Smooth(BackCarry, aim - StabDrawLift, elapsed, 14f);
                    }
                    else if (elapsed < backStabEnd)
                    {
                        back = StabPose(elapsed - ViperTell, aim);
                    }
                    else
                    {
                        back = Smooth(aim + StabSettleDrop, BackCarry, elapsed - backStabEnd, 16f);
                    }

                    // Front: guard through the stab, wind-up behind the head over the stab's settle, descending slash.
                    if (elapsed < ViperSlashWindup)
                    {
                        front = Smooth(FrontCarry, 0f, elapsed, 14f);
                    }
                    else if (elapsed < ViperSlash)
                    {
                        front = Windup(0f, FrontHigh, 1f, elapsed - ViperSlashWindup, ViperSlash - ViperSlashWindup);
                    }
                    else
                    {
                        front = SwingEase.ApplyWeighted(FrontHigh, FrontLow, elapsed - ViperSlash, SlashTicks, 7, 30, 7f);
                    }

                    if (elapsed >= ViperRecoveryStart)
                    {
                        front = Smooth(FrontLow, FrontCarry, elapsed - ViperRecoveryStart, 24f);
                    }

                    return;
                }

                case Move.Pounce:
                {
                    front = Smooth(FrontCarry, FrontHigh, elapsed, 18f);
                    back = Smooth(BackCarry, BackHigh, elapsed, 18f);

                    if (_strikeStart >= 0 && elapsed >= _strikeStart)
                    {
                        int strike = elapsed - _strikeStart;
                        front = SwingEase.ApplyWeighted(FrontHigh, PounceFrontEnd, strike, PounceStrikeTicks, 5, 22, 5f);
                        back = SwingEase.ApplyWeighted(BackHigh, BackScissorEnd, strike, PounceStrikeTicks, 5, 22, 5f);
                    }

                    int recoveryBegin = PounceRecoveryBegin();
                    if (recoveryBegin >= 0 && elapsed >= recoveryBegin)
                    {
                        front = Smooth(PounceFrontEnd, FrontCarry, elapsed - recoveryBegin, 24f);
                        back = Smooth(BackScissorEnd, BackCarry, elapsed - recoveryBegin, 24f);
                    }

                    return;
                }

                case Move.Toss:
                {
                    float cock = aim - ThrowCockLift;
                    float follow = aim + ThrowFollowDrop;
                    int frontRearm = TossTell + 20;
                    int frontSettle = frontRearm + 14;
                    int backRearm = TossBackRelease + 10;
                    int backSettle = backRearm + 14;

                    // Front: cock, release at t26 and follow through, reach to the belt, the fresh knife back to carry.
                    if (elapsed < TossTell)
                    {
                        front = Smooth(FrontCarry, cock, elapsed, 14f);
                    }
                    else if (elapsed < frontRearm)
                    {
                        front = Smooth(cock, follow, elapsed - TossTell, 6f);
                    }
                    else if (elapsed < frontSettle)
                    {
                        front = Smooth(follow, BeltReach, elapsed - frontRearm, 10f);
                    }
                    else
                    {
                        front = Smooth(BeltReach, FrontCarry, elapsed - frontSettle, 16f);
                    }

                    // Back: guard low, cock after the first release, release at t40, belt, carry.
                    if (elapsed < TossTell)
                    {
                        back = Smooth(BackCarry, BackGuard, elapsed, 14f);
                    }
                    else if (elapsed < TossBackRelease)
                    {
                        back = Smooth(BackGuard, cock, elapsed - TossTell, 12f);
                    }
                    else if (elapsed < backRearm)
                    {
                        back = Smooth(cock, follow, elapsed - TossBackRelease, 6f);
                    }
                    else if (elapsed < backSettle)
                    {
                        back = Smooth(follow, BeltReach, elapsed - backRearm, 10f);
                    }
                    else
                    {
                        back = Smooth(BeltReach, BackCarry, elapsed - backSettle, 16f);
                    }

                    return;
                }

                case Move.Backflip:
                {
                    float cock = aim - ThrowCockLift;
                    float follow = aim + ThrowFollowDrop;
                    int rearm = BackflipThrow + 6;
                    int settle = rearm + 14;

                    back = Smooth(BackCarry, BackGuard, elapsed, 10f);
                    if (elapsed < BackflipThrow)
                    {
                        front = Smooth(FrontCarry, cock, elapsed, 14f);
                    }
                    else if (elapsed < rearm)
                    {
                        front = Smooth(cock, follow, elapsed - BackflipThrow, 6f);
                    }
                    else if (elapsed < settle)
                    {
                        front = Smooth(follow, BeltReach, elapsed - rearm, 10f);
                    }
                    else
                    {
                        front = Smooth(BeltReach, FrontCarry, elapsed - settle, 16f);
                        back = Smooth(BackGuard, BackCarry, elapsed - settle, 16f);
                    }

                    return;
                }
            }
        }

        /// <summary>Shadow Pounce's recovery start: the later of the landing beat's end and the strike's end. -1 while
        /// still airborne.</summary>
        int PounceRecoveryBegin()
        {
            if (_landTick < 0)
            {
                return -1;
            }

            int landingBeatEnd = _landTick + PounceLandingBeat;
            int strikeEnd = _strikeStart + PounceStrikeTicks;
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

            // Every machine counts the knife-away timers down from the synced values, so each knife reappears on the
            // same tick everywhere; while one is away its hand holds the hidden twin of the knife.
            if (_frontKnifeAway > 0)
            {
                _frontKnifeAway--;
            }

            if (_backKnifeAway > 0)
            {
                _backKnifeAway--;
            }

            PuppetWeapon frontWeapon = Knife;
            if (_frontKnifeAway > 0)
            {
                frontWeapon = ThrownKnife;
            }

            PuppetWeapon backWeapon = Knife;
            if (_backKnifeAway > 0)
            {
                backWeapon = ThrownKnife;
            }

            EquipWeapon(PuppetHandSlot.Front, frontWeapon);
            EquipWeapon(PuppetHandSlot.Back, backWeapon);

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

        /// <summary>Server, before base.AI: cooldowns, half-health latch, hit and crowding watch, target loss, and the
        /// next pick (Backflip Toss first, then the bag).</summary>
        void ServerPreTick(tsorcRevampGlobalNPC globalNPC)
        {
            foreach (Move move in new[] { Move.Flurry, Move.Viper, Move.Pounce, Move.Toss, Move.Backflip })
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

            // Confirmed hits are life drops seen by the server, never the client-local hit hooks.
            if (_lastLife >= 0 && NPC.life < _lastLife)
            {
                _previousHitAge = _lastHitAge;
                _lastHitAge = _age;
            }

            _lastLife = NPC.life;

            // Below half health latches once: another Pounce and Toss card and a shorter Backflip cooldown (in place
            // of the legacy enrage speed spike).
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

            bool crowded = Neutral && NPC.HasValidTarget && NPC.Distance(Main.player[NPC.target].Center) <= BackflipCrowdRange;
            if (crowded)
            {
                _crowdTicks++;
            }
            else
            {
                _crowdTicks = 0;
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

            if (!ready)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            bool hitTwice = _age - _previousHitAge <= BackflipHitWindow;
            bool crowdedLong = _crowdTicks >= BackflipCrowdTicks;
            bool backflipReady = CooldownReady(Move.Backflip) && _frontKnifeAway == 0;
            if ((hitTwice || crowdedLong) && backflipReady && BackflipHopSafe(target))
            {
                string reason = "crowded " + _crowdTicks + "t";
                if (hitTwice)
                {
                    reason = "hit twice in " + (_age - _previousHitAge) + "t";
                }

                _previousHitAge = -1000;
                _crowdTicks = 0;
                BeginMove(globalNPC, Move.Backflip, target, false, reason);
                return;
            }

            SelectMove(globalNPC, target);
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

            Mod.Logger.Info("AMAZON pick=" + _move + " kind=" + kind + " gate=" + gate + " finisher=" + finisher
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
                    case Move.Flurry:
                        return "Twin Fang Flurry (" + stage + ")";
                    case Move.Viper:
                        return "Viper's Kiss (" + stage + ")";
                    case Move.Pounce:
                        return "Shadow Pounce (" + stage + ")";
                    case Move.Toss:
                        return "Twin Toss (" + stage + ")";
                    default:
                        return "Backflip Toss (" + stage + ")";
                }
            }

            if (!KnivesInHand)
            {
                return "Re-arming";
            }

            return null;
        }

        // ── Selection ───────────────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Server, neutral and ready. The bag deals Flurry x2, Viper x2, Pounce, Toss x2 (+ Pounce, Toss below half
        /// health), skipping ineligible cards without dropping them. An immediate repeat is blocked until a different
        /// move completes or 90t of neutral pass. Once 3 blocks are banked (the unblockable finisher), only the Pounce
        /// may start for up to 120t of neutral; after that the other moves resume, never unblockable. With nothing
        /// eligible she keeps advancing.
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

            bool pounceOnly = finisherReady && _finisherWaitTicks < 120;

            _bag ??= new AttackBag<Move>(cards =>
            {
                cards.Add(Move.Flurry);
                cards.Add(Move.Flurry);
                cards.Add(Move.Viper);
                cards.Add(Move.Viper);
                cards.Add(Move.Pounce);
                cards.Add(Move.Toss);
                cards.Add(Move.Toss);

                if (_phaseTwo)
                {
                    cards.Add(Move.Pounce);
                    cards.Add(Move.Toss);
                }
            });

            bool Eligible(Move move)
            {
                bool repeatBlocked = move == _lastMove && _neutralTicks < 90;
                if (repeatBlocked || !CooldownReady(move) || !KnivesInHand)
                {
                    return false;
                }

                if (pounceOnly && move != Move.Pounce)
                {
                    return false;
                }

                switch (move)
                {
                    case Move.Flurry:
                        return distance <= 72f && verticalGap < 48f;
                    case Move.Viper:
                        return distance <= 64f && verticalGap < 48f;
                    case Move.Pounce:
                        return distance >= 128f && distance <= 288f && verticalGap < 96f && PounceHeadroom();
                    case Move.Toss:
                        return distance >= 160f && distance <= 480f;
                    default:
                        return false;
                }
            }

            if (!_bag.TryDraw(Eligible, out Move chosen))
            {
                return;
            }

            BeginMove(globalNPC, chosen, target, finisherReady, "band " + (int)distance + "px");
        }

        bool CooldownReady(Move move)
        {
            return !_cooldowns.TryGetValue(move, out int remaining) || remaining <= 0;
        }

        /// <summary>Server: face and lock the target, and start the chosen action's Custom phase.</summary>
        void BeginMove(tsorcRevampGlobalNPC globalNPC, Move move, Player target, bool finisherReady, string gate)
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
            _flurryFinisher = true;
            _flurryHit = false;
            _strikeStart = -1;
            _landTick = -1;
            _leapVelocityX = 0f;
            _abortRecovery = false;

            // Only the Pounce may carry the unblockable finisher. The base tags whichever attack starts while blocks are
            // banked; for any other move the threshold is lifted for this one start (the base restores the puppet's
            // value next tick), so the banked blocks wait for the next Pounce.
            bool carriesFinisher = finisherReady && move == Move.Pounce;
            if (move != Move.Pounce)
            {
                globalNPC.GuardPressureFinisherBlocks = int.MaxValue;
            }

            int total = FlurryTotal;
            switch (move)
            {
                case Move.Viper:
                    total = ViperTotal;
                    break;
                case Move.Pounce:
                    total = PounceTell + PounceMaxAirTicks + PounceStrikeTicks + PounceSlowWalk;
                    break;
                case Move.Toss:
                    total = TossTotal;
                    break;
                case Move.Backflip:
                    total = BackflipTell + BackflipMaxAirTicks + 16;
                    break;
            }

            _actionTotal = total;
            StartCustomAttack(total, MeleeWeaponItemType);
            Trace("pick", gate, carriesFinisher);
            Snapshot();
        }

        /// <summary>Server: end the current action's offense and play a harmless recovery of the given length,
        /// starting from the exact pose it stopped in.</summary>
        void AbortIntoRecovery(int recoveryTicks)
        {
            ResolvePose(out float front, out float back);
            _abortFront = front;
            _abortBack = back;
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
                case Move.Flurry:
                    cooldown = 180;
                    break;
                case Move.Viper:
                    cooldown = 120;
                    break;
                case Move.Pounce:
                    cooldown = 300;
                    break;
                case Move.Toss:
                    cooldown = 150;
                    break;
                case Move.Backflip:
                    cooldown = 300;
                    if (_phaseTwo)
                    {
                        cooldown = 240;
                    }

                    break;
            }

            _cooldowns[_move] = cooldown;
            // A 10t neutral planning beat on top of every authored recovery.
            _restTicks = 10;
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
            _frontLive = false;
            _backLive = false;
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

            // Facing and aim follow the target only in each action's tracking windows (tells before their lock, the
            // wind-ups between beats), then hold the locked side and pitch.
            bool tracking = false;
            float pitchLimit = StabPitchLimit;
            switch (_move)
            {
                case Move.Flurry:
                {
                    // Re-face only during each beat's wind-up; the scissor's wind-up starts after its decision tick.
                    bool tell = elapsed < 20;
                    bool cutTwoWindup = elapsed >= FlurryCutTwoWindup && elapsed < FlurryCutTwo;
                    bool stabDraw = elapsed >= FlurryStabDraw && elapsed < FlurryStab;
                    bool scissorWindup = elapsed > FlurryDecision && elapsed < FlurryScissor;
                    tracking = tell || cutTwoWindup || stabDraw || scissorWindup;
                    break;
                }
                case Move.Viper:
                    tracking = elapsed < 16 || (elapsed >= ViperSlashWindup && elapsed < ViperSlash);
                    break;
                case Move.Pounce:
                    tracking = elapsed < PounceLock;
                    break;
                case Move.Toss:
                    tracking = elapsed < TossFrontLock || (elapsed >= TossTell && elapsed < TossBackLock);
                    pitchLimit = ThrowPitchLimit;
                    break;
                case Move.Backflip:
                    tracking = elapsed < BackflipLock;
                    pitchLimit = ThrowPitchLimit;
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
                case Move.Flurry:
                    TickFlurry(elapsed, target);
                    break;
                case Move.Viper:
                    TickViper(elapsed);
                    break;
                case Move.Pounce:
                    TickPounce(elapsed, target);
                    break;
                case Move.Toss:
                    TickToss(elapsed, target);
                    break;
                case Move.Backflip:
                    TickBackflip(elapsed, target);
                    break;
            }
        }

        void TickFlurry(int elapsed, Player target)
        {
            int tier = DamageTier();

            if (!Main.dedServ)
            {
                // Steel glint on the raised front knife for the last 10t of the tell, then on whichever knife winds up next.
                bool tellGlint = elapsed >= FlurryTell - 10 && elapsed < FlurryTell;
                bool stabGlint = elapsed >= FlurryStab - 8 && elapsed < FlurryStab;
                bool frontScissorGlint = _flurryFinisher && elapsed >= FlurryScissor - 8 && elapsed < FlurryScissor;
                if ((tellGlint || stabGlint || frontScissorGlint) && elapsed % 2 == 0)
                {
                    Glint(PuppetHandPosition + PuppetWeaponDirection * KnifeReach, 1);
                }

                bool backGlint = elapsed >= FlurryCutTwo - 8 && elapsed < FlurryCutTwo;
                if ((backGlint || frontScissorGlint) && elapsed % 2 == 0)
                {
                    Glint(PuppetBackHandPosition + PuppetBackWeaponDirection * KnifeReach, 1);
                }

                bool slashStarts = elapsed == FlurryTell || elapsed == FlurryCutTwo || (_flurryFinisher && elapsed == FlurryScissor);
                if (slashStarts)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = 0.35f }, NPC.Center);
                }

                if (elapsed == FlurryStab)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = 0.6f }, NPC.Center);
                }
            }

            // Facing and pitch lock at each strike; the server's value is synced.
            bool strikeLock = elapsed == FlurryCutTwo || elapsed == FlurryStab || elapsed == FlurryScissor;
            if (strikeLock && Server)
            {
                Snapshot();
            }

            // Eased push-in on every beat, shaped like that beat's own speed curve.
            int cutOne = elapsed - FlurryTell;
            int cutTwo = elapsed - FlurryCutTwo;
            int stab = elapsed - FlurryStab;
            int scissor = elapsed - FlurryScissor;
            if (cutOne >= 0 && cutOne < SlashTicks)
            {
                PushIn(ShapedSpeed(FlurrySlashStepIn, cutOne, 7, 0, 30, 7f));
            }
            else if (cutTwo >= 0 && cutTwo < SlashTicks)
            {
                PushIn(ShapedSpeed(FlurrySlashStepIn, cutTwo, 7, 0, 30, 7f));
            }
            else if (stab >= 0 && stab < 10)
            {
                PushIn(ShapedSpeed(FlurryStabStepIn, stab, 1, 0, 9, 4f));
            }
            else if (_flurryFinisher && scissor >= 0 && scissor < ScissorTicks)
            {
                PushIn(ShapedSpeed(FlurryScissorStepIn, scissor, 8, 0, 32, 7f));
            }

            // Finisher decision, before the wind-up re-faces: any beat connected, or the target close and in front.
            if (elapsed == FlurryDecision && Server)
            {
                bool close = false;
                if (target != null)
                {
                    int targetSide = Math.Sign(target.Center.X - NPC.Center.X);
                    close = targetSide == _lockFacing && NPC.Distance(target.Center) <= FlurryFinisherRange;
                }

                _flurryFinisher = _flurryHit || close;
                if (!_flurryFinisher)
                {
                    _actionTotal = FlurryDecision + FlurryRecovery;
                    PhaseTimer = _actionTotal - elapsed;
                }

                Trace("flurry-finisher", "hit " + _flurryHit + " close " + close, false);
                Snapshot();
            }

            // Live windows: slashes while speed is >= 30% of peak; the stab for its drive + hold; the scissor on both hands.
            if (cutOne >= 5 && cutOne < 14)
            {
                _hitWindow = 1;
                _frontLive = true;
                _strikeExpert = SlashExpert[tier];
            }
            else if (cutTwo >= 5 && cutTwo < 14)
            {
                _hitWindow = 2;
                _backLive = true;
                _strikeExpert = SlashExpert[tier];
            }
            else if (stab >= 0 && stab < 10)
            {
                _hitWindow = 3;
                _frontLive = true;
                _strikeExpert = StabExpert[tier];
            }
            else if (_flurryFinisher && scissor >= 5 && scissor < 15)
            {
                _hitWindow = 4;
                _frontLive = true;
                _backLive = true;
                _strikeExpert = SlashExpert[tier];
            }
        }

        void TickViper(int elapsed)
        {
            int tier = DamageTier();

            if (!Main.dedServ)
            {
                if (elapsed >= ViperTell - 8 && elapsed < ViperTell && elapsed % 2 == 0)
                {
                    Glint(PuppetBackHandPosition + PuppetBackWeaponDirection * KnifeReach, 1);
                }

                if (elapsed >= ViperSlash - 8 && elapsed < ViperSlash && elapsed % 2 == 0)
                {
                    Glint(PuppetHandPosition + PuppetWeaponDirection * KnifeReach, 1);
                }

                if (elapsed == ViperTell)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = 0.6f }, NPC.Center);
                }

                if (elapsed == ViperSlash)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = 0.35f }, NPC.Center);
                }
            }

            if ((elapsed == ViperTell || elapsed == ViperSlash) && Server)
            {
                Snapshot();
            }

            int stab = elapsed - ViperTell;
            int slash = elapsed - ViperSlash;
            if (stab >= 0 && stab < 10)
            {
                PushIn(ShapedSpeed(ViperStabStepIn, stab, 1, 0, 9, 4f));
                _hitWindow = 5;
                _backLive = true;
                _strikeExpert = StabExpert[tier];
            }
            else if (slash >= 0 && slash < SlashTicks)
            {
                PushIn(ShapedSpeed(ViperSlashStepIn, slash, 7, 0, 30, 7f));

                if (slash >= 5 && slash < 14)
                {
                    _hitWindow = 6;
                    _frontLive = true;
                    _strikeExpert = SlashExpert[tier];
                }
            }
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

        void TickPounce(int elapsed, Player target)
        {
            int tier = DamageTier();

            if (!Main.dedServ)
            {
                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
                }

                // Both raised knives glint, and dust kicks off her heels, for the last 10t of the tell.
                if (elapsed >= PounceTell - 10 && elapsed < PounceTell)
                {
                    if (elapsed % 2 == 0)
                    {
                        Glint(PuppetHandPosition + PuppetWeaponDirection * KnifeReach, 1);
                        Glint(PuppetBackHandPosition + PuppetBackWeaponDirection * KnifeReach, 1);
                    }

                    Vector2 heel = NPC.Bottom + new Vector2(-_lockFacing * 6f, -2f);
                    Dust kick = Dust.NewDustPerfect(heel, DustID.Smoke, new Vector2(-_lockFacing * 1.5f, -0.8f), 100, default, 1f);
                    kick.noGravity = true;
                }
            }

            // Lock the landing 6t before takeoff: vx solved for the flat-ground airtime, 16px past the target.
            if (elapsed == PounceLock && Server)
            {
                _leapVelocityX = 0f;
                if (target != null)
                {
                    float airtime = 2f * PounceLaunchSpeed / PounceGravity;
                    float travel = Math.Abs(target.Center.X - NPC.Center.X) + PounceOvershoot;
                    float forward = Math.Min(travel / airtime, PounceMaxForward);
                    _leapVelocityX = _lockFacing * forward;
                }

                Trace("pounce-lock", "vx " + _leapVelocityX.ToString("0.00"), false);
                Snapshot();
            }

            if (elapsed < PounceTell)
            {
                return;
            }

            if (elapsed == PounceTell)
            {
                NPC.velocity = new Vector2(_leapVelocityX, -PounceLaunchSpeed);

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.DoubleJump with { Pitch = 0.2f }, NPC.Center);
                }
            }

            int airTicks = elapsed - PounceTell;
            bool airborne = _landTick < 0;

            if (airborne)
            {
                NPC.velocity.X = _leapVelocityX;

                // Strike once past the apex with the target this close; the landing then adds no second hit.
                bool pastApex = airTicks > 2 && NPC.velocity.Y >= 0f;
                bool inRange = target != null && NPC.Distance(target.Center) <= PounceStrikeRange;
                if (Server && _strikeStart < 0 && pastApex && inRange)
                {
                    _strikeStart = elapsed;
                    Trace("pounce-air-strike", "in range", false);
                    Snapshot();
                }

                bool landed = airTicks >= 6 && NPC.velocity.Y == 0f;
                bool timedOut = airTicks >= PounceMaxAirTicks;
                if (Server && (landed || timedOut))
                {
                    _landTick = elapsed;
                    if (_strikeStart < 0)
                    {
                        _strikeStart = elapsed;
                    }

                    _actionTotal = PounceRecoveryBegin() + PounceSlowWalk;
                    PhaseTimer = _actionTotal - elapsed;
                    Trace("pounce-land", "strike t" + _strikeStart, false);
                    Snapshot();
                }
            }
            else
            {
                NPC.velocity.X *= 0.6f;

                // Slow walk after the landing beat.
                int recoveryBegin = PounceRecoveryBegin();
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
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = 0.1f }, NPC.Center);
                }

                if (strike >= 4 && strike < 12)
                {
                    _hitWindow = 7;
                    _frontLive = true;
                    _backLive = true;
                    _strikeExpert = PounceExpert[tier];
                }
            }
        }

        void TickToss(int elapsed, Player target)
        {
            if (!Main.dedServ)
            {
                // Steel glint at the cocked tip for the last 10t before each release.
                bool frontGlint = elapsed >= TossTell - 10 && elapsed < TossTell;
                if (frontGlint && elapsed % 2 == 0)
                {
                    Glint(PuppetHandPosition + PuppetWeaponDirection * KnifeReach, 1);
                }

                bool backGlint = elapsed >= TossBackRelease - 8 && elapsed < TossBackRelease;
                if (backGlint && elapsed % 2 == 0)
                {
                    Glint(PuppetBackHandPosition + PuppetBackWeaponDirection * KnifeReach, 1);
                }

                if (elapsed == TossTell || elapsed == TossBackRelease)
                {
                    SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                }

                // Each fresh knife flashes as it is drawn at the belt.
                if (elapsed == TossTell + TossFrontAwayTicks)
                {
                    Glint(PuppetHandPosition, 3);
                }

                if (elapsed == TossBackRelease + TossBackAwayTicks)
                {
                    Glint(PuppetBackHandPosition, 3);
                }
            }

            if ((elapsed == TossFrontLock || elapsed == TossBackLock) && Server)
            {
                Snapshot();
            }

            if (elapsed == TossTell)
            {
                _frontKnifeAway = TossFrontAwayTicks;
                Vector2 tip = PuppetHandPosition + PuppetWeaponDirection * KnifeReach;
                ThrowKnife(tip, target);
            }

            if (elapsed == TossBackRelease)
            {
                _backKnifeAway = TossBackAwayTicks;
                Vector2 tip = PuppetBackHandPosition + PuppetBackWeaponDirection * KnifeReach;
                ThrowKnife(tip, target);
            }
        }

        void TickBackflip(int elapsed, Player target)
        {
            if (!Main.dedServ)
            {
                bool throwGlint = elapsed >= BackflipThrow - 10 && elapsed < BackflipThrow;
                if (throwGlint && elapsed % 2 == 0)
                {
                    Glint(PuppetHandPosition + PuppetWeaponDirection * KnifeReach, 1);
                }

                if (elapsed == BackflipTell)
                {
                    SoundEngine.PlaySound(SoundID.DoubleJump with { Pitch = 0.4f }, NPC.Center);
                }

                if (elapsed == BackflipThrow)
                {
                    SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                }

                if (elapsed == BackflipThrow + BackflipAwayTicks)
                {
                    Glint(PuppetHandPosition, 3);
                }
            }

            if (elapsed == BackflipLock && Server)
            {
                Snapshot();
            }

            if (elapsed < BackflipTell)
            {
                return;
            }

            if (elapsed == BackflipTell)
            {
                _leapVelocityX = -_lockFacing * BackflipHopForward;
                NPC.velocity = new Vector2(_leapVelocityX, -BackflipHopUp);
            }

            if (elapsed == BackflipThrow)
            {
                _frontKnifeAway = BackflipAwayTicks;
                Vector2 tip = PuppetHandPosition + PuppetWeaponDirection * KnifeReach;
                ThrowKnife(tip, target);
            }

            int airTicks = elapsed - BackflipTell;
            if (_landTick < 0)
            {
                NPC.velocity.X = _leapVelocityX;

                bool landed = airTicks >= 6 && NPC.velocity.Y == 0f;
                bool timedOut = airTicks >= BackflipMaxAirTicks;
                if (Server && (landed || timedOut))
                {
                    _landTick = elapsed;
                    int end = Math.Max(elapsed + 16, BackflipThrow + 36);
                    _actionTotal = end;
                    PhaseTimer = _actionTotal - elapsed;
                    Trace("backflip-land", "end t" + end, false);
                    Snapshot();
                }
            }
            else
            {
                NPC.velocity.X *= 0.6f;
            }
        }

        /// <summary>Server: one TibianAmazonKnife from the drawn tip, straight at the target's current centre (the aim
        /// tracks until 6t before each release, so this is where the tell was pointing). With no target it flies along
        /// the locked facing and pitch.</summary>
        void ThrowKnife(Vector2 tip, Player target)
        {
            if (!Server)
            {
                return;
            }

            float worldPitch = _pitch;
            Vector2 direction = new Vector2(_lockFacing * (float)Math.Cos(worldPitch), (float)Math.Sin(worldPitch));
            if (target != null)
            {
                direction = (target.Center - tip).SafeNormalize(direction);
            }

            Vector2 velocity = direction * TibianAmazonKnife.Speed;
            int damage = EnemyDamage.Projectile(KnifeExpert[DamageTier()]);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), tip, velocity, ModContent.ProjectileType<TibianAmazonKnife>(), damage, 2f, Main.myPlayer);
            Snapshot();
        }

        /// <summary>Shadow Pounce needs open air above for the ~107px rise (8^2 / 0.6).</summary>
        bool PounceHeadroom()
        {
            for (float rise = 16f; rise <= 112f; rise += 16f)
            {
                Vector2 bodyPosition = NPC.position + new Vector2(0f, -rise);
                if (Collision.SolidCollision(bodyPosition, NPC.width, NPC.height))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Backflip Toss hop check, server: the arc away from the target (~158px back, apex ~73px at the midpoint)
        /// must be clear at every 16px, and the landing needs floor within 6 tiles below and no lava there.</summary>
        bool BackflipHopSafe(Player target)
        {
            int away = -1;
            if (target.Center.X < NPC.Center.X)
            {
                away = 1;
            }

            float airtime = 2f * BackflipHopUp / PounceGravity;
            float hopDistance = BackflipHopForward * airtime;
            float apexHeight = BackflipHopUp * BackflipHopUp / (2f * PounceGravity);

            for (float distance = 16f; distance <= hopDistance; distance += 16f)
            {
                float progress = distance / hopDistance;
                float height = 4f * apexHeight * progress * (1f - progress);
                Vector2 bodyPosition = NPC.position + new Vector2(away * distance, -height - 2f);
                if (Collision.SolidCollision(bodyPosition, NPC.width, NPC.height))
                {
                    return false;
                }
            }

            Vector2 landingFoot = NPC.Bottom + new Vector2(away * hopDistance, 0f);
            for (float drop = 0f; drop <= 96f; drop += 8f)
            {
                Point tilePoint = (landingFoot + new Vector2(0f, drop)).ToTileCoordinates();
                Tile tile = Framing.GetTileSafely(tilePoint);
                if (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Lava)
                {
                    return false;
                }

                bool solidTop = tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                if (solidTop)
                {
                    return true;
                }
            }

            return false;
        }

        static void Glint(Vector2 position, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Dust glint = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(3f, 3f), DustID.Silver, Vector2.Zero, 40, default, 1.1f);
                glint.noGravity = true;
            }
        }

        // ── Blade hits (server, after base.AI) ──────────────────────────────────────────────────────────────
        /// <summary>Tests each live hand's knife capsule (4px out from the hand to the 31px tip) against every player not
        /// yet hit in this strike. A both-hands strike shares one hit list, so it lands once per player.</summary>
        void TickWeaponHits()
        {
            if (_hitWindow != _lastHitWindow)
            {
                _hitPlayers.Clear();
                _frontHasPrevious = false;
                _backHasPrevious = false;
                _lastHitWindow = _hitWindow;
            }

            if (_hitWindow == 0)
            {
                return;
            }

            if (_frontLive)
            {
                Vector2 hand = PuppetHandPosition;
                Vector2 blade = PuppetWeaponDirection;
                Vector2 origin = hand + blade * KnifeGuardSkip;
                Vector2 tip = hand + blade * KnifeReach;
                TestCapsule(origin, tip, PuppetHandSlot.Front, ref _frontHasPrevious, ref _frontPreviousOrigin, ref _frontPreviousTip);
            }
            else
            {
                _frontHasPrevious = false;
            }

            if (_backLive)
            {
                Vector2 hand = PuppetBackHandPosition;
                Vector2 blade = PuppetBackWeaponDirection;
                Vector2 origin = hand + blade * KnifeGuardSkip;
                Vector2 tip = hand + blade * KnifeReach;
                TestCapsule(origin, tip, PuppetHandSlot.Back, ref _backHasPrevious, ref _backPreviousOrigin, ref _backPreviousTip);
            }
            else
            {
                _backHasPrevious = false;
            }
        }

        // One hand's capsule this tick, swept from last tick's so a fast cut can't tunnel past a player; on overlap a
        // blade hitbox spawns at the contact point (the base's damage, block and finisher pipeline).
        void TestCapsule(Vector2 origin, Vector2 tip, PuppetHandSlot hand, ref bool hasPrevious, ref Vector2 previousOrigin, ref Vector2 previousTip)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || _hitPlayers.Contains(i))
                {
                    continue;
                }

                if (Vector2.Distance(player.Center, NPC.Center) > 300f)
                {
                    continue;
                }

                Vector2 hitOrigin = origin;
                Vector2 hitTip = tip;
                bool intersects;
                if (hasPrevious)
                {
                    intersects = MeleeBladeCollision.SweptSegmentIntersectsRect(previousOrigin, previousTip, origin, tip, KnifeBladeWidth,
                        player.getRect(), out hitOrigin, out hitTip);
                }
                else
                {
                    intersects = MeleeBladeCollision.SegmentIntersectsRect(origin, tip, KnifeBladeWidth, player.getRect());
                }

                if (!intersects)
                {
                    continue;
                }

                Vector2 hitPoint = MeleeBladeCollision.ClosestPointOnSegment(hitOrigin, hitTip, player.Center);
                int box = (int)KnifeBladeWidth;
                int damage = EnemyDamage.Projectile(_strikeExpert);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), hitPoint, Vector2.Zero, ModContent.ProjectileType<PuppetMeleeHitbox>(),
                    damage, 2f, Main.myPlayer, box, box, PuppetMeleeHitbox.EncodeHand(hand));
                _hitPlayers.Add(i);

                if (_move == Move.Flurry)
                {
                    _flurryHit = true;
                }
            }

            previousOrigin = origin;
            previousTip = tip;
            hasPrevious = true;
        }

        public void OnHumanoidMeleeHit(Player target)
        {
            // Confirmed contact only: a small steel and blood puff where the knife landed.
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                int dustType = DustID.Silver;
                if (i % 2 == 0)
                {
                    dustType = DustID.Blood;
                }

                Dust puff = Dust.NewDustPerfect(target.Center, dustType, Main.rand.NextVector2Circular(3f, 3f), 60, default, 1f);
                puff.noGravity = true;
            }
        }

        // ── Movement ────────────────────────────────────────────────────────────────────────────────────────
        // Neutral: advance via SmartFighter4 (navigating patrol around the give-up point with no target). The old random
        // kiting and evasive steps are gone; Backflip Toss is her one deterministic disengage. Actions own their
        // movement (push-ins, pounce, hop); every other action tick brakes.
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

            bool pounceWalk = Phase == AttackPhase.Custom && _move == Move.Pounce && _landTick >= 0;
            if (pounceWalk)
            {
                return;
            }

            NPC.velocity.X *= 0.65f;
        }

        // ── Interruption ────────────────────────────────────────────────────────────────────────────────────
        // Only a completed poise break lands here. The interrupted action still counts as exited (cooldown starts).
        // A thrown knife stays thrown: the away counters keep running.
        public override void OnStagger(NPC npc)
        {
            if (Server && _move != Move.None)
            {
                Trace("poise-interruption", "stagger", false);
                CompleteAction();
            }

            _move = Move.None;
            _hitWindow = 0;
            _frontLive = false;
            _backLive = false;
            base.OnStagger(npc);
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
            NPC.lifeMax = 100;
            NPC.damage = 20;
            NPC.scale = 1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 450;
            NPC.defense = 2;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.TibianAmazonBanner>();

            if (Main.hardMode)
            {
                NPC.lifeMax = 250;
                NPC.defense = 16;
                NPC.value = 950;
                NPC.damage = 50;
            }

            EquipWeapon(PuppetHandSlot.Front, Knife);
            EquipWeapon(PuppetHandSlot.Back, Knife);

            // Poise (a stagger cancels the wind-up) is tuned centrally in tsorcRevampGlobalNPC.PopulatePoiseProfiles().
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.MaxJumpPower = 9f;
            globalNPC.MaxJumpBoost = 5f;
            globalNPC.NavSearchRadius = 70;
            globalNPC.CanUseRopes = true;
            globalNPC.PatrolMode = NPCs.PatrolMode.Pace;
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

            npcLoot.Add(ItemDropRule.Common(ItemID.Torch, 20, 10, 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.ThrowingKnife, 1, 20, 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DeadChicken>(), 25));
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 1);
        }

        //Spawns on the Surface and into the Underground. Does not spawn in the Dungeon, Hardmode, Meteor, or if there are Town NPCs.

        #region Spawn

        // Legacy fallback for when New Enemy Spawns is off; with it on, the registry (Systems/EnemySpawns.cs) decides.
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            Player p = spawnInfo.Player;
            if (spawnInfo.Invasion || Sky(p) || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneDesert)
            {
                chance = 0;
                return chance;
            }

            if (spawnInfo.Player.townNPCs > 0f || tsorcRevampWorld.SuperHardMode || spawnInfo.Player.ZoneDungeon) return 0f;

            // Locked until the first boss is down. The Vessel of Souls (the Eye of Cthulhu replacement) sets NPC.downedBoss1 on death, so this covers either boss.
            if (!NPC.downedBoss1) return 0f;

            // Amazons don't spawn in the Meteor zone or in evil biomes.
            if (spawnInfo.Player.ZoneMeteor || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) return 0f;

            if (spawnInfo.Player.ZoneOverworldHeight || spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight)
            {
                return 0.0542f;
            }

            return chance;
        }
        #endregion

        #region Gore
        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit);

            for (int i = 0; i < 25; i++)
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
                for (int i = 0; i < 4; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }

                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Amazon Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Amazon Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Amazon Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Amazon Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Amazon Gore 3").Type, 1f);
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
            writer.Write(_abortFront);
            writer.Write(_abortBack);
            writer.Write(_lockedTarget);
            writer.Write(_lockFacing);
            writer.Write(_pitch);
            writer.Write(_flurryFinisher);
            writer.Write(_leapVelocityX);
            writer.Write(_strikeStart);
            writer.Write(_landTick);
            writer.Write(_frontKnifeAway);
            writer.Write(_backKnifeAway);
            writer.Write(_restTicks);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _phaseTwo = reader.ReadBoolean();
            _move = (Move)reader.ReadByte();
            _actionTotal = reader.ReadInt32();
            _abortRecovery = reader.ReadBoolean();
            _abortFront = reader.ReadSingle();
            _abortBack = reader.ReadSingle();
            _lockedTarget = reader.ReadInt32();
            _lockFacing = reader.ReadInt32();
            _pitch = reader.ReadSingle();
            _flurryFinisher = reader.ReadBoolean();
            _leapVelocityX = reader.ReadSingle();
            _strikeStart = reader.ReadInt32();
            _landTick = reader.ReadInt32();
            _frontKnifeAway = reader.ReadInt32();
            _backKnifeAway = reader.ReadInt32();
            _restTicks = reader.ReadInt32();
        }
    }
}
