using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Accessories.Damage;
using tsorcRevamp.Content.Items.Armor;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Spears;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Winged spear-and-spellcaster invader built on the Cursed Dragon armor set. Aggressively
    /// pursues (composite-arm swings, a distance-solved leaping thrust, and a fast pursuit/closing
    /// speed) with a bespoke PilgrimSpontoon halberd combo pool - thrusts, a rising-into-overhead
    /// cut, and a spinning windmill that chases while active and can chain into a leaping slam
    /// finisher if the player is still far when it ends. Also kites with three ranged weapons
    /// (arcane ball = primary, an enemy Venom Staff = secondary with burst patterns + a 5-meteor
    /// pentagram finisher, a MeteorStorm = magic with a rare 7-second meteor rain), and breathes red
    /// Ancient-Demon fire. Breath, hyper-armor, sustained magic, and the cross-weapon finisher all
    /// run through the shared <see cref="PuppetNPC"/> state machine.
    /// </summary>
    [AutoloadBossHead]
    public class CursedDragon : PuppetNPC
    {
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Puppets/CursedDragon_Head_Boss";

        // Index into SecondaryRangedBurstPatterns that ends with the meteor pentagram finisher.
        private const int VenomFinisherPattern = 3;
        private const int BreathDamage = 30;
        private const int MeteorRainChance = 14;   // % of magic casts that become the 7 s rain
        private const int MeteorRainTicks  = 420;  // ~7 s sustained rain

        private bool _meteorRainActive;

        // ── Multi-blast meteor triad state (non-rain magic casts) ──────────────────
        private int   _meteorBurstsRemaining;  // blasts left to fire after the one just cast
        private int   _meteorBurstGapTicks;    // ticks between blasts
        private int   _meteorBurstNextInTicks; // countdown to the next blast
        private float _meteorBurstSpread;      // flank x-offset
        private float _meteorBurstFlankJitter;
        private float _meteorBurstCenterJitter;

        protected override string InvaderTitle => "Cursed Dragon";
        protected override bool IsGreatInvader => true;

        // ── Wings / flight ──────────────────────────────────────────────────────────
        protected override bool HasWings => true;
        protected override int WingsAccessoryItemType => ItemID.TatteredFairyWings;
        protected override EnemyFlightConfig FlightConfig => new EnemyFlightConfig
        {
            HoverAltitude = 230f,
            HoverSideOffset = 110f,
            HoverTopSpeed = 4.7f,
            TakeOffSpeed = 8.5f,
            DiveAcceleration = 0.55f,
            DiveTopSpeed = 12f,
            LandSpeed = 6.5f,
            MaxFlightTicks = 660,
            CooldownTicks = 110,
            TakeOffTicks = 32,
            HoverDwellTicks = 90,
            StrafeTicks = 60,
            DiveAttackTicks = 46,
            LandTicks = 80,
            WingFlapSpeed = 0.085f,
            StrafeArcHeight = 55f, // breath-sweep strafe rises through the middle for a natural swoop
        };
        protected override int RandomTakeoffChance => 17; // takes to the air more readily
        protected override float FlightHpEscalationFrac => 0.60f;

        // ── Loadout ───────────────────────────────────────────────────────────────
        protected override int HeadArmorItemType => ModContent.ItemType<CursedDragonHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<CursedDragonArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<CursedDragonGreaves>();

        // Spear drives melee combos (Halberd archetype); its sprite is also shown while casting the
        // arcane ball (primary ranged), since the spear is the arcane ball's caster. The standalone
        // spear-poke phase (SpearWeaponItemType >= 0) is retired: Pike Thrust / Skewer String /
        // Leaping Pike in the combo pool below fully replace it with proper easing and reach.
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyPilgrimSpontoon>();
        protected override int SpearWeaponItemType => -1;
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyPilgrimSpontoon>();
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<EnemyVenomStaff>();
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyMeteorStorm>();

        // ── Damage (late hardmode: post-mech, ~Golem/Plantera tier; no SHM scaling) ──
        protected override int MeleeDamage => 60;   // spear combos
        protected override int SpearDamage => 55;   // aerial dive thrust only (standalone poke retired)
        protected override int RangedDamage => 45;  // arcane ball
        protected override int SecondaryRangedDamage => 40; // venom fang (multi-projectile)
        protected override int MagicDamage => 50;   // meteor

        // ── Melee ───────────────────────────────────────────────────────────────────
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Halberd;
        protected override int MeleeComboChance => 60;
        // 520, not the old 260: puts the Leaping Pike gap-closer (RangedStartOnly) in reach of a
        // kiting player instead of only ever firing at near-point-blank range.
        protected override float ComboMaxStartRange => 520f;
        protected override float MeleeRange => 88f;
        protected override float StabRange => 170f;
        // The standalone spear poke is retired (SpearWeaponItemType => -1 above) so its old timing
        // knobs (SpearRange/SpearTelegraphTicks/etc.) are gone too. SpearDamage survives - the aerial
        // dive thrust still reads it directly.

        // ── Swing quality (composite arm + authored easing, see attack-timing-design / puppet-swing-tuning) ──
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseLogicalMeleeTelegraphs => true; // wind to the opposite end, then strike
        protected override bool UseSwingEasing => true;
        protected override bool UseAimAdaptiveArc => true;        // arc centres on the player, not a fixed angle
        protected override bool UseAuthoredComboSwingClock => true;
        // JoustDash-motion thrusts (Pike Thrust, Skewer String, Leaping Pike) are authored with a
        // short AttackTicks (~17) well under the spear item's useAnimation (27) - without this, the
        // swing clock sweeps over useAnimation instead and the thrust is cut off mid-arc.
        protected override bool AuthoredClockCoversJoustDash => true;
        // Ties the spear's extend/retract grip pulse to the SAME eased clock as the blade rotation,
        // instead of its own separate PhaseTimer-based hump - the two used to drift apart.
        protected override bool UseAuthoredSpearGrip => true;
        // Spinning Windmill: ease only at the start (wind-up) and end (settle) of the spin, constant
        // speed through the middle - see the combo pool below and PuppetNPC's ComboMotion.Spin case.
        protected override bool UseEasedSpin => true;
        protected override int MeleeComboInterStepLingerTicks => 3;  // hold through inter-step pauses
        protected override int MeleeRecoveryLingerTicks => 10;       // park the finished pose before easing to carry
        protected override bool UseLandingTimedLeapSlam => true;     // Spinning Windmill's leap-slam finisher

        // Trimmed to the drawn spear's actual reach (~140px full extension, ~122px mid-shaft swing
        // grip) instead of the old 230, which hit up to 2.3x past the visible tip. Per-step ReachMult
        // in the combo pool below tunes each motion's grip length the rest of the way.
        protected override float ComboReachBase => 190f;
        // = the longest authored hit reach (Pike Thrust's 133px) plus a small buffer, not the old 200:
        // starting a standing thrust any further out would whiff before the puppet's own approach.
        protected override float MeleeEngageRange => 160f;
        // Keep pursuing through the wind-up so the player can't just step out of range.
        protected override bool SlowDownBeforeMelee => false;

        // A halberd's own reach means it doesn't need the full ~220deg envelope policy default to
        // read as committed; extend the FINISH of each arc (not the start) for a fuller, still
        // deliberately compact cut, and share Reaping Arc's two-step endpoint so there's no snap
        // between the rising cut and the overhead follow-up (attack-timing-design §3/§4).
        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float a0, ref float a1)
        {
            switch (motion)
            {
                case ComboMotion.UnderhandArc:
                    a1 = -1.3f; // ends exactly where OverheadArc's cocked start sits
                    break;
                case ComboMotion.OverheadArc:
                    a1 = 1.6f; // was 1.0
                    break;
            }
        }

        protected override MeleeCombo[] MeleeComboPoolOverride => CursedDragonCombos;

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == LeapingPikeName)
            {
                // Anything closer is already covered by an ordinary ClosingDistance sprint.
                return distance >= LeapingPikeMinRange;
            }
            if (combo.Name == ChargedDescentName)
            {
                return healthFraction <= 0.70f; // unlocks alongside the venom staff / cursed knives
            }
            return true;
        }

        protected override bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == SpinningWindmillName && nextStepIndex == 1)
            {
                // Below 40% HP the finisher always follows - "queue next attack" applies to the
                // signature move too. Otherwise: the spin already chases (ForwardPushMult on the Spin
                // step below); only bolt on the leaping-slam finisher when that pursuit wasn't enough
                // to close the gap by the time the spin ends.
                if ((float)NPC.life / NPC.lifeMax <= 0.40f)
                {
                    return true;
                }
                return NPC.Distance(target.Center) > SpinningWindmillFinisherRange;
            }
            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        // ── HP-scaled combo escalation ───────────────────────────────────────────────
        // "Reduced recovery from the start, longer combos as HP drops" - modeled directly on Gwyn's
        // HalfHealthMovesUnlocked mechanism (NPCs/Bosses/SuperHardMode/Gwyn.cs), generalized to two HP
        // tiers instead of one cliff. Every appended rep's PostStepPause still respects the same
        // MinTicksBetweenLiveWindows floor as the roll-cooldown fairness rule (attack-timing-design
        // §2 rule 3) - "longer" never means "less rollable," only "less dead time between hits."
        // Matches Gwyn's own constant name/value; kept local rather than shared since every puppet
        // that uses it derives it from its own combo's HitWindowEnd/AttackTicks.
        private const int MinTicksBetweenLiveWindows = 30;
        private const int PauseSafetyBuffer = 3; // small cushion above the bare mathematical floor

        /// <summary>Extra repetitions of a combo's last step to append, by HP tier: 0 above 70%
        /// (unchanged), +1 from 40-70% (matches the venom staff / cursed knives unlock), +2 below 40%.</summary>
        private static int ExtraRepsForHealth(float healthFraction)
        {
            if (healthFraction <= 0.40f)
            {
                return 2;
            }
            if (healthFraction <= 0.70f)
            {
                return 1;
            }
            return 0;
        }

        /// <summary>The safe PostStepPause floor for a step: the smallest gap that still keeps the
        /// NEXT live window starting >= MinTicksBetweenLiveWindows after this one's live window ends,
        /// plus a small buffer. Only valid for Weighted-eased steps (their HitWindowEnd/AttackTicks
        /// define a real tail); a step without a hit window is armed for the whole phase (tail = 0).</summary>
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

            // Baseline, every HP tier: Reaping Arc's long tail (armed only ~33% of the attack phase)
            // leaves plenty of room under the fairness rule - the authored 10t pause was more
            // conservative than it needed to be. Tightened here rather than in the table so the
            // math stays next to the rule it depends on.
            if (combo.Name == ReapingArcName)
            {
                MeleeComboStep first = combo.Steps[0];
                first.PostStepPause = SafeFollowUpPause(first);
                combo.Steps[0] = first;
            }

            // Longer combos at low HP: only the jab family chains further - the wide-arc and heavy
            // combos stay single, deliberate commits regardless of HP.
            int extraReps = ExtraRepsForHealth(healthFraction);
            if (extraReps <= 0 || (combo.Name != PikeThrustName && combo.Name != SkewerStringName))
            {
                return;
            }

            MeleeComboStep repTemplate = combo.Steps[combo.Steps.Length - 1];
            int repPause = SafeFollowUpPause(repTemplate);

            MeleeComboStep[] extended = new MeleeComboStep[combo.Steps.Length + extraReps];
            Array.Copy(combo.Steps, extended, combo.Steps.Length);
            // The step that WAS last now has a follow-up, so it needs a real pause instead of going
            // straight to the combo's recovery.
            extended[combo.Steps.Length - 1].PostStepPause = repPause;
            for (int i = 0; i < extraReps; i++)
            {
                MeleeComboStep rep = repTemplate;
                rep.TelegraphTicks = 0; // only step 0 telegraphs; later steps read their own PostStepPause as the tell
                rep.PostStepPause = (i == extraReps - 1) ? 0 : repPause; // only the truly-last rep goes straight to recovery
                extended[combo.Steps.Length + i] = rep;
            }
            combo.Steps = extended;
        }

        // ── Combo pool (see attack-timing-design for the timing-sheet fields below) ─────────────────
        // Names, gates and cross-referenced constants used above and by the combo table below.
        private const string PikeThrustName = "Pike Thrust";
        private const string SkewerStringName = "Skewer String";
        private const string ReapingArcName = "Reaping Arc";
        private const string LeapingPikeName = "Leaping Pike";
        private const string ChargedDescentName = "Charged Descent";
        private const string SpinningWindmillName = "Spinning Windmill";
        private const float LeapingPikeMinRange = 260f;
        private const float SpinningWindmillFinisherRange = 150f;

        // Local step-authoring helpers, kept per-file like Gwyn's GS()/WeightedSwordSwing —
        // MeleeComboSystem's own private S() can't be reused outside that file.
        private static MeleeComboStep GS(ComboMotion m, int tel, int atk, int pause,
            float dmg = 1f, float reach = 1f, float push = 0f, SwingEaseStyle ease = SwingEaseStyle.Smooth)
            => new MeleeComboStep
            {
                Motion = m, TelegraphTicks = tel, AttackTicks = atk, PostStepPause = pause,
                DamageMult = dmg, ReachMult = reach, ForwardPushMult = push, Ease = ease
            };

        // Share of peak speed the blade stays armed at — same convention as Gwyn's Wrath Flurry
        // (attack-timing-design §3): a Weighted swing keeps hitting while speed >= 30% of its peak.
        private const float ArmedSpeedShare = 0.30f;

        /// <summary>Authored Weighted swing: cubic ease-in, exponential settle, and a hit window that
        /// closes once blade speed drops under <see cref="ArmedSpeedShare"/> of peak. Same maths as
        /// Gwyn.WeightedSwordSwing (attack-timing-design §3).</summary>
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

        /// <summary>Spinning Windmill's spin step. <see cref="PuppetNPC.UseEasedSpin"/> ramps its
        /// angular speed up across the telegraph and back down over <paramref name="easeOutTicks"/> —
        /// constant speed through the rest, per the "ease only at the beginning and end" brief.</summary>
        private static MeleeComboStep SpinStep(int telegraph, int attackTicks,
            float damage, float reach, float push, int easeOutTicks, int pause)
        {
            MeleeComboStep step = GS(ComboMotion.Spin, telegraph, attackTicks, pause, damage, reach, push);
            step.EaseOutTicks = easeOutTicks;
            return step;
        }

        /*
         * Timing sheet (attack-timing-design §3), envelope = live sweep / 0.8.
         *
         * Pike Thrust:      tel 24t | strike 5/12 k6, atk 17t, live ~7.4t (HitWindowEnd 0.44) | pause n/a (single step)
         * Skewer String:    tel 26t | strike 5/12 k6 x2, atk 17t each, live ~7.4t each | pause 24t between —
         *                   next live starts ~33.6t after the first live window ends (>= 30, unconditional-safe)
         * Reaping Arc:      tel 30t | Underhand 8/34 k7 -> Overhead 8/34 k7, atk 42t each, live ~13.9t each,
         *                   shared endpoint (-1.3) between steps | pause 10t — next live ~38t after the first ends
         * Leaping Pike:     RangedStartOnly, 260-520px | tel 24t | distance-solved leap, thrust on landing
         * Charged Descent:  <=70% HP | tel 52t | strike 10/45 k7, atk 55t, live ~17.7t (HitWindowEnd 0.32,
         *                   matches Gwyn's Wrath Flurry value) | RecoveryTicks 40 (heavy punish window)
         * Spinning Windmill: tel 36t (ramps 0->full speed) | spin cruises atk 40t, eases down over the last 10t,
         *                   chases at ForwardPushMult 0.9 while spinning | pause 20t (only if it continues) ->
         *                   conditional LeapSlam finisher (ShouldContinueMeleeCombo above) if still >150px away
         *
         * Every live window here is well under the 22-tick roll i-frame window, so every step is fully
         * rollable, and every multi-step follow-up goes live >= 30t after the previous live window ends
         * (attack-timing-design §2 rules 1 and 3) — no follow-up can catch a late roller in the post-roll gap.
         */
        private static readonly MeleeCombo[] CursedDragonCombos = new[]
        {
            new MeleeCombo
            {
                Name = PikeThrustName, BaseWeight = 90, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 50, RecoveryTicks = 18,
                Steps = new[] { WeightedSwing(ComboMotion.JoustDash, 24, 5, 12, 6f, 0, 1.1f, 1.0f) }
            },
            new MeleeCombo
            {
                Name = SkewerStringName, BaseWeight = 70, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 110, RecoveryTicks = 24,
                Steps = new[]
                {
                    WeightedSwing(ComboMotion.JoustDash, 26, 5, 12, 6f, 24, 1.0f, 1.0f),
                    WeightedSwing(ComboMotion.JoustDash,  0, 5, 12, 6f,  0, 1.0f, 1.0f),
                }
            },
            new MeleeCombo
            {
                Name = ReapingArcName, BaseWeight = 60, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 150, RecoveryTicks = 14,
                Steps = new[]
                {
                    WeightedSwing(ComboMotion.UnderhandArc, 30, 8, 34, 7f, 10, 0.9f, 0.9f),
                    WeightedSwing(ComboMotion.OverheadArc,   0, 8, 34, 7f,  0, 1.1f, 0.9f),
                }
            },
            new MeleeCombo
            {
                Name = LeapingPikeName, BaseWeight = 90, Preferred = ComboRangeBand.Far,
                RangedStartOnly = true, InitialFlashColor = Color.Yellow, CooldownAfterUse = 200,
                HeavyCommit = true, HyperArmor = true, RecoveryTicks = 30,
                Steps = new[] { GS(ComboMotion.LeapThrust, 24, 90, 0, 1.2f, 1.0f) }
            },
            new MeleeCombo
            {
                Name = ChargedDescentName, BaseWeight = 30, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red, CooldownAfterUse = 300, HeavyCommit = true, RecoveryTicks = 40,
                Steps = new[] { WeightedSwing(ComboMotion.OverheadArc, 52, 10, 45, 7f, 0, 1.5f, 1.0f) }
            },
            new MeleeCombo
            {
                Name = SpinningWindmillName, BaseWeight = 40, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 220, RecoveryTicks = 30,
                Steps = new[]
                {
                    SpinStep(telegraph: 36, attackTicks: 40, damage: 1.4f, reach: 1.0f, push: 0.9f,
                        easeOutTicks: 10, pause: 20),
                    GS(ComboMotion.LeapSlam, 0, 90, 0, 1.3f, 0.9f),
                }
            },
        };

        // ── Movement / pursuit ───────────────────────────────────────────────────────
        // Calibrated against Souls Mode ground speed with Supersonic Boots or Supersonic Wings I
        // (6.0-7.25 px/t) - the gear tier for this fight (post-Hunter, pre-Attraidies, pre-SHM).
        // Wings II / Wings of Seath (7.5-8.25) are a later-game problem.
        protected override float TopSpeed => 3.5f;
        protected override float Acceleration => 0.16f;
        protected override int CasualStrollChance => 0; // no slow-walk window; always closing or attacking
        protected override float RunDistance => 320f;
        protected override float RunSpeedMult => 1.5f;         // 5.25 px/t beyond RunDistance
        protected override float ClosingDistanceSpeedMult => 2.0f; // 7.0 px/t - matches player ground speed
        protected override int ClosingDistanceMaxTicks => 150;     // room for a full 520px sprint at 7 px/t
        protected override float ComboTelegraphAdvanceSpeedMult => 0.85f; // chase a player backing out of a windup
        protected override int RangedStartMeleeComboChance => 55; // keep a real share of ticks for ranged
        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 7f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 7f;

        protected override void RunMovementAI(float speedMult)
        {
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.NavSearchRadius = 80;
            g.RemembersLastKnownPos = true;

            // Accelerate harder while sprinting to close a melee gap - a kiting player should feel
            // the dragon actually gaining on them, not creeping up at its ordinary walking accel.
            float acceleration = Acceleration * (Phase == AttackPhase.ClosingDistance ? 1.4f : 1f);

            // attackRange is SF4's aggro radius, not a preferred fighting distance - the inherited
            // default (RangedRange, 560) let a kiting player drift out of the pursuit FSM's
            // engagement band entirely.
            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: acceleration,
                doorBreakingDamage: 5,
                attackRange: 850f);
        }

        // ── Primary ranged: arcane ball (also the aerial hover shot) ────────────────
        protected override RangedStyle RangedAnimStyle => RangedStyle.Throw;
        protected override float RangedRange => 560f;
        // 200, not 150: at the ball's 10.5 px/t, distance/speed must clear ~20 ticks of reaction time
        // (attack-timing-design §2) or a shot fired from point-blank is unreadable.
        protected override float MinRangedRange => 200f;
        protected override int RangedTelegraphTicks => 40;
        protected override int RangedCooldownAfterUse => 180;
        protected override int MaxRangedBurst => 1;
        // Down from 45: ranged should mostly fire while still advancing, not become a standing stance.
        protected override int StandingRangedChance => 15;
        protected override int RangedComboChance => 0; // no ranged combos from the spear archetype
        // Full burst patterns (fan/rolling orbs/chain/circle) fire while hovering too, not just a
        // single standing potshot - the base default forces the simplified single shot airborne.
        protected override bool AllowRangedPatternsAirborne => true;

        // Arcane ball volleys (grounded or airborne). Pattern 0's single "shot" is a 5-ball fan;
        // pattern 3's final shot is a 12-ball circle burst.
        private const int ArcaneFanPattern = 0;
        private const int ArcaneCirclePattern = 3;
        protected override int[][] PrimaryRangedBurstPatterns => new int[][]
        {
            new int[] { },                                          // 0: 1 "shot" = a 5-ball fan
            new int[] { 12, 12, 12, 12, 12 },                        // 1: Rolling Orbs — 6 shots, 12 apart, each re-aimed at the player's CURRENT position
            new int[] { 60, 20, 20, 20, 20, 20, 20, 20 },           // 2: 9 shots (2@60, then 7@20)
            new int[] { 10, 60 },                                   // 3: 2 shots (10 apart), 60, then a 12-ball circle
        };
        protected override int[] PrimaryRangedBurstChances => new int[] { 30, 30, 20, 20 };
        protected override int[] PrimaryRangedBurstTelegraphExtras => new int[] { 6, 0, 10, 12 };
        protected override Color[] PrimaryRangedBurstFlashColors => new Color[]
        {
            new Color(150, 200, 255), // fan
            new Color(120, 180, 255),
            new Color(150, 160, 255),
            new Color(180, 120, 255), // circle-burst finisher — brighter
        };

        // ── Secondary ranged: enemy Venom Staff (shotgun blasts + pattern bursts) ───
        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Crossbow;
        protected override float SecondaryRangedRange => 640f;
        protected override float SecondaryRangedMinRange => 150f;
        protected override int SecondaryRangedTelegraphTicks => 36;
        protected override int SecondaryRangedCooldownAfterUse => 160;
        protected override int SecondaryRangedChance => 50;
        protected override int SecondaryStandingRangedChance => 30; // was 70 - fire while advancing, not planted
        protected override Color SecondaryRangedFlashColor => new Color(180, 90, 255);

        // Player got in close → hop back and fire a single venom blast to reset spacing.
        protected override float SecondaryRangedBackhopRange => 120f;  // ~7.5 tiles
        protected override int SecondaryRangedBackhopChance => 40;     // competes with melee combos at close range
        protected override float SecondaryRangedBackhopSpeed => 6.5f;
        protected override float SecondaryRangedBackhopUpSpeed => 5.5f;

        // Pattern 0 — single shot (close band / spacing reset)
        // Pattern 1 — shoot, 30, shoot, 60, shoot            (3 shots)
        // Pattern 2 — shoot, 60, shoot, 30, shoot            (3 shots)
        // Pattern 3 — 6 shots 30 apart, then 60 → meteor pentagram finisher (7th "shot")
        protected override int[][] SecondaryRangedBurstPatterns => new int[][]
        {
            new int[] { },
            new int[] { 30, 60 },
            new int[] { 60, 30 },
            new int[] { 30, 30, 30, 30, 30, 60 },
        };
        protected override int[] SecondaryRangedBurstTelegraphExtras => new int[] { 0, 8, 8, 20 };
        protected override Color[] SecondaryRangedBurstFlashColors => new Color[]
        {
            new Color(180, 90, 255),  // single — violet
            new Color(180, 90, 255),  // pattern 1 — violet
            new Color(150, 60, 230),  // pattern 2 — deeper violet
            Color.Red,                // pattern 3 — finisher danger red
        };
        protected override int[] SecondaryRangedBurstChances => new int[] { 40, 25, 25, 10 };

        // ── Magic: MeteorStorm (instant 3-storm, rare 7 s rain) ─────────────────────
        protected override float MagicRange => 860f;
        protected override float MinMagicRange => 240f;   // usable across more of the fight
        // ── Twin Storm (below 50% HP) ─────────────────────────────────────────────
        // A second-wind magic attack: a long planted 90-tick telegraph at the staff tip, then two
        // tornadoes peeling off to either side. Chains 2-3 casts back to back with ZERO recovery, so
        // once it starts the pressure is relentless until the chain runs out.
        private const int TwinStormTelegraphTicks = 90;
        /// <summary>Ticks before the strike when the gathered swirl bursts outward — the "it's coming" beat.</summary>
        private const int TwinStormBurstLeadTicks = 15;
        /// <summary>Radius of the gathering swirl. 16px radius = the 32px circle.</summary>
        private const float TwinStormSwirlRadius = 16f;
        /// <summary>Chance per idle tick of arming a chain once below half health.</summary>
        private const int TwinStormArmChance = 200;

        private int _twinStormCastsLeft;

        /// <summary>True while a Twin Storm chain is queued or mid-flight. Drives the longer telegraph and
        /// the zero recovery, so it must be armed BEFORE the magic phase begins (see PostAI).</summary>
        private bool TwinStormQueued => _twinStormCastsLeft > 0;

        protected override int MagicTelegraphTicks => TwinStormQueued ? TwinStormTelegraphTicks : 60;
        protected override int MagicRecoveryTicks => TwinStormQueued ? 0 : 75;
        protected override int MagicCooldownAfterUse => 180; // fires more often
        protected override int MagicPreferenceChance => 55;  // meteors compete with arcane ball, not just fill gaps

        // ── Fire breath: red Ancient-Demon flame, usable grounded or in a flying sweep ──
        protected override bool CanBreathe => true;
        protected override bool BreathAllowedAirborne => true;
        protected override float BreathRange => 520f;
        protected override float MinBreathRange => 60f;
        // 64, not 110: a 1.8s wind-up gave the player time to simply walk away before it even started.
        protected override int BreathTelegraphTicks => 64;
        protected override int BreathDurationTicks => 70;
        protected override int BreathRecoveryTicks => 45;
        protected override int BreathCooldownAfterUse => 360;
        protected override int BreathChance => 4;
        protected override Color BreathTelegraphFlashColor => Color.OrangeRed;
        // Walk forward through the stream on the ground too (already sweeps via a strafe while
        // airborne) - the breath doubles as advancing pressure instead of a planted hose.
        protected override bool AdvanceDuringGroundedBreath => true;

        // ── HP gating for the venom staff / cursed knives unlock ────────────────────
        private bool BelowEnrageHpThreshold => NPC.life <= NPC.lifeMax * 0.70f;

        // Venom staff (secondary ranged) is locked until the dragon drops below 70% HP.
        protected override bool SecondaryRangedAvailable => BelowEnrageHpThreshold;

        // ── Agility: proactive projectile evasion + preemptive quick-step ───────────
        protected override bool EvadesProjectiles => true;          // jump / roll vs incoming ranged
        protected override int PreemptiveQuickStepChance => 35;     // dash THROUGH a meleeing player
        // Land past the player with room, and a recovery window so it can't instantly re-attack — but short
        // enough that the dash still buys tempo.  (~0.6 s recovery, then the next attack still telegraphs.)
        protected override int QuickStepRecoveryTicks => 36;
        protected override float QuickStepForwardRoom => 56f;

        // ── Cursed Knives (unlocks below 70% HP; usable grounded or flying) ──────────
        protected override bool CanThrowCursedKnives => BelowEnrageHpThreshold;
        protected override int CursedKnivesWeaponItemType => ModContent.ItemType<EnemyCursedKnife>();
        protected override int CursedKnivesChance => 18;
        protected override float CursedKnivesRange => 780f;
        protected override float CursedKnivesMinRange => 0f;
        protected override float CursedKnivesCloseRange => 220f; // ≤ this: 1 volley, tight 45° spread
        protected override float CursedKnivesMidRange => 470f;   // ≤ this: 2 back-to-back volleys
        protected override int CursedKnivesTelegraphTicks => 45;
        protected override int CursedKnivesBackToBackGap => 8;
        protected override int CursedKnivesFarGap => 30;
        protected override int CursedKnivesCooldownAfterUse => 420;
        private const int CursedKnivesDamage = 35;

        // ── Hyper-armor across the whole telegraph+attack window ────────────────────
        protected override bool HyperArmorDuringTelegraph => true;

        // ── Telegraph flash colors / weapon draw tuning ─────────────────────────────
        protected override Color MeleeTelegraphFlashColor => new Color(255, 230, 180);
        protected override Color RangedTelegraphFlashColor => new Color(120, 180, 255);
        protected override Color MagicTelegraphFlashColor => new Color(255, 80, 60);
        protected override Vector2 MeleeHandleNorm => new Vector2(0.18f, 0.78f);
        protected override float MeleeWeaponRotationOffset => MathHelper.ToRadians(-8f);

        // ── Spear draw: grip slides along the shaft so the spear extends/retracts like the player's ──
        protected override bool DrawWeaponAsSpear => true;
        // Use the holdout-projectile sprite (full shaft + head) instead of the small item icon.
        protected override string SpearDrawTexturePath => "tsorcRevamp/Content/Projectiles/Melee/Spears/PilgrimSpontoonProj";
        // Measured directly from the PilgrimSpontoonProj.png pixels (110x110): the ornate head/tip
        // sits at the top-left corner and the shaft runs straight to the bottom-right corner — a
        // NW-SE diagonal, not the NE-SW diagonal the broadsword convention assumes.
        protected override Vector2 SpearHeadNorm => new Vector2(0.03f, 0.03f); // tip, top-left
        protected override Vector2 SpearBaseNorm => new Vector2(0.95f, 0.95f); // butt, bottom-right
        // TickWeaponAnim's rotation targets assume the standard broadsword natural angle (-45°,
        // handle lower-left / blade upper-right).  This sprite's natural angle is -135° (90° off in
        // the other direction), so a +90° draw-only correction realigns it — after which every
        // existing rotation target (thrust, telegraph, overhead/underhand arcs, ranged throw, etc.)
        // reads correctly without further per-phase tuning.
        protected override float SpearDrawRotationOffset => MathHelper.PiOver2;
        // ComboReachBase / MeleeEngageRange / SlowDownBeforeMelee are set in the swing-quality block above.
        // Meteor staff is gripped low on the shaft during the cast (≈30% from the bottom).
        protected override Vector2 MagicGripNorm => new Vector2(0.5f, 0.72f);

        // 30, not the base default of 140 - matches Black Ninja's tuning. Used by both the generic
        // aggressive teleport (SetDefaults above) and the Poison Cloud flee-counter's directed one.
        protected override int TeleportTelegraphTicks => 30;

        private Vector2 MouthPosition => NPC.Center + new Vector2(NPC.spriteDirection * 16f, -16f);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 25000;
            NPC.defense = 40;
            NPC.damage = 0;       // all damage via weapon hitboxes / projectiles
            NPC.knockBackResist = 0.12f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 55000f;
            NPC.boss = true;
            NPC.npcSlots = 8f;
            NPC.lavaImmune = true;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 60f;                 // heavy armored dragon — slow to stagger
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;

            // Agile: moderate proactive projectile dodging (jumps / i-frame rolls at incoming shots).
            globalNPC.Agility = 0.35f;

            // Aggressive teleport with the black/purple plague style (leaves a curse cloud on arrival).
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.Plague;
            // TeleportTelegraphTicks override below (30, matching Black Ninja) applies to this AND
            // the Poison Cloud flee-counter's own directed teleport.

            // Aggressive spacing reactions: punish ranged kiting with a LeapForward, charge back in with
            // a hyper-armored RunningDash, RetreatDash to reset, plus an i-frame QuickStep dodge.
            EvasiveProfile.CursedDragon(globalNPC);
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            // EvasiveOnHit self-suppresses while mid-attack (InAttack) / mid-evasion, so breath and
            // committed swings are safe — no manual phase guard needed.
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        // ── Melee / spear ───────────────────────────────────────────────────────────
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

        // DoSpearAttack override removed: the standalone spear-poke phase is retired
        // (SpearWeaponItemType => -1 above), so the base never calls it.

        // ── Ranged: arcane ball (primary) + venom staff (secondary) ─────────────────
        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];

            if (IsSecondaryRangedActive)
            {
                // Venom staff.  Pattern 3's final shot is the 5-meteor pentagram finisher.
                if (ActiveBurstPatternIndex == VenomFinisherPattern && IsFinalBurstShot)
                {
                    FireMeteorPentagram(target);
                }
                else
                {
                    FireVenomBlast(target);
                }
                return;
            }

            // Primary: arcane ball. Pattern 0's single shot is a 5-ball fan; pattern 3's final shot
            // is a 12-ball circle burst. Both fire while hovering too (AllowRangedPatternsAirborne).
            if (ActiveBurstPatternIndex == ArcaneFanPattern)
            {
                FireArcaneFan(target);
            }
            else if (ActiveBurstPatternIndex == ArcaneCirclePattern && IsFinalBurstShot)
            {
                FireArcaneCircle(target);
            }
            else
            {
                FireArcaneBall(target);
            }
        }

        private void FireArcaneFan(Player target)
        {
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.7f, PitchVariance = 0.1f }, NPC.Center);
            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 18f, -8f);
            Vector2 aimAt = target.Center + target.velocity * 8f;
            Vector2 baseAim = (aimAt - muzzle).SafeNormalize(new Vector2(NPC.direction, 0f));
            // 5 balls across a 40 degree fan - tight enough to still threaten a standing target,
            // wide enough that a straight sidestep alone doesn't clear all five.
            const int count = 5;
            const float totalSpreadDeg = 40f;
            for (int i = 0; i < count; i++)
            {
                float angleDeg = -totalSpreadDeg / 2f + totalSpreadDeg * i / (count - 1);
                Vector2 vel = baseAim.RotatedBy(MathHelper.ToRadians(angleDeg)) * 10.5f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(), muzzle, vel,
                    ModContent.ProjectileType<EnemyPilgrimArcaneBall>(),
                    RangedDamage, 2f, Main.myPlayer);
            }
        }

        private void FireArcaneCircle(Player target)
        {
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.85f, Pitch = -0.2f }, NPC.Center);
            Vector2 origin = NPC.Center + new Vector2(0f, -8f);
            const int count = 12;
            float baseAngle = (target.Center - origin).ToRotation(); // first ball aimed at the player
            for (int i = 0; i < count; i++)
            {
                float ang = baseAngle + MathHelper.TwoPi * i / count;
                Vector2 vel = ang.ToRotationVector2() * 8f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(), origin, vel,
                    ModContent.ProjectileType<EnemyPilgrimArcaneBall>(),
                    RangedDamage, 1f, Main.myPlayer);
            }
        }

        private void FireArcaneBall(Player target)
        {
            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 18f, -8f);
            Vector2 aimAt = target.Center + target.velocity * 8f;
            Vector2 vel = (aimAt - muzzle).SafeNormalize(new Vector2(NPC.direction, 0f)).RotatedByRandom(MathHelper.ToRadians(5f)) * 10.5f;

            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.6f, PitchVariance = 0.12f }, NPC.Center);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), muzzle, vel,
                ModContent.ProjectileType<EnemyPilgrimArcaneBall>(),
                RangedDamage, 2f, Main.myPlayer);
        }

        // Fixed fan instead of a random shotgun spread - random angles let a player dodge the WHOLE
        // blast with one sidestep; fixed angles guarantee a spread that has to actually be moved through.
        private static readonly float[] VenomFangAnglesDeg = { -16f, -8f, 0f, 8f, 16f };

        private void FireVenomBlast(Player target)
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.65f, PitchVariance = 0.15f }, NPC.Center);
            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 18f, -6f);
            Vector2 baseAim = (target.Center + target.velocity * 6f - muzzle).SafeNormalize(new Vector2(NPC.direction, 0f));
            foreach (float angleDeg in VenomFangAnglesDeg)
            {
                Vector2 vel = baseAim.RotatedBy(MathHelper.ToRadians(angleDeg)) * 10.5f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(), muzzle, vel,
                    ModContent.ProjectileType<EnemyVenomStaffProj>(),
                    SecondaryRangedDamage, 2f, Main.myPlayer);
            }
        }

        // 5 meteors spawned at the tips of a pentagram above the dragon, all converging on the player.
        private void FireMeteorPentagram(Player target)
        {
            SoundEngine.PlaySound(SoundID.Item88 with { Volume = 0.85f, Pitch = -0.2f }, NPC.Center);
            Vector2 center = new Vector2(NPC.Center.X, NPC.Center.Y - 440f);
            Vector2 aimAt = target.Center;
            float radius = 16f * 5f; // 5 tiles
            for (int i = 0; i < 5; i++)
            {
                // 144° steps trace a 5-point star (pentagram); start at the top tip.
                float ang = MathHelper.ToRadians(-90f + i * 144f);
                Vector2 spawn = center + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * radius;
                Vector2 vel = (aimAt - spawn).SafeNormalize(Vector2.UnitY) * 3.9f;
                SpawnMarkedMeteor(spawn, vel, aimAt);
            }
        }

        /// <summary>Spawns an EnemyMeteorStormMeteor and stamps the ground-warning-mark position
        /// (ai[0]/ai[1] — see the projectile's own AI()) onto it. Every meteor spawn in this file goes
        /// through here so none of them draws its mark at the (0,0) default.</summary>
        private void SpawnMarkedMeteor(Vector2 spawn, Vector2 velocity, Vector2 markAt)
        {
            int index = Projectile.NewProjectile(
                NPC.GetSource_FromThis(), spawn, velocity,
                ModContent.ProjectileType<EnemyMeteorStormMeteor>(),
                MagicDamage, 2f, Main.myPlayer);

            if (index < 0 || index >= Main.maxProjectiles)
            {
                return;
            }

            Projectile meteor = Main.projectile[index];
            meteor.ai[0] = markAt.X;
            meteor.ai[1] = markAt.Y;
            meteor.netUpdate = true;
        }

        // ── Magic: meteor triad burst (HP-scaled blast count/spacing/spread) + rare sustained rain ──
        // >80% HP: 2 blasts, 60 ticks apart, baseline spread.
        // 50-80% HP: 3 blasts, 45 ticks apart, wider spread.
        // <50% HP: 4 blasts, 30 ticks apart, slightly wider spread still.
        protected override void DoMagicAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];

            // Twin Storm takes priority over the meteor kit whenever a chain is armed. Consumes one cast;
            // with MagicRecoveryTicks at 0 the next telegraph begins almost immediately.
            if (TwinStormQueued)
            {
                _twinStormCastsLeft--;
                FireTwinStorm();
                return;
            }

            if (Main.rand.Next(100) < MeteorRainChance)
            {
                // Rare: channel a 7-second meteor rain (DoMagicTick spawns over the extended phase).
                SoundEngine.PlaySound(SoundID.Item88 with { Volume = 0.35f, PitchVariance = 0.08f }, NPC.Center);
                _meteorRainActive = true;
                _magicAttackTicksOverride = MeteorRainTicks;
                return;
            }

            _meteorRainActive = false;

            float hpFrac = (float)NPC.life / NPC.lifeMax;
            int totalBursts;
            if (hpFrac < 0.50f)
            {
                totalBursts = 4; _meteorBurstGapTicks = 30;
                _meteorBurstSpread = 420f; _meteorBurstFlankJitter = 42f; _meteorBurstCenterJitter = 28f;
            }
            else if (hpFrac <= 0.80f)
            {
                totalBursts = 3; _meteorBurstGapTicks = 45;
                _meteorBurstSpread = 380f; _meteorBurstFlankJitter = 38f; _meteorBurstCenterJitter = 25f;
            }
            else
            {
                totalBursts = 2; _meteorBurstGapTicks = 60;
                _meteorBurstSpread = 300f; _meteorBurstFlankJitter = 30f; _meteorBurstCenterJitter = 20f;
            }

            // First blast fires immediately; DoMagicTick fires the rest at _meteorBurstGapTicks apart.
            FireMeteorTriad(target);
            _meteorBurstsRemaining = totalBursts - 1;
            _meteorBurstNextInTicks = _meteorBurstGapTicks;

            // Channel length: enough ticks for every remaining blast, plus a small buffer so the last
            // blast's meteors spawn before MagicAttack ends and hands off to recovery.
            _magicAttackTicksOverride = _meteorBurstsRemaining * _meteorBurstGapTicks + 14;
        }

        protected override void DoMagicTick(int ticksRemaining)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (_meteorRainActive)
            {
                if (ticksRemaining % 14 == 0)
                {
                    // Rain spreads randomly across a wide band over the player.  Unlike the triad burst
                    // (one cast sound per blast), the sustained rain gets its own sound per meteor so
                    // each of the ~20 shots reads as a distinct impact-incoming cue.
                    SoundEngine.PlaySound(SoundID.Item88 with { Volume = 0.35f, PitchVariance = 0.08f }, NPC.Center);
                    SpawnSkyMeteor(Main.player[NPC.target], xOffset: Main.rand.NextFloat(-260f, 260f), jitter: 0f, leadMult: 10f);
                }
                return;
            }

            if (_meteorBurstsRemaining <= 0)
            {
                return;
            }
            if (--_meteorBurstNextInTicks <= 0)
            {
                FireMeteorTriad(Main.player[NPC.target]);
                _meteorBurstsRemaining--;
                _meteorBurstNextInTicks = _meteorBurstGapTicks;
            }
        }

        // Three meteors: left and right flanks land near the player's current position; the center
        // meteor leads aggressively in the player's run direction so simply kiting one way doesn't
        // guarantee a dodge.  Spread/jitter come from the HP-scaled tier set in DoMagicAttack.
        private void FireMeteorTriad(Player target)
        {
            SoundEngine.PlaySound(SoundID.Item88 with { Volume = 0.35f, PitchVariance = 0.08f }, NPC.Center);
            SpawnSkyMeteor(target, xOffset: -_meteorBurstSpread, jitter: _meteorBurstFlankJitter, leadMult: 4f);
            SpawnSkyMeteor(target, xOffset:  0f,                 jitter: _meteorBurstCenterJitter, leadMult: 80f);
            SpawnSkyMeteor(target, xOffset:  _meteorBurstSpread, jitter: _meteorBurstFlankJitter, leadMult: 4f);
        }

        private void SpawnSkyMeteor(Player target, float xOffset, float jitter, float leadMult)
        {
            Vector2 targetPos = target.Center + target.velocity * leadMult;
            // The landing point carries xOffset/jitter; the OLD code applied the offset only to the
            // spawn X and then aimed at the un-offset targetPos, so every "flank" meteor actually
            // curved back and converged on the same central point as the "center" one — the spread
            // this comment (and DoMagicAttack's HP-scaled _meteorBurstSpread) describes never really
            // happened. Falling straight down onto its own landAt also puts it directly over its
            // ground-warning mark (EnemyMeteorStormMeteor's own AI(), stamped via SpawnMarkedMeteor
            // below), which is the clearest read for where to move.
            Vector2 landAt = new Vector2(
                targetPos.X + xOffset + Main.rand.NextFloat(-jitter, jitter),
                targetPos.Y);
            Vector2 spawn = new Vector2(landAt.X, NPC.Center.Y - Main.rand.NextFloat(520f, 680f));
            // Speed restored partway from the earlier "70% slower" nerf, now that the ground-warning
            // mark gives a real read on where it lands for the whole flight.
            Vector2 vel = (landAt - spawn).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(7f, 9f);
            SpawnMarkedMeteor(spawn, vel, landAt);
        }

        // ── Fire breath (red Ancient-Demon flame) ───────────────────────────────────
        protected override void DoBreathWindup(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }
            float t = MathHelper.Clamp(elapsed / (float)BreathTelegraphTicks, 0f, 1f);
            float scale = t < 0.4f ? 0.45f : MathHelper.Lerp(0.6f, 1.9f, (t - 0.4f) / 0.6f);
            Vector2 mouth = MouthPosition;
            int count = t < 0.4f ? 1 : 2;
            for (int i = 0; i < count; i++)
            {
                Dust d = Dust.NewDustPerfect(mouth + Main.rand.NextVector2Circular(3f, 3f), DustID.RedTorch,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 0, default, scale);
                d.noGravity = true;
            }
            Lighting.AddLight(mouth, 0.6f * scale, 0.18f * scale, 0.03f);
        }

        protected override void OnBreathStart()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f, Pitch = -0.1f }, NPC.Center);
            }
        }

        protected override void DoBreathTick(int ticksRemaining)
        {
            Lighting.AddLight(MouthPosition, 0.9f, 0.35f, 0.05f);
            if (Main.netMode == NetmodeID.MultiplayerClient || ticksRemaining % 4 != 0)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            Vector2 mouth = MouthPosition;
            // Waver the aim across a +-20 degree band around the player instead of a perfectly
            // locked-on beam - the old version re-aimed exactly at the player every puff, so simply
            // standing a step away between puffs was always safe. The waver means "near the player"
            // isn't automatically clear, without fighting the base's own per-tick re-facing.
            float progress = 1f - ticksRemaining / (float)BreathDurationTicks;
            float waverDeg = 20f * (float)Math.Sin(progress * MathHelper.TwoPi * 1.5f);
            Vector2 aimDir = (target.Center - mouth).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.ToRadians(waverDeg));
            Vector2 vel = aimDir * 9f + Main.rand.NextVector2Circular(1.5f, 1.5f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                ModContent.ProjectileType<CursedDragonInvaderBreath>(), BreathDamage, 0f, Main.myPlayer);
        }

        // ── Cursed Knives: throw 3 sticky knives in a tight 45° spread ──────────────
        protected override void DoCursedKnivesThrow()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, PitchVariance = 0.2f }, NPC.Center);
            Player target = Main.player[NPC.target];
            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 14f, -10f);
            Vector2 baseAim = (target.Center - muzzle).SafeNormalize(new Vector2(NPC.direction, 0f));
            // 5 knives across a 60° total spread (was 3 across 45°).
            for (int i = -2; i <= 2; i++)
            {
                Vector2 vel = baseAim.RotatedBy(MathHelper.ToRadians(15f) * i) * 11f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(), muzzle, vel,
                    ModContent.ProjectileType<CursedDragonKnife>(),
                    CursedKnivesDamage, 2f, Main.myPlayer);
            }
        }

        // ── Aerial dive: pierce-through thrust (default), or a ground-slam variant ──
        // Roughly 1 dive in 3 redirects at a ground point near the player and ends in a single big
        // AoE impact instead of the continuous pierce-through thrust - the "swoop down and slam"
        // attack. Rolled once per dive request in ModifyAerialDiveWaypoint (the base's own dive
        // trigger calls it right before RequestDive), read by the telegraph/hit overrides below, and
        // resolved on impact via PostAI (which sees the dive controller return to Hover).
        private bool _aerialSlamActive;
        private bool _wasDiving;
        private const int AerialSlamChance = 3; // 1-in-N dives are the slam variant

        protected override Vector2 ModifyAerialDiveWaypoint(Vector2 defaultWaypoint)
        {
            _aerialSlamActive = Main.rand.Next(AerialSlamChance) == 0;
            if (!_aerialSlamActive)
            {
                return defaultWaypoint;
            }
            // Aim well below the player - ordinary tile collision stops the dive at the ground near
            // them; over a pit it just flies further before the flight controller's own timeout ends it.
            Player target = Main.player[NPC.target];
            return new Vector2(target.Center.X, target.Center.Y + 260f);
        }

        protected override void DoAerialDiveTelegraph()
        {
            base.DoAerialDiveTelegraph(); // shows the spear + orange flash
            if (Main.dedServ)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.5f, PitchVariance = 0.12f }, NPC.Center);
            if (_aerialSlamActive)
            {
                // A second, deeper flash on top of the base's orange one - lets a watching player tell
                // "this one ends in a ground slam" apart from the plain pierce-through.
                SpawnTelegraphFlash(new Color(120, 0, 0));
            }
        }

        protected override void DoAerialDiveHit()
        {
            if (_aerialSlamActive)
            {
                // No continuous thrust during a slam dive - the payoff is the single impact on
                // landing (TriggerAerialSlamImpact, called from PostAI when the dive ends).
                return;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            // Forward spear-thrust hitbox in the dive direction.
            int boxW = 150, boxH = 70;
            Vector2 center = NPC.Center + new Vector2(NPC.direction * 30f, 0f);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), center, Vector2.Zero,
                ModContent.ProjectileType<PuppetMeleeHitbox>(),
                (int)(SpearDamage * 1.25f), 4f, Main.myPlayer, boxW, boxH);
        }

        private void TriggerAerialSlamImpact()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = -0.15f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 6f, 12, 6f, 500f);
                for (int i = 0; i < 40; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Bottom, DustID.RedTorch,
                        Main.rand.NextVector2Circular(6f, 3f) + new Vector2(0f, -2f), 0, default,
                        Main.rand.NextFloat(1.2f, 2.2f));
                    d.noGravity = true;
                }
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            int boxW = 220, boxH = 160;
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), NPC.Bottom, Vector2.Zero,
                ModContent.ProjectileType<PuppetMeleeHitbox>(),
                (int)(SpearDamage * 1.6f), 6f, Main.myPlayer, boxW, boxH);
        }

        // ── Poison Cloud flee-counter (debug label: "Poison Cloud") ─────────────────
        // If the player runs more than 60 tiles away: teleport (30t telegraph - see
        // TeleportTelegraphTicks above, matching Black Ninja), landing ~10 tiles in front of their
        // run direction (or on the far side of them from the dragon's current position if they're
        // not actually moving) to cut off the escape route, raise the spear overhead, then throw a
        // large stationary toxic gas cloud (CursedDragonPoisonCloud) that punishes lingering with
        // poison and escalating curse buildup rather than dealing direct damage itself. Built on the
        // generic Custom set-piece phase (StartCustomAttack/DoCustomAttack/DoCustomTick) since it is
        // not a melee/ranged/magic attack shape.
        private const float PoisonCloudTriggerRange = 60f * 16f; // 60 tiles
        private const int PoisonCloudTeleportTicks = 30;         // = TeleportTelegraphTicks above
        private const int PoisonCloudSettleTicks = 6;            // beat after the reveal before raising the arm
        private const int PoisonCloudArmRaiseTicks = 20;
        private const int PoisonCloudRecoveryTicks = 24;
        // Every stage length above is a compile-time constant, so every peer (server and client)
        // computes the SAME total independently - nothing here needs network syncing.
        private const int PoisonCloudTotalTicks =
            PoisonCloudTeleportTicks + PoisonCloudSettleTicks + PoisonCloudArmRaiseTicks + PoisonCloudRecoveryTicks;
        private const int PoisonCloudCooldownTicks = 1800; // 30s - a rare punish, not a repeatable spam
        private const float PoisonCloudLandingOffsetTiles = 10f;

        private int _poisonCloudCooldown;
        private Vector2 _poisonCloudDestination; // server-only: read once by DoCustomAttack, never needs syncing

        /// <summary>Finds a landing spot ~10 tiles ahead of the player's run direction (or on the far
        /// side of them from the dragon's current position if they're not actually moving), scanning
        /// down for solid ground with clearance for the puppet's own hitbox. Tries a few nearby
        /// columns if the first choice is blocked; returns false if none work.</summary>
        private bool TryFindPoisonCloudLandingSpot(Player target, out Vector2 destinationCenter)
        {
            destinationCenter = Vector2.Zero;

            float desiredX;
            if (Math.Abs(target.velocity.X) > 1f)
            {
                desiredX = target.Center.X + Math.Sign(target.velocity.X) * PoisonCloudLandingOffsetTiles * 16f;
            }
            else
            {
                int oppositeSide = target.Center.X < NPC.Center.X ? -1 : 1; // away from the dragon's current side
                desiredX = target.Center.X + oppositeSide * PoisonCloudLandingOffsetTiles * 16f;
            }

            int centerTileX = (int)(desiredX / 16f);
            int startTileY = (int)(target.Center.Y / 16f) - 6; // a few tiles above the player's own footing

            for (int columnTry = 0; columnTry < 5; columnTry++)
            {
                int columnOffset = columnTry == 0 ? 0
                    : (columnTry % 2 == 1 ? (columnTry / 2 + 1) : -(columnTry / 2 + 1));
                int scanX = centerTileX + columnOffset;

                for (int y = startTileY; y < startTileY + 24; y++)
                {
                    if (!UsefulFunctions.IsTileReallySolid(scanX, y))
                    {
                        continue;
                    }

                    float destLeft = scanX * 16f - NPC.width / 2f;
                    float destTop = y * 16f - NPC.height;
                    int clearanceLeft = (int)Math.Floor(destLeft / 16f);
                    int clearanceRight = (int)Math.Floor((destLeft + NPC.width - 0.01f) / 16f);
                    int clearanceTop = (int)Math.Floor(destTop / 16f);
                    int clearanceBottom = (int)Math.Floor((destTop + NPC.height - 0.01f) / 16f);
                    if (Collision.SolidTiles(clearanceLeft, clearanceRight, clearanceTop, clearanceBottom))
                    {
                        continue; // no headroom above this tile - keep scanning down this column
                    }

                    destinationCenter = new Vector2(destLeft + NPC.width / 2f, destTop + NPC.height / 2f);
                    return true;
                }
            }
            return false;
        }

        protected override void DoCustomAttack()
        {
            DebugAttackLabel = "Poison Cloud";
            tsorcRevampAIs.QueueTeleportToDestination(NPC, _poisonCloudDestination, PoisonCloudTeleportTicks);
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            int elapsed = PoisonCloudTotalTicks - ticksRemaining;
            int releaseTick = PoisonCloudTeleportTicks + PoisonCloudSettleTicks + PoisonCloudArmRaiseTicks;
            if (elapsed == releaseTick && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ThrowPoisonCloud();
            }
        }

        /// <summary>Drives the arm through the sequence's three visible beats: held at carry through
        /// the teleport + settle, raised overhead through the wind-up, thrown, then eased back down.</summary>
        protected override float? CustomWeaponRotation
        {
            get
            {
                if (Phase != AttackPhase.Custom)
                {
                    return null;
                }

                const float carryPose = -0.30f;
                const float raisedPose = -1.55f;
                int elapsed = PoisonCloudTotalTicks - PhaseTimer;

                int armRaiseStart = PoisonCloudTeleportTicks + PoisonCloudSettleTicks;
                if (elapsed < armRaiseStart)
                {
                    return carryPose;
                }

                int releaseTick = armRaiseStart + PoisonCloudArmRaiseTicks;
                if (elapsed < releaseTick)
                {
                    float raiseProgress = (elapsed - armRaiseStart) / (float)PoisonCloudArmRaiseTicks;
                    return MathHelper.Lerp(carryPose, raisedPose, MathHelper.SmoothStep(0f, 1f, raiseProgress));
                }

                int recoveryElapsed = elapsed - releaseTick;
                if (recoveryElapsed < PoisonCloudRecoveryTicks)
                {
                    float recoveryProgress = recoveryElapsed / (float)PoisonCloudRecoveryTicks;
                    return MathHelper.Lerp(raisedPose, carryPose, MathHelper.SmoothStep(0f, 1f, recoveryProgress));
                }
                return carryPose;
            }
        }

        private void ThrowPoisonCloud()
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
            Player target = Main.player[NPC.target];
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), target.Center, Vector2.Zero,
                ModContent.ProjectileType<CursedDragonPoisonCloud>(), 0, 0f, Main.myPlayer);
        }

        // ── Visual: white dust + light building at the spear tip through the whole ranged
        // telegraph, with one strong pulse right before the shot fires ─────────────────
        /// <summary>Two tornadoes peeling away to either side. Reuses vanilla's Apprentice storm (the same
        /// projectile the Tome of Infinite Wisdom's right-click summons), so the whole swirling column and
        /// its dust come free — it only needs flipping hostile. Vanilla's own recipe is preserved: spawn
        /// anchored 100px above the caster's FEET with a purely horizontal velocity and 1.75x damage. That
        /// ground-anchored, flat trajectory is what makes it read as a tornado rather than a fired shot.</summary>
        private void FireTwinStorm()
        {
            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.7f, Pitch = -0.2f }, NPC.Center);

            const float StormSpeed = 7f;
            int stormDamage = (int)(MagicDamage * 1.75f);

            for (int side = -1; side <= 1; side += 2)
            {
                int index = Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    new Vector2(NPC.Center.X, NPC.Bottom.Y - 100f),
                    new Vector2(side * StormSpeed, 0f),
                    ProjectileID.DD2ApprenticeStorm,
                    stormDamage,
                    4f,
                    Main.myPlayer);

                if (index < 0 || index >= Main.maxProjectiles)
                {
                    continue;
                }

                Projectile storm = Main.projectile[index];
                storm.friendly = false;
                storm.hostile = true;
                storm.netUpdate = true;
            }
        }

        /// <summary>Twin Storm's tell: energy winds into a tight 32px circle at the staff tip for most of
        /// the telegraph, then bursts outward over the final 15 ticks as the storms are released.</summary>
        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (!TwinStormQueued || Main.dedServ)
            {
                return;
            }

            Vector2 staffTip = GetSpearTipWorldPosition(92f);
            float burstStart = 1f - (TwinStormBurstLeadTicks / (float)TwinStormTelegraphTicks);

            if (progress < burstStart)
            {
                // Gathering: motes ride the rim of the circle, tightening and speeding up as it fills.
                float gather = progress / burstStart;
                float radius = TwinStormSwirlRadius * MathHelper.Lerp(1.15f, 0.75f, gather);
                float spin = Main.GlobalTimeWrappedHourly * MathHelper.Lerp(4f, 11f, gather);

                for (int i = 0; i < 3; i++)
                {
                    float angle = spin + MathHelper.TwoPi * i / 3f;
                    Vector2 rim = staffTip + angle.ToRotationVector2() * radius;

                    // Velocity runs TANGENT to the circle, which is what makes it read as a swirl
                    // rather than a ring of static sparks.
                    Vector2 tangent = (angle + MathHelper.PiOver2).ToRotationVector2() * MathHelper.Lerp(1.2f, 3.2f, gather);

                    Dust swirl = Dust.NewDustPerfect(rim, DustID.Cloud, tangent, 90,
                        Color.Lerp(new Color(180, 225, 255), Color.White, gather),
                        Main.rand.NextFloat(0.9f, 1.45f));
                    swirl.noGravity = true;
                }

                Lighting.AddLight(staffTip, 0.25f * gather, 0.4f * gather, 0.6f * gather);
                return;
            }

            // Burst: the gathered ring blows outward. Scales up hard through the final ticks so the
            // release is unmistakable.
            float burst = (progress - burstStart) / (1f - burstStart);

            for (int i = 0; i < 6; i++)
            {
                Vector2 outward = Main.rand.NextVector2CircularEdge(1f, 1f);

                Dust blast = Dust.NewDustPerfect(
                    staffTip + outward * TwinStormSwirlRadius,
                    DustID.Cloud,
                    outward * MathHelper.Lerp(3f, 9f, burst),
                    70,
                    Color.White,
                    Main.rand.NextFloat(1.2f, 2f));
                blast.noGravity = true;
            }

            Lighting.AddLight(staffTip, 0.5f, 0.7f, 1f);
        }

        public override void PostAI()
        {
            base.PostAI();

            // Arm a Twin Storm chain while idle, NOT at cast time: MagicTelegraphTicks and
            // MagicRecoveryTicks are both read when the phase is entered, so the flag has to already be
            // set or the first cast would use the short 60-tick telegraph and full recovery.
            bool belowHalfHealth = NPC.life < NPC.lifeMax * 0.5f;

            if (belowHalfHealth && !TwinStormQueued && Phase == AttackPhase.Idle
                && Main.netMode != NetmodeID.MultiplayerClient
                && Main.rand.Next(TwinStormArmChance) == 0)
            {
                _twinStormCastsLeft = Main.rand.Next(2, 4); // 2-3 casts back to back
                NPC.netUpdate = true;
            }

            // Aerial slam: fires once, the exact tick the dive controller returns from DiveAttack to
            // Hover - checked here (runs every tick regardless of Phase) rather than inside the base's
            // shared airborne ladder, so nothing there needs editing for a single subclass's variant.
            bool nowDiving = Flight != null && Flight.IsDiving;
            if (_wasDiving && !nowDiving && _aerialSlamActive)
            {
                TriggerAerialSlamImpact();
                _aerialSlamActive = false;
            }
            _wasDiving = nowDiving;

            if (_poisonCloudCooldown > 0)
            {
                _poisonCloudCooldown--;
            }
            if (Phase == AttackPhase.Idle && _poisonCloudCooldown <= 0
                && Main.netMode != NetmodeID.MultiplayerClient
                && NPC.HasValidTarget
                && (Flight == null || !Flight.IsAirborne))
            {
                Player fleeingTarget = Main.player[NPC.target];
                if (NPC.Distance(fleeingTarget.Center) > PoisonCloudTriggerRange
                    && TryFindPoisonCloudLandingSpot(fleeingTarget, out Vector2 landingSpot))
                {
                    _poisonCloudDestination = landingSpot;
                    _poisonCloudCooldown = PoisonCloudCooldownTicks;
                    StartCustomAttack(PoisonCloudTotalTicks, MeleeWeaponItemType, swingPose: false);
                    NPC.netUpdate = true;
                }
            }

            if (Phase != AttackPhase.RangedTelegraph || IsSecondaryRangedActive || Main.dedServ)
                return;

            Vector2 spearTip = GetSpearTipWorldPosition(92f);
            float t = RangedTelegraphTicks > 0
                ? 1f - (float)PhaseTimer / RangedTelegraphTicks // 0 at telegraph start, 1 at the shot
                : 1f;

            // Steady sparkle for the full telegraph, growing denser as the shot nears.
            int dustCount = 2 + (int)(t * 3f); // 2 -> 5 particles/tick
            for (int i = 0; i < dustCount; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    spearTip + Main.rand.NextVector2Circular(7f, 7f),
                    DustID.WhiteTorch,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    0, Color.White,
                    Main.rand.NextFloat(1.2f, 2.0f));
                d.noGravity = true;
                d.fadeIn = 0.9f;
            }

            // Strong pulse burst in the final few ticks — the clearest "it's about to fire" cue.
            if (PhaseTimer <= 6)
            {
                for (int i = 0; i < 4; i++)
                {
                    Dust d = Dust.NewDustPerfect(spearTip, DustID.WhiteTorch,
                        Main.rand.NextVector2Circular(3.5f, 3.5f), 0, Color.White, 2.4f);
                    d.noGravity = true;
                }
            }

            Lighting.AddLight(spearTip, MathHelper.Lerp(0.5f, 1.4f, t), MathHelper.Lerp(0.5f, 1.4f, t), MathHelper.Lerp(0.6f, 1.5f, t));
        }

        // ── Multiplayer: Twin Storm chain state ─────────────────────────────────────
        // _twinStormCastsLeft is rolled server-only (PostAI above), but it's also READ every tick by
        // every peer: MagicTelegraphTicks/MagicRecoveryTicks size the phase a client is predicting,
        // and DoMagicTelegraphVFX decides whether to draw the gathering swirl at all. Unsynced, a
        // non-host client always sees TwinStormQueued as false — no swirl, and the wrong (60t instead
        // of 90t) telegraph length predicted before the server's snapshot corrects it.
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)Math.Clamp(_twinStormCastsLeft, 0, byte.MaxValue));
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _twinStormCastsLeft = reader.ReadByte();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DragonCharm>()));
            npcLoot.Add(ItemDropRule.ByCondition(new FirstBossKillRule(), ModContent.ItemType<StaminaVessel>()));
            npcLoot.Add(ItemDropRule.ByCondition(new FirstBossKillRule(), ModContent.ItemType<SublimeBoneDust>()));
        }
    }
}
