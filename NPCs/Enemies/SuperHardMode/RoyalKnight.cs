using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Armor.Enemy.RoyalKnightSet;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Gwyn;
using tsorcRevamp.Content.Projectiles.Enemy.RoyalKnightBolts;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    /// <summary>
    /// The Royal Knight (formerly Knight of Gwyn): a Super Hardmode elite puppet in the RoyalKnightSet armor with Angel Wings,
    /// a two-handed royal halberd and a thunder bow. Melee is Cursed Dragon's whole spear kit retuned for the halberd (thrusts,
    /// a reaping under-over, a leap, a windmill, an uppercut, a dive from the air) plus Blaidd's Moonfang Lance as the
    /// Gilded Charge. Anti-air: Ascending Pike, Skyward Pike (Artorias's Skyward Lunge) and wing pursuit of an airborne target.
    /// Ranged: Thunder Arrow, the hovering Storm Volley and, in phase two, the Grounding Shot that calls Gwyn's lightning.
    /// One phase change at 50%. Spawns naturally only in sandbox worlds (Systems/EnemySpawns.cs); Adventure Mode places it
    /// with events.
    /// </summary>
    public class RoyalKnight : PuppetNPC, IHumanoidMeleeHitEffects
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        protected override string InvaderTitle => "Royal Knight";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        // Stats come from SetDefaults + the EnemyBalance registry; the namespace gives the SHM defense scaling.
        public override bool UsesPuppetDifficultyScaling => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override string DespawnFlavorText => LangUtils.GetTextValue("NPCs.RoyalKnight.DespawnHandler");
        protected override Color DespawnFlavorColor => Color.Gold;
        protected override int DespawnDustType => DustID.GoldFlame;

        // The rig draws at Knight of Gwyn's old 1.35 size. DrawPlayer scales every layer about the feet, so the drawn
        // grips stay aligned; gameplay points (blade capsule, arrow nock, tip dust) are scaled the same way below.
        public const float BodyScale = 1.35f;
        protected override float PuppetDrawScale => BodyScale;

        // ── Loadout ───────────────────────────────────────────────────────────────
        protected override int HeadArmorItemType => ModContent.ItemType<RoyalKnightHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<RoyalKnightBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<RoyalKnightGreaves>();
        protected override int MeleeWeaponItemType => ModContent.ItemType<RoyalKnightHalberd>();
        protected override int SpearWeaponItemType => -1;
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyRoyalThunderBow>();
        protected override int MagicWeaponItemType => -1;

        // ── Damage (Expert hits before defense; spawn damage = Expert / 4) ───────────
        public const int MeleeExpertDamage = 180;
        public const int SwoopExpertDamage = 170;
        public const int ThunderArrowExpertDamage = 120;
        public const int VolleyArrowExpertDamage = 100;
        public const int GroundingStrikeExpertDamage = 160;
        public const int SparkExpertDamage = 100;
        public const float GildedChargeDamageMult = 1.3f;
        public const float SkywardPikeDamageMult = 1.3f;
        protected override int MeleeDamage => EnemyDamage.Projectile(MeleeExpertDamage);
        protected override int SpearDamage => EnemyDamage.Projectile(SwoopExpertDamage);
        protected override int RangedDamage => EnemyDamage.Projectile(ThunderArrowExpertDamage);

        // ── Halberd geometry (measured from the 62x62 art: point (61, 0.5), butt (0.5, 61.5), a true 45 degree diagonal) ──
        // The front hand grips 25% up the shaft from the butt. Grip->point = 0.75 x 86.3 texels x 1.3 draw x 1.35 body = 113px,
        // so ComboReachBase 162 x 0.7 = 113px puts the hitbox on the drawn point.
        private const float HalberdTextureSize = 62f;
        private static readonly Vector2 HalberdPoint = new Vector2(61f, 0.5f);
        private static readonly Vector2 HalberdButt = new Vector2(0.5f, 61.5f);
        private const float HalberdGripFromButt = 0.25f;
        private const float HalberdDrawScale = 1.3f;
        private const float ThrustReachMult = 1.2f;   // 136px: the grip slides to ~0.10 at full extension
        private const float SweepReachMult = 1.0f;    // 113px
        private const float SunfallReachMult = 1.15f; // 130px

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Halberd;
        protected override bool DrawWeaponAsSpear => true;
        protected override bool MirrorSpearRotationByFacing => true;
        protected override string SpearDrawTexturePath => UsefulFunctions.RefactorableFilepath(typeof(RoyalKnightHalberd));
        protected override Vector2 SpearTextureSize => new Vector2(HalberdTextureSize, HalberdTextureSize);
        protected override Vector2 SpearHeadNorm => HalberdPoint / HalberdTextureSize;
        protected override Vector2 SpearBaseNorm => HalberdButt / HalberdTextureSize;
        protected override float SpearDrawRotationOffset => 0f;
        protected override Vector2 MeleeHandleNorm => (HalberdButt + (HalberdPoint - HalberdButt) * HalberdGripFromButt) / HalberdTextureSize;
        protected override float MeleeWeaponDrawScale => HalberdDrawScale;
        protected override float ComboReachBase => 162f;
        protected override float MeleeBladeWidth => 14f;
        protected override float MeleeRange => 113f;
        protected override float StabRange => 200f;
        protected override float MeleeEngageRange => 190f;
        protected override float ComboMaxStartRange => 520f;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override bool HoldNeutralWeaponOnCompositeArm => true;
        protected override bool ShowMeleeWeaponWhileClosingDistance => true;

        // ── Swing quality (Cursed Dragon's settings) ───────────────────────────────
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool AuthoredClockCoversJoustDash => true;
        protected override bool UseAuthoredSpearGrip => true;
        protected override bool UseEasedSpin => true;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 10;
        protected override bool UseLandingTimedLeapSlam => true;
        protected override bool UseTwoHandedCompositeSwing => true;
        // Rear hand 14px down the shaft toward the butt (Cursed Dragon uses -8 on a shorter spear).
        protected override float TwoHandedBackGripOffset => -14f;
        protected override bool SlowDownBeforeMelee => false;
        protected override int MeleeComboChance => 60;
        protected override int RangedStartMeleeComboChance => 55;
        protected override Color MeleeTelegraphFlashColor => new Color(255, 225, 140);

        // Spear-path grip, measured from the POINT (0 = head, 1 = butt). Carry and cuts hold 0.75 (a quarter up from the butt);
        // a thrust slides the shaft out through the hands to 0.90 and back on the swing clock (sin pulse), and the Gilded
        // Charge and Skyward Pike dash ride fully extended. 0.90 x 86.3 texels x 1.3 x 1.35 = 136px = the thrust reach.
        public const float CarryGrip = 0.75f;
        public const float ExtendedGrip = 0.90f;

        protected override float? OverrideSpearGrip(float swingClockT)
        {
            bool thrustCombo = ActiveMeleeComboName == RoyalThrustName || ActiveMeleeComboName == TwinThrustName;
            if (Phase == AttackPhase.MeleeComboAttack && thrustCombo)
            {
                float pulse = (float)Math.Sin(MathHelper.Clamp(swingClockT, 0f, 1f) * Math.PI);
                return MathHelper.Lerp(CarryGrip, ExtendedGrip, pulse);
            }

            bool chargeDash = Phase == AttackPhase.PierceDash;
            bool pikeDash = SkywardPikeActive && (_skyStage == SkyStage.Aim || _skyStage == SkyStage.Dash);
            if (chargeDash || pikeDash)
            {
                return ExtendedGrip;
            }

            return CarryGrip;
        }

        // Blade capsule, scaled with the drawn rig: the base measures the hand on the unscaled puppet, so the hand point is
        // pushed out from the feet by BodyScale and the tip keeps its authored world reach from there.
        protected override void ModifyFrontBladeCapsule(ref Vector2 origin, ref Vector2 tip)
        {
            Vector2 reachVector = tip - origin;
            origin = ScaleAboutFeet(origin);
            tip = origin + reachVector;
        }

        /// <summary>Maps an unscaled puppet point (hand, bow grip) onto the drawn 1.35x rig: DrawPlayer scales every draw
        /// layer about the bottom centre of the body.</summary>
        private Vector2 ScaleAboutFeet(Vector2 unscaledPoint)
        {
            Vector2 offset = unscaledPoint - NPC.Bottom;
            return NPC.Bottom + offset * BodyScale;
        }

        /// <summary>World point <paramref name="reach"/> px out along the drawn halberd from the drawn front hand.</summary>
        private Vector2 HalberdPointAt(float reach)
        {
            Vector2 drawnHand = ScaleAboutFeet(PuppetWeaponTipPosition(0f));
            return drawnHand + PuppetWeaponDirection * reach;
        }

        // ── Arc endpoints (attack-timing-design §3) ─────────────────────────────────
        // Thrust: couched 17 degrees above level (0.30 rad) easing to level; the grip extension carries the strike.
        // Halberd Reap: underhand 2.20 -> -1.45 then overhead -1.45 -> 2.36 (shared joint, no snap); envelopes 209 / 218 deg.
        // Sunfall Cleave: cocked past vertical to straight down, 228 deg envelope, ~182 deg live.
        private const float ThrustCouchRaise = 0.30f;
        private const float ReapLowStart = 2.20f;
        private const float ReapJoint = -1.45f;
        private const float ReapOverheadEnd = 2.36f;
        private const float SunfallStart = -1.62f;
        private const float SunfallEnd = 2.36f;
        private const float LevelForward = MathHelper.PiOver4;

        private bool SunfallSwinging =>
            ActiveMeleeComboName == SunfallCleaveName
            && (Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause);

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float a0, ref float a1)
        {
            switch (motion)
            {
                case ComboMotion.JoustDash:
                    a0 = a1 - ThrustCouchRaise;
                    break;

                case ComboMotion.UnderhandArc:
                    a0 = ReapLowStart;
                    a1 = ReapJoint;
                    break;

                case ComboMotion.OverheadArc:
                    if (SunfallSwinging)
                    {
                        a0 = SunfallStart;
                        a1 = SunfallEnd;
                    }
                    else
                    {
                        a0 = ReapJoint;
                        a1 = ReapOverheadEnd;
                    }
                    break;
            }
        }

        // ── Combo pool ──────────────────────────────────────────────────────────────
        private const string RoyalThrustName = "Royal Thrust";
        private const string TwinThrustName = "Twin Thrust";
        private const string HalberdReapName = "Halberd Reap";
        private const string RoyalLeapName = "Royal Leap";
        private const string SunfallCleaveName = "Sunfall Cleave";
        private const string HalberdWindmillName = "Halberd Windmill";
        private const string AscendingPikeName = "Ascending Pike";
        private const string WingDiveName = "Wing Dive";

        protected override MeleeCombo[] MeleeComboPoolOverride => RoyalKnightCombos;

        private static MeleeComboStep GS(ComboMotion motion, int tel, int atk, int pause,
            float dmg = 1f, float reach = 1f, float push = 0f, SwingEaseStyle ease = SwingEaseStyle.Smooth)
            => new MeleeComboStep
            {
                Motion = motion, TelegraphTicks = tel, AttackTicks = atk, PostStepPause = pause,
                DamageMult = dmg, ReachMult = reach, ForwardPushMult = push, Ease = ease
            };

        // A Weighted swing stays armed while blade speed >= 30% of its peak (attack-timing-design §3).
        private const float ArmedSpeedShare = 0.30f;

        /// <summary>Authored Weighted swing: cubic ease-in, exponential settle, hit window closing once the blade drops
        /// under <see cref="ArmedSpeedShare"/> of peak speed.</summary>
        private static MeleeComboStep WeightedSwing(ComboMotion motion, int telegraph,
            int easeInTicks, int easeOutTicks, float decay, int pause, float damage, float reach, float push = 0f)
        {
            int attackTicks = easeInTicks + easeOutTicks;
            MeleeComboStep step = GS(motion, telegraph, attackTicks, pause, damage, reach, push, SwingEaseStyle.Weighted);
            step.EaseInTicks = easeInTicks;
            step.EaseOutTicks = easeOutTicks;
            step.EaseOutDecay = decay;
            float armedSettleTicks = easeOutTicks * (float)Math.Log(1f / ArmedSpeedShare) / decay;
            step.HitWindowEnd = (easeInTicks + armedSettleTicks) / attackTicks;
            return step;
        }

        /// <summary>A thrust (Blaidd's Fang Thrust via Cursed Dragon's Pike Thrust): JoustDash Whip over 24t, live for the
        /// first 75% (18t, rollable). Push 1.6 x 3.4 = 5.4 px/t over 24t = 131px, plus the 136px reach.</summary>
        private static MeleeComboStep ThrustStep(int telegraph, int pause, float damage)
        {
            MeleeComboStep step = GS(ComboMotion.JoustDash, telegraph, 24, pause, damage, ThrustReachMult, 1.6f, SwingEaseStyle.Whip);
            step.HitWindowEnd = 0.75f;
            return step;
        }

        /// <summary>A LeapSlam with its jump shape. leapStrikeRange > 0 swings mid-air once past the apex with the target
        /// that close; otherwise the landing slam hits.</summary>
        private static MeleeComboStep LeapStep(int telegraph, int attackTicks, float damage, float reach,
            float heightMult, float forwardMult, float leapStrikeRange, SwingEaseStyle ease)
        {
            MeleeComboStep step = GS(ComboMotion.LeapSlam, telegraph, attackTicks, 0, damage, reach, 0f, ease);
            step.LeapHeightMult = heightMult;
            step.LeapForwardSpeedMult = forwardMult;
            step.LeapStrikeRange = leapStrikeRange;
            return step;
        }

        /*
         * Timing sheet (attack-timing-design §3). On-screen tell = max(30, authored x 1.35).
         *
         * Royal Thrust:     tel 27 -> 36t | JoustDash Whip 24t, live 18t (< 22, rollable), 6t tail + 18 recovery |
         *                   travels 131px + 136px reach = 267px | gate 135-260px + LOS
         * Twin Thrust:      two thrusts, pause 24t -> the second goes live 30t after the first ends; conditional (hit, or
         *                   target within 136 + 40px)
         * Halberd Reap:     Underhand 2.20 -> -1.45, Overhead -1.45 -> 2.36 (envelopes 209 / 218 deg, live ~167 / ~175),
         *                   8/34 k7 each (peak ~28 / 29 deg/t), live ~14t, pause = SafeFollowUpPause; overhead conditional
         * Royal Leap:       tel 36 -> 49t | LeapSlam height 0.62, forward 1.7, swept 10t downswing within 160px past the
         *                   apex, else the landing slam | gate 260px .. ReliableJumpStartRange + LOS | hyper-armor
         * Sunfall Cleave:   phase two. tel 52 -> 70t | Overhead -1.62 -> 2.36 (228 deg, live ~182), 10/45 k7 -> 23.4
         *                   deg/t, live ~18t | push 0.22 | one floor spark each way on impact, jumpable | 37t tail + 40
         * Halberd Windmill: tel 36t ramp | spin 40t ARMED ALL 40 (a spacing check: running 6-7 px/t outpaces the 3.1 px/t
         *                   chase), danger ring at the 136px hit radius | conditional LeapSlam finisher (target > 150px)
         * Ascending Pike:   tel 34 -> 46t | gate: target 48-240px above, within 160px horizontally, LOS
         * Wing Dive:        airborne only, at a grounded target. tel 36 -> 49t hovering | ~9px/t dive, swept 10t
         *                   downswing within 270px, else landing slam | two floor sparks each way, the second pair 10t later
         *
         * Every live window is under 22t except the Windmill's spin, and every follow-up goes live >= 30t after the
         * previous live window ends or is conditional (§2 rules 1 and 3).
         */
        private static readonly MeleeCombo[] RoyalKnightCombos = new[]
        {
            new MeleeCombo
            {
                Name = RoyalThrustName, BaseWeight = 90, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 90, RecoveryTicks = 18, MoveBrake = 0f,
                Steps = new[] { ThrustStep(27, 0, 1.1f) }
            },
            new MeleeCombo
            {
                Name = TwinThrustName, BaseWeight = 70, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 150, RecoveryTicks = 24, MoveBrake = 0f,
                Steps = new[]
                {
                    ThrustStep(27, 24, 1.0f),
                    ThrustStep( 0,  0, 1.0f),
                }
            },
            new MeleeCombo
            {
                Name = HalberdReapName, BaseWeight = 60, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Gold, CooldownAfterUse = 150, RecoveryTicks = 14,
                Steps = new[]
                {
                    WeightedSwing(ComboMotion.UnderhandArc, 30, 8, 34, 7f, 10, 0.9f, SweepReachMult),
                    WeightedSwing(ComboMotion.OverheadArc,   0, 8, 34, 7f,  0, 1.1f, SweepReachMult),
                }
            },
            new MeleeCombo
            {
                Name = RoyalLeapName, BaseWeight = 90, Preferred = ComboRangeBand.Far,
                RangedStartOnly = true, InitialFlashColor = Color.Yellow, CooldownAfterUse = 200,
                HeavyCommit = true, HyperArmor = true, RecoveryTicks = 30, MoveBrake = 0.06f,
                Steps = new[] { LeapStep(36, 82, 1.25f, SweepReachMult, 0.62f, 1.70f, 160f, SwingEaseStyle.Whip) }
            },
            new MeleeCombo
            {
                Name = SunfallCleaveName, BaseWeight = 35, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed, CooldownAfterUse = 300, HeavyCommit = true, RecoveryTicks = 40,
                MoveBrake = 0.48f,
                Steps = new[] { WeightedSwing(ComboMotion.OverheadArc, 52, 10, 45, 7f, 0, 1.5f, SunfallReachMult, 0.22f) }
            },
            new MeleeCombo
            {
                Name = HalberdWindmillName, BaseWeight = 40, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed, CooldownAfterUse = 220, RecoveryTicks = 30,
                Steps = new[]
                {
                    WindmillSpinStep(),
                    GS(ComboMotion.LeapSlam, 0, 90, 0, 1.3f, SweepReachMult),
                }
            },
            new MeleeCombo
            {
                Name = AscendingPikeName, BaseWeight = 120, Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.LightCyan, CooldownAfterUse = 210, HeavyCommit = true, MoveBrake = 0.08f,
                Steps = new[] { GS(ComboMotion.RisingUppercutLeap, 34, 150, 0, 1.22f, ThrustReachMult) }
            },
            new MeleeCombo
            {
                Name = WingDiveName, BaseWeight = 100, Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 300, HeavyCommit = true, HyperArmor = true,
                AirborneStartOnly = true, MoveBrake = 0f,
                // No ease: LeapSlam never reads it (landing-timed 10t SmoothStep downswing).
                Steps = new[] { LeapStep(36, 90, 1.28f, 1.1f, 1f, 1f, 270f, SwingEaseStyle.Smooth) }
            },
        };

        /// <summary>The Windmill spin: UseEasedSpin ramps speed up across the 36t tell and down over the last 10t, constant
        /// in between. Armed all 40t; chases at 0.9 x TopSpeed.</summary>
        private static MeleeComboStep WindmillSpinStep()
        {
            MeleeComboStep step = GS(ComboMotion.Spin, 36, 40, 20, 1.4f, ThrustReachMult, 0.9f);
            step.EaseOutTicks = 10;
            return step;
        }

        // ── Selection gates ────────────────────────────────────────────────────────
        // rise = how far the target stands ABOVE the knight (px). Ground moves never start at a target more than 48px up;
        // Ascending Pike owns 48-240px. Every move needs a clear line.
        private const float GroundCutMaxRise = 48f;
        private const float AscendingPikeMaxRise = 240f;
        private const float AscendingPikeMaxHorizontal = 160f;
        private const float ThrustMinRange = 135f;
        private const float ThrustMaxRange = 260f;
        private const float RoyalLeapMinRange = 260f;
        private const float FollowUpPushAllowance = 40f;
        private const float WindmillFinisherRange = 150f;

        protected override float ComboStartMaxTargetRise => AscendingPikeMaxRise;

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            Player target = Main.player[NPC.target];
            float rise = NPC.Center.Y - target.Center.Y;
            float horizontal = Math.Abs(target.Center.X - NPC.Center.X);
            bool clearLine = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            // Wing Dive comes down on a standing target; an airborne one gets Wing Sweep or Skyward Pike instead.
            if (combo.AirborneStartOnly)
            {
                bool targetStanding = target.velocity.Y == 0f;
                return clearLine && targetStanding;
            }

            if (combo.Name == AscendingPikeName)
            {
                bool aboveInBand = rise > GroundCutMaxRise && rise <= AscendingPikeMaxRise;
                return aboveInBand && horizontal <= AscendingPikeMaxHorizontal && clearLine;
            }

            if (rise > GroundCutMaxRise)
            {
                return false;
            }

            if (!clearLine)
            {
                return false;
            }

            if (combo.Name == SunfallCleaveName)
            {
                return _phaseTwo;
            }

            if (combo.Name == RoyalLeapName)
            {
                return distance >= RoyalLeapMinRange && distance <= ReliableJumpStartRange(combo.Steps[0]);
            }

            if (combo.Name == RoyalThrustName || combo.Name == TwinThrustName)
            {
                return distance >= ThrustMinRange && distance <= ThrustMaxRange;
            }

            return true;
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            float targetDistance = NPC.Distance(target.Center);

            // The leaping finisher only bolts on when the chasing spin didn't close the gap.
            if (comboName == HalberdWindmillName && nextStepIndex == 1)
            {
                return targetDistance > WindmillFinisherRange;
            }

            // A hit always earns the next step; a whiff only does if the next step can still reach.
            float nextReachMult = 0f;
            if (comboName == RoyalThrustName || comboName == TwinThrustName)
            {
                nextReachMult = ThrustReachMult;
            }
            else if (comboName == HalberdReapName)
            {
                nextReachMult = SweepReachMult + 0.5f; // 190px: the overhead's reach plus its step-through
            }

            if (nextReachMult > 0f)
            {
                float followUpReach = ComboReachBase * 0.7f * nextReachMult + FollowUpPushAllowance;
                bool stillInReach = targetDistance <= followUpReach;
                return previousStepHit || stillInReach;
            }

            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        // A fully whiffed Royal Thrust sprints back in and re-opens with Twin Thrust, once per chain.
        protected override int MeleeFollowUpChancePercent(int followUpsDone) => followUpsDone == 0 ? 60 : 0;

        protected override string MeleeFollowUpComboName(string finishedComboName)
        {
            if (finishedComboName == RoyalThrustName)
            {
                return TwinThrustName;
            }

            return null;
        }

        // ── Phase-two escalation: +1 conditional thrust rep, every rep still >= 30t after the last live window ──
        private const int MinTicksBetweenLiveWindows = 30;
        private const int PauseSafetyBuffer = 3;

        /// <summary>The smallest PostStepPause that keeps the next live window >= 30t after this one's ends, plus 3t.</summary>
        private static int SafeFollowUpPause(MeleeComboStep step)
        {
            int liveTicks = (int)Math.Ceiling(step.HitWindowEnd * step.AttackTicks);
            int tailTicks = Math.Max(0, step.AttackTicks - liveTicks);
            return Math.Max(1, MinTicksBetweenLiveWindows - tailTicks) + PauseSafetyBuffer;
        }

        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            base.CustomizeMeleeCombo(ref combo, healthFraction);

            if (combo.Steps == null || combo.Steps.Length == 0)
            {
                return;
            }

            if (combo.Name == HalberdReapName)
            {
                MeleeComboStep first = combo.Steps[0];
                first.PostStepPause = SafeFollowUpPause(first);
                combo.Steps[0] = first;
            }

            bool thrustFamily = combo.Name == RoyalThrustName || combo.Name == TwinThrustName;
            if (!_phaseTwo || !thrustFamily)
            {
                return;
            }

            MeleeComboStep repTemplate = combo.Steps[combo.Steps.Length - 1];
            int repPause = SafeFollowUpPause(repTemplate);

            MeleeComboStep[] extended = new MeleeComboStep[combo.Steps.Length + 1];
            Array.Copy(combo.Steps, extended, combo.Steps.Length);
            extended[combo.Steps.Length - 1].PostStepPause = repPause;

            MeleeComboStep rep = repTemplate;
            rep.TelegraphTicks = 0;
            rep.PostStepPause = 0;
            extended[combo.Steps.Length] = rep;
            combo.Steps = extended;
        }

        // ── Melee VFX hooks ─────────────────────────────────────────────────────────
        // Halberd trail: one gold mote at the blade point per armed tick (three on Sunfall); the Windmill draws its
        // 136px hit ring in gold and electric for every armed tick (the dust is the telegraph). Cosmetic, every peer.
        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboAttackTick(combo, step, elapsed, total);

            if (Main.dedServ)
            {
                return;
            }

            float reach = ComboReachBase * 0.7f * step.ReachMult;
            bool windmillSpin = combo.Name == HalberdWindmillName && step.Motion == ComboMotion.Spin;

            if (windmillSpin)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 rimPoint = NPC.Center + Main.rand.NextVector2CircularEdge(reach, reach);
                    int rimDust = DustID.GoldFlame;
                    if (i == 0)
                    {
                        rimDust = DustID.Electric;
                    }

                    Dust rim = Dust.NewDustPerfect(rimPoint, rimDust, Vector2.Zero, 0, default, 1.3f);
                    rim.noGravity = true;
                }

                return;
            }

            int trailCount = 1;
            if (combo.Name == SunfallCleaveName)
            {
                trailCount = 3;
            }

            for (int i = 0; i < trailCount; i++)
            {
                Vector2 bladePoint = HalberdPointAt(reach * Main.rand.NextFloat(0.7f, 1f));
                Dust trail = Dust.NewDustPerfect(bladePoint, DustID.GoldFlame, Vector2.Zero, 60, default, 1.1f);
                trail.noGravity = true;
            }
        }

        // Sunfall Cleave: one floor spark each way when the overhead lands (Gwyn's spark, 8 tiles, jumpable).
        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            bool sunfallImpact = ActiveMeleeComboName == SunfallCleaveName && step.Motion == ComboMotion.OverheadArc;
            if (!sunfallImpact)
            {
                return;
            }

            UsefulFunctions.ScreenShake(NPC.Bottom, 3.5f, 10, distanceFalloff: 560f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            SpawnFloorSparkPair();
        }

        // Wing Dive: two spark pairs from the landing, the second 10t later so the eruption visibly travels.
        private const int DiveSecondSparkDelay = 10;
        private int _diveSparkTimer; // server-only

        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (ActiveMeleeComboName != WingDiveName)
            {
                return;
            }

            if (!Main.dedServ)
            {
                UsefulFunctions.ScreenShake(NPC.Bottom, 1.5f, 8);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            SpawnFloorSparkPair();
            _diveSparkTimer = DiveSecondSparkDelay;
        }

        private const int SparkReachTiles = 8;

        /// <summary>Server: one GwynFloorSpark each way from the knight's feet, ground-snapped (ai0 direction, ai1 tiles).</summary>
        private void SpawnFloorSparkPair()
        {
            int sparkType = ModContent.ProjectileType<GwynFloorSpark>();
            for (int direction = -1; direction <= 1; direction += 2)
            {
                float startX = NPC.Bottom.X + direction * 14f;
                if (!PuppetGroundDustWave.TryFindGroundY(startX, NPC.Bottom.Y, out float groundY))
                {
                    continue;
                }

                Vector2 sparkPosition = new Vector2(startX, groundY - 16f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), sparkPosition, Vector2.Zero, sparkType,
                    EnemyDamage.Projectile(SparkExpertDamage), 3f, Main.myPlayer, direction, SparkReachTiles);
            }
        }

        // Halberd hits (combo blades, the swoop, the charge) carry Fracturing Armor. Runs on the hit player's machine.
        public const int FracturingArmorTicks = 300 * 60;

        public void OnHumanoidMeleeHit(Player target)
        {
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), FracturingArmorTicks, false);
        }

        // ── Gilded Charge (the base Piercing Dash, Blaidd's Moonfang Lance) ─────────
        // 200-520px on the same level (±48px): 44t tell with the halberd couched, then a 14 px/t dash solved to stop 96px
        // past the target (max 46t), contact live the whole way. Roll through it as it arrives or jump it. 60t open recovery.
        private const float GildedChargeMinRange = 200f;
        private const float GildedChargeMaxRange = 520f;
        private const float GildedChargeMaxRise = 48f;
        private const int GildedChargeTellTicks = 44;

        protected override bool CanPierce
        {
            get
            {
                if (!NPC.HasValidTarget || _setPiece != KnightSetPiece.None || InWingFlight)
                {
                    return false;
                }

                Player target = Main.player[NPC.target];
                bool sameLevel = Math.Abs(target.Center.Y - NPC.Center.Y) <= GildedChargeMaxRise;
                return sameLevel && NPC.velocity.Y == 0f;
            }
        }

        protected override float PierceRange => GildedChargeMaxRange;
        protected override float MinPierceRange => GildedChargeMinRange;
        protected override int PierceChance => 3;
        protected override int PierceTelegraphTicks => GildedChargeTellTicks;
        protected override float PierceDashSpeed => 14f;
        protected override int PierceDashAccelTicks => 6;
        protected override float PierceOvershootPastTarget => 96f;
        protected override int PierceDashMaxTicks => 46;
        protected override int PierceRecoveryTicks => 60;
        protected override int PierceCooldownAfterUse => 420;
        protected override int PierceStabChance => 0;
        protected override float PierceTelegraphRotation => LevelForward - ThrustCouchRaise;

        // Tell: gold motes converge on the halberd point and a white glint grows there; a ring at the start, a draw at launch.
        protected override void DoPierceWindup(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (elapsed == 0)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center);
            }

            float pointReach = ComboReachBase * 0.7f * ThrustReachMult;
            Vector2 point = HalberdPointAt(pointReach);
            float charge = elapsed / (float)GildedChargeTellTicks;

            if (elapsed % 2 == 0)
            {
                Vector2 from = point + Main.rand.NextVector2CircularEdge(30f, 30f);
                Dust gather = Dust.NewDustPerfect(from, DustID.GoldFlame, (point - from) * 0.1f, 80, default, 1.1f);
                gather.noGravity = true;
            }

            float glintScale = MathHelper.Lerp(0.5f, 1.6f, charge);
            Dust glint = Dust.NewDustPerfect(point, DustID.AncientLight, Vector2.Zero, 30, Color.White, glintScale);
            glint.noGravity = true;

            if (elapsed == GildedChargeTellTicks - 1)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.95f, Pitch = -0.45f }, NPC.Center);
            }
        }

        protected override void DoPierceDashTick()
        {
            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(NPC.Center, 0.8f, 0.7f, 0.3f);
            Dust trail = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.GoldFlame, -NPC.velocity.X * 0.3f, 0f, 100, default, 1.3f);
            trail.noGravity = true;

            Vector2 point = HalberdPointAt(ComboReachBase * 0.7f * ThrustReachMult);
            Dust streak = Dust.NewDustPerfect(point, DustID.Electric, -NPC.velocity * 0.15f, 60, default, 0.9f);
            streak.noGravity = true;
        }

        // Server: one hit on contact; the hitbox carries Fracturing Armor via OnHumanoidMeleeHit.
        protected override void OnPierceContact(Player target, bool isStab)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int chargeDamage = (int)(MeleeDamage * GildedChargeDamageMult);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), target.Center, new Vector2(NPC.direction * 0.01f, 0f),
                ModContent.ProjectileType<PuppetMeleeHitbox>(), chargeDamage, 6f, Main.myPlayer,
                target.width + 12f, target.height + 8f);
        }

        // ── Movement ────────────────────────────────────────────────────────────────
        protected override float TopSpeed => 3.4f;
        protected override float Acceleration => 0.16f;
        protected override float RunDistance => 320f;
        protected override float RunSpeedMult => 1.5f;
        protected override float ClosingDistanceSpeedMult => 2.0f;
        protected override int ClosingDistanceMaxTicks => 150;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.85f;
        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 7f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 7f;

        protected override void RunMovementAI(float speedMult)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 80;
            globalNPC.RemembersLastKnownPos = true;

            // Accelerate harder while sprinting to close a melee gap, so a kiting player sees the knight gaining.
            float acceleration = Acceleration;
            if (Phase == AttackPhase.ClosingDistance)
            {
                acceleration *= 1.4f;
            }

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: acceleration,
                doorBreakingDamage: 5,
                attackRange: 850f);
        }

        // ── Wings ───────────────────────────────────────────────────────────────────
        // Angel Wings: hidden on the ground, spread in the air. No random flight: every takeoff comes from the anti-air
        // ladder in TickServerDecisions (the target airborne out of reach, or standing on a ledge far above).
        protected override bool HasWings => true;
        protected override int WingsAccessoryItemType => ItemID.AngelWings;
        protected override bool ShowWingsWhenGrounded => false;
        protected override int RandomTakeoffChance => 0;
        protected override float FlightHeightTrigger => 99999f;
        protected override float FlightHpEscalationFrac => 0f;
        protected override int AirborneComboChancePercent => 50;

        // The hover keeps station 80px above and 150px beside the target; at 6 px/t it is slower than a winged player at
        // full speed, so the Skyward Pike dash is the catch. MaxFlightTicks 480 caps any one flight at 8s.
        protected override EnemyFlightConfig FlightConfig => new EnemyFlightConfig
        {
            HoverAltitude = 80f,
            HoverSideOffset = 150f,
            HoverTopSpeed = 6.0f,
            TakeOffSpeed = 8.5f,
            DiveAcceleration = 0.55f,
            DiveTopSpeed = 12f,
            LandSpeed = 6.5f,
            MaxFlightTicks = 480,
            CooldownTicks = 60,
            TakeOffTicks = 32,
            HoverDwellTicks = 90,
            StrafeTicks = 60,
            DiveAttackTicks = 46,
            LandTicks = 60,
            WingFlapSpeed = 0.085f,
            StrafeArcHeight = 40f,
        };

        private bool InWingFlight => Flight != null && Flight.IsAirborne;

        protected override bool ForceWingsSpread
        {
            get
            {
                if (_setPiece == KnightSetPiece.PhaseTwo)
                {
                    return true;
                }

                bool pikeInAir = _setPiece == KnightSetPiece.SkywardPike && _skyStage != SkyStage.Landing;
                return pikeInAir;
            }
        }

        // The swoop (the flight controller's pierce-through dive): a forward thrust hitbox in the dive direction.
        protected override void DoAerialDiveHit()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int boxWidth = 150;
            int boxHeight = 70;
            Vector2 center = NPC.Center + new Vector2(NPC.direction * 30f, 0f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), center, Vector2.Zero,
                ModContent.ProjectileType<PuppetMeleeHitbox>(), SpearDamage, 4f, Main.myPlayer, boxWidth, boxHeight);
        }

        // ── Bow: Thunder Arrow / Storm Volley / Grounding Shot ───────────────────────
        // Ulhan's melee + bow split: the halberd in neutral, the bow only through the three shot phases.
        private const float ArrowSpeed = 16f;
        private const int AimLockTicks = 12;
        private const float NockForward = 18f;
        private const float BowDrawScale = 0.55f;

        protected override RangedStyle RangedAnimStyle => RangedStyle.Bow;
        protected override bool UseBowStringDrawPose => true;
        protected override bool ShowRangedWeaponDuringRecovery => true;
        protected override bool AllowRangedPatternsAirborne => true;
        protected override float RangedRange => 900f;
        protected override float MinRangedRange => 280f;
        protected override int RangedTelegraphTicks => 42;
        protected override int RangedRecoveryTicks => 36;
        protected override int RangedCooldownAfterUse => 150;
        protected override int StandingRangedChance => 100;
        protected override Color RangedTelegraphFlashColor => new Color(150, 200, 255);
        protected override bool UseCompositeArmForAdditionalPhase =>
            Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack
            || Phase == AttackPhase.CrossbowBurstPause || Phase == AttackPhase.Custom;
        // ForgottenThunderBow: right-facing 30x78; riser gem (24, 39); string column x6-7, rows 4-74.
        protected override Vector2 GetHeldRangedGripNorm(int itemType) => new Vector2(0.80f, 0.50f);
        protected override float GetHeldRangedDrawScale(int itemType) => BowDrawScale;
        protected override Rectangle GetBowStringTexels(int itemType) => new Rectangle(6, 4, 2, 70);
        protected override string NockedBowArrowTexture => UsefulFunctions.RefactorableFilepath(typeof(ArcherBolt));

        // Grounded: two arrows 45t apart. In the air: Storm Volley, three arrows 8t apart. Grounding Shot: one skyward arrow.
        // The getter reads synced fields only, because a client rebuilds the burst from it in ReceiveExtraAI.
        private static readonly int[][] GroundedArrowPattern = { new[] { 45 } };
        private static readonly int[][] AirVolleyPattern = { new[] { 8, 8 } };
        private static readonly int[][] GroundingShotPattern = { Array.Empty<int>() };

        protected override int[][] PrimaryRangedBurstPatterns
        {
            get
            {
                if (_groundingArmed)
                {
                    return GroundingShotPattern;
                }

                if (_airVolley)
                {
                    return AirVolleyPattern;
                }

                return GroundedArrowPattern;
            }
        }

        // Aim in facing space (positive = down). The Grounding Shot draws to the 45 degree up limit of the bow hold.
        protected override float BowAimAngle
        {
            get
            {
                if (_groundingArmed)
                {
                    return -MathHelper.PiOver4;
                }

                return (float)Math.Atan2(_shotAim.Y, _shotAim.X * NPC.direction);
            }
        }

        protected override void FaceTargetForRangedShot(Player target)
        {
            // The last AimLockTicks of the tell and the release keep the synced shot's facing.
            if (Phase == AttackPhase.RangedAttack || PhaseTimer <= AimLockTicks)
            {
                int facing = 1;
                if (_shotAim.X < 0f)
                {
                    facing = -1;
                }

                NPC.direction = facing;
                NPC.spriteDirection = facing;
                return;
            }

            base.FaceTargetForRangedShot(target);
        }

        private Vector2 BowNock => ScaleAboutFeet(PuppetBowGripPosition) + _shotAim * NockForward;

        /// <summary>Server: a straight lead (no drop - the bolt has no gravity), clamped to the bow's ±45 degree hold.</summary>
        private Vector2 SolveShotAim()
        {
            if (!NPC.HasValidTarget)
            {
                return new Vector2(NPC.direction, 0f);
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = ScaleAboutFeet(PuppetBowGripPosition);
            Vector2 aimAt = target.Center;

            for (int i = 0; i < 3; i++)
            {
                float flightTicks = Vector2.Distance(origin, aimAt) / ArrowSpeed;
                aimAt = target.Center + target.velocity * flightTicks;
            }

            Vector2 direction = (aimAt - origin).SafeNormalize(new Vector2(NPC.direction, 0f));
            int facing = 1;
            if (direction.X < 0f)
            {
                facing = -1;
            }

            float angle = (float)Math.Atan2(direction.Y, Math.Abs(direction.X));
            angle = MathHelper.Clamp(angle, -MathHelper.PiOver4, MathHelper.PiOver4);
            return new Vector2((float)Math.Cos(angle) * facing, (float)Math.Sin(angle));
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, PitchVariance = 0.25f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoStabAttack()
        {
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.65f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: StabRange * 0.65f);
        }

        // Runs on every peer: sounds play everywhere, projectiles spawn on the server only.
        protected override void DoRangedAttack()
        {
            if (!NPC.HasValidTarget)
            {
                return;
            }

            bool server = Main.netMode != NetmodeID.MultiplayerClient;

            if (_groundingArmed)
            {
                // Grounding Shot: a visual-only arrow streaks up off screen; the strike lands under the target 30t later.
                Vector2 skyward = new Vector2(NPC.direction * 0.35f, -1f).SafeNormalize(-Vector2.UnitY);
                Vector2 nock = ScaleAboutFeet(PuppetBowGripPosition) + skyward * NockForward;
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.8f, Pitch = -0.5f }, nock);

                if (server)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), nock, skyward * 20f,
                        ModContent.ProjectileType<RoyalThunderArrow>(), 0, 0f, Main.myPlayer, 1f);
                    _groundingArmed = false;
                    _groundingCooldown = GroundingCooldownTicks;
                    _groundingStrikeTimer = GroundingStrikeDelayTicks;
                    NPC.netUpdate = true;
                }

                return;
            }

            // Per-shot re-aim (Ulhan): the server re-solves, and the synced aim drives every client's pose.
            if (server)
            {
                _shotAim = SolveShotAim();
            }

            Vector2 origin = BowNock;
            SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.7f, Pitch = -0.3f, PitchVariance = 0.1f }, origin);

            if (!server)
            {
                return;
            }

            int arrowDamage = EnemyDamage.Projectile(ThunderArrowExpertDamage);
            if (_airVolley)
            {
                arrowDamage = EnemyDamage.Projectile(VolleyArrowExpertDamage);
            }

            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, _shotAim * ArrowSpeed,
                ModContent.ProjectileType<RoyalThunderArrow>(), arrowDamage, 2f, Main.myPlayer);
            NPC.netUpdate = true;
        }

        // Tell: electric and gold motes converge on the nocked arrowhead through the last 35% of the draw; the Grounding
        // Shot's arrowhead burns brighter. Cosmetic, every peer.
        protected override void DoRangedTelegraphVFX(bool secondary, float progress)
        {
            if (Main.dedServ || progress < 0.65f)
            {
                return;
            }

            Vector2 aim = _shotAim;
            if (_groundingArmed)
            {
                aim = new Vector2(NPC.direction * 0.35f, -1f).SafeNormalize(-Vector2.UnitY);
            }

            Vector2 arrowhead = ScaleAboutFeet(PuppetBowGripPosition) + aim * (NockForward + 10f);
            Vector2 from = arrowhead + Main.rand.NextVector2CircularEdge(20f, 20f);
            int moteType = DustID.Electric;
            if (Main.rand.NextBool(3))
            {
                moteType = DustID.GoldFlame;
            }

            Dust mote = Dust.NewDustPerfect(from, moteType, (arrowhead - from) * 0.12f, 100, default, 0.8f);
            mote.noGravity = true;

            float lightStrength = 0.6f;
            if (_groundingArmed)
            {
                lightStrength = 1.0f;
            }

            Lighting.AddLight(arrowhead, 0.4f * lightStrength, 0.55f * lightStrength, 0.9f * lightStrength);
        }

        // ── Phases ──────────────────────────────────────────────────────────────────
        // One change at 50%: a 30t hyper-armored flare (wings spread even on the ground), then Sunfall Cleave, the Grounding
        // Shot, +1 thrust rep and shorter anti-air/flight cooldowns. _phaseTwo is set when the flare STARTS and is synced.
        private const float PhaseTwoLifeFraction = 0.50f;
        private const int PhaseTwoTicks = 30;
        private bool _phaseTwo;

        // ── Custom set-pieces: the phase-two flare and Skyward Pike ─────────────────
        private enum KnightSetPiece : byte
        {
            None,
            PhaseTwo,
            SkywardPike,
        }

        private enum SkyStage : byte
        {
            None,
            Hop,
            Aim,
            Dash,
            Fall,
            Landing,
        }

        private KnightSetPiece _setPiece;
        private SkyStage _skyStage;
        private int _skyStageTicks;
        private float _skyRotation;
        private Vector2 _skyDirection;
        private int _skyDashTicks;
        private int _skyAirTicks;
        private bool _skyBladeHit; // server-only
        private bool _skyFromHover;

        /*
         * Skyward Pike (Artorias's Skyward Lunge, retuned for the halberd and the wings).
         * Hop:     ground only. Straight up, the rise solved to the target's height 10t ahead + 24px (Shadow Ninja's Front
         *          Flip solver), clamped 64..240px, gated on headroom. From the hover it skips straight to the aim.
         * Aim:     30t paused (gravity suspended), wings spread, body + halberd shake 1 -> 3px, motes converge on the point;
         *          the last 20t draw the dash line and the aim is locked when it starts.
         * Dash:    straight, gravity off, at a point 96px past the target; speed 0 -> 24 px/t (squared) over 3t; ends on
         *          arrival + 8t, at 70t, on a tile or on a hit; the blade is live the whole dash (136px thrust reach).
         * Fall:    momentum x 0.35 and a normal fall. Landing: 50t planted, open.
         * Counter: roll through the launch (the dash crosses the reach in ~6t, well inside 22t of i-frames), or leave the
         *          line during the aim. One live window: no post-roll gap.
         * Armor:   the dash, the fall and the last 20t of aim are committed; the hop and the first 10t of aim can be staggered.
         * MP:      the server starts and branches every stage (RequestNetworkSnapshot on each); every peer runs the motion.
         */
        private const int SkyPhaseSafetyTicks = 600;
        private const int SkyAimTicks = 30;
        private const int SkyAimLineTicks = 20;
        private const float SkyDashSpeed = 24f;
        private const int SkyDashAccelTicks = 3;
        private const int SkyDashMaxTicks = 70;
        private const int SkyOvershootTicks = 8;
        private const float SkyAimPastTarget = 96f;
        private const float SkyMissMomentum = 0.35f;
        private const int SkyLandingTicks = 50;
        private const int SkyMaxAirTicks = 200;
        private const float SkyGravity = 0.3f;
        private const float SkyHopLeadTicks = 10f;
        private const float SkyHopOvershoot = 24f;
        private const float SkyHopMinRise = 64f;
        private const float SkyHopMaxRise = 240f;
        private const float SkyHopMaxUpSpeed = 12f;

        private bool SkywardPikeActive => Phase == AttackPhase.Custom && _setPiece == KnightSetPiece.SkywardPike;

        protected override bool SlowDownDuringCustom => _setPiece != KnightSetPiece.SkywardPike;

        protected override bool CustomAttackCommitted
        {
            get
            {
                if (_setPiece == KnightSetPiece.PhaseTwo)
                {
                    return true;
                }

                if (_setPiece != KnightSetPiece.SkywardPike)
                {
                    return false;
                }

                bool lateAim = _skyStage == SkyStage.Aim && _skyStageTicks >= SkyAimTicks - SkyAimLineTicks;
                return lateAim || _skyStage == SkyStage.Dash || _skyStage == SkyStage.Fall;
            }
        }

        // The phase-two flare holds the halberd raised high; Skyward Pike drives the halberd itself.
        private const float FlareRaisedRotation = -1.2f;

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (_setPiece == KnightSetPiece.SkywardPike)
                {
                    return _skyRotation;
                }

                if (_setPiece == KnightSetPiece.PhaseTwo)
                {
                    return FlareRaisedRotation;
                }

                return null;
            }
        }

        // Skyward Pike's aim shake: two sines at unrelated rates, so every draw call in a frame agrees. Visual only.
        protected override Vector2 PuppetVisualOffset
        {
            get
            {
                if (!SkywardPikeActive || _skyStage != SkyStage.Aim)
                {
                    return Vector2.Zero;
                }

                float aimProgress = MathHelper.Clamp(_skyStageTicks / (float)SkyAimTicks, 0f, 1f);
                float amplitude = MathHelper.Lerp(1f, 3f, aimProgress);
                float time = Main.GameUpdateCount;
                float shakeX = (float)Math.Sin(time * 2.7f) * amplitude;
                float shakeY = (float)Math.Sin(time * 3.9f) * amplitude * 0.5f;
                return new Vector2(shakeX, shakeY);
            }
        }

        protected override void OnBladeHit(Player player)
        {
            base.OnBladeHit(player);
            _skyBladeHit = true;
        }

        private void StartSetPiece(KnightSetPiece setPiece, int totalTicks)
        {
            _setPiece = setPiece;
            StartCustomAttack(totalTicks, MeleeWeaponItemType, swingPose: false);
            NPC.netUpdate = true;
        }

        protected override void DoCustomAttack()
        {
            if (_setPiece != KnightSetPiece.PhaseTwo)
            {
                return;
            }

            DebugAttackLabel = "Royal Oath (phase two)";
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 3f, 12);

            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 40; i++)
            {
                int burstDust = DustID.GoldFlame;
                if (i % 3 == 0)
                {
                    burstDust = DustID.Electric;
                }

                Dust burst = Dust.NewDustPerfect(NPC.Center, burstDust, Main.rand.NextVector2Circular(6f, 6f), 60, default, 1.4f);
                burst.noGravity = true;
            }
        }

        private void StartSkywardPike(bool fromHover)
        {
            _skyFromHover = fromHover;
            _skyBladeHit = false;
            _skyAirTicks = 0;
            _antiAirCooldown = AntiAirCooldown;
            _skyRotation = BladeRotationToward(PuppetWeaponDirection);

            SkyStage openingStage = SkyStage.Hop;
            if (fromHover)
            {
                // The wings stop driving the body: the pike's stage machine owns velocity from here.
                Flight?.EndFlightNow();
                openingStage = SkyStage.Aim;
            }

            EnterSkyStage(openingStage);
            StartSetPiece(KnightSetPiece.SkywardPike, SkyPhaseSafetyTicks);
            DebugAttackLabel = "Skyward Pike";
            PlaySkyStageCue(openingStage);
        }

        private void EnterSkyStage(SkyStage stage)
        {
            _skyStage = stage;
            _skyStageTicks = 0;
            RequestNetworkSnapshot();
        }

        private void EndSkywardPike()
        {
            NPC.noGravity = false;
            StopMeleeHit();
            _skyStage = SkyStage.None;
            _setPiece = KnightSetPiece.None;
            DebugAttackLabel = null;
            PhaseTimer = 1;
            RequestNetworkSnapshot();
        }

        private void PlaySkyStageCue(SkyStage stage)
        {
            switch (stage)
            {
                case SkyStage.Hop:
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.35f }, NPC.Center);
                    break;

                case SkyStage.Aim:
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.55f, Pitch = 0.3f }, NPC.Center);
                    break;

                case SkyStage.Dash:
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center);
                    break;
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (_setPiece != KnightSetPiece.SkywardPike)
            {
                if (_setPiece == KnightSetPiece.PhaseTwo && ticksRemaining <= 1)
                {
                    _setPiece = KnightSetPiece.None;
                }

                return;
            }

            // Every stage ends itself; keep the base Custom timer from expiring under it.
            PhaseTimer = SkyPhaseSafetyTicks;
            _skyStageTicks++;

            bool server = Main.netMode != NetmodeID.MultiplayerClient;
            Player target = Main.player[NPC.target];
            bool grounded = NPC.velocity.Y == 0f;

            if (!grounded)
            {
                _skyAirTicks++;
            }

            // Stuck in the air far longer than any arc takes: end and let the navigator recover.
            if (server && _skyAirTicks > SkyMaxAirTicks)
            {
                EndSkywardPike();
                return;
            }

            switch (_skyStage)
            {
                case SkyStage.Hop:
                {
                    FaceTarget(target);

                    if (_skyStageTicks == 1)
                    {
                        // Shadow Ninja's solver: rise to where the target will be 10t from now, plus 24px.
                        float ledTargetY = target.Center.Y + target.velocity.Y * SkyHopLeadTicks;
                        float rise = NPC.Center.Y - ledTargetY + SkyHopOvershoot;
                        rise = MathHelper.Clamp(rise, SkyHopMinRise, SkyHopMaxRise);
                        float upSpeed = (float)Math.Sqrt(2f * SkyGravity * rise);
                        upSpeed = Math.Min(upSpeed, SkyHopMaxUpSpeed);
                        NPC.velocity = new Vector2(0f, -upSpeed);
                        NPC.netUpdate = true;
                    }

                    // Past the apex: freeze there, gravity suspended, for the aim.
                    bool pastApex = NPC.velocity.Y >= 0f && _skyStageTicks > 1;
                    if (server && pastApex)
                    {
                        NPC.noGravity = true;
                        NPC.velocity = Vector2.Zero;
                        EnterSkyStage(SkyStage.Aim);
                        PlaySkyStageCue(SkyStage.Aim);
                    }
                    break;
                }

                case SkyStage.Aim:
                {
                    // SmartFighter4 hands gravity back every tick: re-assert it so the pause never turns into a fall.
                    NPC.noGravity = true;
                    NPC.velocity = Vector2.Zero;
                    FaceTarget(target);

                    bool aimLocked = _skyStageTicks > SkyAimTicks - SkyAimLineTicks;
                    if (!aimLocked)
                    {
                        _skyDirection = SolveSkywardAim(target);
                    }

                    // AngleLerp takes the short way round, so the halberd swings onto the aim instead of snapping.
                    float aimRotation = BladeRotationToward(_skyDirection);
                    _skyRotation = _skyRotation.AngleLerp(aimRotation, 0.35f);

                    if (!Main.dedServ)
                    {
                        float pointReach = ComboReachBase * 0.7f * ThrustReachMult;
                        Vector2 point = HalberdPointAt(pointReach);
                        float ringRadius = MathHelper.Lerp(60f, 20f, _skyStageTicks / (float)SkyAimTicks);
                        Vector2 moteStart = point + Main.rand.NextVector2CircularEdge(ringRadius, ringRadius);
                        int moteType = DustID.GoldFlame;
                        if (Main.rand.NextBool(3))
                        {
                            moteType = DustID.Electric;
                        }

                        Dust mote = Dust.NewDustPerfect(moteStart, moteType, (point - moteStart) * 0.12f, 100, default, 1f);
                        mote.noGravity = true;

                        // The dash line: 8 motes from the knight to the locked aim point every 4t across the last 20t.
                        bool drawPathLine = aimLocked && _skyStageTicks % 4 == 0;
                        if (drawPathLine)
                        {
                            float pathLength = SkyDashPathLength(target);
                            for (int i = 1; i <= 8; i++)
                            {
                                Vector2 linePoint = NPC.Center + _skyDirection * (pathLength * i / 8f);
                                Dust lineDust = Dust.NewDustPerfect(linePoint, DustID.GoldFlame, Vector2.Zero, 100, Color.White, 0.9f);
                                lineDust.noGravity = true;
                            }
                        }
                    }

                    if (server && _skyStageTicks >= SkyAimTicks)
                    {
                        // The squared ramp covers a third of full-speed distance, so it adds 2/3 of its ticks; then the 8t
                        // overshoot so an on-time dash always reaches the aim point.
                        float pathTicks = SkyDashPathLength(target) / SkyDashSpeed;
                        float rampLossTicks = SkyDashAccelTicks * 2f / 3f;
                        int flightTicks = (int)Math.Ceiling(pathTicks + rampLossTicks) + SkyOvershootTicks;
                        _skyDashTicks = Math.Min(flightTicks, SkyDashMaxTicks);
                        _skyRotation = BladeRotationToward(_skyDirection);
                        _skyBladeHit = false;
                        TryMeleeHit(reach: ComboReachBase * 0.7f * ThrustReachMult);
                        EnterSkyStage(SkyStage.Dash);
                        PlaySkyStageCue(SkyStage.Dash);
                    }
                    break;
                }

                case SkyStage.Dash:
                {
                    NPC.noGravity = true;
                    float rampProgress = Math.Min(1f, _skyStageTicks / (float)SkyDashAccelTicks);
                    float dashSpeed = SkyDashSpeed * rampProgress * rampProgress;
                    NPC.velocity = _skyDirection * dashSpeed;

                    if (_skyDirection.X != 0f)
                    {
                        NPC.direction = Math.Sign(_skyDirection.X);
                        NPC.spriteDirection = NPC.direction;
                    }

                    _skyRotation = BladeRotationToward(_skyDirection);
                    TickBladeHit();

                    if (!Main.dedServ)
                    {
                        Vector2 trailPoint = NPC.Center + Main.rand.NextVector2Circular(12f, 12f);
                        Dust trail = Dust.NewDustPerfect(trailPoint, DustID.GoldFlame, -NPC.velocity * 0.2f, 100, default, Main.rand.NextFloat(1f, 1.4f));
                        trail.noGravity = true;
                        Dust spark = Dust.NewDustPerfect(HalberdPointAt(ComboReachBase * 0.7f * ThrustReachMult), DustID.Electric, -NPC.velocity * 0.1f, 60, default, 0.8f);
                        spark.noGravity = true;
                    }

                    // collideX/Y come from last tick's physics step; skip the first ticks, when the feet still touch the floor.
                    bool blocked = _skyStageTicks > 2 && (NPC.collideX || NPC.collideY);
                    bool dashOver = _skyStageTicks >= _skyDashTicks;
                    if (server && (_skyBladeHit || blocked || dashOver))
                    {
                        StopMeleeHit();
                        NPC.noGravity = false;
                        NPC.velocity *= SkyMissMomentum;
                        NPC.netUpdate = true;
                        EnterSkyStage(SkyStage.Fall);
                    }
                    break;
                }

                case SkyStage.Fall:
                {
                    NPC.noGravity = false;
                    // The halberd settles back toward the carry while falling.
                    _skyRotation = _skyRotation.AngleLerp(-0.3f, 0.12f);

                    bool landed = _skyStageTicks > 2 && NPC.velocity.Y == 0f;
                    if (server && landed)
                    {
                        EnterSkyStage(SkyStage.Landing);
                        UsefulFunctions.ScreenShake(NPC.Bottom, 1.5f, 8);
                    }
                    break;
                }

                case SkyStage.Landing:
                {
                    NPC.noGravity = false;
                    NPC.velocity.X *= 0.8f;
                    _skyRotation = _skyRotation.AngleLerp(-0.3f, 0.12f);

                    if (server && _skyStageTicks >= SkyLandingTicks)
                    {
                        EndSkywardPike();
                    }
                    break;
                }
            }
        }

        /// <summary>Direction of the Skyward Pike dash: at the target's position led by the dash's flight time, then 96px
        /// beyond it on the far side, so the halberd passes through and the knight lands past them.</summary>
        private Vector2 SolveSkywardAim(Player target)
        {
            float leadTicks = Math.Min(NPC.Distance(target.Center) / SkyDashSpeed, SkyDashMaxTicks);
            Vector2 interceptPoint = target.Center + target.velocity * leadTicks;

            int pastSide = Math.Sign(interceptPoint.X - NPC.Center.X);
            if (pastSide == 0)
            {
                pastSide = NPC.direction;
            }

            Vector2 aimPoint = interceptPoint + new Vector2(pastSide * SkyAimPastTarget, 0f);
            return (aimPoint - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));
        }

        /// <summary>Length of the dash path along the locked direction: the target's distance projected on it, plus the
        /// 96px overshoot.</summary>
        private float SkyDashPathLength(Player target)
        {
            Vector2 toTarget = target.Center - NPC.Center;
            float alongDash = Math.Max(0f, Vector2.Dot(toTarget, _skyDirection));
            return alongDash + SkyAimPastTarget;
        }

        private void FaceTarget(Player target)
        {
            int facing = 1;
            if (target.Center.X < NPC.Center.X)
            {
                facing = -1;
            }

            NPC.direction = facing;
            NPC.spriteDirection = facing;
        }

        /// <summary>Swing-space rotation that draws the halberd along <paramref name="worldDirection"/> at the current facing
        /// (Artorias's inverse of PuppetNPC.GetWeaponWorldDirection's mirrored path, no blade flip).</summary>
        private float BladeRotationToward(Vector2 worldDirection)
        {
            float worldAngle = worldDirection.ToRotation();
            float drawOffset = FrontHandWeapon.RotationOffset;
            float restRadians = MathHelper.ToRadians(MeleeNaturalRestAngleDeg);

            if (NPC.direction == 1)
            {
                return MathHelper.WrapAngle(worldAngle + restRadians) - drawOffset;
            }

            float mirroredRestRadians = MathHelper.Pi - restRadians;
            return -MathHelper.WrapAngle(worldAngle + mirroredRestRadians) - drawOffset;
        }

        // ── Server decisions: the anti-air ladder, flight, phase two, Grounding Shot ──
        // The anti-air ladder (first match wins):
        //   1. airborne 48-240px above, <= 160px across -> Ascending Pike (the combo gate above).
        //   2. airborne >= 30t, <= 600px, LOS -> Skyward Pike from the ground: 6%/tick from neutral, or out of a melee recovery.
        //   3. airborne >= 60t and (> 240px above or > 300px away), headroom -> take off (Sky Pursuit); in the hover the base
        //      picks Storm Volley / Wing Sweep / Wing Dive and this picks Skyward Pike from the hover.
        //   4. grounded >= 240px above for 45t -> take off (Rise) to volley or dive onto the ledge.
        // "Airborne" is Artorias's test: not standing (two still ticks), or hanging from a hook. Server-only counters.
        private const int AirborneTriggerTicks = 30;
        private const int PursuitTriggerTicks = 60;
        private const int LedgeTriggerTicks = 45;
        private const int LandAfterGroundedTicks = 60;
        private const float AntiAirMaxRange = 600f;
        private const float PursuitMinRise = 240f;
        private const float PursuitMinDistance = 300f;
        private const int SkyOpenerChancePercent = 6;
        private const int HoverSettleTicks = 40;
        private const int HoverPikeChanceDenominator = 60;
        private const int AntiAirCooldownPhaseOne = 360;
        private const int AntiAirCooldownPhaseTwo = 240;
        private const int FlightCooldownPhaseOne = 300;
        private const int FlightCooldownPhaseTwo = 200;

        private int AntiAirCooldown => _phaseTwo ? AntiAirCooldownPhaseTwo : AntiAirCooldownPhaseOne;
        private int FlightCooldown => _phaseTwo ? FlightCooldownPhaseTwo : FlightCooldownPhaseOne;

        private int _targetAirborneTicks;
        private int _targetGroundedTicks;
        private int _targetOnLedgeTicks;
        private int _antiAirCooldown;
        private int _flightCooldown;
        private int _hoverTicks;

        // Grounding Shot: armed while idle (phase two, target >= 400px, LOS, cooldown ready, ~1 in 4 eligible seconds), so
        // the next ranged pick is the single skyward shot. The strike lands 30t after the release, under the target.
        private const float GroundingMinRange = 400f;
        private const int GroundingArmChanceDenominator = 240;
        private const int GroundingCooldownTicks = 600;
        private const int GroundingStrikeDelayTicks = 30;
        private bool _groundingArmed;
        private int _groundingCooldown;
        private int _groundingStrikeTimer;

        private Vector2 _shotAim = Vector2.UnitX;
        private bool _airVolley;

        private void TickServerDecisions()
        {
            if (_antiAirCooldown > 0)
            {
                _antiAirCooldown--;
            }

            if (_flightCooldown > 0)
            {
                _flightCooldown--;
            }

            if (_groundingCooldown > 0)
            {
                _groundingCooldown--;
            }

            // Wing Dive's second spark pair.
            if (_diveSparkTimer > 0)
            {
                _diveSparkTimer--;
                if (_diveSparkTimer == 0)
                {
                    SpawnFloorSparkPair();
                }
            }

            if (!NPC.HasValidTarget)
            {
                _targetAirborneTicks = 0;
                _targetGroundedTicks = 0;
                _targetOnLedgeTicks = 0;
                return;
            }

            Player target = Main.player[NPC.target];

            // The Grounding Shot's strike: Gwyn's lightning planted under the target's feet (it floor-snaps itself and
            // runs its own 40t rune before striking; its floor sparks follow).
            if (_groundingStrikeTimer > 0)
            {
                _groundingStrikeTimer--;
                if (_groundingStrikeTimer == 0)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), target.Center, Vector2.Zero,
                        ModContent.ProjectileType<GwynLightningStrike>(), EnemyDamage.Projectile(GroundingStrikeExpertDamage),
                        4f, Main.myPlayer);
                }
            }

            bool targetStanding = target.velocity.Y == 0f && target.oldVelocity.Y == 0f && target.grappling[0] < 0;
            float rise = NPC.Center.Y - target.Center.Y;
            float distance = NPC.Distance(target.Center);
            bool clearLine = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            if (targetStanding)
            {
                _targetAirborneTicks = 0;
                _targetGroundedTicks++;
            }
            else
            {
                _targetAirborneTicks++;
                _targetGroundedTicks = 0;
            }

            bool onLedgeAbove = targetStanding && rise >= PursuitMinRise;
            if (onLedgeAbove)
            {
                _targetOnLedgeTicks++;
            }
            else
            {
                _targetOnLedgeTicks = 0;
            }

            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
            bool inMeleeRecovery = Phase == AttackPhase.MeleeComboRecovery || Phase == AttackPhase.MeleeRecovery;
            bool knightStanding = NPC.velocity.Y == 0f && !InWingFlight;

            // Phase two: once, from neutral on the ground.
            bool phaseTwoDue = !_phaseTwo && NPC.life <= NPC.lifeMax * PhaseTwoLifeFraction;
            if (phaseTwoDue && neutral && knightStanding)
            {
                _phaseTwo = true;
                StartSetPiece(KnightSetPiece.PhaseTwo, PhaseTwoTicks);
                return;
            }

            // In the air: count the hover, pick Skyward Pike from it, and land once the target has stood still a while.
            if (InWingFlight)
            {
                if (Flight.Mode == FlightMode.Hover)
                {
                    _hoverTicks++;
                }
                else
                {
                    _hoverTicks = 0;
                }

                bool pikeReady = _antiAirCooldown <= 0 && _targetAirborneTicks >= AirborneTriggerTicks
                    && distance <= AntiAirMaxRange && clearLine;
                bool hoverSettled = Flight.Mode == FlightMode.Hover && _hoverTicks >= HoverSettleTicks;
                if (Phase == AttackPhase.Idle && hoverSettled && pikeReady && Main.rand.NextBool(HoverPikeChanceDenominator))
                {
                    StartSkywardPike(fromHover: true);
                    return;
                }

                // The base gets the first 30 of these ticks to pick a Wing Dive at the standing target; then the knight lands.
                bool targetSettled = _targetGroundedTicks >= LandAfterGroundedTicks && _targetOnLedgeTicks == 0;
                if (Phase == AttackPhase.Idle && targetSettled)
                {
                    Flight.RequestLand();
                }

                return;
            }

            _hoverTicks = 0;

            // Grounding Shot arming.
            bool groundingEligible = _phaseTwo && !_groundingArmed && _groundingCooldown <= 0 && distance >= GroundingMinRange
                && clearLine && neutral && knightStanding;
            if (groundingEligible && Main.rand.NextBool(GroundingArmChanceDenominator))
            {
                _groundingArmed = true;
                NPC.netUpdate = true;
            }

            if (!knightStanding)
            {
                return;
            }

            // 1. Ascending Pike's band belongs to the combo pool; nothing here.
            bool pikeBand = rise > GroundCutMaxRise && rise <= AscendingPikeMaxRise
                && Math.Abs(target.Center.X - NPC.Center.X) <= AscendingPikeMaxHorizontal;

            // 2. Skyward Pike from the ground.
            bool skywardEligible = _antiAirCooldown <= 0 && _targetAirborneTicks >= AirborneTriggerTicks
                && distance <= AntiAirMaxRange && clearLine && !pikeBand && HasHeadroom(SkyHopMaxRise);
            bool openerRoll = neutral && Main.rand.Next(100) < SkyOpenerChancePercent;
            if (skywardEligible && (openerRoll || inMeleeRecovery))
            {
                StartSkywardPike(fromHover: false);
                return;
            }

            // 3 and 4. Take off: Sky Pursuit at an airborne target out of reach, or Rise at one standing on a high ledge.
            if (!neutral || _flightCooldown > 0)
            {
                return;
            }

            bool pursuitDue = _targetAirborneTicks >= PursuitTriggerTicks && (rise > PursuitMinRise || distance > PursuitMinDistance);
            bool riseDue = _targetOnLedgeTicks >= LedgeTriggerTicks;
            float neededRise = MathHelper.Clamp(rise, 0f, SkyHopMaxRise);
            if ((pursuitDue || riseDue) && HasHeadroom(neededRise) && Flight != null && Flight.RequestTakeoff())
            {
                _flightCooldown = FlightCooldown;
                _hoverTicks = 0;
                DebugAttackLabel = pursuitDue ? "Sky Pursuit" : "Rise";
                NPC.netUpdate = true;
            }
        }

        /// <summary>Shadow Ninja's headroom check: a body-wide column <paramref name="height"/> px above the knight is open air.</summary>
        private bool HasHeadroom(float height)
        {
            int clearHeight = Math.Max(1, (int)height);
            Vector2 aboveHead = NPC.position - new Vector2(0f, clearHeight);
            return !Collision.SolidCollision(aboveHead, NPC.width, clearHeight);
        }

        // ── AI ──────────────────────────────────────────────────────────────────────
        private AttackPhase _lastPhase = AttackPhase.Idle;
        private bool _wasDiving;

        public override void AI()
        {
            bool server = Main.netMode != NetmodeID.MultiplayerClient;

            if (server)
            {
                // The burst pattern getter reads this when a ranged burst starts; synced.
                bool airVolley = InWingFlight;
                if (airVolley != _airVolley)
                {
                    _airVolley = airVolley;
                    NPC.netUpdate = true;
                }

                // Aim tracks through the tell and locks AimLockTicks before each release (Ulhan's pattern).
                bool shotTell = Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.CrossbowBurstPause;
                if (shotTell && PhaseTimer > AimLockTicks && NPC.HasValidTarget)
                {
                    _shotAim = SolveShotAim();
                    if (PhaseTimer % 6 == 0 || PhaseTimer == AimLockTicks + 1)
                    {
                        NPC.netUpdate = true;
                    }
                }

                TickServerDecisions();
            }

            AttackPhase previousPhase = Phase;
            base.AI();

            // All damage comes from the halberd, the arrows and the sparks.
            NPC.damage = 0;

            TickWeaponSwapCues(previousPhase);
            TickPresentation();
            _lastPhase = Phase;
        }

        /// <summary>Weapon swap (attack-quality §2): the bow appears on the first ranged-telegraph tick with a gold+electric
        /// puff at the bow hand and Item60; the halberd returns on the first tick after the shot's recovery with a gold
        /// puff. Every peer (the phases are synced).</summary>
        private void TickWeaponSwapCues(AttackPhase previousPhase)
        {
            bool bowDrawn = Phase == AttackPhase.RangedTelegraph && previousPhase != AttackPhase.RangedTelegraph
                && previousPhase != AttackPhase.CrossbowBurstPause && previousPhase != AttackPhase.RangedAttack;
            bool shotPhase = Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack
                || Phase == AttackPhase.CrossbowBurstPause || Phase == AttackPhase.RangedRecovery;
            bool wasShotPhase = _lastPhase == AttackPhase.RangedTelegraph || _lastPhase == AttackPhase.RangedAttack
                || _lastPhase == AttackPhase.CrossbowBurstPause || _lastPhase == AttackPhase.RangedRecovery;
            bool halberdBack = wasShotPhase && !shotPhase;

            if (Phase == AttackPhase.RangedRecovery)
            {
                SetDisplayWeapon(RangedWeaponItemType, swing: false);
            }

            if (halberdBack)
            {
                SetDisplayWeapon(MeleeWeaponItemType, swing: false);
            }

            if (Main.dedServ)
            {
                return;
            }

            if (bowDrawn)
            {
                SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.5f }, NPC.Center);
                Vector2 bowHand = ScaleAboutFeet(PuppetBowGripPosition);
                for (int i = 0; i < 12; i++)
                {
                    int puffDust = DustID.GoldFlame;
                    if (i % 2 == 0)
                    {
                        puffDust = DustID.Electric;
                    }

                    Dust puff = Dust.NewDustPerfect(bowHand, puffDust, Main.rand.NextVector2Circular(2f, 2f), 80, default, 1f);
                    puff.noGravity = true;
                }
            }

            if (halberdBack)
            {
                Vector2 grip = ScaleAboutFeet(PuppetWeaponTipPosition(0f));
                for (int i = 0; i < 10; i++)
                {
                    Dust puff = Dust.NewDustPerfect(grip, DustID.GoldFlame, Main.rand.NextVector2Circular(2f, 2f), 80, default, 1f);
                    puff.noGravity = true;
                }
            }
        }

        /// <summary>Cosmetic, every peer: the swoop's dive sound edge, hover dust from the wing roots, the landing puff and
        /// the phase-two aura.</summary>
        private void TickPresentation()
        {
            bool nowDiving = Flight != null && Flight.IsDiving;
            if (nowDiving && !_wasDiving && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.5f, PitchVariance = 0.12f }, NPC.Center);
            }
            _wasDiving = nowDiving;

            if (Main.dedServ)
            {
                return;
            }

            // Hover: 1 gold + 1 white mote per 3t from the wing roots (just behind the shoulders).
            if (InWingFlight && Main.GameUpdateCount % 3 == 0)
            {
                Vector2 wingRoot = NPC.Center + new Vector2(-NPC.direction * 10f, -8f);
                Dust gold = Dust.NewDustPerfect(wingRoot + Main.rand.NextVector2Circular(8f, 6f), DustID.GoldFlame, new Vector2(0f, 1f), 100, default, 0.9f);
                gold.noGravity = true;
                Dust white = Dust.NewDustPerfect(wingRoot + Main.rand.NextVector2Circular(8f, 6f), DustID.AncientLight, new Vector2(0f, 0.8f), 100, Color.White, 0.7f);
                white.noGravity = true;
            }

            // Phase two: one rising gold or electric mote every 4t.
            if (_phaseTwo && Main.GameUpdateCount % 4 == 0)
            {
                Vector2 auraSpot = NPC.Center + Main.rand.NextVector2Circular(14f, 26f);
                int auraDust = DustID.GoldFlame;
                if (Main.rand.NextBool(3))
                {
                    auraDust = DustID.Electric;
                }

                Dust aura = Dust.NewDustPerfect(auraSpot, auraDust, new Vector2(0f, -1.2f), 0, default, 0.9f);
                aura.noGravity = true;
            }
        }

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);

            // A stagger takes the Custom phase away: drop the pike cleanly and give gravity back.
            NPC.noGravity = false;
            _skyStage = SkyStage.None;
            if (_setPiece == KnightSetPiece.SkywardPike)
            {
                _setPiece = KnightSetPiece.None;
            }
        }

        // ── Stats ─────────────────────────────────────────────────────────────────
        public override void SetStaticDefaults()
        {
            // RoyalKnight.png is the old Knight of Gwyn sheet (80x930, 15 frames of 62px). The puppet draws itself; the
            // frame count only keeps the bestiary portrait to the standing frame instead of the whole strip.
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 30;
            NPC.height = 40;
            NPC.npcSlots = 7;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 75;
            NPC.lifeMax = 25000; // the EnemyBalance registry raises this to 150,000 in Super Hardmode
            NPC.value = 100000;  // Expert: 4,000 Dark Souls
            NPC.knockBackResist = 0f; // replaced by the PoiseProfiles entry (0.15, 120)
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lavaImmune = true;
            NPC.coldDamage = true;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.AllowDynamicEventNaturalDespawn = true;
            globalNPC.NavSearchRadius = 80;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.Agility = 0f;
            globalNPC.PoiseStaggerResetsAI = true;
            // Nav stuck-recovery only: no on-hit or line-of-sight teleports. The wings and gap-closers do the chasing.
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.RecoveryOnly;
        }

        public override void OnKill()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                string goreName = "Easterling Gore " + (1 + i % 3);
                Vector2 goreVelocity = new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, goreVelocity, Mod.Find<ModGore>(goreName).Type, 1f);
            }
        }

        // ── Multiplayer payload: subclass fields FIRST (the base rebuilds the burst pattern from PrimaryRangedBurstPatterns
        // inside its own read, which reads _groundingArmed / _airVolley), then base. Same order on read. ──────────────────
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_phaseTwo);
            writer.Write(_groundingArmed);
            writer.Write(_airVolley);
            writer.Write(_shotAim.X);
            writer.Write(_shotAim.Y);
            writer.Write((byte)_setPiece);
            writer.Write((byte)_skyStage);
            writer.Write((short)Math.Clamp(_skyStageTicks, 0, short.MaxValue));
            writer.Write(_skyRotation);
            writer.Write(_skyDirection.X);
            writer.Write(_skyDirection.Y);
            writer.Write((byte)Math.Clamp(_skyDashTicks, 0, byte.MaxValue));
            writer.Write(_skyFromHover);
            base.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _phaseTwo = reader.ReadBoolean();
            _groundingArmed = reader.ReadBoolean();
            _airVolley = reader.ReadBoolean();
            _shotAim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _setPiece = (KnightSetPiece)reader.ReadByte();

            // A different stage is adopted with the server's clock and its start cue; the same stage keeps this clock.
            SkyStage stage = (SkyStage)reader.ReadByte();
            int stageTicks = reader.ReadInt16();
            if (stage != _skyStage)
            {
                _skyStage = stage;
                _skyStageTicks = stageTicks;
                PlaySkyStageCue(stage);
            }

            _skyRotation = reader.ReadSingle();
            _skyDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _skyDashTicks = reader.ReadByte();
            _skyFromHover = reader.ReadBoolean();
            base.ReceiveExtraAI(reader);
        }
    }
}
