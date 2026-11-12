using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Enemy.SkeletonSet;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    /// <summary>
    /// Ice Skeleton: a slow, deliberate frost pikeman. It walks forward empty-handed and conjures an ice spear only
    /// for a tell and attack (frosts in over 8t, dissolves 10t after the recovery). It taxes spacing with a reactive
    /// Guard Thrust, and fights with Twin Thrust, Haft Sweep, Hoarfrost Lunge and Lance of Rime.
    /// Frozen Reassembly: the first lethal hit collapses it into a frozen bone pile with its own small HP pool.
    /// Shatter the pile for the real death and loot, or it reforms at 75% HP after 4s.
    ///
    /// Every action is one Custom phase on one clock (ResolvePose is the single pose source). Spear hits arm the base
    /// tracked blade per live window. Multiplayer: picks, locks, the lunge lane, the shard aim and the whole
    /// reassembly (latch, pile HP, timer, reform) are server decisions, synced in Send/ReceiveExtraAI after the base
    /// payload. Shards spawn on the server; dust, mist, glints and the pile draw run on clients.
    /// </summary>
    public class IceSkeleton : SkeletonPuppet, IHumanoidMeleeHitEffects
    {
        // ── Damage: declared Expert damage before defense, converted once by EnemyDamage ──────────────────────
        public const int GuardThrustExpertDamage = 200;
        public const int TwinThrustExpertDamage = 170;
        public const int HaftSweepExpertDamage = 150;
        public const int LungeExpertDamage = 230;
        public const int RimeShardExpertDamage = 150;
        public const int ContactExpertDamage = 40;

        // ── Spear geometry, measured from the 128x128 IceSkeletonSpear art: head (4.5, 2.5) -> butt (123.5, 125.5) ──
        // The shaft is 171.1 texels. Grip fractions run from the head toward the butt (the base spear-grip convention),
        // drawn at scale 1: carry 0.65 = hand 35% up from the butt, 111.2px hand->tip; extended 0.77 = 131.7px (+20);
        // drawn back 0.51 = 87.3px (-24); Lance draw-back 0.42 = 71.9px. The ice head is the first 38px from the point.
        public const float SpearDrawScale = 1f;
        public const float SpearTextureSide = 128f;
        public static readonly Vector2 SpearHeadTexel = new Vector2(4.5f, 2.5f);
        public static readonly Vector2 SpearButtTexel = new Vector2(123.5f, 125.5f);
        public static readonly float ShaftLength = (SpearButtTexel - SpearHeadTexel).Length() * SpearDrawScale;
        // The art's shaft points up-left (-134 degrees, head first); this turns it onto the -45 degree swing-space convention.
        public static readonly float SpearRotationOffset = -MathHelper.PiOver4
            - (float)Math.Atan2(SpearHeadTexel.Y - SpearButtTexel.Y, SpearHeadTexel.X - SpearButtTexel.X);
        public const float CarryGrip = 0.65f;
        public const float ExtendedGrip = 0.77f;
        public const float DrawnGrip = 0.51f;
        public const float LanceGrip = 0.42f;
        const float SpearHeadLength = 38f;
        const float SpearBladeWidth = 12f;

        // ── Swing-space poses: world angle = rotation - 45 degrees facing right; level = pi/4 ─────────────────────
        public const float LevelRotation = MathHelper.PiOver4;
        // Guard visibly dropped in recoveries: the point sinks 0.22 rad below level, which rests the 111px carry
        // reach's point on the floor (hand ~20px above the feet). Steeper drops push the point through the tiles.
        public const float GuardDropRotation = LevelRotation + 0.22f;
        // Twin Thrust tell holds the spear low at the hip, 0.25 rad under the aim.
        const float HipLower = 0.25f;
        const float LungeLower = 0.15f;
        const float LanceLift = -0.12f;
        const float ThrustPitchLimit = 0.35f;
        const float LungePitchLimit = 0.2f;
        const float LancePitchLimit = 0.6f;

        // ── Haft Sweep: a low reap around the body, drawn in side view ────────────────────────────────────────
        // Poses run the long way through straight down: world 150 -> 165 degrees in the tell (dragged back behind the
        // heel), then 165 -> 15 degrees (envelope 150). In side view a horizontal sweep reads as the point staying at
        // shin height, so the reach is solved per frame: reach = clearance / sin(world angle below level), clamped
        // [30, 100]px. Far from vertical the spear is long; through the bottom it is choked short and passes the feet.
        public const float SweepDragFrom = 2.618f + MathHelper.PiOver4;
        public const float SweepStart = 2.880f + MathHelper.PiOver4;
        public const float SweepEnd = 0.262f + MathHelper.PiOver4;
        const float SweepClearance = 20f;
        const float SweepMinReach = 30f;
        const float SweepMaxReach = 100f;

        static readonly Color Frost = new Color(150, 205, 255);

        public enum Move : byte { None, GuardThrust, TwinThrust, HaftSweep, Lunge, Lance }

        // ── Pike Guard -> Guard Thrust (reaction, outside the bag; ticks from the action's start) ─────────────────
        //   Trigger: target 64-128px in front, |dy| < 48, 6 consecutive server ticks, off its 180t cooldown.
        //   Tell 26: spear frosts in levelled (t0-8), drawn back 24px over 18t; ice glint last 10t; aim locks t20.
        //   Strike t26: 6t cubic ease-in extension (87 -> 132px) | 4t hold | 12t decaying retract to carry.
        //   Live [26, 36) = 10t (< 22: one roll). Push-in 16px, eased, gone by the hold's end.
        //   Recovery 40 from t48: point sinks to GuardDropRotation over 10t (the opening to step inside). Total 88.
        public const int GuardTell = 26;
        const int GuardLock = 20;
        public const int GuardTotal = 26 + 22 + 40;
        const float GuardPushIn = 16f;

        // ── Twin Thrust (2 bag cards, 64-150px) ───────────────────────────────────────────────────────────────
        //   Tell 32: spear frosts in low at the hip, drawn back over 18t, rises onto the aim over t20-32; glint last 12t.
        //   Strike 1 t32: extension 6 | hold 4 | live [32, 42). The 12t decaying retract runs straight back past carry
        //   into the drawn pose (the second wind-up overlaps the first retract), then holds; aim re-tracks t54-66.
        //   Strike 2 t72 (40t strike-to-strike): live [72, 82); 30t between live windows, so each rolls separately and
        //   both can land (post-hit immunity is 40t). Push-in 28px eased on each strike.
        //   Retract to carry by t94 | recovery 36: point sinks over 12t. Total 130.
        public const int TwinTell = 32;
        const int TwinLock = 26;
        public const int TwinSecondStrike = 72;
        const int TwinSecondTrackStart = 54;
        const int TwinSecondLock = 66;
        public const int TwinTotal = 72 + 22 + 36;
        const float TwinPushIn = 28f;

        // ── Haft Sweep (1 bag card at <= 56px; or at once when the target crossed behind it within 80px) ─────────
        //   Tell 26: turns to face in the first 12t, spear frosts in dragged low behind the heel (150 -> 165 deg over
        //   20t); frost kicks off the floor.
        //   Sweep t26: Weighted 8/0/36 k7 over 150 deg -> peak 150 x 0.1281 = 19.2 deg/t.
        //   Live while speed >= 30% of peak: 8 + 36 x 1.204 / 7 = 14.2 -> [26, 40) = 14t, ~120 deg of the low arc.
        //   Counter: jump it (the point stays at shin height) or roll (14t < 22). Push-in 12px with the sweep.
        //   Tail to t70 (30t near-still) | recovery 40: rises back to level and to the carry grip. Total 110.
        public const int SweepTell = 26;
        public const int SweepTicks = 44;
        public const int SweepTotal = 26 + 44 + 40;
        const int SweepLiveTicks = 14;
        const float SweepPushIn = 12f;

        // ── Hoarfrost Lunge (1 bag card, ranged start 140-280px with a ground lane; cooldown 300t) ─────────────
        //   Tell 36: spear levelled low, drawn back over 20t; lane locks t24 and faint blue motes rise along the
        //   floor lane over t24-36 (one per 3t); ice glint at the point for the last 12t.
        //   Dash t36: lane re-solved at release (to 48px past the target, cut short at walls and at drops deeper than 3
        //   tiles). ShapedSpeed 4/6/10 k3 over 20t (304px -> peak 29px/t). The spear is live the whole dash: [36, 56).
        //   Hyper-armor during the dash only. Recovery 44: slides to a stop with the spear out 20t, then retracts.
        //   After 3 shield blocks this is the unblockable finisher (red aura over its tell). Total 100.
        public const int LungeTell = 36;
        const int LungeLock = 24;
        public const int LungeDashTicks = 20;
        public const int LungeTotal = 36 + 20 + 44;
        const float LungeOvershoot = 48f;
        const float LungeMinimumTravel = 64f;
        const float LungeMaxDrop = 48f;

        // ── Lance of Rime (1 bag card, 200-520px with line of sight) ──────────────────────────────────────────
        //   Tell 40: spear drawn back to the shoulder (grip slides to 72px); an ice shard forms AT the point and grows
        //   1/4 -> full over t20-40; motes converge on it; aim tracks until t28, then locks; a dust core grows at the point.
        //   Release t40: a 6t thrust (grip to extended) frees the shard from the drawn point. 12px/t, straight.
        //   Reach 200 / 360 / 520px = 17 / 30 / 43t of flight. Retract 10 | recovery 30 (point sinks). Total 86.
        public const int LanceTell = 40;
        const int LanceLock = 28;
        public const int LanceTotal = 40 + 6 + 10 + 30;

        // ── Frozen Reassembly (the shared SkeletonPuppet pile) ────────────────────────────────────────────────
        //   First lethal hit: the pile with 600 HP, frost mist rising; it stands at 75% max HP after 240t + a 30t
        //   reform, then holds 60t before it attacks. A magic killing blow skips the pile.
        const int FrozenPileLife = 600;
        const float FrozenReformLifeFraction = 0.75f;
        const float KnockbackResist = 0.05f;

        const int SpearFormTicks = 8;
        const int SpearDissolveTicks = 10;

        // ── Synced state (SendExtraAI / ReceiveExtraAI, in this order) ─────────────────────────────────────────
        Move _move;
        int _actionTotal;
        bool _abortRecovery;
        float _abortRotation;
        float _abortGrip;
        int _lockedTarget = -1;
        int _lockFacing = 1;
        float _pitch;
        float _lungeDistance;
        bool _lungeFinisher;
        Vector2 _lanceDirection = Vector2.UnitX;
        bool _shardReleased;
        int _restTicks;

        // ── Server-only selection state (never synced; clients follow the chosen action) ───────────────────────
        AttackBag<Move> _bag;
        readonly Dictionary<Move, int> _cooldowns = new Dictionary<Move, int>();
        Move _lastMove;
        int _age;
        int _neutralTicks;
        int _guardZoneTicks;
        int _finisherWaitTicks;
        int _crossCheckTicks;
        int _lastActionFacing = 1;

        // ── Per machine: per-tick live flags, the local spear fade and reassembly transition tracking ──────────
        bool _customLive;
        bool _customArmed;
        int _customExpertDamage;
        bool _wasSpearOut;
        int _dissolveTicks;
        float _dissolveRotation = GuardDropRotation;
        float _dissolveGrip = CarryGrip;
        bool _drawingSpearFade;

        bool Server => Main.netMode != NetmodeID.MultiplayerClient;
        bool Neutral => Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
        int CustomElapsed => _actionTotal - PhaseTimer;
        bool SpearOut => Phase == AttackPhase.Custom && _move != Move.None;

        /// <summary>Spear opacity on every machine: frosts in over the first 8t of an action, solid through tell, live,
        /// tail and recovery (and any aborted recovery), then fades over the 10t local dissolve.</summary>
        float SpearOpacity
        {
            get
            {
                if (SpearOut)
                {
                    if (_abortRecovery)
                    {
                        return 1f;
                    }

                    return MathHelper.Clamp((CustomElapsed + 1) / (float)SpearFormTicks, 0f, 1f);
                }

                return _dissolveTicks / (float)SpearDissolveTicks;
            }
        }

        // ── Puppet configuration ─────────────────────────────────────────────────────────────────────────────
        protected override string InvaderTitle => "Adept of Seath";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override int CasualStrollChance => 0;
        protected override int EstusChargesMax => 0;
        // Bright Sky Blue: pale ice-blue bones (picked from the side-by-side dye render), and blue eye glow through the
        // shared skeleton eye-glow layer, which tints with the head dye.
        protected override int HeadArmorDyeItemType => ItemID.BrightSkyBlueDye;
        protected override int BodyArmorDyeItemType => ItemID.BrightSkyBlueDye;
        protected override int LegsArmorDyeItemType => ItemID.BrightSkyBlueDye;
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyIceSkeletonSpear>();
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => EnemyDamage.Projectile(GuardThrustExpertDamage);
        protected override int RangedDamage => 0;
        protected override bool HoldAttackSelection => true;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Halberd;
        protected override MeleeCombo[] MeleeComboPoolOverride => InertPool;
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction) => false;
        protected override void DoMeleeAttack() { }
        protected override void DoRangedAttack() { }
        protected override int OnKillDustType => DustID.BlueCrystalShard;
        protected override int OnKillDustCount => 70;

        // The combo engine is never used (HoldAttackSelection, nothing selectable); the pool only satisfies the base.
        static readonly MeleeCombo[] InertPool =
        {
            new MeleeCombo
            {
                Name = "Inert", BaseWeight = 0, Preferred = ComboRangeBand.Any,
                Steps = new[] { new MeleeComboStep { Motion = ComboMotion.OverheadArc, AttackTicks = 1, DamageMult = 0f, ReachMult = 1f, SwingSpeedMult = 1f } },
            },
        };

        // Hands are empty in neutral. The spear only exists for an action, plus the 10t dissolve after it, which holds
        // the last pose on the composite arm while it fades.
        protected override bool ShowWeaponDuringNeutral => _dissolveTicks > 0;
        protected override bool ShowMeleeWeaponWhileClosingDistance => _dissolveTicks > 0;
        protected override bool HoldNeutralWeaponOnCompositeArm => true;
        protected override float MeleeCarryRotation => _dissolveRotation;

        protected override bool DrawWeaponAsSpear => true;
        protected override bool MirrorSpearRotationByFacing => true;
        protected override string SpearDrawTexturePath => EnemyIceSkeletonSpear.ArtPath;
        protected override Vector2 SpearTextureSize => new Vector2(SpearTextureSide, SpearTextureSide);
        protected override Vector2 SpearHeadNorm => SpearHeadTexel / SpearTextureSide;
        protected override Vector2 SpearBaseNorm => SpearButtTexel / SpearTextureSide;
        protected override float SpearDrawRotationOffset => SpearRotationOffset;
        protected override float MeleeWeaponDrawScale => SpearDrawScale;
        protected override float MeleeBladeWidth => SpearBladeWidth;
        // Only an early-out radius for the blade test; the capsule itself is set in ModifyFrontBladeCapsule.
        protected override float ComboReachBase => 200f;
        protected override float MeleeRange => 128f;
        protected override float MeleeEngageRange => 128f;

        protected override bool UseCompositeArmSwing => true;
        // Two-handed pike grip on the base offset (far hand 4px up the shaft from the near hand). A grip behind the
        // near hand is out of the far arm's reach on a levelled spear: the preview solve misses it by 11-19px.
        // The Haft Sweep is one-handed: its back-low poses put the haft behind the body, out of the far arm's reach.
        protected override bool UseTwoHandedCompositeSwing => SpearOut && _move != Move.HaftSweep;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom;
        protected override bool SuppressSlashVFXForCurrentPhase => Phase == AttackPhase.Custom;

        protected override float TopSpeed => 1.8f;
        protected override float Acceleration => 0.06f;
        protected override float PuppetJumpPower => 9f;
        protected override float PuppetJumpBoost => 5f;

        protected override bool HasUnblockableBodyAura => SpearOut && _move == Move.Lunge && _lungeFinisher
            && !_abortRecovery && CustomElapsed < LungeTell;

        /// <summary>True on the ticks the action itself drives the body: thrust and sweep push-ins, and the lunge from
        /// release through its slide. Pure timeline, so it does not depend on DoCustomTick having run first.</summary>
        bool ActionOwnsMovement
        {
            get
            {
                if (!SpearOut || _abortRecovery)
                {
                    return false;
                }

                int elapsed = CustomElapsed;
                switch (_move)
                {
                    case Move.GuardThrust:
                        return elapsed >= GuardTell && elapsed < GuardTell + 10;
                    case Move.TwinThrust:
                    {
                        bool firstPush = elapsed >= TwinTell && elapsed < TwinTell + 10;
                        bool secondPush = elapsed >= TwinSecondStrike && elapsed < TwinSecondStrike + 10;
                        return firstPush || secondPush;
                    }
                    case Move.HaftSweep:
                        return elapsed >= SweepTell && elapsed < SweepTell + SweepTicks;
                    case Move.Lunge:
                        return elapsed >= LungeTell;
                    default:
                        return false;
                }
            }
        }

        protected override bool SlowDownDuringCustom => !ActionOwnsMovement;

        // Hyper-armor only while the lunge is travelling; tells, thrusts and recoveries stay poise-breakable.
        protected override bool CustomAttackCommitted => SpearOut && _move == Move.Lunge && !_abortRecovery
            && CustomElapsed >= LungeTell && CustomElapsed < LungeTell + LungeDashTicks;

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (_move == Move.None)
                {
                    return null;
                }

                ResolvePose(out float rotation, out _);
                return rotation;
            }
        }

        protected override float? OverrideSpearGrip(float swingClockT)
        {
            if (SpearOut)
            {
                ResolvePose(out _, out float grip);
                return grip;
            }

            return _dissolveGrip;
        }

        // Thrusts and the lunge connect with the ice head only (38px back from the drawn point). The sweep is a haft
        // swing: everything from 8px out of the hand to the point.
        protected override void ModifyFrontBladeCapsule(ref Vector2 origin, ref Vector2 tip)
        {
            Vector2 point = PuppetSpearTipPosition;
            Vector2 shaftDirection = (point - origin).SafeNormalize(new Vector2(NPC.direction, 0f));

            if (_move == Move.HaftSweep)
            {
                origin += shaftDirection * 8f;
            }
            else
            {
                origin = point - shaftDirection * SpearHeadLength;
            }

            tip = point;
        }

        /// <summary>Spear grip over one thrust, from its strike tick: 6t cubic ease-in extension from
        /// <paramref name="from"/> to ExtendedGrip (the point accelerates into full reach), 4t hold, then a 12t
        /// decaying retract (k5) onto <paramref name="to"/>. Live = extension + hold.</summary>
        static float ThrustGrip(int strike, float from, float to)
        {
            if (strike < 6)
            {
                float extension = (strike + 1) / 6f;
                float eased = extension * extension * extension;
                return MathHelper.Lerp(from, ExtendedGrip, eased);
            }

            if (strike < 10)
            {
                return ExtendedGrip;
            }

            float retract = MathHelper.Clamp((strike - 10) / 12f, 0f, 1f);
            float settled = (1f - (float)Math.Exp(-5f * retract)) / (1f - (float)Math.Exp(-5f));
            return MathHelper.Lerp(ExtendedGrip, to, settled);
        }

        static float Smooth(float from, float to, float elapsed, float duration)
        {
            float progress = MathHelper.Clamp(elapsed / duration, 0f, 1f);
            return MathHelper.SmoothStep(from, to, progress);
        }

        /// <summary>
        /// The single source of every spear pose: swing-space rotation and grip fraction for the current action and
        /// tick. Read by CustomWeaponRotation and OverrideSpearGrip, so the drawn spear and its hit capsule can never
        /// disagree. Aim = _pitch, tracked on every machine until the action's lock, then the server's synced value.
        /// Public for SwingPreview, which calls it on an uninitialized instance.
        /// </summary>
        public void ResolvePose(out float rotation, out float grip)
        {
            int elapsed = CustomElapsed;
            float aim = LevelRotation + _pitch;
            rotation = aim;
            grip = CarryGrip;

            if (_abortRecovery)
            {
                // Hold the pose the action stopped in for 6t, then sink the guard and settle the grip over 12t.
                float easeBack = MathHelper.Clamp((elapsed - 6) / 12f, 0f, 1f);
                rotation = MathHelper.SmoothStep(_abortRotation, GuardDropRotation, easeBack);
                grip = MathHelper.SmoothStep(_abortGrip, CarryGrip, easeBack);
                return;
            }

            switch (_move)
            {
                case Move.GuardThrust:
                {
                    if (elapsed < GuardTell)
                    {
                        grip = Smooth(CarryGrip, DrawnGrip, elapsed, 18f);
                        return;
                    }

                    int strike = elapsed - GuardTell;
                    grip = ThrustGrip(strike, DrawnGrip, CarryGrip);

                    if (strike >= 22)
                    {
                        rotation = Smooth(aim, GuardDropRotation, strike - 22, 10f);
                    }

                    return;
                }

                case Move.TwinThrust:
                {
                    if (elapsed < TwinTell)
                    {
                        rotation = Smooth(aim + HipLower, aim, elapsed - 20, 12f);
                        grip = Smooth(CarryGrip, DrawnGrip, elapsed, 18f);
                        return;
                    }

                    if (elapsed < TwinSecondStrike)
                    {
                        // Strike 1, whose retract pulls straight back into strike 2's drawn pose and holds it.
                        grip = ThrustGrip(elapsed - TwinTell, DrawnGrip, DrawnGrip);
                        return;
                    }

                    int second = elapsed - TwinSecondStrike;
                    grip = ThrustGrip(second, DrawnGrip, CarryGrip);

                    if (second >= 22)
                    {
                        rotation = Smooth(aim, GuardDropRotation, second - 22, 12f);
                    }

                    return;
                }

                case Move.HaftSweep:
                {
                    int sweep = elapsed - SweepTell;
                    if (sweep < 0)
                    {
                        rotation = Smooth(SweepDragFrom, SweepStart, elapsed, 20f);
                    }
                    else if (sweep < SweepTicks)
                    {
                        rotation = SwingEase.ApplyWeighted(SweepStart, SweepEnd, sweep, SweepTicks, 8, 36, 7f);
                    }
                    else
                    {
                        rotation = Smooth(SweepEnd, GuardDropRotation, sweep - SweepTicks, 20f);
                    }

                    // Point held at shin height: solve the reach from how far below level the shaft points.
                    float belowLevel = rotation - MathHelper.PiOver4;
                    float sine = (float)Math.Sin(belowLevel);
                    float reach = SweepMaxReach;
                    if (sine > 0.01f)
                    {
                        reach = MathHelper.Clamp(SweepClearance / sine, SweepMinReach, SweepMaxReach);
                    }

                    grip = reach / ShaftLength;

                    // Recovery: slide back up to the carry grip as the shaft rises to the dropped guard.
                    if (sweep >= SweepTicks)
                    {
                        grip = Smooth(grip, CarryGrip, sweep - SweepTicks, 20f);
                    }

                    return;
                }

                case Move.Lunge:
                {
                    rotation = aim + LungeLower;

                    if (elapsed < LungeTell)
                    {
                        grip = Smooth(CarryGrip, DrawnGrip, elapsed, 20f);
                        return;
                    }

                    int dash = elapsed - LungeTell;
                    grip = Smooth(DrawnGrip, ExtendedGrip, dash, 4f);

                    // Slide to a stop with the point still out for 20t, then retract and sink the guard.
                    int afterDash = dash - LungeDashTicks - 20;
                    if (afterDash >= 0)
                    {
                        grip = Smooth(ExtendedGrip, CarryGrip, afterDash, 12f);
                        rotation = Smooth(aim + LungeLower, GuardDropRotation, afterDash, 12f);
                    }

                    return;
                }

                case Move.Lance:
                {
                    rotation = aim + LanceLift;

                    if (elapsed < LanceTell)
                    {
                        grip = Smooth(CarryGrip, LanceGrip, elapsed, 20f);
                        return;
                    }

                    int thrust = elapsed - LanceTell;
                    if (thrust < 6)
                    {
                        float extension = (thrust + 1) / 6f;
                        grip = MathHelper.Lerp(LanceGrip, ExtendedGrip, extension * extension * extension);
                        return;
                    }

                    grip = Smooth(ExtendedGrip, CarryGrip, thrust - 6, 10f);

                    if (thrust >= 16)
                    {
                        rotation = Smooth(aim + LanceLift, GuardDropRotation, thrust - 16, 12f);
                    }

                    return;
                }
            }
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

        // ── Per-tick AI ──────────────────────────────────────────────────────────────────────────────────────
        // Skipped while collapsed (SkeletonPuppet.AI), so _age and _restTicks pause in the pile.
        protected override void SkeletonAI()
        {
            _age++;

            if (_restTicks > 0)
            {
                _restTicks--;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (Server)
            {
                ServerPreTick(globalNPC);
            }

            RunPuppetAI();

            // Spear hits sample AFTER base.AI, so the capsule uses this tick's visible pose. Each live window arms
            // the tracked blade afresh (one hit per player per window).
            if (Phase == AttackPhase.Custom && _customLive && Server)
            {
                if (!_customArmed)
                {
                    float damageMult = _customExpertDamage / (float)GuardThrustExpertDamage;
                    DoComboMeleeHit(new MeleeComboStep { DamageMult = damageMult, ReachMult = 1f });
                    _customArmed = true;
                }

                TickBladeHit();
            }
            else if (_customArmed)
            {
                StopMeleeHit();
                _customArmed = false;
            }

            if (Server)
            {
                if (SpearOut && !_abortRecovery)
                {
                    bool targetLost = _lockedTarget < 0 || _lockedTarget >= Main.maxPlayers
                        || !Main.player[_lockedTarget].active || Main.player[_lockedTarget].dead;

                    if (targetLost)
                    {
                        Trace("target-lost");
                        AbortIntoRecovery(36);
                    }
                }

                if (Neutral && _move != Move.None)
                {
                    CompleteAction();
                }
            }

            globalNPC.AttackCommitted = CustomAttackCommitted;
            globalNPC.AttackTelegraphing = !Neutral;

            // Local spear fade. While the spear is out, remember its pose; the tick it goes away, start the dissolve
            // from exactly that pose (held on the composite arm through MeleeCarryRotation).
            bool spearOut = SpearOut;
            if (spearOut)
            {
                ResolvePose(out _dissolveRotation, out _dissolveGrip);
                _dissolveTicks = 0;
            }
            else if (_wasSpearOut)
            {
                _dissolveTicks = SpearDissolveTicks;
            }
            else if (_dissolveTicks > 0)
            {
                _dissolveTicks--;
            }

            _wasSpearOut = spearOut;

            if (!Main.dedServ)
            {
                // Spear frosting in: motes converge on the shaft from both sides, and a glint lands on the point at t8.
                if (spearOut && !_abortRecovery && CustomElapsed < SpearFormTicks)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float along = Main.rand.NextFloat(0.1f, 1f);
                        Vector2 onShaft = Vector2.Lerp(PuppetHandPosition, PuppetSpearTipPosition, along);
                        Vector2 side = PuppetWeaponDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-14f, 14f);
                        Dust mote = Dust.NewDustPerfect(onShaft + side, DustID.IceTorch, -side * 0.12f, 80, default, 1f);
                        mote.noGravity = true;
                    }
                }

                if (spearOut && !_abortRecovery && CustomElapsed == SpearFormTicks)
                {
                    Dust glint = Dust.NewDustPerfect(PuppetSpearTipPosition, DustID.IceTorch, Vector2.Zero, 0, Color.White, 1.6f);
                    glint.noGravity = true;
                }

                // Dissolving: the shaft sheds falling frost motes.
                if (_dissolveTicks > 0)
                {
                    float along = Main.rand.NextFloat(0f, 1f);
                    Vector2 onShaft = Vector2.Lerp(PuppetHandPosition, PuppetSpearTipPosition, along);
                    Dust mote = Dust.NewDustPerfect(onShaft, DustID.Snow, new Vector2(0f, 0.6f), 80, default, 0.9f);
                    mote.noGravity = false;
                }

                // Pike Guard: frost mist trails off its empty hands while it advances on a target.
                if (Neutral && NPC.HasValidTarget && _dissolveTicks == 0 && _age % 5 == 0)
                {
                    Vector2 drift = new Vector2(-NPC.direction * 0.4f, -0.3f);
                    Dust mist = Dust.NewDustPerfect(PuppetHandPosition, DustID.IceTorch, drift, 140, default, 0.8f);
                    mist.noGravity = true;
                }
            }

            string label = null;
            if (SpearOut)
            {
                string stage = "t" + CustomElapsed;
                if (_abortRecovery)
                {
                    stage = "aborted";
                }

                label = _move switch
                {
                    Move.GuardThrust => "Guard Thrust (" + stage + ")",
                    Move.TwinThrust => "Twin Thrust (" + stage + ")",
                    Move.HaftSweep => "Haft Sweep (" + stage + ")",
                    Move.Lunge => "Hoarfrost Lunge (" + stage + ")",
                    _ => "Lance of Rime (" + stage + ")",
                };
            }
            else if (Neutral && NPC.HasValidTarget)
            {
                label = "Pike Guard";
            }

            DebugAttackLabel = label;
        }

        // ── Frozen Reassembly: the Ice Skeleton's values and cues for the shared pile ─────────────────────────
        protected override int PileLife => FrozenPileLife;
        protected override float ReformLifeFraction => FrozenReformLifeFraction;
        protected override float StandingKnockbackResist => KnockbackResist;
        protected override string ReassemblyName => "Frozen Reassembly";
        protected override Color PileEyeColor => Frost;

        protected override Color TintPile(Color drawColor)
        {
            return Color.Lerp(drawColor, Frost, 0.25f);
        }

        /// <summary>Every machine while collapsed: the spear went down with it (drawn in the pile), so no dissolve plays
        /// after it stands again. Clients add the frost light, rising mist, the crystal burst and the reform pull.</summary>
        protected override void TickCollapsed()
        {
            _wasSpearOut = false;
            _dissolveTicks = 0;

            if (Main.dedServ)
            {
                return;
            }

            Reassembly phase = ReassemblyPhase;
            int timer = ReassemblyTimer;
            Vector2 pileCenter = NPC.Bottom + new Vector2(0f, -6f);
            Lighting.AddLight(pileCenter, new Vector3(0.15f, 0.3f, 0.45f));

            // Mist rises off the pile; thicker once the reform tell starts.
            int mistInterval = 6;
            if (timer >= TwitchStart)
            {
                mistInterval = 3;
            }

            if (phase == Reassembly.Pile && timer % mistInterval == 0)
            {
                Vector2 spot = pileCenter + new Vector2(Main.rand.NextFloat(-18f, 18f), 0f);
                Dust mist = Dust.NewDustPerfect(spot, DustID.IceTorch, new Vector2(0f, -0.8f), 150, default, 1f);
                mist.noGravity = true;
            }

            // Collapse burst: blue crystal shards for the first 4t.
            if (phase == Reassembly.Pile && timer <= 4)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f) + new Vector2(0f, -1.5f);
                    Dust.NewDustPerfect(NPC.Center, DustID.BlueCrystalShard, velocity, 40, default, 1.2f);
                }
            }

            // Reform: ice drawn inward from around the pile into the standing frame.
            if (phase == Reassembly.Reform)
            {
                Vector2 from = NPC.Center + Main.rand.NextVector2CircularEdge(40f, 40f);
                Dust pull = Dust.NewDustPerfect(from, DustID.IceTorch, (NPC.Center - from) * 0.08f, 60, default, 1.2f);
                pull.noGravity = true;
            }
        }

        protected override void PlayReassemblyCue(Reassembly from, Reassembly to)
        {
            if (to == Reassembly.Pile)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath2 with { Pitch = -0.2f }, NPC.Center);
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.8f }, NPC.Center);
            }

            if (to == Reassembly.Reform)
            {
                SoundEngine.PlaySound(SoundID.Item30 with { Pitch = -0.4f }, NPC.Center);
            }

            if (to == Reassembly.None && from == Reassembly.Reform)
            {
                // Eyes flare and the frame snaps together in a ring of ice.
                SoundEngine.PlaySound(SoundID.Item28 with { Pitch = -0.3f }, NPC.Center);
                for (int i = 0; i < 30; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(3.5f, 3.5f);
                    Dust ring = Dust.NewDustPerfect(NPC.Center, DustID.IceTorch, velocity, 60, default, 1.4f);
                    ring.noGravity = true;
                }
            }
        }

        // Server, on the first death: the spear goes down with it, mid-action or not.
        protected override void OnCollapse()
        {
            _customArmed = false;
            _customLive = false;

            if (_move != Move.None)
            {
                CompleteAction();
            }
        }

        /// <summary>Server, before base.AI: cooldowns, the post-reform hold, and the next pick.</summary>
        void ServerPreTick(tsorcRevampGlobalNPC globalNPC)
        {
            foreach (Move move in new[] { Move.GuardThrust, Move.TwinThrust, Move.HaftSweep, Move.Lunge, Move.Lance })
            {
                if (_cooldowns.TryGetValue(move, out int remaining) && remaining > 0)
                {
                    _cooldowns[move] = remaining - 1;
                }
            }

            if (_crossCheckTicks > 0)
            {
                _crossCheckTicks--;
            }

            if (!NPC.HasValidTarget)
            {
                NPC.TargetClosest();
            }

            if (Neutral)
            {
                _neutralTicks++;
            }
            else
            {
                _neutralTicks = 0;
                _guardZoneTicks = 0;
            }

            bool grounded = NPC.velocity.Y == 0f;
            bool ready = Neutral && NPC.HasValidTarget && grounded && _restTicks == 0 && PostReformTicks == 0
                && globalNPC.StaggerTimer == 0 && _age >= 30 && !InFinisherPunishWindow
                && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height);

            if (!ready)
            {
                _guardZoneTicks = 0;
                return;
            }

            SelectMove(globalNPC, Main.player[NPC.target]);
        }

        // ── Selection ──────────────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Server, neutral and ready. In order:
        ///   1. Crossed up: the target is behind it (relative to its facing, or to the facing of the action it just
        ///      finished, for 30t) within 80px -> Haft Sweep at once, ignoring its cooldown.
        ///   2. Pike Guard: the target has stood 64-128px in front for 6 consecutive ticks -> Guard Thrust (180t cd).
        ///   3. After a 24t deliberate beat of neutral, the bag deals Twin x2, Sweep, Lunge, Lance, skipping ineligible
        ///      cards without dropping them; an immediate repeat is blocked until another move or 90t of neutral.
        /// Once 3 blocks are banked (the unblockable finisher), only the Lunge may start, for up to 120t of neutral.
        /// </summary>
        void SelectMove(tsorcRevampGlobalNPC globalNPC, Player target)
        {
            float horizontalGap = target.Center.X - NPC.Center.X;
            float distance = NPC.Distance(target.Center);
            float verticalGap = Math.Abs(target.Center.Y - NPC.Center.Y);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            if (!lineOfSight)
            {
                _guardZoneTicks = 0;
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

            bool lungeOnly = finisherReady && _finisherWaitTicks < 120;
            int targetSide = Math.Sign(horizontalGap);
            float sideGap = Math.Abs(horizontalGap);

            if (lungeOnly)
            {
                _guardZoneTicks = 0;
                bool lungeReach = distance >= 140f && distance <= 280f && SolveLungeLane(target) >= LungeMinimumTravel;
                if (lungeReach)
                {
                    Trace("pick=Lunge gate=finisher");
                    BeginMove(Move.Lunge, target, true);
                }

                return;
            }

            bool behindNow = targetSide == -NPC.direction;
            bool crossedAction = _crossCheckTicks > 0 && targetSide == -_lastActionFacing;
            if ((behindNow || crossedAction) && sideGap <= 80f && verticalGap < 48f)
            {
                Trace("pick=HaftSweep gate=behind");
                BeginMove(Move.HaftSweep, target, false);
                return;
            }

            bool inGuardZone = targetSide == NPC.direction && sideGap >= 64f && sideGap <= 128f && verticalGap < 48f;
            if (inGuardZone)
            {
                _guardZoneTicks++;
            }
            else
            {
                _guardZoneTicks = 0;
            }

            if (_guardZoneTicks >= 6 && CooldownReady(Move.GuardThrust))
            {
                Trace("pick=GuardThrust gate=zone");
                BeginMove(Move.GuardThrust, target, false);
                return;
            }

            if (_neutralTicks < 24)
            {
                return;
            }

            _bag ??= new AttackBag<Move>(cards =>
            {
                cards.Add(Move.TwinThrust);
                cards.Add(Move.TwinThrust);
                cards.Add(Move.HaftSweep);
                cards.Add(Move.Lunge);
                cards.Add(Move.Lance);
            });

            bool Eligible(Move move)
            {
                bool repeatBlocked = move == _lastMove && _neutralTicks < 90;
                if (repeatBlocked || !CooldownReady(move))
                {
                    return false;
                }

                switch (move)
                {
                    case Move.TwinThrust:
                        return distance >= 64f && distance <= 150f && verticalGap < 48f;
                    case Move.HaftSweep:
                        return distance <= 56f && verticalGap < 40f;
                    case Move.Lunge:
                        return distance >= 140f && distance <= 280f && SolveLungeLane(target) >= LungeMinimumTravel;
                    case Move.Lance:
                        return distance >= 200f && distance <= 520f;
                    default:
                        return false;
                }
            }

            if (!_bag.TryDraw(Eligible, out Move chosen))
            {
                return;
            }

            Trace("pick=" + chosen + " gate=bag");
            BeginMove(chosen, target, false);
        }

        bool CooldownReady(Move move)
        {
            return !_cooldowns.TryGetValue(move, out int remaining) || remaining <= 0;
        }

        /// <summary>Server: face and lock the target and start the chosen action's Custom phase.</summary>
        void BeginMove(Move move, Player target, bool finisher)
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
            _lungeDistance = 0f;
            _lungeFinisher = finisher;
            _shardReleased = false;
            _abortRecovery = false;
            _customLive = false;
            _guardZoneTicks = 0;
            _crossCheckTicks = 0;

            int total = move switch
            {
                Move.GuardThrust => GuardTotal,
                Move.TwinThrust => TwinTotal,
                Move.HaftSweep => SweepTotal,
                Move.Lunge => LungeTotal,
                _ => LanceTotal,
            };

            _actionTotal = total;
            StartCustomAttack(total, MeleeWeaponItemType);
            Snapshot();
        }

        /// <summary>Server: end the current action's offense and play a harmless recovery of the given length, starting
        /// from the exact pose it stopped in.</summary>
        void AbortIntoRecovery(int recoveryTicks)
        {
            ResolvePose(out float rotation, out float grip);
            _abortRotation = rotation;
            _abortGrip = grip;
            _abortRecovery = true;
            _customLive = false;
            _actionTotal = recoveryTicks;
            PhaseTimer = recoveryTicks + 1;
            NPC.velocity.X = 0f;
            StopMeleeHit();
            Snapshot();
        }

        /// <summary>Server: the action returned to neutral (or was interrupted). Cooldowns start on full exit; a 12t
        /// planning beat follows every action; the crossed-up check watches the facing it attacked with for 30t.</summary>
        void CompleteAction()
        {
            int cooldown = _move switch
            {
                Move.GuardThrust => 180,
                Move.TwinThrust => 150,
                Move.HaftSweep => 180,
                Move.Lunge => 300,
                Move.Lance => 240,
                _ => 0,
            };

            _cooldowns[_move] = cooldown;
            _restTicks = 12;
            _lastActionFacing = _lockFacing;
            _crossCheckTicks = 30;
            Trace("complete-" + _move);
            _lastMove = _move;
            _move = Move.None;
            _abortRecovery = false;
            _lungeFinisher = false;
            _lockedTarget = -1;
            Snapshot();
        }

        void Snapshot()
        {
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        protected override void Trace(string kind)
        {
            if (!Server || !ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                return;
            }

            string json = System.Text.Json.JsonSerializer.Serialize(new
            {
                kind,
                npc = NPC.whoAmI,
                tick = Main.GameUpdateCount,
                move = _move.ToString(),
                phase = Phase.ToString(),
                remaining = PhaseTimer,
                reassembly = ReassemblyPhase.ToString(),
                used = ReassemblyUsed,
                life = NPC.life,
                lane = (int)_lungeDistance,
            });
            Mod.Logger.Info("ICESKELETON " + json);
        }

        // ── Custom phase tick (every machine; locks, spawns and the lunge lane are server only) ───────────────
        protected override void DoCustomTick(int ticksRemaining)
        {
            _customLive = false;

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

            // Facing and aim follow the target only in each action's tracking window, then hold the locked values.
            bool tracking = false;
            float pitchLimit = ThrustPitchLimit;
            switch (_move)
            {
                case Move.GuardThrust:
                    tracking = elapsed < GuardLock;
                    break;
                case Move.TwinThrust:
                    tracking = elapsed < TwinLock || (elapsed >= TwinSecondTrackStart && elapsed < TwinSecondLock);
                    break;
                case Move.HaftSweep:
                    // Re-facing is part of the tell (first 12t), never a turn on the hit frame. The sweep has no pitch.
                    tracking = elapsed < 12;
                    pitchLimit = 0f;
                    break;
                case Move.Lunge:
                    tracking = elapsed < LungeLock;
                    pitchLimit = LungePitchLimit;
                    break;
                case Move.Lance:
                    tracking = elapsed < LanceLock;
                    pitchLimit = LancePitchLimit;
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

            bool lockTick = elapsed == GuardLock && _move == Move.GuardThrust;
            lockTick |= _move == Move.TwinThrust && (elapsed == TwinLock || elapsed == TwinSecondLock);
            lockTick |= _move == Move.HaftSweep && elapsed == 12;
            if (lockTick && Server)
            {
                Snapshot();
            }

            if (elapsed == 0 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.5f, Pitch = 0.3f }, NPC.Center);
            }

            switch (_move)
            {
                case Move.GuardThrust:
                {
                    int strike = elapsed - GuardTell;
                    if (strike >= 0 && strike < 10)
                    {
                        PushIn(ShapedSpeed(GuardPushIn, strike, 1, 0, 9, 4f));
                    }

                    if (!Main.dedServ)
                    {
                        bool glint = elapsed >= GuardTell - 10 && elapsed < GuardTell && elapsed % 2 == 0;
                        if (glint)
                        {
                            Glint(PuppetSpearTipPosition);
                        }

                        if (strike == 0)
                        {
                            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = 0.2f }, NPC.Center);
                        }
                    }

                    _customLive = strike >= 0 && strike < 10;
                    _customExpertDamage = GuardThrustExpertDamage;
                    break;
                }

                case Move.TwinThrust:
                {
                    int first = elapsed - TwinTell;
                    int second = elapsed - TwinSecondStrike;
                    bool firstLive = first >= 0 && first < 10;
                    bool secondLive = second >= 0 && second < 10;

                    if (firstLive)
                    {
                        PushIn(ShapedSpeed(TwinPushIn, first, 1, 0, 9, 4f));
                    }
                    else if (secondLive)
                    {
                        PushIn(ShapedSpeed(TwinPushIn, second, 1, 0, 9, 4f));
                    }

                    if (!Main.dedServ)
                    {
                        bool firstGlint = elapsed >= TwinTell - 12 && elapsed < TwinTell;
                        bool secondGlint = elapsed >= TwinSecondStrike - 12 && elapsed < TwinSecondStrike;
                        if ((firstGlint || secondGlint) && elapsed % 2 == 0)
                        {
                            Glint(PuppetSpearTipPosition);
                        }

                        if (first == 0 || second == 0)
                        {
                            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = 0.3f }, NPC.Center);
                        }
                    }

                    _customLive = firstLive || secondLive;
                    _customExpertDamage = TwinThrustExpertDamage;
                    break;
                }

                case Move.HaftSweep:
                {
                    int sweep = elapsed - SweepTell;
                    if (sweep >= 0 && sweep < SweepTicks)
                    {
                        PushIn(ShapedSpeed(SweepPushIn, sweep, 8, 0, 36, 7f));
                    }

                    if (!Main.dedServ)
                    {
                        // Frost kicks off the floor at its heel while it drags the spear back, then along the sweep.
                        bool dragging = elapsed >= 8 && elapsed < SweepTell && elapsed % 3 == 0;
                        bool sweeping = sweep >= 0 && sweep < SweepLiveTicks;
                        if (dragging || sweeping)
                        {
                            Vector2 floorPoint = new Vector2(PuppetSpearTipPosition.X, NPC.Bottom.Y - 2f);
                            Dust kick = Dust.NewDustPerfect(floorPoint, DustID.Snow, new Vector2(Main.rand.NextFloat(-1f, 1f), -1.2f), 60, default, 1f);
                            kick.noGravity = false;
                        }

                        if (sweep == 0)
                        {
                            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f, Pitch = 0.1f }, NPC.Center);
                        }
                    }

                    _customLive = sweep >= 0 && sweep < SweepLiveTicks;
                    _customExpertDamage = HaftSweepExpertDamage;
                    break;
                }

                case Move.Lunge:
                    TickLunge(elapsed, target);
                    break;

                case Move.Lance:
                {
                    if (elapsed == LanceLock && Server)
                    {
                        // Aim locks from where the shard is (the drawn point) to the target, then holds for 12t.
                        Vector2 point = PuppetSpearTipPosition;
                        Vector2 aimPoint = point + new Vector2(_lockFacing * 100f, 0f);
                        if (target != null)
                        {
                            aimPoint = target.Center;
                        }

                        _lanceDirection = (aimPoint - point).SafeNormalize(new Vector2(_lockFacing, 0f));
                        Trace("lance-lock");
                        Snapshot();
                    }

                    if (elapsed == LanceTell && !_shardReleased && Server)
                    {
                        _shardReleased = true;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), PuppetSpearTipPosition, _lanceDirection * IceSkeletonRimeShard.Speed,
                            ModContent.ProjectileType<IceSkeletonRimeShard>(), EnemyDamage.Projectile(RimeShardExpertDamage), 2f, Main.myPlayer);
                        Trace("lance-release");
                        Snapshot();
                    }

                    if (!Main.dedServ)
                    {
                        // Frost gathers at the point while the shard grows there.
                        if (elapsed >= 12 && elapsed < LanceTell)
                        {
                            Vector2 point = PuppetSpearTipPosition;
                            Vector2 from = point + Main.rand.NextVector2CircularEdge(22f, 22f);
                            Dust gather = Dust.NewDustPerfect(from, DustID.IceTorch, (point - from) * 0.12f, 80, default, 1f);
                            gather.noGravity = true;

                            // Growing core at the point: the aim tell. Scale 0.6 -> 2.0 over t12-40, with a second
                            // white-hot mote once the aim locks (t28), so the read is "it is about to fire" without a lane line.
                            float charge = (elapsed - 12) / (float)(LanceTell - 12);
                            float coreScale = MathHelper.Lerp(0.6f, 2f, charge);
                            Dust core = Dust.NewDustPerfect(point, DustID.IceTorch, Vector2.Zero, 40, default, coreScale);
                            core.noGravity = true;

                            if (elapsed >= LanceLock)
                            {
                                Dust hotCore = Dust.NewDustPerfect(point, DustID.IceTorch, Vector2.Zero, 0, Color.White, coreScale * 0.6f);
                                hotCore.noGravity = true;
                            }
                        }

                        if (elapsed == LanceTell)
                        {
                            SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.8f, Pitch = 0.1f }, NPC.Center);
                        }
                    }

                    break;
                }
            }
        }

        /// <summary>Hoarfrost Lunge: lane locked at t24 (faint motes rise along the floor lane), re-solved at the
        /// t36 release, then a 20t distance-solved dash with the spear live the whole way. A wall the lane check missed
        /// ends the dash where it is.</summary>
        void TickLunge(int elapsed, Player target)
        {
            if (!Main.dedServ)
            {
                bool glint = elapsed >= LungeTell - 12 && elapsed < LungeTell && elapsed % 2 == 0;
                if (glint)
                {
                    Glint(PuppetSpearTipPosition);
                }

                // Lane tell, kept minimal: every 3rd tick from the t24 lock, one faint blue mote rises from a random point
                // on the floor along the locked dash lane (its feet -> _lungeDistance ahead, synced).
                bool laneTell = elapsed >= LungeLock && elapsed < LungeTell && _lungeDistance > 0f && elapsed % 3 == 0;
                if (laneTell)
                {
                    float along = Main.rand.NextFloat(_lungeDistance);
                    Vector2 floorPoint = NPC.Bottom + new Vector2(_lockFacing * along, -2f);
                    Dust mote = Dust.NewDustPerfect(floorPoint, DustID.IceTorch, new Vector2(0f, -0.4f), 120, default, 0.8f);
                    mote.noGravity = true;
                }

                if (elapsed == LungeTell)
                {
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f, Pitch = 0.5f }, NPC.Center);
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.2f }, NPC.Center);
                }
            }

            bool lockTick = elapsed == LungeLock;
            bool releaseTick = elapsed == LungeTell;
            if ((lockTick || releaseTick) && Server)
            {
                _lungeDistance = 0f;
                if (target != null)
                {
                    _lungeDistance = SolveLungeLane(target);
                }

                if (_lungeDistance < LungeMinimumTravel)
                {
                    Trace("lunge-lane rejected");
                    AbortIntoRecovery(36);
                    return;
                }

                Trace("lunge-lane");
                Snapshot();
            }

            int dash = elapsed - LungeTell;
            if (dash < 0)
            {
                return;
            }

            if (dash >= LungeDashTicks)
            {
                NPC.velocity.X *= 0.85f;
                return;
            }

            float speed = ShapedSpeed(_lungeDistance, dash, 4, 6, 10, 3f);
            NPC.velocity.X = _lockFacing * speed;
            _customLive = true;
            _customExpertDamage = LungeExpertDamage;

            if (!Main.dedServ)
            {
                // Frost glitters along the floor behind it; the motes linger ~30t and fade on their own.
                Vector2 feet = NPC.Bottom + new Vector2(0f, -2f);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 spot = feet + new Vector2(-_lockFacing * Main.rand.NextFloat(0f, speed), 0f);
                    Dust sparkle = Dust.NewDustPerfect(spot, DustID.IceTorch, Vector2.Zero, 60, default, 1.1f);
                    sparkle.noGravity = true;
                }
            }

            if (Server)
            {
                Vector2 nextPosition = NPC.position + new Vector2(NPC.velocity.X, -2f);
                if (Collision.SolidCollision(nextPosition, NPC.width, NPC.height))
                {
                    NPC.velocity.X = 0f;
                    Trace("lunge-wall");
                    AbortIntoRecovery(36);
                }
            }
        }

        /// <summary>
        /// Hoarfrost Lunge lane, server: travel from here to 48px past the target's X, cut short at the first 8px step
        /// that is blocked at body height or has no floor within 48px (3 tiles) below. Feet within 32px of the target's.
        /// Returns the travel in px (0 = no lane).
        /// </summary>
        float SolveLungeLane(Player target)
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

            float wanted = Math.Abs(target.Center.X - NPC.Center.X) + LungeOvershoot;
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
                for (float drop = 2f; drop <= LungeMaxDrop + 2f; drop += 8f)
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

        /// <summary>Eased push-in for one tick of a strike: forward at <paramref name="speed"/>, or stopped by a wall.</summary>
        void PushIn(float speed)
        {
            Vector2 stepAhead = NPC.position + new Vector2(_lockFacing * 6f, -2f);
            bool wallAhead = Collision.SolidCollision(stepAhead, NPC.width, NPC.height);
            NPC.velocity.X = 0f;
            if (!wallAhead)
            {
                NPC.velocity.X = _lockFacing * speed;
            }
        }

        static void Glint(Vector2 position)
        {
            Dust glint = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(3f, 3f), DustID.IceTorch, Vector2.Zero, 0, Color.White, 1.3f);
            glint.noGravity = true;
        }

        public void OnHumanoidMeleeHit(Player target)
        {
            // Confirmed contact only: a puff of ice where the spear landed. No status effects.
            for (int i = 0; i < 10; i++)
            {
                Dust puff = Dust.NewDustPerfect(target.Center, DustID.IceTorch, Main.rand.NextVector2Circular(3f, 3f), 60, default, 1.2f);
                puff.noGravity = true;
            }
        }

        // ── Movement ─────────────────────────────────────────────────────────────────────────────────────────
        // Neutral: a planted advance via SmartFighter4 - 1.4px/t once a target is within 280px (Pike Guard), 1.8px/t
        // otherwise. No kiting, leaps at the player, quick-steps or teleports. Actions own their push-ins and dash.
        protected override void RunMovementAI(float speedMult)
        {
            if (ActionOwnsMovement)
            {
                return;
            }

            if (Neutral && _restTicks == 0)
            {
                float walkSpeed = TopSpeed;
                if (NPC.HasValidTarget && NPC.Distance(Main.player[NPC.target].Center) <= 280f)
                {
                    walkSpeed = 1.4f;
                }

                SmartFighter4AI.Run(NPC, topSpeed: walkSpeed, acceleration: Acceleration, doorBreakingDamage: 3, attackRange: 700f);
                return;
            }

            NPC.velocity.X *= 0.65f;
        }

        // Only a completed poise break lands here: the interrupted action counts as exited (its cooldown starts).
        public override void OnStagger(NPC npc)
        {
            if (Server && _move != Move.None)
            {
                Trace("poise-interruption");
                CompleteAction();
            }

            _move = Move.None;
            _customLive = false;
            base.OnStagger(npc);
        }

        // ── Spear fade draw (client) ───────────────────────────────────────────────────────────────────────
        // Draws the spear through the normal held-weapon path once more, then scales its alpha by SpearOpacity.
        protected override bool DrawSpecialHeldWeapon(ref PlayerDrawSet drawInfo)
        {
            if (_drawingSpearFade)
            {
                return false;
            }

            float opacity = SpearOpacity;
            if (opacity >= 1f)
            {
                return false;
            }

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

        // ── Telegraph drawing (client) ───────────────────────────────────────────────────────────────────────
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Collapsed)
            {
                return;
            }

            base.PostDraw(spriteBatch, screenPos, drawColor);

            if (!SpearOut || _abortRecovery)
            {
                return;
            }

            int elapsed = CustomElapsed;

            if (_move == Move.Lance && elapsed < LanceTell)
            {
                // The shard forms at the point: 1/4 -> full size over t20-40, point leading along the spear.
                float growth = MathHelper.Clamp((elapsed - 20) / 20f, 0f, 1f);
                float scale = MathHelper.Lerp(0.25f, 1f, growth);
                float alpha = MathHelper.Clamp((elapsed - 12) / 8f, 0f, 1f);
                if (alpha > 0f)
                {
                    Texture2D shard = TextureAssets.Projectile[ModContent.ProjectileType<IceSkeletonRimeShard>()].Value;
                    Vector2 point = PuppetSpearTipPosition;
                    float rotation = PuppetWeaponDirection.ToRotation() + MathHelper.Pi * 0.75f;
                    Color light = Color.Lerp(drawColor, Color.White, 0.5f) * alpha;
                    spriteBatch.Draw(shard, point - screenPos, null, light, rotation, shard.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }
            }
        }

        // ── Frozen Reassembly: the pile bones and the spear lying with them ───────────────────────────────────
        // Bone pile rest layout at its feet (facing right; mirrored for left): gore, offset from NPC.Bottom, rotation,
        // and the standing offset each bone flies from (collapse) and back to (reform). Built on first use, once the
        // mod's gore types exist.
        PileBone[] _pileBones;

        protected override PileBone[] PileBones
        {
            get
            {
                if (_pileBones == null)
                {
                    int vert = Mod.Find<ModGore>("Ice Skelly Vert").Type;
                    int piece = Mod.Find<ModGore>("Ice Skelly Piece").Type;
                    int head = Mod.Find<ModGore>("Ice Skelly Head").Type;
                    _pileBones = new[]
                    {
                        new PileBone { GoreType = vert, RestOffset = new Vector2(-12f, -4f), Rotation = -0.4f, StandingOffset = new Vector2(0f, -30f) },
                        new PileBone { GoreType = vert, RestOffset = new Vector2(9f, -4f), Rotation = 1.2f, StandingOffset = new Vector2(0f, -22f) },
                        new PileBone { GoreType = piece, RestOffset = new Vector2(14f, -7f), Rotation = -1.0f, StandingOffset = new Vector2(4f, -14f) },
                        new PileBone { GoreType = piece, RestOffset = new Vector2(-2f, -7f), Rotation = 0.8f, StandingOffset = new Vector2(-4f, -26f) },
                        new PileBone { GoreType = head, RestOffset = new Vector2(-5f, -10f), Rotation = 0.3f, StandingOffset = new Vector2(0f, -46f), IsSkull = true },
                    };
                }

                return _pileBones;
            }
        }

        // The spear lies flat at its feet, point forward; it dissolves over the first 10t of the reform.
        protected override void DrawPileProps(SpriteBatch spriteBatch, Vector2 screenPos, Vector2 anchor, int facing, Color light)
        {
            int timer = ReassemblyTimer;
            float spearAlpha = MathHelper.Clamp(timer / 6f, 0f, 1f) * 0.9f;
            if (ReassemblyPhase == Reassembly.Reform)
            {
                spearAlpha = MathHelper.Clamp(1f - timer / 10f, 0f, 1f) * 0.9f;
            }

            if (spearAlpha <= 0f)
            {
                return;
            }

            Texture2D spear = ModContent.Request<Texture2D>(EnemyIceSkeletonSpear.ArtPath).Value;
            float headingAngle = 0f;
            if (facing == -1)
            {
                headingAngle = MathHelper.Pi;
            }

            float nativeAngle = (float)Math.Atan2(SpearHeadTexel.Y - SpearButtTexel.Y, SpearHeadTexel.X - SpearButtTexel.X);
            Vector2 spearCenter = anchor + new Vector2(facing * 10f, -3f);
            spriteBatch.Draw(spear, spearCenter - screenPos, null, light * spearAlpha, headingAngle - nativeAngle,
                spear.Size() * 0.5f, 0.8f, SpriteEffects.None, 0f);
        }

        // ── Spawn, loot and death (carried over from the legacy enemy) ─────────────────────────────────────
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 40;
            NPC.knockBackResist = KnockbackResist;
            NPC.value = 4000;
            NPC.timeLeft = 750;
            NPC.defense = 78;
            NPC.scale = 1f;
            NPC.lavaImmune = true;
            NPC.HitSound = SoundID.NPCHit2;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.lifeMax = 2200;
            EnemyDamage.SetContact(NPC, ContactExpertDamage);
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.IceSkeletonBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PatrolMode = PatrolMode.Wander;
            globalNPC.PatrolAnchorSource = PatrolAnchorSource.GiveUpLocation;
            globalNPC.NavSearchRadius = 60;
            globalNPC.CanUseRopes = true;
            globalNPC.PatrolUsesNavigation = true;
            globalNPC.MaxJumpPower = 9f;
            globalNPC.MaxJumpBoost = 5f;
            globalNPC.CanTeleport = false;
            globalNPC.TeleportMaxCharges = 0;
            globalNPC.TeleportChargesRemaining = 0;
            globalNPC.EvasiveTeleportAway = false;
            globalNPC.EvasiveQuickStep = false;
            globalNPC.EvasiveRetreatAndShoot = false;
            globalNPC.KiteRangeMin = 0f;
            globalNPC.KiteRangeMax = 0f;
        }

        // Client, on the real death: the frost bones scatter, and a shattered pile bursts into ice with breaking glass.
        protected override void ScatterRealDeath(bool fromPile)
        {
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Ice Skelly Head").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Ice Skelly Vert").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Ice Skelly Vert").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Ice Skelly Piece").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Ice Skelly Piece").Type, 1.1f);

            if (!fromPile)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Shatter, NPC.Center);
            for (int i = 0; i < 60; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f) + new Vector2(0f, -2f);
                int dustType = DustID.IceTorch;
                if (i % 2 == 0)
                {
                    dustType = DustID.BlueCrystalShard;
                }

                Dust shard = Dust.NewDustPerfect(NPC.Bottom + new Vector2(0f, -8f), dustType, velocity, 40, default, 1.4f);
                shard.noGravity = dustType == DustID.IceTorch;
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlueTitanite>(), 1, 1, 2));
        }

        // ── Multiplayer payload: base first (PuppetNPC, then the SkeletonPuppet reassembly), then these fields ───────────────────────
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_move);
            writer.Write(_actionTotal);
            writer.Write(_abortRecovery);
            writer.Write(_abortRotation);
            writer.Write(_abortGrip);
            writer.Write(_lockedTarget);
            writer.Write(_lockFacing);
            writer.Write(_pitch);
            writer.Write(_lungeDistance);
            writer.Write(_lungeFinisher);
            writer.WriteVector2(_lanceDirection);
            writer.Write(_shardReleased);
            writer.Write(_restTicks);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _move = (Move)reader.ReadByte();
            _actionTotal = reader.ReadInt32();
            _abortRecovery = reader.ReadBoolean();
            _abortRotation = reader.ReadSingle();
            _abortGrip = reader.ReadSingle();
            _lockedTarget = reader.ReadInt32();
            _lockFacing = reader.ReadInt32();
            _pitch = reader.ReadSingle();
            _lungeDistance = reader.ReadSingle();
            _lungeFinisher = reader.ReadBoolean();
            _lanceDirection = reader.ReadVector2();
            _shardReleased = reader.ReadBoolean();
            _restTicks = reader.ReadInt32();
        }
    }
}
