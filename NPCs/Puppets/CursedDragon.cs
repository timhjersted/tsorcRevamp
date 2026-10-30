using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Accessories.Damage;
using tsorcRevamp.Content.Items.Armor;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Spears;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Winged spear-and-spellcaster invader built on the Cursed Dragon armor set, fought in a mountain-top
    /// event arena. Halberd combo pool on the PilgrimSpontoon: thrusts, a widened rising-into-overhead cut, a
    /// chasing windmill, the Dragoon Leap gap-closer, the Ascending Pike anti-air, and two dragoon signatures:
    /// Dragoon Plunge (a dive from the air into cursed pyres) and Wyrmfall Cleave (phase two, an overhead that
    /// sends cursed flame along the ground). Ranged kit: a cursed-fire arcane ball, venom staff, meteors and
    /// Twin Storm. Set-pieces on the Custom phase: the Poison Cloud flask, Cursed Blink (line-of-sight
    /// teleport into a thrust), Cinder Wingbeat (anti-hug gust) and Dragon Ascendant (the 50% phase change).
    /// Gives up (fades out) after 9s with the target off screen and out of sight.
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
        protected override float FlightHpEscalationFrac => PhaseTwoLifeFraction;
        // Half of aerial melee picks are Dragoon Plunge; the other half keep the generic pierce-through swoop.
        protected override int AirborneComboChancePercent => 50;

        // ── Loadout ───────────────────────────────────────────────────────────────
        protected override int HeadArmorItemType => ModContent.ItemType<CursedDragonHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<CursedDragonArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<CursedDragonGreaves>();

        // Spear drives melee combos (Halberd archetype); its sprite is also shown while casting the
        // arcane ball (primary ranged), since the spear is the arcane ball's caster. The standalone
        // spear-poke phase (SpearWeaponItemType >= 0) is retired: Pike Thrust / Skewer String /
        // Dragoon Leap in the combo pool below fully replace it with proper easing and reach.
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyPilgrimSpontoon>();
        protected override int SpearWeaponItemType => -1;
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyPilgrimSpontoon>();
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<EnemyVenomStaff>();
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyMeteorStorm>();

        // ── Damage (late hardmode: post-mech, ~Golem/Plantera tier; no SHM scaling) ──
        protected override int MeleeDamage => 60;   // spear combos
        protected override int SpearDamage => 55;   // pierce-through aerial swoop only (standalone poke retired)
        protected override int RangedDamage => 45;  // arcane ball
        protected override int SecondaryRangedDamage => 40; // venom fang (multi-projectile)
        protected override int MagicDamage => 50;   // meteor

        // ── Melee ───────────────────────────────────────────────────────────────────
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Halberd;
        protected override int MeleeComboChance => 60;
        // 520, not the old 260: puts the Dragoon Leap gap-closer (RangedStartOnly) in reach of a
        // kiting player instead of only ever firing at near-point-blank range.
        protected override float ComboMaxStartRange => 520f;
        protected override float MeleeRange => 88f;
        protected override float StabRange => 170f;
        // The standalone spear poke is retired (SpearWeaponItemType => -1 above) so its old timing
        // knobs (SpearRange/SpearTelegraphTicks/etc.) are gone too. SpearDamage survives - the
        // pierce-through swoop still reads it directly.

        // ── Swing quality (composite arm + authored easing, see attack-timing-design / puppet-swing-tuning) ──
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseLogicalMeleeTelegraphs => true; // wind to the opposite end, then strike
        protected override bool UseSwingEasing => true;
        protected override bool UseAimAdaptiveArc => true;        // arc centres on the player, not a fixed angle
        protected override bool UseAuthoredComboSwingClock => true;
        // JoustDash-motion thrusts (Pike Thrust, Skewer String) are authored with AttackTicks (24)
        // under the spear item's useAnimation (27) - without this, the swing clock sweeps over
        // useAnimation instead and the thrust is cut off mid-arc.
        protected override bool AuthoredClockCoversJoustDash => true;
        // Ties the spear's extend/retract grip pulse to the SAME eased clock as the blade rotation,
        // instead of its own separate PhaseTimer-based hump - the two used to drift apart.
        protected override bool UseAuthoredSpearGrip => true;
        // Spinning Windmill: ease only at the start (wind-up) and end (settle) of the spin, constant
        // speed through the middle - see the combo pool below and PuppetNPC's ComboMotion.Spin case.
        protected override bool UseEasedSpin => true;
        protected override int MeleeComboInterStepLingerTicks => 3;  // hold through inter-step pauses
        protected override int MeleeRecoveryLingerTicks => 10;       // park the finished pose before easing to carry
        protected override bool UseLandingTimedLeapSlam => true;     // Windmill finisher, Dragoon Leap, Dragoon Plunge
        // Halberd held in both hands (D7 trial). Judge from a SwingPreview --body render before keeping it.
        protected override bool UseTwoHandedCompositeSwing => true;
        // Rear hand 8px down the shaft toward the butt (a halberd grip), like GreatRedKnight's lance (-5).
        protected override float TwoHandedBackGripOffset => -8f;

        // Trimmed to the drawn spear's actual reach (~140px full extension, ~122px mid-shaft swing
        // grip) instead of the old 230, which hit up to 2.3x past the visible tip. Per-step ReachMult
        // in the combo pool below tunes each motion's grip length the rest of the way.
        protected override float ComboReachBase => 190f;
        // = the longest authored hit reach (Pike Thrust's 133px) plus a small buffer, not the old 200:
        // starting a standing thrust any further out would whiff before the puppet's own approach.
        protected override float MeleeEngageRange => 160f;
        // Keep pursuing through the wind-up so the player can't just step out of range.
        protected override bool SlowDownBeforeMelee => false;

        // Arc endpoints, keyed per motion and (for Wyrmfall Cleave) per live combo (attack-timing-design §3/§5).
        // Thrust: the old JoustDash tell dipped from the carry pose to 45 degrees below level (107 degrees of
        //   tell against a 45 degree strike, so the tell outran the strike). Couched 17 degrees above level
        //   instead: the tell is a ~48 degree draw, the strike settles 17 degrees to level, and the grip
        //   extension (UseAuthoredSpearGrip) carries the actual thrust.
        // Reaping Arc: both cuts widened at the low end (the underhand starts at the low-forward 1.6 pose, the
        //   overhead follows through to 2.1). The shared -1.3 joint between them is unchanged, so no snap.
        // Wyrmfall Cleave: the signature heavy, cocked past vertical to straight down: 228 degree envelope,
        //   ~182 degree live sweep.
        private const float ThrustCouchRaise = 0.30f;
        private const float ReapingLowStart = 1.6f;
        private const float ReapingOverheadEnd = 2.1f;
        private const float WyrmfallStart = -1.62f;
        private const float WyrmfallEnd = 2.36f;

        private bool WyrmfallCleaveSwinging =>
            ActiveMeleeComboName == WyrmfallCleaveName
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
                    a0 = ReapingLowStart;
                    a1 = -1.3f; // ends exactly where OverheadArc's cocked start sits
                    break;
                case ComboMotion.OverheadArc:
                    if (WyrmfallCleaveSwinging)
                    {
                        a0 = WyrmfallStart;
                        a1 = WyrmfallEnd;
                    }
                    else
                    {
                        a1 = ReapingOverheadEnd;
                    }
                    break;
            }
        }

        protected override MeleeCombo[] MeleeComboPoolOverride => CursedDragonCombos;

        // ── Selection gates (Blaidd / OwlFather lineage) ─────────────────────────────
        // rise = how far the target stands ABOVE the dragon (px). Ground moves never start at a target more than
        // GroundCutMaxRise above; Ascending Pike owns the band above that. Every move needs a clear line, so
        // nothing launches into a wall. ComboStartMaxTargetRise is raised to the anti-air ceiling so the base
        // lets Ascending Pike start at all.
        private const float GroundCutMaxRise = 48f;
        private const float AscendingPikeMaxRise = 240f;
        private const float AscendingPikeMaxHorizontal = 160f;
        private const float ThrustMinRange = 100f;
        private const float ThrustMaxRange = 260f;
        private const float DragoonLeapMinRange = 260f;
        // A whiffed follow-up continues only while the target is within the next step's reach plus this (Blaidd).
        private const float FollowUpPushAllowance = 40f;

        protected override float ComboStartMaxTargetRise => AscendingPikeMaxRise;

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            Player target = Main.player[NPC.target];
            float rise = NPC.Center.Y - target.Center.Y;
            float horizontal = Math.Abs(target.Center.X - NPC.Center.X);
            bool clearLine = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            // Dragoon Plunge starts from the hover and dives at the target in any direction.
            if (combo.AirborneStartOnly)
            {
                return clearLine;
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

            if (combo.Name == WyrmfallCleaveName)
            {
                return _phaseTwoStarted;
            }

            // Leaps only from where they reliably land, so they never come down short.
            if (combo.Name == DragoonLeapName)
            {
                return distance >= DragoonLeapMinRange && distance <= ReliableJumpStartRange(combo.Steps[0]);
            }

            if (combo.Name == PikeThrustName || combo.Name == SkewerStringName)
            {
                return distance >= ThrustMinRange && distance <= ThrustMaxRange;
            }

            return true;
        }

        protected override bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            float targetDistance = NPC.Distance(target.Center);

            if (comboName == SpinningWindmillName && nextStepIndex == 1)
            {
                // Below 25% the finisher always follows. Otherwise the spin already chases (ForwardPushMult on the
                // Spin step); the leaping slam only bolts on when that wasn't enough to close the gap.
                if ((float)NPC.life / NPC.lifeMax <= FinalTierLifeFraction)
                {
                    return true;
                }
                return targetDistance > SpinningWindmillFinisherRange;
            }

            // Conditional follow-ups: a hit always earns the next step; a whiff only does if the next step can still
            // reach. A clean roll-away ends the string instead of stabbing air (HP-escalation reps included).
            float nextReachMult = 0f;
            if (comboName == PikeThrustName || comboName == SkewerStringName)
            {
                nextReachMult = ThrustReachMult;
            }
            else if (comboName == ReapingArcName)
            {
                nextReachMult = ReapingReachMult;
            }

            if (nextReachMult > 0f)
            {
                float followUpReach = ComboReachBase * 0.7f * nextReachMult + FollowUpPushAllowance;
                bool stillInReach = targetDistance <= followUpReach;
                return previousStepHit || stillInReach;
            }

            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        // A fully whiffed Pike Thrust sprints back in and re-opens with Skewer String, once per chain (Blaidd G3).
        protected override int MeleeFollowUpChancePercent(int followUpsDone) => followUpsDone == 0 ? 60 : 0;

        protected override string MeleeFollowUpComboName(string finishedComboName)
        {
            if (finishedComboName == PikeThrustName)
            {
                return SkewerStringName;
            }
            return null;
        }

        // Cursed Blink and Cinder Wingbeat both end by opening a window in which the next combo the base starts
        // is Pike Thrust (still gated by CanSelectMeleeCombo: range band + clear line). Server-only state.
        private const int ThrustFollowUpWindowTicks = 60;
        private int _thrustFollowUpWindow;

        protected override int ReactiveComboIndex(float dist, ComboRangeBand band, int[] ready)
        {
            if (_thrustFollowUpWindow <= 0)
            {
                return -1;
            }

            for (int i = 0; i < CursedDragonCombos.Length && i < ready.Length; i++)
            {
                if (CursedDragonCombos[i].Name == PikeThrustName && ready[i] > 0)
                {
                    _thrustFollowUpWindow = 0;
                    return i;
                }
            }

            return -1;
        }

        // ── HP-scaled combo escalation ───────────────────────────────────────────────
        // "Reduced recovery from the start, longer combos as HP drops" - Gwyn's HalfHealthMovesUnlocked pattern.
        // Two tiers: +1 rep from the 50% phase change, +2 from 25%. Every appended rep's PostStepPause respects the
        // same MinTicksBetweenLiveWindows floor as the roll-cooldown rule (attack-timing-design §2 rule 3), and
        // every rep is conditional (ShouldContinueMeleeCombo above).
        private const int MinTicksBetweenLiveWindows = 30;
        private const int PauseSafetyBuffer = 3; // small cushion above the bare mathematical floor

        /// <summary>Extra repetitions of a combo's last step to append: 0 above 50%, +1 at or below 50%, +2 at or
        /// below 25%.</summary>
        private static int ExtraRepsForHealth(float healthFraction)
        {
            if (healthFraction <= FinalTierLifeFraction)
            {
                return 2;
            }
            if (healthFraction <= PhaseTwoLifeFraction)
            {
                return 1;
            }
            return 0;
        }

        /// <summary>The safe PostStepPause floor for a step: the smallest gap that still keeps the
        /// NEXT live window starting >= MinTicksBetweenLiveWindows after this one's live window ends,
        /// plus a small buffer. Needs a real HitWindowEnd; a step without one is armed for the whole
        /// phase (tail = 0).</summary>
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
            // leaves room under the fairness rule; tightened here so the maths sits next to the rule.
            if (combo.Name == ReapingArcName)
            {
                MeleeComboStep first = combo.Steps[0];
                first.PostStepPause = SafeFollowUpPause(first);
                combo.Steps[0] = first;
            }

            // Longer combos at low HP: only the thrust family chains further - the wide-arc and heavy
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
        private const string PikeThrustName = "Pike Thrust";
        private const string SkewerStringName = "Skewer String";
        private const string ReapingArcName = "Reaping Arc";
        private const string DragoonLeapName = "Dragoon Leap";
        private const string WyrmfallCleaveName = "Wyrmfall Cleave";
        private const string SpinningWindmillName = "Spinning Windmill";
        private const string AscendingPikeName = "Ascending Pike";
        private const string DragoonPlungeName = "Dragoon Plunge";
        private const float SpinningWindmillFinisherRange = 150f;
        private const float ThrustReachMult = 1.05f;
        private const float ReapingReachMult = 0.9f;

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

        /// <summary>Pike Thrust's step, borrowed from Blaidd's Fang Thrust: JoustDash on the authored clock, Whip,
        /// live for the first 75% (18t, rollable), then a harmless extension. Push 1.6 x 3.5 = 5.6px/t over 24t
        /// = 134px, so it overshoots a standing target.</summary>
        private static MeleeComboStep ThrustStep(int telegraph, int pause, float damage)
        {
            MeleeComboStep step = GS(ComboMotion.JoustDash, telegraph, 24, pause, damage, ThrustReachMult, 1.6f, SwingEaseStyle.Whip);
            step.HitWindowEnd = 0.75f;
            return step;
        }

        /// <summary>A LeapSlam step with its jump shape (OwlFather's leap family). leapStrikeRange > 0 swings the
        /// spear mid-air once past the apex with the target that close; otherwise the landing slam hits.</summary>
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
         * Pike Thrust:      Blaidd's Fang Thrust. tel 27 -> 36t | JoustDash Whip atk 24t, live 18t (< 22,
         *                   rollable), 6t harmless extension + recovery 18 | travels ~134px | gate 100-260px + LOS
         * Skewer String:    two thrusts, pause 24t -> next live 30t after the first ends (6t tail + 24); the second
         *                   is conditional (hit, or target within 140 + 40px)
         * Reaping Arc:      Underhand 1.6 -> -1.3 then Overhead -1.3 -> 2.1 (envelopes 166 / 195 deg, live ~133 /
         *                   ~156), 8/34 k7 each, live ~14t, pause = SafeFollowUpPause; overhead conditional
         * Dragoon Leap:     OwlFather's Greatfire Pursuit Slam step (Blaidd PounceStep). tel 36 -> 49t | LeapSlam
         *                   height 0.62, forward 1.7, swept 10t downswing within 120px past the apex, else the
         *                   landing slam | gate 260px .. ReliableJumpStartRange + LOS
         * Wyrmfall Cleave:  phase two. tel 52 -> 70t | Overhead -1.62 -> 2.36 (228 deg, live ~182), 10/45 k7, live
         *                   ~18t | push 0.22 (~40px) | then one cursed flame wave each way along the ground,
         *                   jumpable | open: 37t tail + 40 recovery
         * Spinning Windmill: tel 36t ramp | spin 40t, ARMED ALL 40 (not rollable: a spacing check - running at 6-7
         *                   px/t outpaces the 3.15 px/t chase), danger ring drawn at the 133px hit radius |
         *                   conditional LeapSlam finisher (target still > 150px, or always below 25%)
         * Ascending Pike:   StuddedLeatherWarrior's Leaping Uppercut (Blaidd Rising Fang). tel 34 -> 46t | gate:
         *                   target 48-240px above, within 160px horizontally, LOS
         * Dragoon Plunge:   OwlFather's Greatfire Dive. Airborne only. tel 36 -> 49t hovering | ~9px/t dive, 10t
         *                   swept downswing within 200px, else landing slam | six cursed pyres on landing
         *
         * Every live window above is under 22t except the Windmill's spin (see its line), and every multi-step
         * follow-up goes live >= 30t after the previous live window ends or is conditional (§2 rules 1 and 3).
         */
        private static readonly MeleeCombo[] CursedDragonCombos = new[]
        {
            new MeleeCombo
            {
                Name = PikeThrustName, BaseWeight = 90, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 90, RecoveryTicks = 18, MoveBrake = 0f,
                Steps = new[] { ThrustStep(27, 0, 1.1f) }
            },
            new MeleeCombo
            {
                Name = SkewerStringName, BaseWeight = 70, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 150, RecoveryTicks = 24, MoveBrake = 0f,
                Steps = new[]
                {
                    ThrustStep(27, 24, 1.0f),
                    ThrustStep( 0,  0, 1.0f),
                }
            },
            new MeleeCombo
            {
                Name = ReapingArcName, BaseWeight = 60, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 150, RecoveryTicks = 14,
                Steps = new[]
                {
                    WeightedSwing(ComboMotion.UnderhandArc, 30, 8, 34, 7f, 10, 0.9f, ReapingReachMult),
                    WeightedSwing(ComboMotion.OverheadArc,   0, 8, 34, 7f,  0, 1.1f, ReapingReachMult),
                }
            },
            new MeleeCombo
            {
                Name = DragoonLeapName, BaseWeight = 90, Preferred = ComboRangeBand.Far,
                RangedStartOnly = true, InitialFlashColor = Color.Yellow, CooldownAfterUse = 200,
                HeavyCommit = true, HyperArmor = true, RecoveryTicks = 30, MoveBrake = 0.06f,
                Steps = new[] { LeapStep(36, 82, 1.25f, 1.25f, 0.62f, 1.70f, 120f, SwingEaseStyle.Whip) }
            },
            new MeleeCombo
            {
                Name = WyrmfallCleaveName, BaseWeight = 35, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red, CooldownAfterUse = 300, HeavyCommit = true, RecoveryTicks = 40,
                MoveBrake = 0.48f,
                Steps = new[] { WeightedSwing(ComboMotion.OverheadArc, 52, 10, 45, 7f, 0, 1.5f, 1.0f, 0.22f) }
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
            new MeleeCombo
            {
                Name = AscendingPikeName, BaseWeight = 120, Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.LightCyan, CooldownAfterUse = 210, HeavyCommit = true, MoveBrake = 0.08f,
                Steps = new[] { GS(ComboMotion.RisingUppercutLeap, 34, 150, 0, 1.22f, 1.20f) }
            },
            new MeleeCombo
            {
                Name = DragoonPlungeName, BaseWeight = 100, Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.OrangeRed, CooldownAfterUse = 300, HeavyCommit = true, HyperArmor = true,
                AirborneStartOnly = true, MoveBrake = 0f,
                // No ease: LeapSlam never reads it (landing-timed 10t SmoothStep downswing).
                Steps = new[] { LeapStep(36, 90, 1.30f, 1.28f, 1f, 1f, 200f, SwingEaseStyle.Smooth) }
            },
        };

        // ── Melee VFX hooks ─────────────────────────────────────────────────────────
        // Spinning Windmill: a cursed-flame ring at the spin's 133px hit radius for every armed tick, so "don't be
        // inside the circle" is drawn (the dust is the telegraph). Runs on every peer; cosmetic only.
        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboAttackTick(combo, step, elapsed, total);

            bool windmillSpin = combo.Name == SpinningWindmillName && step.Motion == ComboMotion.Spin;
            if (!windmillSpin || Main.dedServ)
            {
                return;
            }

            float hitRadius = ComboReachBase * 0.7f * step.ReachMult;
            for (int i = 0; i < 3; i++)
            {
                Vector2 rimPoint = NPC.Center + Main.rand.NextVector2CircularEdge(hitRadius, hitRadius);
                Dust flame = Dust.NewDustPerfect(rimPoint, DustID.RedTorch, Vector2.Zero, 0, default, 1.5f);
                flame.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 accentPoint = NPC.Center + Main.rand.NextVector2CircularEdge(hitRadius, hitRadius);
                PowerfulCurseBuildup.SpawnAccentDust(accentPoint, 0, 0, Vector2.Zero, 1.3f);
            }
        }

        // Wyrmfall Cleave: one cursed flame wave each way along the ground when the overhead lands (OwlFather's
        // Greatfire Breaker crawler, ground-following mode). 7px/t, so a wave reaches 240px ~34t later: jump it.
        private const int WyrmfallWaveDamage = 15; // hostile projectile damage is doubled on hit -> 30
        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            bool wyrmfallImpact = ActiveMeleeComboName == WyrmfallCleaveName && step.Motion == ComboMotion.OverheadArc;
            if (!wyrmfallImpact)
            {
                return;
            }

            UsefulFunctions.ScreenShake(NPC.Bottom, 3.75f, 11, distanceFalloff: 560f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int waveType = ModContent.ProjectileType<PuppetGreatfireCrescent>();
            for (int direction = -1; direction <= 1; direction += 2)
            {
                float startX = NPC.Bottom.X + direction * 14f;
                if (!PuppetGroundDustWave.TryFindGroundY(startX, NPC.Bottom.Y, out float groundY))
                {
                    continue;
                }

                Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(startX, groundY - 26f),
                    new Vector2(direction * 7f, 0f), waveType, WyrmfallWaveDamage, 4f, Main.myPlayer,
                    PuppetGreatfireCrescent.GroundMode);
            }
        }

        // Dragoon Plunge: six pyres erupt outward from the landing (OwlFather's High Leap columns), each farther pair
        // 6 ticks later, so the eruption visibly travels. Gaps of 48px between columns: step between them or roll.
        private const int PlungePyreDamage = 20;
        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (ActiveMeleeComboName != DragoonPlungeName || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int direction = -1; direction <= 1; direction += 2)
            {
                for (int column = 0; column < 3; column++)
                {
                    float x = NPC.Bottom.X + direction * (24f + column * 48f);
                    if (!PuppetGroundDustWave.TryFindGroundY(x, NPC.Bottom.Y, out float groundY))
                    {
                        continue;
                    }

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(x, groundY), Vector2.Zero,
                        ModContent.ProjectileType<PuppetFireWaveColumn>(), PlungePyreDamage, 3f, Main.myPlayer,
                        96f, direction * 6f, PuppetFireWaveColumn.EncodeSlamDelay(column * 6));
                }
            }
        }

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
            new Color(255, 90, 80),   // fan
            new Color(230, 60, 70),
            new Color(255, 110, 120),
            new Color(200, 80, 220),  // circle-burst finisher — plum
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

        // ── Phases ──────────────────────────────────────────────────────────────────
        // One visible phase change at 50% (Dragon Ascendant, below) unlocks the venom staff, cursed knives, Twin
        // Storm and Wyrmfall Cleave, +1 thrust rep and the flight escalation. A final tier at 25% adds the second
        // rep, always chains the Windmill finisher and fires 4 meteor blasts. _phaseTwoStarted is set when the
        // transition STARTS (so it can't fire twice) and is synced: clients read it for the ember aura and gates.
        private const float PhaseTwoLifeFraction = 0.50f;
        private const float FinalTierLifeFraction = 0.25f;
        private bool _phaseTwoStarted;

        protected override bool SecondaryRangedAvailable => _phaseTwoStarted;

        // ── Agility: proactive projectile evasion + preemptive quick-step ───────────
        protected override bool EvadesProjectiles => true;          // jump / roll vs incoming ranged
        protected override int PreemptiveQuickStepChance => 35;     // dash THROUGH a meleeing player
        // Land past the player with room, and a recovery window so it can't instantly re-attack — but short
        // enough that the dash still buys tempo.  (~0.6 s recovery, then the next attack still telegraphs.)
        protected override int QuickStepRecoveryTicks => 36;
        protected override float QuickStepForwardRoom => 56f;

        // ── Cursed Knives (phase two; usable grounded or flying) ─────────────────────
        protected override bool CanThrowCursedKnives => _phaseTwoStarted;
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
        protected override Color RangedTelegraphFlashColor => new Color(255, 90, 80); // cursed-fire arcane ball
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

        // 30, not the base default of 140 - matches Black Ninja's tuning. Used by the lava-escape blink and
        // by both directed teleports (Cursed Blink, Poison Cloud).
        protected override int TeleportTelegraphTicks => 30;

        // ── Leash ───────────────────────────────────────────────────────────────────
        // Gives up after 9s of the target being off screen AND out of line of sight: it fades out through the base
        // party-wipe despawn (which also resets the arena event). Any sight of the target, or the target coming
        // back on screen, resets the clock. Off screen = beyond half a 1080p screen from the dragon.
        private const int LeashGiveUpTicks = 540;
        private const float OffScreenHalfWidth = 1000f;
        private const float OffScreenHalfHeight = 580f;

        protected override int AbandonedDespawnTicks => LeashGiveUpTicks;

        protected override bool IsEncounterAbandoned()
        {
            if (!NPC.HasValidTarget)
            {
                return false; // no target at all is the party-wipe handler's case
            }

            Player target = Main.player[NPC.target];
            float horizontalGap = Math.Abs(target.Center.X - NPC.Center.X);
            float verticalGap = Math.Abs(target.Center.Y - NPC.Center.Y);
            bool offScreen = horizontalGap > OffScreenHalfWidth || verticalGap > OffScreenHalfHeight;
            bool canSee = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            return offScreen && !canSee;
        }

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
            NPC.npcSlots = 10f;
            NPC.lavaImmune = true;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 60f;                 // heavy armored dragon — slow to stagger
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;

            // Agile: moderate proactive projectile dodging (jumps / i-frame rolls at incoming shots).
            globalNPC.Agility = 0.35f;

            // Plague-style teleports, but no automatic "lost sight -> blink back" (Aggressive): the dragon teleports
            // only as an attack with line of sight (Cursed Blink, 10s cooldown) and for the Poison Cloud cut-off.
            // Out of sight and off screen it gives up instead (IsEncounterAbandoned). RecoveryOnly keeps lava escape.
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.RecoveryOnly;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.Plague;
            // TeleportTelegraphTicks override below (30, matching Black Ninja) applies to both directed teleports.

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
        // Runs on every peer: each Fire helper plays its sound first, then returns on multiplayer clients before
        // spawning (the server owns the projectiles). The branch inputs (burst pattern, shot index) are synced.
        protected override void DoRangedAttack()
        {
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
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

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
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

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
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.6f, PitchVariance = 0.12f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 18f, -8f);
            Vector2 aimAt = target.Center + target.velocity * 8f;
            Vector2 vel = (aimAt - muzzle).SafeNormalize(new Vector2(NPC.direction, 0f)).RotatedByRandom(MathHelper.ToRadians(5f)) * 10.5f;

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
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

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
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 center = new Vector2(NPC.Center.X, NPC.Center.Y - 440f);
            Vector2 aimAt = SnapMeteorMarkToGround(target.Center);
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

        /// <summary>Moves a meteor's landing point down onto the floor under it, so the warning ring sits on the
        /// ground (not mid-air at a jumping player's centre) and the meteor, which detonates on reaching the mark's
        /// height, bursts at floor level. MeteorHalfHeight lifts the mark so the 48px meteor's centre stops just
        /// above the surface. Keeps the point unchanged when there's no floor within the search range.</summary>
        private const float MeteorHalfHeight = 24f;
        private static Vector2 SnapMeteorMarkToGround(Vector2 point)
        {
            bool foundGround = PuppetGroundDustWave.TryFindGroundY(point.X, point.Y + 32f, out float groundY);
            if (!foundGround)
            {
                return point;
            }

            return new Vector2(point.X, groundY - MeteorHalfHeight);
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

        // ── Magic: meteor triad burst (phase-scaled blast count/spacing/spread) + rare sustained rain ──
        // Phase one: 2 blasts, 60 ticks apart, baseline spread.
        // Phase two: 3 blasts, 45 ticks apart, wider spread.
        // Final tier (<= 25%): 4 blasts, 30 ticks apart, slightly wider spread still.
        protected override void DoMagicAttack()
        {
            // Twin Storm takes priority over the meteor kit whenever a chain is armed. _twinStormCastsLeft is synced,
            // so every peer takes this branch and hears the cast; only the server consumes a cast and spawns.
            // With MagicRecoveryTicks at 0 the next telegraph begins almost immediately.
            if (TwinStormQueued)
            {
                FireTwinStorm();
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];

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
            if (hpFrac <= FinalTierLifeFraction)
            {
                totalBursts = 4; _meteorBurstGapTicks = 30;
                _meteorBurstSpread = 420f; _meteorBurstFlankJitter = 42f; _meteorBurstCenterJitter = 28f;
            }
            else if (_phaseTwoStarted)
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
            landAt = SnapMeteorMarkToGround(landAt);
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
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, PitchVariance = 0.2f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
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

        // ── Aerial: the generic pierce-through swoop ─────────────────────────────────
        // Half the aerial melee picks (AirborneComboChancePercent) take this flight-controller dive, which leads the
        // target and spears through; the other half are Dragoon Plunge. The old Aerial Slam variant is retired - its
        // roll and impact flag only ever existed on the server. The dive sound now plays on every peer from the
        // synced flight mode (PostAI), instead of in DoAerialDiveTelegraph, which only the server runs.
        private bool _wasDiving; // last tick's Flight.IsDiving, per peer, for that edge

        protected override void DoAerialDiveHit()
        {
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

        // ── Custom set-pieces ──────────────────────────────────────────────────────
        // Four bespoke sequences share the base Custom phase. _setPiece says which one is running; it is synced
        // (SendExtraAI) because clients run DoCustomAttack/DoCustomTick/CustomWeaponRotation too, and each reads its
        // own stage lengths. Stage lengths are constants, so every peer computes the same timeline from PhaseTimer.
        // A queued teleport freezes the phase timer (TickPuppetTeleport returns early), so teleport ticks are not
        // part of any total below.
        private enum DragonSetPiece : byte
        {
            None,
            PoisonCloud,
            CursedBlink,
            CinderWingbeat,
            DragonAscendant,
        }

        private DragonSetPiece _setPiece;

        private DragonSetPiece ActiveSetPiece => Phase == AttackPhase.Custom ? _setPiece : DragonSetPiece.None;

        // The phase change and the wing gust are hyper-armored; the teleports and the flask throw can be staggered.
        protected override bool CustomAttackCommitted =>
            ActiveSetPiece == DragonSetPiece.DragonAscendant || ActiveSetPiece == DragonSetPiece.CinderWingbeat;

        protected override bool ForceWingsSpread =>
            ActiveSetPiece == DragonSetPiece.DragonAscendant || ActiveSetPiece == DragonSetPiece.CinderWingbeat;

        private void StartSetPiece(DragonSetPiece setPiece, int totalTicks)
        {
            _setPiece = setPiece;
            StartCustomAttack(totalTicks, MeleeWeaponItemType, swingPose: false);
            NPC.netUpdate = true;
        }

        protected override void DoCustomAttack()
        {
            switch (_setPiece)
            {
                case DragonSetPiece.PoisonCloud:
                    DebugAttackLabel = "Poison Cloud";
                    tsorcRevampAIs.QueueTeleportToDestination(NPC, _teleportDestination, TeleportTelegraphTicks);
                    break;
                case DragonSetPiece.CursedBlink:
                    DebugAttackLabel = "Cursed Blink";
                    tsorcRevampAIs.QueueTeleportToDestination(NPC, _teleportDestination, TeleportTelegraphTicks);
                    break;
                case DragonSetPiece.CinderWingbeat:
                    DebugAttackLabel = "Cinder Wingbeat";
                    break;
                case DragonSetPiece.DragonAscendant:
                    DebugAttackLabel = "Dragon Ascendant";
                    break;
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            switch (_setPiece)
            {
                case DragonSetPiece.PoisonCloud:
                    TickPoisonCloud(PoisonCloudTotalTicks - ticksRemaining);
                    break;
                case DragonSetPiece.CursedBlink:
                    // Last tick: open the window in which the next combo started is Pike Thrust.
                    if (ticksRemaining == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        _thrustFollowUpWindow = ThrustFollowUpWindowTicks;
                    }
                    break;
                case DragonSetPiece.CinderWingbeat:
                    TickCinderWingbeat(CinderWingbeatTotalTicks - ticksRemaining);
                    break;
                case DragonSetPiece.DragonAscendant:
                    TickDragonAscendant(DragonAscendantTotalTicks - ticksRemaining);
                    break;
            }
        }

        protected override float? CustomWeaponRotation
        {
            get
            {
                const float carryPose = -0.30f;
                switch (ActiveSetPiece)
                {
                    case DragonSetPiece.PoisonCloud:
                        return PoisonCloudArmRotation(PoisonCloudTotalTicks - PhaseTimer, carryPose);
                    case DragonSetPiece.CursedBlink:
                        return carryPose;
                    case DragonSetPiece.CinderWingbeat:
                        return 1.2f; // spear lowered and held out of the way while the wings do the work
                    case DragonSetPiece.DragonAscendant:
                        return 2.2f; // spear planted point-down at its feet
                    default:
                        return null;
                }
            }
        }

        // Shared by Cursed Blink and Poison Cloud: the directed teleport's landing point. Server-only, read once by
        // DoCustomAttack on the server (QueueTeleportToDestination self-gates to the server).
        private Vector2 _teleportDestination;

        /// <summary>Finds a landing spot <paramref name="offsetPx"/> from the player - ahead of their run direction
        /// when <paramref name="aheadOfRun"/> and they are moving, else on the far side of them from the dragon -
        /// scanning down for solid ground with clearance for the puppet's hitbox. Tries a few nearby columns if the
        /// first is blocked; returns false if none work.</summary>
        private bool TryFindLandingSpotNearPlayer(Player target, float offsetPx, bool aheadOfRun, out Vector2 destinationCenter)
        {
            destinationCenter = Vector2.Zero;

            float desiredX;
            if (aheadOfRun && Math.Abs(target.velocity.X) > 1f)
            {
                desiredX = target.Center.X + Math.Sign(target.velocity.X) * offsetPx;
            }
            else
            {
                int oppositeSide = target.Center.X < NPC.Center.X ? -1 : 1; // away from the dragon's current side
                desiredX = target.Center.X + oppositeSide * offsetPx;
            }

            int centerTileX = (int)(desiredX / 16f);
            int startTileY = (int)(target.Center.Y / 16f) - 6; // a few tiles above the player's own footing

            for (int columnTry = 0; columnTry < 5; columnTry++)
            {
                int columnOffset = 0;
                if (columnTry % 2 == 1)
                {
                    columnOffset = columnTry / 2 + 1;
                }
                else if (columnTry > 0)
                {
                    columnOffset = -(columnTry / 2);
                }
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

        // ── Poison Cloud flee-counter (debug label: "Poison Cloud") ─────────────────
        // If the player runs more than 60 tiles away: teleport (30t) ~10 tiles in front of their run direction to
        // cut off the escape, raise the spear, then lob a green poison flask from the spear tip that bursts into a
        // large stationary toxic cloud (CursedDragonPoisonFlask -> CursedDragonPoisonCloud). The cloud punishes
        // lingering with poison and curse buildup (Powerful Curse after 3s inside) rather than direct damage.
        private const float PoisonCloudTriggerRange = 60f * 16f; // 60 tiles
        private const int PoisonCloudSettleTicks = 6;            // beat after the reveal before raising the arm
        private const int PoisonCloudArmRaiseTicks = 20;
        private const int PoisonCloudRecoveryTicks = 24;
        private const int PoisonCloudTotalTicks = PoisonCloudSettleTicks + PoisonCloudArmRaiseTicks + PoisonCloudRecoveryTicks;
        private const int PoisonCloudCooldownTicks = 1800; // 30s - a rare punish, not a repeatable spam
        private const float PoisonCloudLandingOffset = 10f * 16f;
        // The flask lands this far past the player along the throw, so a standing target is inside the cloud and a
        // target walking at the dragon is too. 18-30t flight: long enough to see the arc, short enough to commit.
        private const float FlaskOvershoot = 40f;
        private const float FlaskSpeedForFlightTime = 12f; // px per tick of straight-line distance -> flight ticks
        private const int FlaskMinFlightTicks = 18;
        private const int FlaskMaxFlightTicks = 30;

        private int _poisonCloudCooldown;

        private void TickPoisonCloud(int elapsed)
        {
            int releaseTick = PoisonCloudSettleTicks + PoisonCloudArmRaiseTicks;
            if (elapsed == releaseTick)
            {
                ThrowPoisonFlask();
            }
        }

        /// <summary>The arm through the cloud's three visible beats: carry through the settle, raised overhead
        /// through the wind-up, thrown, then eased back down.</summary>
        private static float PoisonCloudArmRotation(int elapsed, float carryPose)
        {
            const float raisedPose = -1.55f;

            if (elapsed < PoisonCloudSettleTicks)
            {
                return carryPose;
            }

            int releaseTick = PoisonCloudSettleTicks + PoisonCloudArmRaiseTicks;
            if (elapsed < releaseTick)
            {
                float raiseProgress = (elapsed - PoisonCloudSettleTicks) / (float)PoisonCloudArmRaiseTicks;
                return MathHelper.Lerp(carryPose, raisedPose, MathHelper.SmoothStep(0f, 1f, raiseProgress));
            }

            int recoveryElapsed = elapsed - releaseTick;
            float recoveryProgress = MathHelper.Clamp(recoveryElapsed / (float)PoisonCloudRecoveryTicks, 0f, 1f);
            return MathHelper.Lerp(raisedPose, carryPose, MathHelper.SmoothStep(0f, 1f, recoveryProgress));
        }

        /// <summary>Lobs the flask from the spear tip on a ballistic arc solved to land FlaskOvershoot past the
        /// player's feet. Discrete solve matching the flask's own integration (gravity added, then moved):
        /// y(T) = vy*T + g*T(T+1)/2.</summary>
        private void ThrowPoisonFlask()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 tip = GetSpearTipWorldPosition(92f);
            int throwDirection = target.Center.X >= tip.X ? 1 : -1;
            Vector2 landAt = new Vector2(target.Center.X + throwDirection * FlaskOvershoot, target.Bottom.Y - 8f);

            Vector2 toLanding = landAt - tip;
            int flightTicks = (int)(toLanding.Length() / FlaskSpeedForFlightTime);
            flightTicks = Math.Clamp(flightTicks, FlaskMinFlightTicks, FlaskMaxFlightTicks);

            float gravity = CursedDragonPoisonFlask.Gravity;
            float velocityX = toLanding.X / flightTicks;
            float gravityDrop = gravity * flightTicks * (flightTicks + 1) / 2f;
            float velocityY = (toLanding.Y - gravityDrop) / flightTicks;

            Projectile.NewProjectile(NPC.GetSource_FromThis(), tip, new Vector2(velocityX, velocityY),
                ModContent.ProjectileType<CursedDragonPoisonFlask>(), 0, 0f, Main.myPlayer);
        }

        // ── Cursed Blink (debug label: "Cursed Blink") ──────────────────────────────
        // A teleport used as an attack, only with line of sight: 30t plague-cloud telegraph at BOTH ends (the arrival
        // cloud shows where it lands), reappearing 150px on the far side of the player, then Pike Thrust's 36t tell.
        // Counter: watch the arrival cloud and turn, or move off the thrust's line. 10s cooldown.
        private const int CursedBlinkSettleTicks = 8;
        private const int CursedBlinkCooldownTicks = 600;
        private const float CursedBlinkMinRange = 300f;
        private const float CursedBlinkMaxRange = 900f;
        private const float CursedBlinkLandingOffset = 150f;
        private const int CursedBlinkChanceDenominator = 60; // per eligible idle tick: ~1s average wait once ready

        private int _cursedBlinkCooldown;

        // ── Cinder Wingbeat (debug label: "Cinder Wingbeat") ────────────────────────
        // Anti-hug. After the target has spent CinderHugTicks within CinderHugRange, the wings flare open (24t tell,
        // plum embers drawn INWARD to the body), then one gust shoves every player within 140px in front away:
        // velocity (+-9, -3), no damage, stopped by tiles like any player movement. Then a Pike Thrust window, so a
        // target blown into the 120-260px band gets thrust at. Counter: stand behind him, or be airborne
        // (the shove still applies, but you were leaving anyway). Rolling doesn't help: i-frames don't stop a push.
        private const float CinderHugRange = 80f;
        private const float CinderHugReleaseRange = 120f;
        private const int CinderHugTicks = 45;
        private const int CinderWingbeatTellTicks = 24;
        private const int CinderWingbeatRecoveryTicks = 20;
        private const int CinderWingbeatTotalTicks = CinderWingbeatTellTicks + 1 + CinderWingbeatRecoveryTicks;
        private const int CinderWingbeatCooldownTicks = 480;
        private const float CinderGustRange = 140f;
        private const float CinderGustSpeedX = 9f;
        private const float CinderGustSpeedY = -3f;

        private int _hugTicks;
        private int _cinderWingbeatCooldown;

        private void TickCinderWingbeat(int elapsed)
        {
            if (elapsed < CinderWingbeatTellTicks)
            {
                if (!Main.dedServ)
                {
                    // Embers sucked inward from a 120px ring - the "inhale" before the gust.
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(120f, 120f);
                        Vector2 inward = -offset * 0.06f;
                        Dust ember = Dust.NewDustPerfect(NPC.Center + offset, DustID.RedTorch, inward, 0, default, 1.4f);
                        ember.noGravity = true;
                    }

                    if (Main.rand.NextBool(2))
                    {
                        Vector2 accentOffset = Main.rand.NextVector2CircularEdge(100f, 100f);
                        PowerfulCurseBuildup.SpawnAccentDust(NPC.Center + accentOffset, 0, 0, -accentOffset * 0.06f, 1.3f);
                    }
                }
                return;
            }

            if (elapsed != CinderWingbeatTellTicks)
            {
                return;
            }

            // The gust tick. Each peer pushes only its own player: player velocity is owned by that player's client.
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.9f, Pitch = -0.4f }, NPC.Center);
                SoundEngine.PlaySound(SoundID.DD2_BetsyWindAttack with { Volume = 0.8f }, NPC.Center);

                for (int i = 0; i < 40; i++)
                {
                    Vector2 velocity = new Vector2(NPC.direction * Main.rand.NextFloat(4f, 10f), Main.rand.NextFloat(-3f, 1f));
                    Dust gust = Dust.NewDustPerfect(NPC.Center, DustID.RedTorch, velocity, 0, default, Main.rand.NextFloat(1.3f, 2f));
                    gust.noGravity = true;
                }

                Player localPlayer = Main.LocalPlayer;
                float forwardGap = (localPlayer.Center.X - NPC.Center.X) * NPC.direction;
                bool inFront = forwardGap > -8f;
                bool inRange = Vector2.Distance(localPlayer.Center, NPC.Center) <= CinderGustRange;

                if (localPlayer.active && !localPlayer.dead && inFront && inRange)
                {
                    localPlayer.velocity = new Vector2(NPC.direction * CinderGustSpeedX, CinderGustSpeedY);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _thrustFollowUpWindow = ThrustFollowUpWindowTicks;
            }
        }

        // ── Dragon Ascendant (debug label: "Dragon Ascendant") — the 50% phase change ──
        // 90t, hyper-armored, wings held open: spear planted point-down, plum flame spirals inward for 60t, then a
        // roar, screen shake and a burst ring at t=60, held to t=90. From then on a faint ember aura marks phase two.
        private const int DragonAscendantGatherTicks = 60;
        private const int DragonAscendantTotalTicks = 90;

        private void TickDragonAscendant(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (elapsed < DragonAscendantGatherTicks)
            {
                float gather = elapsed / (float)DragonAscendantGatherTicks;
                float radius = MathHelper.Lerp(140f, 40f, gather);
                float spin = elapsed * 0.18f;

                for (int i = 0; i < 3; i++)
                {
                    float angle = spin + MathHelper.TwoPi * i / 3f;
                    Vector2 rim = NPC.Center + angle.ToRotationVector2() * radius;
                    Vector2 inwardSwirl = (angle + MathHelper.PiOver2).ToRotationVector2() * 2f - angle.ToRotationVector2() * 1.5f;
                    PowerfulCurseBuildup.SpawnAccentDust(rim, 0, 0, inwardSwirl, MathHelper.Lerp(1.2f, 2f, gather));
                }

                Lighting.AddLight(NPC.Center, 0.5f * gather, 0.1f * gather, 0.4f * gather);
                return;
            }

            if (elapsed != DragonAscendantGatherTicks)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.9f, Pitch = -0.3f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 4f, 14, distanceFalloff: 900f);

            for (int i = 0; i < 60; i++)
            {
                Vector2 outward = Main.rand.NextVector2CircularEdge(1f, 1f);
                PowerfulCurseBuildup.SpawnAccentDust(NPC.Center, 0, 0, outward * Main.rand.NextFloat(5f, 9f), 2f);
                Dust flame = Dust.NewDustPerfect(NPC.Center, DustID.RedTorch, outward * Main.rand.NextFloat(4f, 8f), 0, default, 1.8f);
                flame.noGravity = true;
            }
        }

        /// <summary>Two tornadoes peeling away to either side. Reuses vanilla's Apprentice storm (the same
        /// projectile the Tome of Infinite Wisdom's right-click summons), so the whole swirling column and
        /// its dust come free — it only needs flipping hostile (HostileVanillaMarker). Vanilla's own recipe is preserved: spawn
        /// anchored 100px above the caster's FEET with a purely horizontal velocity and 1.75x damage. That
        /// ground-anchored, flat trajectory is what makes it read as a tornado rather than a fired shot.</summary>
        private void FireTwinStorm()
        {
            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.7f, Pitch = -0.2f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            _twinStormCastsLeft--;
            NPC.netUpdate = true;

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
                    Main.myPlayer,
                    ai2: tsorcGlobalProjectile.HostileVanillaMarker);

                if (index < 0 || index >= Main.maxProjectiles)
                {
                    continue;
                }

                // hostile/friendly aren't in the projectile sync packet, so flipping them here alone left the storm
                // harmless on every remote client. The ai[2] marker (synced) makes each peer flip it in
                // tsorcGlobalProjectile.PreAI; the local flip just makes the spawn tick correct here too.
                Projectile storm = Main.projectile[index];
                storm.friendly = false;
                storm.hostile = true;
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

            // ── Every peer: cosmetic and sound edges from synced state ──
            // Pierce-through swoop: the dive sound on the tick the (synced) flight mode enters a dive.
            bool nowDiving = Flight != null && Flight.IsDiving;
            if (nowDiving && !_wasDiving && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.5f, PitchVariance = 0.12f }, NPC.Center);
            }
            _wasDiving = nowDiving;

            // Phase two's ember aura: one rising red/plum mote every 4 ticks.
            if (_phaseTwoStarted && !Main.dedServ && Main.GameUpdateCount % 4 == 0)
            {
                Vector2 emberSpot = NPC.Center + Main.rand.NextVector2Circular(14f, 22f);
                if (Main.rand.NextBool(3))
                {
                    PowerfulCurseBuildup.SpawnAccentDust(emberSpot, 0, 0, new Vector2(0f, -1.2f), 1f);
                }
                else
                {
                    Dust ember = Dust.NewDustPerfect(emberSpot, DustID.RedTorch, new Vector2(0f, -1.2f), 0, default, 1f);
                    ember.noGravity = true;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                PostAIRangedTelegraphVfx();
                return;
            }

            // ── Server: timers ──
            if (_poisonCloudCooldown > 0)
            {
                _poisonCloudCooldown--;
            }
            if (_cursedBlinkCooldown > 0)
            {
                _cursedBlinkCooldown--;
            }
            if (_cinderWingbeatCooldown > 0)
            {
                _cinderWingbeatCooldown--;
            }
            if (_thrustFollowUpWindow > 0)
            {
                _thrustFollowUpWindow--;
            }

            // ── Server: set-piece and Twin Storm selection, in priority order, only from Idle ──
            bool hasTarget = NPC.HasValidTarget;
            Player target = Main.player[NPC.target];
            bool grounded = Flight == null || !Flight.IsAirborne;
            float targetDistance = NPC.Distance(target.Center);

            // Hug counter for Cinder Wingbeat: counts while the target is within 80px (any phase), clears once they
            // step beyond 120px, so brief separations mid-scuffle don't reset it.
            if (hasTarget && targetDistance <= CinderHugRange)
            {
                _hugTicks++;
            }
            else if (!hasTarget || targetDistance > CinderHugReleaseRange)
            {
                _hugTicks = 0;
            }

            bool idle = Phase == AttackPhase.Idle;
            bool phaseTwoDue = !_phaseTwoStarted && NPC.life <= NPC.lifeMax * PhaseTwoLifeFraction;

            if (idle && hasTarget && grounded && phaseTwoDue)
            {
                _phaseTwoStarted = true;
                StartSetPiece(DragonSetPiece.DragonAscendant, DragonAscendantTotalTicks);
            }
            else if (idle && hasTarget && grounded && _hugTicks >= CinderHugTicks && _cinderWingbeatCooldown <= 0)
            {
                _hugTicks = 0;
                _cinderWingbeatCooldown = CinderWingbeatCooldownTicks;
                NPC.direction = target.Center.X >= NPC.Center.X ? 1 : -1;
                NPC.spriteDirection = NPC.direction;
                StartSetPiece(DragonSetPiece.CinderWingbeat, CinderWingbeatTotalTicks);
            }
            else if (idle && hasTarget && grounded && _poisonCloudCooldown <= 0
                && targetDistance > PoisonCloudTriggerRange
                && TryFindLandingSpotNearPlayer(target, PoisonCloudLandingOffset, aheadOfRun: true, out Vector2 cloudLanding))
            {
                _teleportDestination = cloudLanding;
                _poisonCloudCooldown = PoisonCloudCooldownTicks;
                StartSetPiece(DragonSetPiece.PoisonCloud, PoisonCloudTotalTicks);
            }
            else if (idle && hasTarget && grounded && _cursedBlinkCooldown <= 0
                && targetDistance >= CursedBlinkMinRange && targetDistance <= CursedBlinkMaxRange
                && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1)
                && Main.rand.NextBool(CursedBlinkChanceDenominator)
                && TryFindLandingSpotNearPlayer(target, CursedBlinkLandingOffset, aheadOfRun: false, out Vector2 blinkLanding))
            {
                _teleportDestination = blinkLanding;
                _cursedBlinkCooldown = CursedBlinkCooldownTicks;
                StartSetPiece(DragonSetPiece.CursedBlink, CursedBlinkSettleTicks);
            }
            else if (idle && _phaseTwoStarted && !TwinStormQueued && Main.rand.Next(TwinStormArmChance) == 0)
            {
                // Armed while idle, NOT at cast time: MagicTelegraphTicks and MagicRecoveryTicks are read when the
                // phase is entered, so the flag must already be set or the first cast gets the short telegraph.
                _twinStormCastsLeft = Main.rand.Next(2, 4); // 2-3 casts back to back
                NPC.netUpdate = true;
            }

            PostAIRangedTelegraphVfx();
        }

        /// <summary>Arcane ball tell: white dust and light building at the spear tip through the ranged telegraph,
        /// with one strong pulse right before the shot. Cosmetic, every non-server peer.</summary>
        private void PostAIRangedTelegraphVfx()
        {
            if (Phase != AttackPhase.RangedTelegraph || IsSecondaryRangedActive || Main.dedServ)
            {
                return;
            }

            Vector2 spearTip = GetSpearTipWorldPosition(92f);
            float t = RangedTelegraphTicks > 0
                ? 1f - (float)PhaseTimer / RangedTelegraphTicks // 0 at telegraph start, 1 at the shot
                : 1f;

            // Steady sparkle for the full telegraph, growing denser as the shot nears.
            int dustCount = 2 + (int)(t * 3f); // 2 -> 5 particles/tick
            for (int i = 0; i < dustCount; i++)
            {
                Dust spark = Dust.NewDustPerfect(
                    spearTip + Main.rand.NextVector2Circular(7f, 7f),
                    DustID.RedTorch,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    0, default,
                    Main.rand.NextFloat(1.2f, 2.0f));
                spark.noGravity = true;
                spark.fadeIn = 0.9f;
            }

            // Strong pulse burst in the final few ticks — the clearest "it's about to fire" cue.
            if (PhaseTimer <= 6)
            {
                for (int i = 0; i < 4; i++)
                {
                    PowerfulCurseBuildup.SpawnAccentDust(spearTip, 0, 0, Main.rand.NextVector2Circular(3.5f, 3.5f), 2.2f);
                }
            }

            Lighting.AddLight(spearTip, MathHelper.Lerp(0.5f, 1.4f, t), MathHelper.Lerp(0.08f, 0.2f, t), MathHelper.Lerp(0.12f, 0.35f, t));
        }

        // ── Multiplayer: Twin Storm chain, phase flag, active set-piece ─────────────
        // _twinStormCastsLeft is rolled server-only (PostAI above), but it's also READ every tick by
        // every peer: MagicTelegraphTicks/MagicRecoveryTicks size the phase a client is predicting,
        // and DoMagicTelegraphVFX decides whether to draw the gathering swirl at all. Unsynced, a
        // non-host client always sees TwinStormQueued as false — no swirl, and the wrong (60t instead
        // of 90t) telegraph length predicted before the server's snapshot corrects it.
        // _phaseTwoStarted gates the ember aura and the unlocks clients predict; _setPiece picks which set-piece
        // timeline DoCustomTick/CustomWeaponRotation/ForceWingsSpread follow on every peer.
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)Math.Clamp(_twinStormCastsLeft, 0, byte.MaxValue));
            writer.Write(_phaseTwoStarted);
            writer.Write((byte)_setPiece);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _twinStormCastsLeft = reader.ReadByte();
            _phaseTwoStarted = reader.ReadBoolean();
            _setPiece = (DragonSetPiece)reader.ReadByte();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DragonCharm>()));
            npcLoot.Add(ItemDropRule.ByCondition(new FirstBossKillRule(), ModContent.ItemType<StaminaVessel>()));
            npcLoot.Add(ItemDropRule.ByCondition(new FirstBossKillRule(), ModContent.ItemType<SublimeBoneDust>()));
        }
    }
}
