using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Projectiles.Melee.Broadswords;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Utilities;

// NOTE: the folder is Gwyn/, but the namespace stays flat (…SuperHardMode) — a namespace segment
// named "Gwyn" would collide with the old Gwyn class still living in this same parent namespace.
namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    ///<summary>
    ///Gwyn, Lord of Cinder — the final boss, rebuilt from near-scratch on the Puppet system (the
    ///Artorias-revamp pattern: a puppet Player wearing the LordGwyn armor set, swinging the Great
    ///Lord Greatsword through the melee combo system).
    ///
    ///PHASE 1 (this file): the foundation. Core stats carried over from the old Gwyn, plus the three
    ///kept systems at their original distances —
    ///  • DEFENSE RING (1000px): beyond it his defense locks to 9999 ("protected by the First Flame");
    ///    inside it he is fightable. The old SwordOfLordGwyn guardian-NPC mechanic is GONE — base
    ///    defense sits at the old post-sword value permanently.
    ///  • COWARD'S RING (2000px): the flame-wall dust boundary; flight is torn down inside it, and
    ///    fleeing beyond it applies Coward's Affliction after a 90-tick grace.
    ///  • RAIN OF DEATH (&gt;600px): running keeps you under a random rain of death orbs.
    ///The old kit lives on in Soul of Cinder; everything else here is built new. The full 12-attack
    ///state machine lands in Phase 2.
    ///</summary>
    [AutoloadBossHead]
    class Gwyn : PuppetNPC
    {
        // PuppetNPC overrides Texture to the shared puppet placeholder, so point the boss-head
        // icon at the existing Gwyn head texture explicitly (same workaround as Artorias).
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Bosses/SuperHardMode/Gwyn_Head_Boss";

        protected override string InvaderTitle => "Gwyn, Lord of Cinder";

        // ── Loadout: the Lord of Cinder's own regalia ────────────────────────────
        protected override int HeadArmorItemType => ModContent.ItemType<LordGwynHelm>();
        protected override int BodyArmorItemType => ModContent.ItemType<LordGwynArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<LordGwynLeggings>();
        protected override float PuppetDrawScale => 1.1f;
        // The composite body sheet supplies the gray sleeve, gold wrist, and brown hand. Its
        // transparent joins still use the synthetic player's skin substrate, so match that to
        // Gwyn's dark-brown authored palette instead of PuppetNPC's bright orange invader default.
        protected override Color PuppetSkinColor => new Color(130, 90, 60);

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemySwordOfGwyn>();
        protected override int RangedWeaponItemType => -1; // melee + bespoke fire/lightning magic
        protected override Vector2 MeleeHandleNorm => new Vector2(0.14f, 0.86f);
        protected override float MeleeWeaponDrawScale => 0.75f;
        protected override float ComboReachBase => 125f;
        protected override float MeleeBladeWidth => 30f;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Greatsword;

        protected override int MeleeDamage => TooEarly ? TooEarlyDamage : 95; // the old contact damage, now via weapon hitboxes
        protected override int RangedDamage => 0;

        // DS1 Gwyn is shockingly fast for his size — relentless pressure, heavier than Artorias' grace.
        // Wrath of Gwyn (below 30% HP) turns the dial up: faster, more aggressive.
        protected override float TopSpeed => _wrathActive ? 3.2f : 2.6f;
        protected override float Acceleration => _wrathActive ? 0.19f : 0.14f;
        protected override int MeleeComboChance => _wrathActive ? 82 : 75;

        // ── Wings (storm-only flight; hidden whenever Gwyn is grounded) ─────────
        // Angel wings for the god of sunlight — traded for flame wings once the Wrath ignites.
        // Autonomous flight is fully disabled: the flight controller only lifts off when the
        // Sunlight Spear Storm commands it (Flight.RequestTakeoff in TickSpearStorm).
        protected override bool HasWings => true;
        protected override int WingsAccessoryItemType => _wrathActive ? ItemID.FlameWings : ItemID.AngelWings;
        protected override bool ShowWingsWhenGrounded => false;
        protected override int RandomTakeoffChance => 0;
        protected override float FlightHeightTrigger => 99999f;
        protected override float FlightHpEscalationFrac => 0f;

        // ── Greatsword reach (bigger than a normal blade) + combat feel ──────────
        protected override float MeleeRange => 110f;
        protected override float StabRange => 180f;
        //480 rather than 340 so the RangedStartOnly approach pool is reachable from the spacing a
        //player actually kites at. MeleeEngageRange is deliberately NOT raised: it is inherited as
        //MeleeRange + 24 = 134, and his real blade reach is ComboReachBase * 0.7 * 1.1 = 96px, so
        //starting standing swings any further out would whiff.
        protected override float ComboMaxStartRange => 480f;

        // ── Aggression ───────────────────────────────────────────────────────────
        // Gwyn is a melee-first boss whose ranged kit exists to punctuate the pressure, not to
        // replace it. Everything here was inherited at a value tuned for ordinary puppets and read
        // as passivity on him. In particular the first two were doing real damage to the fight:
        // ApplyComboTelegraphPressure is a NO-OP at 0, so he never chased a player backing out of a
        // windup; and CasualStroll only triggers beyond StabRange + 40 = 220px, which is exactly the
        // band where he should be closing — and the melee-combo intercept does not run during that
        // phase while his bespoke ranged attacks do, making it a ranged-only window at 0.35x speed.
        protected override float ComboTelegraphAdvanceSpeedMult => 0.85f;
        protected override int   CasualStrollChance             => 0;
        protected override int   ClosingDistanceMaxTicks        => 150;
        protected override float ClosingDistanceSpeedMult       => 1.75f;

        ///<summary>Melee gets most ticks when he is already on top of the player, but no longer
        ///starves the bespoke set-pieces during Wrath. Gwyn's ranged attacks
        ///(Firestorm, Descent, Spear Storm, Gravity of the Sun, ...) only roll while the
        ///phase is Idle, and entering ClosingDistance locks the phase for up to 150 ticks — so
        ///inheriting the in-range chance out here would hand melee most of the out-of-range ticks
        ///and reduce a boss with thirteen ranged set-pieces to a chaser. 50 leaves a real share of
        ///ticks for them while still meaning most gaps end in him arriving.</summary>
        protected override int   RangedStartMeleeComboChance    => 50;
        // Restores the authored 12-44 telegraph spread. At the inherited Max(30, tel * 1.35) every
        // windup below 23 ticks was flattened to the same 30, so his quick swings, pressure strings
        // and heavy punishes all opened identically and there was no rhythm to read.
        protected override int   MinComboTelegraphTicks         => 20;
        protected override float ComboTelegraphMultiplier       => 1.15f;

        ///<summary>SF4's attackRange is its aggro radius, not a preferred fighting distance. The
        ///inherited default is RangedRange (520), which lets a kiting player drift out of the
        ///pursuit FSM's engagement band. 800 keeps a melee-first boss coming.</summary>
        protected override void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.RemembersLastKnownPos = true;

            // The unarmed walk after Wrath Flurry is a slow, spent advance, not a chase.
            if (FlurryRecoveryWalking)
            {
                speedMult *= FlurryRecoveryWalkSpeedMult;
            }

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 4,
                attackRange: 800f);
        }
        // The sword's authored arc now drives the arm, weapon, swept collision, and slash VFX.
        // Alternating the arc would muddy the explicit Backhand and Guillotine telegraphs.
        protected override bool UseSwingEasing => true;
        protected override bool UseAlternateFlip => false;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool UseLogicalMeleeTelegraphs => true;

        // Required for the per-step Ease values in GwynCombos to be read at all: the swing angle
        // falls back to a plain lerp unless this is on. It costs nothing else here — the swing clock
        // it also selects resolves to step.AttackTicks either way, because GetMeleeSwingTicks
        // returns its argument unchanged for any positive tick count and GS() sets no SwingSpeedMult.
        protected override bool UseAuthoredComboSwingClock => true;

        // Riposte's Custom phase is a true raised-blade guard, not an idle recovery with a label.
        // The pose is held locally by the puppet rig for all 60 synchronized guard ticks.
        protected override float? CustomWeaponRotation => _riposteTimer > 0 ? RefinedRaisedPose : null;

        // Holds the finished swing pose for 14 ticks before the arm eases back to the carry angle, so
        // a strike reads as a follow-through instead of drifting home on recovery frame one. Combo
        // recoveries keep the blade drawn throughout (MeleeComboRecovery is always a visible phase);
        // only the bespoke recoveries (JumpSlash, Stab, Tendril...) stop drawing it after the hold.
        // V2 clips ignore this entirely.
        // Artorias runs 30 with a comparable greatsword; the shorter beat suits Gwyn's faster kit.
        protected override int MeleeRecoveryLingerTicks => 14;

        // Wrath Flurry's swipes share endpoints, so the pause between them must HOLD the landed pose
        // rather than drift back toward the outgoing arc (the base behaviour at 0 linger, which
        // re-raises the blade and then snaps it to the next start). After 3 held ticks the pause
        // "eases" toward the next start - which, with shared endpoints and the leaps landing in the
        // lowered pose, is always the pose it is already in, so every flurry pause is a pure hold.
        protected override int MeleeComboInterStepLingerTicks
        {
            get
            {
                if (WrathFlurrySwinging || UnderOverSwinging || ThreeHitSwinging)
                {
                    return 3;
                }
                return 0;
            }
        }

        // The combo phases only. ActiveMeleeComboName is never cleared when a combo ends, so these
        // checks stop stale names from leaking per-combo arcs into later one-shot melee attacks.
        bool ComboSwinging(string name) =>
            ActiveMeleeComboName == name
            && (Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause);
        bool WrathFlurrySwinging => ComboSwinging(WrathFlurryName);
        bool CinderingLeapSwinging => ComboSwinging(CinderingLeapName);
        bool CinderfallSwinging => ComboSwinging(CinderfallName);
        bool UnderOverSwinging => ComboSwinging(UnderOverName);
        bool ThreeHitSwinging => ComboSwinging(ThreeHitName);

        // Wrath Flurry's recovery once the landing beat (MeleeRecoveryLingerTicks) is over: the
        // sword is put away and he walks. PhaseTimer counts DOWN from FlurryFinalRecoveryTicks.
        bool FlurryRecoveryWalking =>
            Phase == AttackPhase.MeleeComboRecovery
            && ActiveMeleeComboName == WrathFlurryName
            && PhaseTimer <= FlurryFinalRecoveryTicks - MeleeRecoveryLingerTicks;

        protected override bool WeaponSheathed => FlurryRecoveryWalking;

        // The flurry's opening raise: FlurryWindupRaiseTicks of the telegraph ease up into the raised
        // pose before the drop to the first swipe's start. The tick count is the same formula as
        // PuppetNPC.GetComboTelegraphTicks, which is private.
        protected override float LogicalWindupSettleFraction
        {
            get
            {
                if (!WrathFlurrySwinging)
                {
                    if (CinderingLeapSwinging)
                    {
                        // Finish the tell in the raised carry pose, then keep that exact frame in air.
                        return 0.35f;
                    }
                    if (UnderOverSwinging)
                    {
                        // Settle raised early, then spend most of the tell lowering into the rising cut.
                        return 0.35f;
                    }
                    return base.LogicalWindupSettleFraction;
                }

                int telegraphTicks = Math.Max(MinComboTelegraphTicks, (int)(FlurryTelegraphTicks * ComboTelegraphMultiplier));
                return FlurryWindupRaiseTicks / (float)telegraphTicks;
            }
        }

        // The flurry's leaps carry the sword overhead through the air and swing when the player is
        // in reach or on the projected landing (Artorias's leap pose). Without this the chop sweeps
        // over the sword's 32-tick useAnimation from takeoff and finishes mid-ascent. Cindering Leap
        // uses the same landing-timed path; Roll-Catch keeps its existing pose and countdown.
        protected override bool UseLandingTimedLeapSlam => WrathFlurrySwinging || CinderingLeapSwinging;

        // Carry the leaps in the raised swipe pose (the cocked frame the underhands finish in), so
        // takeoff continues straight from the previous swipe, and slam down the full 185° swipe arc.
        protected override float LeapSlamCarryRotation
        {
            get
            {
                if (WrathFlurrySwinging || CinderingLeapSwinging)
                {
                    return FlurryRaisedPose;
                }
                return base.LeapSlamCarryRotation;
            }
        }

        protected override float LeapSlamImpactRotation
        {
            get
            {
                if (WrathFlurrySwinging)
                {
                    return FlurryLoweredPose;
                }
                if (CinderingLeapSwinging)
                {
                    return CinderingLeapImpactPose;
                }
                return base.LeapSlamImpactRotation;
            }
        }

        // Wrath Flurry's 185° swipes. The base arcs are 149° overhead (-1.30 - 17° overshoot -> 1.0)
        // and 115° underhand (1.0 -> -1.0). Both now run between the same two poses: blade past
        // vertical behind the head (-1.62), and low-forward, 47° below level (1.61). The extra reach
        // is all at the bottom. 1.61 is past the 1.40 that V2 aim correction clamps to, but the
        // legacy slams already go further (GroundSlam 1.5, the landing-timed impact ~2.1).
        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (WrathFlurrySwinging)
            {
                if (motion == ComboMotion.OverheadArc)
                {
                    startRotation = FlurryRaisedPose;
                    endRotation = FlurryLoweredPose;
                }
                else if (motion == ComboMotion.UnderhandArc)
                {
                    startRotation = FlurryLoweredPose;
                    endRotation = FlurryRaisedPose;
                }
                return;
            }

            if (CinderfallSwinging && motion == ComboMotion.GroundSlam)
            {
                startRotation = RefinedRaisedPose;
                endRotation = CinderfallLoweredPose;
                return;
            }

            if ((UnderOverSwinging || ThreeHitSwinging)
                && (motion == ComboMotion.HorizontalSweep
                    || motion == ComboMotion.OverheadArc
                    || motion == ComboMotion.UnderhandArc))
            {
                bool rising = motion == ComboMotion.UnderhandArc;
                startRotation = rising ? RefinedLoweredPose : RefinedRaisedPose;
                endRotation = rising ? RefinedRaisedPose : RefinedLoweredPose;
            }
        }

        // The repaired armor sheet now supplies complete front and back composite-arm frames,
        // allowing the greatsword pose to retain smooth rotation without exposing player skin.
        protected override bool UseCompositeArmSwing => true;
        protected override bool UseTwoHandedCompositeSwing => !IsDashGrabSequence;
        protected override bool UseCompositeArmForAdditionalPhase => IsDashGrabSequence;
        protected override bool HasUnblockableBodyAura =>
            Phase == AttackPhase.TendrilTelegraph || Phase == AttackPhase.PierceDash;
        protected override float UnblockableBodyAuraScale => 1.1f;
        protected override float UnblockableBodyAuraOpacity => 0.3f;
        // Gwyn never disengages to drink Estus; pressure and authored recoveries remain his only
        // neutral breaks. Zero charges keeps the shared flee/heal intercept permanently disabled.
        protected override int EstusChargesMax => 0;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool HasSlashVFX => false; // Gwyn spawns the tracked four-frame VanillaSwordArc directly.
        protected override float WalkAnimationSpeedMultiplier => 0.35f;
        protected override float OverheadWindupOvershoot => MathHelper.ToRadians(17f);
        protected override bool SlowDownBeforeMelee => false; // pursue through the windup — no walking out of the telegraph

        // ── The greatsword moveset (bespoke, reactive) ───────────────────────────
        // Fully reactive: ReactiveComboIndex reads the player's live dodge/launch/flank state each
        // time a combo starts and picks the counter, falling back to the weighted roll otherwise.
        // Flash colors are the Lord of Cinder's fire — orange bread & butter, red heavy commits.
        const int CB_CLEAVE = 0, CB_UNDEROVER = 1, CB_LEAP = 2, CB_SLIDE = 3, CB_SPIN = 4,
                  CB_CINDERFALL = 5, CB_GUILLOTINE = 6, CB_BACKHAND = 7, CB_THREEHIT = 8,
                  CB_ROLLCATCH = 9, CB_FLURRY = 10, CB_PURSUIT = 11, CB_JUDGMENT = 12,
                  CB_RIPOSTE = 13;

        // `ease` shapes the arc's velocity curve. It defaults to Smooth to match the shared S()
        // helper in MeleeComboSystem: leaving it off produced SwingEaseStyle.Linear (enum value 0),
        // i.e. constant angular velocity with no ease-in or ease-out, which is what made every one
        // of Gwyn's swings start and stop dead rather than wind up and settle.
        static MeleeComboStep GS(ComboMotion m, int tel, int atk, int pause, float dmg = 1f, float reach = 1f, float push = 0f,
                                 SwingEaseStyle ease = SwingEaseStyle.Smooth)
            => new MeleeComboStep { Motion = m, TelegraphTicks = tel, AttackTicks = atk, PostStepPause = pause, DamageMult = dmg, ReachMult = reach, ForwardPushMult = push, Ease = ease };

        const string UnderOverName = "Under-Over";
        const string CinderingLeapName = "Cindering Leap";
        const string CinderfallName = "Cinderfall";
        const string ThreeHitName = "3-Hit";
        const string JudgmentGuillotineName = "Judgment Guillotine";
        const string RiposteCounterName = "Riposte Counter";

        // Default sword-language poses: a safe raised start and an end close to the player's own
        // straight-down broadsword finish. Their 3.91-rad (224-degree) envelope leaves about 180
        // degrees live once a Weighted swing is disarmed below 30% of peak speed.
        const float RefinedRaisedPose = -1.62f;
        const float RefinedLoweredPose = 2.29f;
        // Cinderfall is the signature heavy: 233 degrees of envelope / about 187 degrees live.
        const float CinderfallLoweredPose = 2.45f;
        // A landing-timed LeapSlam is armed across its whole ten-tick downswing, so this endpoint is
        // 185 degrees from the carried pose instead of using the wider Weighted-swing envelope.
        const float CinderingLeapImpactPose = 1.61f;
        const float RefinedArmedSpeedShare = 0.30f;

        /// <summary>Authored sword swing with cubic acceleration, an exponential harmless settle,
        /// and a hit window that closes once blade speed drops below 30% of peak.</summary>
        static MeleeComboStep WeightedSwordSwing(
            ComboMotion motion, int telegraph, int easeInTicks, int easeOutTicks, float decay,
            int pause, float damage, float reach, float push)
        {
            int attackTicks = easeInTicks + easeOutTicks;
            MeleeComboStep step = GS(motion, telegraph, attackTicks, pause, damage, reach, push,
                SwingEaseStyle.Weighted);
            step.EaseInTicks = easeInTicks;
            step.EaseOutTicks = easeOutTicks;
            step.EaseOutDecay = decay;
            float armedSettleTicks = easeOutTicks
                * (float)Math.Log(1f / RefinedArmedSpeedShare) / decay;
            step.HitWindowEnd = (easeInTicks + armedSettleTicks) / attackTicks;
            return step;
        }

        static MeleeComboStep CinderingLeapStep()
        {
            // Ninety ticks is only the airborne safety timeout. The landing or in-range trigger
            // starts a ten-tick, fully armed 185-degree downswing from the held raised pose.
            MeleeComboStep step = GS(ComboMotion.LeapSlam, 25, 90, 0, 1.5f, 1.45f);
            step.LeapStrikeRange = FlurryLeapStrikeRange;
            return step;
        }

        // ── Wrath Flurry tuning ─────────────────────────────────────────────────
        const string WrathFlurryName = "Wrath Flurry";
        // The two poses every swipe runs between (185° apart): blade past vertical behind the head,
        // and low-forward 47° below level. Also the leaps' carry and impact. See ModifyMeleeArcEndpoints.
        const float FlurryRaisedPose = -1.62f;
        const float FlurryLoweredPose = 1.61f;
        // Authored telegraph; x1.15 ComboTelegraphMultiplier = 44 ticks on screen. The first 25 are a
        // slow ease up into the raised pose (LogicalWindupSettleFraction), the rest drop to the start.
        const int FlurryTelegraphTicks = 39;
        const int FlurryWindupRaiseTicks = 25;
        // Each swipe: speed builds for 10 ticks, peaks, then decays exponentially for 45 onto the
        // next swipe's start. Decay 7 puts the 185° arc's peak near 19°/tick; the settle is down to
        // 21% of that 10 ticks later and near-still for its last half.
        const int FlurrySwipeEaseIn = 10;
        const int FlurrySwipeEaseOut = 45;
        const float FlurrySwipeDecay = 7f;
        const int FlurrySwipePause = 10;    // held pose; re-faces the player between swipes
        // The blade is live while it moves at >= this share of its top speed; slower is follow-through.
        const float FlurryArmedSpeedShare = 0.3f;
        // Gap-closer: after this many whiffs in a row, with the player past MeleeEngageRange, the
        // next overhand becomes a leap slam, followed by a 12-tick landing beat.
        const int FlurryLeapAfterMisses = 2;
        const int FlurryLeapPause = 12;
        // Both leaps swing in the air once past the apex with the player this close (centre to
        // centre): MeleeRange 110 plus the ~40px he still travels during the 10-tick downswing.
        const float FlurryLeapStrikeRange = 150f;
        // The finale's punish window: three seconds with no attack. The first MeleeRecoveryLingerTicks
        // are the planted landing beat, then the sword is put away and he walks forward at this
        // fraction of TopSpeed.
        const int FlurryFinalRecoveryTicks = 180;
        const float FlurryRecoveryWalkSpeedMult = 0.35f;

        /// <summary>One Wrath Flurry swipe on the Weighted ease. Armed through the ease-in and the
        /// part of the exponential settle still above FlurryArmedSpeedShare of top speed - the
        /// speed is v*e^(-decay*p), so that lasts ln(1/share)/decay of the ease-out.</summary>
        static MeleeComboStep FlurrySwipe(ComboMotion motion, int telegraph, float dmg, float reach, float push)
        {
            int attackTicks = FlurrySwipeEaseIn + FlurrySwipeEaseOut;
            MeleeComboStep step = GS(motion, telegraph, attackTicks, FlurrySwipePause, dmg, reach, push, SwingEaseStyle.Weighted);
            step.EaseInTicks = FlurrySwipeEaseIn;
            step.EaseOutTicks = FlurrySwipeEaseOut;
            step.EaseOutDecay = FlurrySwipeDecay;

            float armedSettleTicks = FlurrySwipeEaseOut * (float)Math.Log(1f / FlurryArmedSpeedShare) / FlurrySwipeDecay;
            step.HitWindowEnd = (FlurrySwipeEaseIn + armedSettleTicks) / attackTicks;
            return step;
        }

        /// <summary>A Wrath Flurry leap slam: carried in the raised swipe pose (LeapSlamCarryRotation),
        /// swung in the air when the player is in reach, else on the landing. 90 ticks is only the
        /// timeout, as for every authored LeapSlam; the step really ends on landing.</summary>
        static MeleeComboStep FlurryLeap(int pause, float dmg)
        {
            MeleeComboStep step = GS(ComboMotion.LeapSlam, 0, 90, pause, dmg, 1.2f);
            step.LeapStrikeRange = FlurryLeapStrikeRange;
            return step;
        }

        // Complete grounded strikes use the single-clock runtime. Movement attacks and linked
        // strings stay on MeleeCombo because their locomotion and continuation are the behavior.
        // Windup/recovery across the four clips is deliberately spread 20/26/34/44 and 12/16/40/48.
        // They were all 30-ish windup and 16-30 recovery, which made a "quick re-engaging sweep" and
        // a "respect me" heavy punish open and end at the same speed — the moveset had no rhythm to
        // read regardless of how different the arcs looked.
        static readonly PuppetAttackClip CleaveV2 = new PuppetAttackClip(
            name: "Cleave",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 26,
            activeTicks: 24,
            recoveryTicks: 16,
            oppositeWindupRotation: 0.85f,
            attackStartRotation: -0.75f,
            attackEndRotation: 0.85f,
            hitWindowStart: 0.12f,
            hitWindowEnd: 0.82f,
            swingEase: SwingEaseStyle.Snap,
            maxAimCorrection: 0.30f,
            aimLockTicksBeforeActive: 14);

        static readonly PuppetAttackClip GuillotineV2 = new PuppetAttackClip(
            name: "Guillotine",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 44,
            activeTicks: 30,
            recoveryTicks: 48,
            oppositeWindupRotation: 1.10f,
            attackStartRotation: -1.75f,
            attackEndRotation: 1.15f,
            hitWindowStart: 0.22f,
            hitWindowEnd: 0.82f,
            swingEase: SwingEaseStyle.Whip,
            maxAimCorrection: 0.22f,
            aimLockTicksBeforeActive: 20);

        // Judgment Guillotine timing sheet
        // Poses -1.62 -> 2.29 rad: 224° envelope; the 0.22-0.82 armed interval covers about
        // 180° of the Whip-shaped cut. Tell 60t on screen: 40t of eased sword movement, then a
        // literal 20t hold at the attack-start pose. The active swing re-accelerates from rest and
        // remains damaging for 19t, inside the player's 22t base roll. Its final ~6t are harmless
        // follow-through, then the 48t recovery is the punish window. This dedicated clip is only
        // eligible after Judgment's marked teleport; ordinary Guillotine keeps its shorter tell.
        static readonly PuppetAttackClip JudgmentGuillotineV2 = new PuppetAttackClip(
            name: JudgmentGuillotineName,
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 60,
            activeTicks: 32,
            recoveryTicks: 48,
            oppositeWindupRotation: 0.45f,
            attackStartRotation: RefinedRaisedPose,
            attackEndRotation: RefinedLoweredPose,
            hitWindowStart: 0.22f,
            hitWindowEnd: 0.82f,
            swingEase: SwingEaseStyle.Whip,
            maxAimCorrection: 0f,
            aimLockTicksBeforeActive: 20,
            windupHoldTicks: 20);

        // Riposte Counter timing sheet
        // The 60t guard has already raised the sword. On a successful melee bait, the counter holds
        // that pose for 18t while the clang lands, then cuts from -1.62 to 2.29 rad. Its 0.15-0.78
        // live interval is 15 of the 24 swing ticks, comfortably inside the player's 22t roll.
        // The 40t recovery is the reward for baiting or rolling the counter instead of trading.
        static readonly PuppetAttackClip RiposteCounterV2 = new PuppetAttackClip(
            name: RiposteCounterName,
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 18,
            activeTicks: 24,
            recoveryTicks: 40,
            oppositeWindupRotation: RefinedRaisedPose,
            attackStartRotation: RefinedRaisedPose,
            attackEndRotation: RefinedLoweredPose,
            hitWindowStart: 0.15f,
            hitWindowEnd: 0.78f,
            swingEase: SwingEaseStyle.Whip,
            maxAimCorrection: 0f,
            aimLockTicksBeforeActive: 18,
            windupStartRotation: RefinedRaisedPose);

        static readonly PuppetAttackClip BackhandV2 = new PuppetAttackClip(
            name: "Backhand Step",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 20,
            activeTicks: 18,
            recoveryTicks: 12,
            oppositeWindupRotation: -0.65f,
            attackStartRotation: 0.80f,
            attackEndRotation: -0.65f,
            hitWindowStart: 0.12f,
            hitWindowEnd: 0.78f,
            swingEase: SwingEaseStyle.Snap,
            maxAimCorrection: 0.30f,
            aimLockTicksBeforeActive: 10);

        // TWO POOLS, split by RangedStartOnly. This is the single most important thing about this
        // table. A combo's Preferred band is only a x2.0 / x0.4 weight, and a combo can only START
        // within MeleeEngageRange (MeleeRange + 24 = 134) — which is below closeMax (MeleeRange + 30
        // = 140). So for a puppet with everything in one pool the band is ALWAYS Close, and every
        // Mid-preferred gap-closer eats the 0.4x penalty on every roll forever. Cindering Leap,
        // Sliding Thrust, Cinderfall and Roll-Catch shared 8.3% of picks between them; Gwyn was
        // effectively six standing swings.
        //
        // RangedStartOnly combos are drawn from a SEPARATE call made only when he is out of reach,
        // so approach attacks are picked in the situation they were designed for and the in-your-
        // face pool is all Close-preferred. Cinderfall moved to Close because it never travelled —
        // it is a stationary ground slam and its Mid tag was simply wrong.
        static readonly MeleeCombo[] GwynCombos = new[]
        {
            // 0 — Cleave: the bread-and-butter wide swing
            new MeleeCombo { Name = "Cleave", BaseWeight = 100, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Orange, CooldownAfterUse = 40, RuntimeV2Clip = CleaveV2,
                Steps = new[] { GS(ComboMotion.HorizontalSweep, 15, 18, 0, 1.0f, 1.1f, 0.55f) } },
            // 1 — Under-Over timing sheet
            // Poses 2.29 -> -1.62 -> 2.29 rad: 224° envelope / about 180° live per cut.
            // Tell 40t on screen. Strike 10t cubic acceleration + 45t k7 decay: peak ~23°/t,
            // live 18t. The harmless 37t tail + 10t held/re-facing pause leaves 47t between
            // live windows, so both hits are independently rollable. Recovery adds a 30t opening.
            new MeleeCombo { Name = UnderOverName, BaseWeight = 70, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Gold, CooldownAfterUse = 130, RecoveryTicks = 30,
                Steps = new[] {
                    WeightedSwordSwing(ComboMotion.UnderhandArc, 35, 10, 45, 7f, 10, 1.0f, 1.45f, 0.65f),
                    WeightedSwordSwing(ComboMotion.OverheadArc,   0, 10, 45, 7f,  0, 1.3f, 1.50f, 0.85f),
                } },
            // 2 — Cindering Leap timing sheet
            // Pose -1.62 -> 1.61 rad: 185° fully armed downswing. The 28t opening tell finishes
            // raised; that exact frame is carried through flight. The blade only moves after the
            // apex inside 150px or during the projected last 10t before ground contact. Recovery 30t.
            new MeleeCombo { Name = CinderingLeapName, BaseWeight = 55, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.OrangeRed, CooldownAfterUse = 150, RecoveryTicks = 30,
                HeavyCommit = true, RangedStartOnly = true,
                Steps = new[] { CinderingLeapStep() } },
            // 3 — Sliding Thrust: low dash pierce, gap-closer
            new MeleeCombo { Name = "Sliding Thrust", BaseWeight = 55, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 130, RecoveryTicks = 22,
                RangedStartOnly = true,
                // Snap: the lunge commits early and coasts, so the thrust lands with the dash.
                Steps = new[] { GS(ComboMotion.JoustDash, 20, 16, 0, 1.2f, 1.4f, 1.8f, ease: SwingEaseStyle.Snap) } },
            // 4 — Sunspin: 360° sweep, anti-flank
            new MeleeCombo { Name = "Sunspin", BaseWeight = 45, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 170, RecoveryTicks = 34,
                HeavyCommit = true,
                Steps = new[] { GS(ComboMotion.Spin, 20, 26, 0, 1.1f, 1.15f, 0.6f) } },
            // 5 — Cinderfall timing sheet
            // Poses -1.62 -> 2.45 rad: 233° envelope / about 187° live. The raised-sword tell is
            // 44t on screen with a converging ember charge. Strike 8t cubic acceleration + 45t k8
            // decay: peak ~28°/t, live 15t. Its 38t harmless tail + 48t recovery is the punish window.
            // Legacy MeleeCombo is deliberate: V2 clips cannot run the Weighted curve.
            new MeleeCombo { Name = CinderfallName, BaseWeight = 40, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red, CooldownAfterUse = 200, RecoveryTicks = 48, HeavyCommit = true,
                Steps = new[] {
                    WeightedSwordSwing(ComboMotion.GroundSlam, 39, 8, 45, 8f, 0, 1.6f, 1.50f, 0.45f),
                } },
            // 6 — Guillotine Drop: heavy standing overhead, the "respect me" punish
            new MeleeCombo { Name = "Guillotine", BaseWeight = 45, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red, CooldownAfterUse = 200, HeavyCommit = true, RuntimeV2Clip = GuillotineV2,
                // Whip: the 30-tick tell plus a held apex is what makes this the "respect me" punish.
                // Played by GuillotineV2's swingEase; this step's Ease is unused while the clip is set.
                Steps = new[] { GS(ComboMotion.OverheadArc, 30, 22, 0, 1.8f, 1.15f, 0.35f, ease: SwingEaseStyle.Whip) } },
            // 7 — Backhand + Step: quick re-engaging sweep, denies a roll-back
            new MeleeCombo { Name = "Backhand Step", BaseWeight = 60, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Orange, CooldownAfterUse = 70, RuntimeV2Clip = BackhandV2,
                // Snap: front-loaded so the re-engage is out and back before a roll can answer it.
                // Played by BackhandV2's swingEase; this step's Ease is unused while the clip is set.
                Steps = new[] { GS(ComboMotion.HorizontalSweep, 12, 16, 0, 0.9f, 1.1f, 0.85f, ease: SwingEaseStyle.Snap) } },
            // 8 — 3-Hit timing sheet
            // Three alternating 224° envelopes share endpoints, yielding about 180° live each with
            // no re-raise snap. Tell 32t; each strike is 9t cubic acceleration + 36t k6.5 decay,
            // peak ~26°/t and live 16t. Tail 29t + pause 10t = 39t between live windows. Each pause
            // re-faces; forward pressure rises per cut and the next cut surges if the target left reach.
            new MeleeCombo { Name = ThreeHitName, BaseWeight = 55, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed, CooldownAfterUse = 180, RecoveryTicks = 34,
                Steps = new[] {
                    WeightedSwordSwing(ComboMotion.HorizontalSweep, 28, 9, 36, 6.5f, 10, 0.9f, 1.45f, 0.75f),
                    WeightedSwordSwing(ComboMotion.UnderhandArc,     0, 9, 36, 6.5f, 10, 0.9f, 1.45f, 0.95f),
                    WeightedSwordSwing(ComboMotion.OverheadArc,      0, 9, 36, 6.5f,  0, 1.3f, 1.50f, 1.15f),
                } },
            // 9 — Roll-Catch: leap in, then the flip slam lands where a panicked roll ends
            new MeleeCombo { Name = "Roll-Catch", BaseWeight = 30, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Red, CooldownAfterUse = 240, RecoveryTicks = 40,
                HeavyCommit = true, RangedStartOnly = true,
                Steps = new[] {
                    // The leap travels Smooth; the slam that catches the roll is the delayed payoff.
                    GS(ComboMotion.LeapSlam,   25, 22, 14, 1.3f, 1.2f),
                    GS(ComboMotion.GroundSlam,  0, 24,  0, 1.5f, 1.3f, 0.35f, ease: SwingEaseStyle.Whip),
                } },
            // 10 — Wrath Flurry: the full-commit chain unlocked at half health. Seven player-style
            //      swipes, rising and falling in turn, each its own hit. The arcs are widened to 185°
            //      and share endpoints (see ModifyMeleeArcEndpoints), so every swipe begins exactly
            //      where the last one ended: no re-raise between hits. Facing is locked only while a
            //      swipe is live; the 2-tick pause re-faces the player, so a roll through him costs
            //      the current swipe and then the next one comes from the other side. Two whiffs in
            //      a row against a retreating player turn the next overhand into a leap slam
            //      (ModifyNextMeleeComboStep). It ends on a leap slam from the cocked pose the last
            //      underhand finishes in, then a long planted recovery - the punish window.
            new MeleeCombo { Name = WrathFlurryName, BaseWeight = 25, Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.Red, CooldownAfterUse = 300, RecoveryTicks = FlurryFinalRecoveryTicks,
                HeavyCommit = true, HyperArmor = true,
                Steps = new[] {
                    // 55 ticks each (10 building speed, 45 decaying onto the next start) + a 10-tick
                    // hold. Reach stays under 1.15 so only the leaps throw a crescent.
                    FlurrySwipe(ComboMotion.UnderhandArc, FlurryTelegraphTicks, 0.8f, 1.1f, 0.45f),
                    FlurrySwipe(ComboMotion.OverheadArc,  0, 0.8f, 1.1f, 0.45f),
                    FlurrySwipe(ComboMotion.UnderhandArc, 0, 0.8f, 1.1f, 0.45f),
                    FlurrySwipe(ComboMotion.OverheadArc,  0, 0.8f, 1.1f, 0.45f),
                    FlurrySwipe(ComboMotion.UnderhandArc, 0, 0.8f, 1.1f, 0.45f),
                    FlurrySwipe(ComboMotion.OverheadArc,  0, 0.8f, 1.1f, 0.45f),
                    FlurrySwipe(ComboMotion.UnderhandArc, 0, 0.8f, 1.1f, 0.45f),
                    // Finale: the underhand above ends cocked behind his head, which is exactly the
                    // leap's carry pose, so he launches straight out of the 10-tick hold.
                    FlurryLeap(0, 1.3f),
                } },
            // 11 — Sunlight Pursuit: the long-range answer, and the only attack that crosses a
            //      whole arena. He drops the greatsword low and BEHIND him and sprints — the run
            //      itself is the telegraph, held for as long as the chase takes — then tears it up
            //      through the player on arrival. LowAxeRun ends the moment he is inside
            //      MeleeRange * 1.05, so the run is exactly as long as the gap requires.
            //      Ported from StuddedLeatherWarrior's "Rising Pursuit"; both motions were already
            //      weapon-agnostic, so this is a data entry plus the VFX guards in the tick hooks.
            new MeleeCombo { Name = "Sunlight Pursuit", BaseWeight = 55, Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.Orange, CooldownAfterUse = 300, RecoveryTicks = 34,
                HeavyCommit = true, HyperArmor = true, RangedStartOnly = true,
                Steps = new[] {
                    // The run is a carried pose, not a swing, so it stays Linear — easing a 140-tick
                    // sprint would make the blade drift instead of holding low and steady.
                    GS(ComboMotion.LowAxeRun,          32, 140, 1, 0f, ease: SwingEaseStyle.Linear),
                    // Safety timeout only; the step normally ends 30 ticks after the leap apex.
                    GS(ComboMotion.RisingUppercutLeap,  0, 150, 0, 1.35f, 1.20f, ease: SwingEaseStyle.Whip),
                } },
            // 12 — Judgment Guillotine: internal-only follow-up for Judgment from Behind. Its
            // CanSelect gate is closed unless the marked teleport has completed, so it never enters
            // Gwyn's ordinary weighted bag. A real positive weight lets ReactiveComboIndex select it.
            new MeleeCombo { Name = JudgmentGuillotineName, BaseWeight = 1, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Gold, CooldownAfterUse = 0, RecoveryTicks = 48,
                HeavyCommit = true, HyperArmor = true, RuntimeV2Clip = JudgmentGuillotineV2,
                Steps = new[] { GS(ComboMotion.OverheadArc, 60, 32, 0, 1.8f, 1.50f, 0.35f,
                    ease: SwingEaseStyle.Whip) } },
            // 13 — Riposte Counter: opened only after a melee strike commits into the active guard.
            // Reach stays at 1.1 so this is a sword punish, not the old disconnected fire crescent.
            new MeleeCombo { Name = RiposteCounterName, BaseWeight = 1, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Gold, CooldownAfterUse = 0, RecoveryTicks = 40,
                HeavyCommit = true, HyperArmor = true, RuntimeV2Clip = RiposteCounterV2,
                Steps = new[] { GS(ComboMotion.OverheadArc, 18, 24, 0, 1.55f, 1.10f, 0.30f,
                    ease: SwingEaseStyle.Whip) } },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => GwynCombos;

        ///<summary>Fully reactive combo pick: read the player's live state and answer it. Falls
        ///through to the weighted roll (returns -1) when no specific read applies, or when the
        ///counter it wants is on cooldown.</summary>
        protected override int ReactiveComboIndex(float dist, ComboRangeBand band, int[] ready)
        {
            Player player = Main.player[NPC.target];
            if (player == null || !player.active || player.dead)
            {
                return -1;
            }
            // A live guard has just caught a melee strike. Consume its internal-only counter before
            // reading the ordinary movement reactions, so a roll-state cannot replace the promised punish.
            if (_riposteCounterPending)
            {
                if (Ready(ready, CB_RIPOSTE)) { _riposteCounterPending = false; return CB_RIPOSTE; }
                if (Ready(ready, CB_GUILLOTINE)) { _riposteCounterPending = false; return CB_GUILLOTINE; }
            }
            //Judgment from Behind: the teleport just planted him at the player's back — the queued
            //punish is the dedicated 60-tick heavy overhead the moment a combo can start.
            if (_judgmentPending > 0)
            {
                if (Ready(ready, CB_JUDGMENT)) { _judgmentPending = 0; return CB_JUDGMENT; }
                // Defensive fallback if a future pool edit makes the internal entry unavailable.
                if (Ready(ready, CB_GUILLOTINE)) { _judgmentPending = 0; return CB_GUILLOTINE; }
                if (Ready(ready, CB_UNDEROVER)) { _judgmentPending = 0; return CB_UNDEROVER; }
            }
            //Gravity of the Sun just reeled them in — greet them with the pressure string
            if (_pullComboNudge > 0)
            {
                if (Ready(ready, CB_THREEHIT)) { _pullComboNudge = 0; return CB_THREEHIT; }
                if (Ready(ready, CB_CLEAVE)) { _pullComboNudge = 0; return CB_CLEAVE; }
            }
            bool rolling = player.GetModPlayer<tsorcRevampPlayer>().isDodging;
            bool launched = player.velocity.Y < -3f && player.Center.Y < NPC.Center.Y - 24f;
            float awaySign = Math.Sign(player.Center.X - NPC.Center.X);      // side the player is on
            bool rollingAway = rolling && Math.Sign(player.velocity.X) == awaySign && Math.Abs(player.velocity.X) > 2f;
            bool rollingThrough = rolling && dist < MeleeRange + 20f;         // dodging through him at point-blank

            // Every branch below has to offer an answer from BOTH pools, because this runs for the
            // approach pool as well as the close one and `ready` is already filtered to whichever is
            // live. Naming a combo from the other pool just falls through to the weighted roll, so a
            // read with only one answer silently stops being reactive at half its distances — which
            // is what the old `dist > StabRange` roll-catch branch did: it could never fire, since
            // combos only ever started at 134px back when nothing was RangedStartOnly.
            if (launched)
            {
                // Popped into the air: catch them with the tracking leap, or juggle if already close.
                if (Ready(ready, CB_LEAP)) { return CB_LEAP; }
                if (Ready(ready, CB_UNDEROVER)) { return CB_UNDEROVER; }
            }
            // Player rolled through/behind at close range → spin covers every side
            if (rollingThrough && Ready(ready, CB_SPIN))
            {
                return CB_SPIN;
            }
            // Player rolling away → chase. Answer scales with the gap they just opened: the
            // full-arena run from far, the roll-catch leap at mid, a slide or the re-engaging
            // backhand when they only bought themselves a few pixels.
            if (rollingAway)
            {
                if (dist > StabRange + 90f && Ready(ready, CB_PURSUIT))
                {
                    return CB_PURSUIT;
                }
                if (dist > StabRange && Ready(ready, CB_ROLLCATCH))
                {
                    return CB_ROLLCATCH;
                }
                if (Ready(ready, CB_SLIDE))
                {
                    return CB_SLIDE;
                }
                if (Ready(ready, CB_BACKHAND))
                {
                    return CB_BACKHAND;
                }
            }
            return -1; // no live read — let the weighted roll pick a standard swing
        }

        static bool Ready(int[] ready, int idx) => idx >= 0 && idx < ready.Length && ready[idx] > 0;

        ///<summary>Hard reach gate for the approach pool. Preferred bands are only a x2.0 / x0.4
        ///weight, so a gap-closer stays selectable well past the distance it can physically cross —
        ///and raising ComboMaxStartRange to 480 put every one of them in that position:
        ///
        ///  Cindering Leap / Roll-Catch  LeapAttackUpSpeed 9.5 gives 2*9.5/0.3 = 63 ticks aloft, and
        ///                               horizontal speed is capped at TopSpeed * 1.7 = 4.42 px/t,
        ///                               so the arc tops out near 280px however far the target is.
        ///  Sliding Thrust               ForwardPushMult 1.8 over 16 attack ticks is only ~75px of
        ///                               dash, plus ~50px of telegraph pressure.
        ///
        ///Past those numbers the move lands short and the "gap closer" becomes a whiff in front of
        ///the player. Sunlight Pursuit is deliberately ungated: its run step ends the moment he is
        ///inside MeleeRange * 1.05, so it crosses whatever the gap actually is.</summary>
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            // This entry exists so the V2 network snapshot can resolve a dedicated clip by pool
            // index. It is never part of normal selection; Judgment's teleport opens the gate.
            if (combo.Name == JudgmentGuillotineName)
            {
                return _judgmentPending > 0;
            }
            if (combo.Name == RiposteCounterName)
            {
                return _riposteCounterPending;
            }
            //This was described as the enrage chain, but it was selectable for the entire fight.
            //Keep one unmistakable melee reveal for the second half of the fight.
            if (combo.Name == WrathFlurryName)
            {
                return HalfHealthMovesUnlocked;
            }
            if (combo.Name == CinderingLeapName || combo.Name == "Roll-Catch")
            {
                return distance <= 300f;
            }
            if (combo.Name == "Sliding Thrust")
            {
                return distance <= 260f;
            }
            return true;
        }

        // Consecutive Wrath Flurry swipes that connected with nobody. Feeds the gap-close leap below.
        int _flurryMissStreak;

        ///<summary>Counts Wrath Flurry whiffs. Runs at the end of every step, before the pause; the
        ///flurry always continues - this only records whether the swipe that just ended connected.</summary>
        protected override bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == WrathFlurryName)
            {
                // nextStepIndex 1 = the first swipe just ended, so this is a fresh flurry.
                if (nextStepIndex == 1)
                {
                    _flurryMissStreak = 0;
                }

                if (previousStepHit)
                {
                    _flurryMissStreak = 0;
                }
                else
                {
                    _flurryMissStreak++;
                }
            }
            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        ///<summary>Wrath Flurry gap-closer. After FlurryLeapAfterMisses whiffs in a row, with the player
        ///past MeleeEngageRange (where no swipe can start a combo either), the next OVERHAND becomes a
        ///leap slam at them. Only overhands are swapped: the leap is an overhead - carried high, slammed
        ///down - so the under/over rhythm continues, and it can never come more than every other swipe.
        ///A LeapSlam tops out near 280px of travel, so from further it closes most of the gap.</summary>
        protected override void ModifyNextMeleeComboStep(
            string comboName, int nextStepIndex, Player target, ref MeleeComboStep nextStep)
        {
            if (comboName == ThreeHitName)
            {
                float nextReach = ComboReachBase * 0.7f * nextStep.ReachMult;
                if (NPC.Distance(target.Center) > nextReach)
                {
                    // About 170px over the 45t step at base TopSpeed, enough to stay attached to a
                    // retreat without tracking through a player who rolled behind the committed cut.
                    nextStep.ForwardPushMult = Math.Max(nextStep.ForwardPushMult, 1.45f);
                }
                return;
            }

            if (comboName != WrathFlurryName || nextStep.Motion != ComboMotion.OverheadArc)
            {
                return;
            }

            bool enoughWhiffs = _flurryMissStreak >= FlurryLeapAfterMisses;
            bool outOfReach = NPC.Distance(target.Center) > MeleeEngageRange;
            if (!enoughWhiffs || !outOfReach)
            {
                return;
            }

            // Reach 1.2 throws the fire crescent on impact (OnComboStepCompleted).
            nextStep = FlurryLeap(FlurryLeapPause, 1.0f);
            _flurryMissStreak = 0;
        }

        // ── Kept systems (original distances) ────────────────────────────────────
        const float DefenseRingRadius = 1000f; // beyond this: defense 9999
        const int FightableDefense = 130;      // the old post-sword value; the sword mechanic is removed
        const float CowardRingRadius = 2000f;  // beyond this: Coward's Affliction (after grace)
        const float CompressedCowardRingRadius = CowardRingRadius * 0.5f;
        const int CowardRingShrinkTicks = 180;
        const int CowardRingHoldTicks = 16 * 60;
        const int CowardRingExpandTicks = 180;
        const int CowardRingCompressionTotalTicks = CowardRingShrinkTicks
            + CowardRingHoldTicks + CowardRingExpandTicks;
        // ── Rain of Death: anchored to the ARENA, not to Gwyn ────────────────────
        // The trigger used to be NPC.Distance(player), which meant the hazard switched on and off
        // as Gwyn's own pathing drifted toward or away from a stationary player — the player could
        // not learn where it was safe to stand, because "safe" moved with the boss. Capturing the
        // spawn point once and measuring from there gives a fixed, learnable arena boundary, the
        // same contract Artorias uses for its ring (see Artorias.OnSpawn / _ringCenter).
        const float RainOfDeathRange = 600f;   // distance from the ARENA CENTRE, not from Gwyn
        const int BaseRainOfDeathDamage = 77;  // the old herosArrowDamage
        //One orb every 12 ticks = 10 per 2 seconds; every 8 ticks (15 per 2s) once Wrath ignites.
        const int RainIntervalTicks = 12;
        const int RainIntervalTicksWrath = 8;
        //Half-width of the band the orbs fall through, so the full spread is 1000px.
        const float RainHalfWidth = 500f;
        const float RainSpawnHeight = -650f;

        Vector2 _arenaCenter;
        bool _arenaCenterSet;
        int _cowardRingCompressionTimer;
        int _rainTimer;
        bool _rainAnnounced;

        ///<summary>Centre of the horizontal band the orbs fall through. Anchored to the PLAYER, not
        ///to the arena centre, because the two cannot both be arena-anchored: the rain only starts
        ///once the player is more than RainOfDeathRange (600px) from the centre, so a band of
        ///+-RainHalfWidth (500px) around that same centre would never contain the person it is
        ///meant to punish. Trigger stays arena-anchored so "where is it safe to stand" is fixed and
        ///learnable; coverage tracks the player so the hazard actually reaches them.</summary>
        float RainBandCenterX => Main.player[NPC.target].Center.X;

        ///<summary>The fixed arena centre, captured the first tick Gwyn exists. Falls back to his
        ///live position until then so nothing can read a zero vector.</summary>
        Vector2 ArenaCenter => _arenaCenterSet ? _arenaCenter : NPC.Center;

        /// <summary>The outer coward ring smoothly compresses once per Unbroken Advance: 3 seconds
        ///from 2,000px to 1,000px, 16 seconds held, then a 3-second restoration. The timer itself
        ///is synchronized so both its collision rule and its dust wall agree in multiplayer.</summary>
        float CurrentCowardRingRadius
        {
            get
            {
                if (_cowardRingCompressionTimer <= 0)
                {
                    return CowardRingRadius;
                }

                int elapsed = CowardRingCompressionTotalTicks - _cowardRingCompressionTimer;
                if (elapsed < CowardRingShrinkTicks)
                {
                    float progress = elapsed / (float)CowardRingShrinkTicks;
                    return MathHelper.Lerp(CowardRingRadius, CompressedCowardRingRadius,
                        MathHelper.SmoothStep(0f, 1f, progress));
                }

                elapsed -= CowardRingShrinkTicks;
                if (elapsed < CowardRingHoldTicks)
                {
                    return CompressedCowardRingRadius;
                }

                elapsed -= CowardRingHoldTicks;
                float restoreProgress = MathHelper.Clamp(elapsed / (float)CowardRingExpandTicks, 0f, 1f);
                return MathHelper.Lerp(CompressedCowardRingRadius, CowardRingRadius,
                    MathHelper.SmoothStep(0f, 1f, restoreProgress));
            }
        }
        const int TooEarlyDamage = 10000;
        const float ProximityDebuffRange = 700f;

        int RainOfDeathDamage => TooEarly ? TooEarlyDamage : BaseRainOfDeathDamage;

        bool TooEarly =>
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Artorias>())) ||
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Seath.SeathTheScalelessHead>())) ||
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<EarthFiendLich>())) ||
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<FireFiendMarilith>())) ||
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WaterFiendKraken>())) ||
            !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<GhostWyvernMage.WyvernMageShadow>()));

        int protectedHoldTimer;   // keeps the 9999 penalty (and the broadcast) from re-triggering every tick
        float cowardGraceTimer = 90;
        bool announcedCoward;

        bool HalfHealthMovesUnlocked => NPC.life <= NPC.lifeMax * 0.50f;

        NPCDespawnHandler despawnHandler;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            // Lord's Embrace gets three times its former cached body trail: 72 positions sampled
            // every other tick produce 36 echoes. Other moves retain their original first 24 slots.
            NPCID.Sets.TrailCacheLength[NPC.type] = 72;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0;
            NPC.damage = 0; // all damage via weapon hitboxes
            NPC.defense = FightableDefense;
            NPC.height = 40;
            NPC.width = 30;
            NPC.lifeMax = 750000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 2000000;
            NPC.rarity = 44;
            NPC.boss = true;
            NPC.lavaImmune = true;
            Music = 12;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Gwyn.DespawnHandler"), Color.OrangeRed, DustID.Torch);

            tsorcRevampGlobalNPC gwynGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            gwynGlobalNPC.Agility = 0.35f; // proactive dodges (see EvadesProjectiles)
            gwynGlobalNPC.NavSearchRadius = 80;

            // On-hit dodgeroll bundle, same family as Artorias — the Lord does not stand in combos
            EvasiveProfile.RedKnight(gwynGlobalNPC);
        }

        protected override bool EvadesProjectiles => true;

        ///<summary>The old Gwyn's on-hit debuff stack — a hit from the Lord of Cinder RUINS you.</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.OnFire, 10 * 60, false);
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 40 * 60, false); //lose defense on hit
            target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 30 * 60, false);
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 30 * 60, false);    //lose knockback resistance
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Weak, 10 * 60, false);
                target.AddBuff(BuffID.BrokenArmor, 3 * 60, false);
            }
        }

        public override void OnSpawn(IEntitySource source)
        {
            //Captured here rather than in SetDefaults: SetDefaults runs before the NPC is placed,
            //so NPC.Center is not yet the arena position.
            _arenaCenter = NPC.Center;
            _arenaCenterSet = true;
            NPC.netUpdate = true;
            base.OnSpawn(source);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_arenaCenterSet);
            writer.WriteVector2(_arenaCenter);
            writer.Write((short)_cowardRingCompressionTimer);
            writer.Write(_spearJumpActive);
            writer.Write((byte)_spearThrowsThisJump);
            writer.Write((byte)_spearFollowupsRemaining);
            writer.Write(_spearAirTargetSpeed);
            writer.Write(_wrathActive);
            writer.Write((short)_stormTimer);
            writer.Write((short)_gravityTimer);
            writer.Write((short)_pullComboNudge);
            writer.Write(_dashGrabEndX);
            writer.Write((short)_plungeTimer);
            writer.Write((byte)_plungePhase);
            writer.WriteVector2(_plungeTarget);
            writer.Write(_plungeApexY);
            writer.Write((byte)_judgmentTeleportTimer);
            writer.WriteVector2(_judgmentOriginCenter);
            writer.WriteVector2(_judgmentDestinationBottom);
            writer.Write((short)_riposteTimer);
            writer.Write((short)_riposteCd);
            writer.Write(_riposteCounterPending);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _arenaCenterSet = reader.ReadBoolean();
            _arenaCenter = reader.ReadVector2();
            _cowardRingCompressionTimer = reader.ReadInt16();
            _spearJumpActive = reader.ReadBoolean();
            _spearThrowsThisJump = reader.ReadByte();
            _spearFollowupsRemaining = reader.ReadByte();
            _spearAirTargetSpeed = reader.ReadSingle();
            _wrathActive = reader.ReadBoolean();
            _stormTimer = reader.ReadInt16();
            _gravityTimer = reader.ReadInt16();
            _pullComboNudge = reader.ReadInt16();
            _dashGrabEndX = reader.ReadSingle();
            _plungeTimer = reader.ReadInt16();
            _plungePhase = reader.ReadByte();
            _plungeTarget = reader.ReadVector2();
            _plungeApexY = reader.ReadSingle();
            _judgmentTeleportTimer = reader.ReadByte();
            _judgmentOriginCenter = reader.ReadVector2();
            _judgmentDestinationBottom = reader.ReadVector2();
            _riposteTimer = reader.ReadInt16();
            _riposteCd = reader.ReadInt16();
            _riposteCounterPending = reader.ReadBoolean();
        }

        public override void AI()
        {
            // Gwyn owns this check instead of letting the puppet base start a second party-wipe
            // sequence. Despawning must suspend the combat machine before it can move, attack, or
            // continue drawing a committed attack over the dissolve effect.
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (!NPC.active)
            {
                return;
            }
            if (despawnHandler.IsDespawning)
            {
                ResetEncounterAfterWipe();
                return;
            }

            //Contact only hurts during the Unbroken Advance march (all other damage is weapon hitboxes)
            NPC.damage = TooEarly ? TooEarlyDamage : (_advanceTimer > 0 ? MeleeDamage : 0);

            base.AI();

            // Wrath Flurry's 180-tick recovery (the punish window). The landing beat is planted -
            // the navigator runs every tick, recovery included, so zero its step. After that he has
            // sheathed the sword (WeaponSheathed) and walks toward the player at the reduced
            // speed RunMovementAI applies. Recovery keeps facing locked to the combo direction, so
            // face the way he is actually walking or a player behind him gets a moonwalk. Runs after
            // base.AI so it has the last word this tick; Terraria moves and draws the NPC after AI.
            bool inFlurryRecovery = Phase == AttackPhase.MeleeComboRecovery && ActiveMeleeComboName == WrathFlurryName;
            if (inFlurryRecovery && !FlurryRecoveryWalking)
            {
                NPC.velocity.X = 0f;
            }
            else if (FlurryRecoveryWalking && Math.Abs(NPC.velocity.X) > 0.1f)
            {
                int walkDirection = Math.Sign(NPC.velocity.X);
                NPC.direction = walkDirection;
                NPC.spriteDirection = walkDirection;
            }

            TickLordEmbraceRecovery();
            TickSpearJumpChoreography();

            // A restrained neutral-white fill keeps Gwyn readable in unlit arena sections without
            // competing with the hotter attack lights on his blade and projectiles.
            if (!Main.dedServ)
                Lighting.AddLight(NPC.Center, 0.42f, 0.42f, 0.42f);

            TickDefenseRing();
            TickCowardRing();
            TickRainOfDeath();
            TickProximityDebuffs();
            TickWrath();
            TickFirestorm();
            TickDescent();
            TickFlashStep();
            TickJudgment();
            TickRiposte();
            TickSpearStorm();
            TickGravity();
            TickAdvance();
            TickWingedPlunge();
            if (_judgmentPending > 0)
            {
                _judgmentPending--;
            }
            if (_pullComboNudge > 0)
            {
                _pullComboNudge--;
            }

            //Debug HUD attack label (DebugMode overlay reads DebugAttackLabel)
            if (_attackLabelTimer > 0 && --_attackLabelTimer == 0)
            {
                DebugAttackLabel = null;
            }
        }

        private void ResetEncounterAfterWipe()
        {
            // The shared handler normally runs a four-second dissolve. That left Gwyn and any
            // already-launched attacks alive across the player's death/respawn boundary. Remove
            // every projectile attributed to this exact NPC instance before releasing the slot.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active)
                {
                    continue;
                }

                var source = projectile.GetGlobalProjectile<Projectiles.tsorcGlobalProjectile>();
                if (source.SourceNPCIndex == NPC.whoAmI && source.SourceNPCType == NPC.type)
                {
                    // Quiet removal is intentional: Kill() can run a projectile's impact burst or
                    // spawn children, which would recreate the exact post-wipe effects being cleared.
                    projectile.active = false;
                    projectile.timeLeft = 0;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.KillProjectile, -1, -1, null,
                            projectile.identity, projectile.owner);
                    }
                }
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active)
                {
                    Main.player[i].GetModPlayer<tsorcRevampPlayer>().ImpaleFreezeTimer = 0;
                }
            }

            NPC.damage = 0;
            NPC.velocity = Vector2.Zero;
            NPC.active = false;
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
        }

        int _attackLabelTimer;
        ///<summary>Announce the named attack to the DebugMode HUD for the given duration.</summary>
        void SetAttackLabel(string name, int ticks = 90)
        {
            DebugAttackLabel = name;
            _attackLabelTimer = ticks;
        }

        // ── Wrath of Gwyn (the <30% HP enrage phase change) ──────────────────────
        // One-time ignition: he wreathes himself in white-hot flame for the rest of the fight, moving
        // faster and attacking more (TopSpeed/Acceleration/MeleeComboChance above key off _wrathActive;
        // the summon cooldowns shrink), and the base combo core already weights the Wrath Flurry chain
        // up as HP drops. The First Flame, given fully.
        bool _wrathActive;

        void TickWrath()
        {
            if (!_wrathActive && NPC.life < NPC.lifeMax * 0.30f)
            {
                _wrathActive = true;
                SetAttackLabel("Wrath of Gwyn", 120);
                _firestormCd = System.Math.Min(_firestormCd, 120);
                _descentCd = System.Math.Min(_descentCd, 240);
                if (Main.netMode != NetmodeID.Server)
                {
                    UsefulFunctions.ScreenShake(NPC.Center, 10f, 24);
                    for (int i = 0; i < 60; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                        int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                        Dust d = Dust.NewDustPerfect(NPC.Center, type, vel, 40, default, 2f);
                        d.noGravity = true;
                    }
                }
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Fury"), 255, 120, 40);
                NPC.netUpdate = true;
            }

            //Permanent white-hot aura once ignited
            if (_wrathActive)
            {
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                    int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                    Dust d = Dust.NewDustPerfect(pos, type, new Vector2(0f, -1.5f), 60, default, 1.4f);
                    d.noGravity = true;
                }
                Lighting.AddLight(NPC.Center, 1f, 0.7f, 0.25f);
            }
        }

        // ── Lord's Embrace (half-health full-arena dash grab) ────────────────────
        // Reuses the proven Artorias pierce/impale state machine, but the weapon is suppressed and
        // the hand itself is the threat. The target position stays live during the readable windup,
        // then locks 100px beyond the player when the dash commits. A hit stops Gwyn immediately;
        // a miss ends at that cached overshoot instead of carrying him blindly across the arena.
        // Attack spec: 45t hand-gather tell; 32px/t dash with 100px overshoot, 36 cached echoes, and
        // the same red body silhouette as Lord's Grasp. On contact the player is held 120t at the
        // aimed hand, 50px higher and 50px farther out. Gold motes collapse into that hand while a
        // presentation-only flame orb draws over the player. The explosion starts the 40t flick;
        // release waits 20t, then launches at 12px/t through normal tile-stopped player movement.
        // Damage is 60% max life, server-authoritative; the orb is harmless and network-synced.
        protected override bool  CanPierce                 => HalfHealthMovesUnlocked;
        protected override float PierceRange               => 2400f;
        protected override float MinPierceRange            => 280f;
        protected override int   PierceChance               => 5;
        protected override int   PierceTelegraphTicks       => 45;
        protected override int   PierceDashTicks            => 90;
        protected override float PierceDashSpeed            => 32f;
        protected override int   PierceRecoveryTicks        => 180;
        protected override int   PierceStabChance            => 100;
        protected override int   PierceStabRaiseTicks       => 120;
        protected override int   PierceStabRaiseAnimTicks   => 18;
        protected override int   PierceStabFlickTicks       => 40;
        protected override int   PierceStabFlickDelayTicks  => 20;
        protected override int   PierceCooldownAfterUse     => 900;

        const float LordEmbraceMaxHealthDamageFraction = 0.60f;
        const float DashGrabOvershoot = 100f;
        const float LordEmbraceHoldRaise = 50f;
        const float LordEmbraceHoldOutward = 50f;
        const float LordEmbraceFlickSpeed = 12f;
        float _dashGrabEndX;

        protected override int AfterimageSampleLimit => IsDashGrabSequence ? 72 : 24;

        bool IsDashGrabSequence =>
            Phase == AttackPhase.PierceTelegraph || Phase == AttackPhase.PierceDash ||
            Phase == AttackPhase.PierceStabHold || Phase == AttackPhase.PierceStabFlick;

        internal bool LordEmbraceOrbActive =>
            Phase == AttackPhase.PierceStabHold || Phase == AttackPhase.PierceStabFlick;

        internal void GetLordEmbraceOrbDraw(out float scale, out float opacity)
        {
            opacity = 1f;
            if (Phase == AttackPhase.PierceStabHold)
            {
                float growth = 1f - PhaseTimer / (float)Math.Max(1, PierceStabRaiseTicks);
                scale = MathHelper.Lerp(1.18f, 2.05f,
                    MathHelper.SmoothStep(0f, 1f, growth));
                return;
            }

            int elapsed = PierceStabFlickTicks - PhaseTimer;
            float release = MathHelper.Clamp(
                (elapsed - PierceStabFlickDelayTicks) /
                (float)Math.Max(1, PierceStabFlickTicks - PierceStabFlickDelayTicks), 0f, 1f);
            scale = MathHelper.Lerp(2.05f, 1.25f, MathHelper.SmoothStep(0f, 1f, release));
            opacity = 1f - release;
        }

        Vector2 LordEmbraceHoldPosition => PuppetHandPosition
            + new Vector2(NPC.direction * (4f + LordEmbraceHoldOutward),
                -4f - LordEmbraceHoldRaise);

        protected override void ModifyAdditionalPhaseWeaponRotation(ref float weaponRotation)
        {
            if (!IsDashGrabSequence)
                return;

            int flickElapsed = PierceStabFlickTicks - PhaseTimer;
            if (Phase == AttackPhase.PierceStabFlick && flickElapsed >= PierceStabFlickDelayTicks)
                return;

            Player target = Main.player[NPC.target];
            if (target == null || !target.active || target.dead)
                return;

            Vector2 aim = (target.Center - NPC.Center)
                .SafeNormalize(new Vector2(NPC.direction, 0f));
            float worldAngle = aim.ToRotation();
            float directionNeutralAngle = NPC.direction == 1
                ? worldAngle
                : MathHelper.Pi - worldAngle;
            float time = (float)Main.GameUpdateCount + NPC.whoAmI * 13f;
            float vibration = (float)Math.Sin(time * 0.9f) * 0.018f
                + (float)Math.Sin(time * 1.7f) * 0.009f;
            weaponRotation = directionNeutralAngle + vibration;
        }

        protected override void DoPierceWindup(int elapsed)
        {
            Player target = Main.player[NPC.target];
            int direction = target.Center.X < NPC.Center.X ? -1 : 1;
            _dashGrabEndX = target.Center.X + direction * DashGrabOvershoot;

            if (elapsed == 0)
            {
                SetAttackLabel("Lord's Embrace", PierceTelegraphTicks + PierceDashTicks
                    + PierceStabRaiseTicks + PierceStabFlickTicks);
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.55f }, NPC.Center);
                }
            }

            if (!Main.dedServ)
            {
                float progress = MathHelper.Clamp(elapsed / (float)PierceTelegraphTicks, 0f, 1f);
                Vector2 hand = PuppetHandPosition;
                int count = 1 + (int)(progress * 3f);
                for (int i = 0; i < count; i++)
                {
                    Vector2 position = hand + Main.rand.NextVector2Circular(10f + progress * 18f, 10f + progress * 18f);
                    Dust ember = Dust.NewDustPerfect(position,
                        Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame,
                        (hand - position) * 0.08f, 45, default, Main.rand.NextFloat(0.9f, 1.35f));
                    ember.noGravity = true;
                }
            }

            if (elapsed >= PierceTelegraphTicks - 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true;
            }
        }

        protected override void DoPierceDashTick()
        {
            AfterimageTicks = Math.Max(AfterimageTicks, 3);

            if (!Main.dedServ)
            {
                Vector2 wake = NPC.Center - new Vector2(NPC.direction * 18f, 0f);
                Dust ember = Dust.NewDustPerfect(wake,
                    Main.rand.NextBool(3) ? DustID.RedTorch : DustID.Torch,
                    -NPC.velocity * Main.rand.NextFloat(0.10f, 0.20f)
                        + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    70, default, Main.rand.NextFloat(1f, 1.45f));
                ember.noGravity = true;
            }

            bool reachedOvershoot = NPC.direction > 0
                ? NPC.Center.X >= _dashGrabEndX
                : NPC.Center.X <= _dashGrabEndX;
            if (reachedOvershoot)
            {
                NPC.Center = new Vector2(_dashGrabEndX, NPC.Center.Y);
                NPC.velocity.X = 0f;
                EnterPhase(AttackPhase.PierceRecovery, PierceRecoveryTicks);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }
        }

        protected override void OnPierceContact(Player target, bool isStab)
        {
            var modPlayer = target.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 10;
            modPlayer.ImpaleWorldPosition = PuppetHandPosition;
            ReportAttackHit();

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.NPCHit13 with { Volume = 0.75f, Pitch = -0.35f }, target.Center);
                UsefulFunctions.ScreenShake(target.Center, 8f, 14);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.Gwyn.GwynEmbraceOrb>(), 0, 0f,
                    Main.myPlayer, NPC.whoAmI, target.whoAmI);
                NPC.netUpdate = true;
            }
        }

        protected override void DoPierceStabHoldTick(Player target, float raiseProgress01)
        {
            if (target.dead)
            {
                target.GetModPlayer<tsorcRevampPlayer>().ImpaleFreezeTimer = 0;
                return;
            }

            Vector2 holdPosition = LordEmbraceHoldPosition;
            var modPlayer = target.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 10;
            modPlayer.ImpaleWorldPosition = holdPosition;

            EmitLordEmbraceHandCollapse(raiseProgress01);
        }

        void EmitLordEmbraceHandCollapse(float progress01)
        {
            if (Main.dedServ)
                return;

            float growth = MathHelper.SmoothStep(0f, 1f,
                MathHelper.Clamp(progress01, 0f, 1f));
            Vector2 hand = PuppetHandPosition;
            int count = 2 + (int)(growth * 5f);
            float radius = MathHelper.Lerp(18f, 48f, growth);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius)
                    * Main.rand.NextFloat(0.72f, 1f);
                Vector2 inward = (-offset).SafeNormalize(Vector2.Zero);
                Dust ember = Dust.NewDustPerfect(hand + offset,
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch,
                    inward * Main.rand.NextFloat(1.4f, 3.2f), 55,
                    new Color(255, 182, 52), Main.rand.NextFloat(0.65f, 1.25f));
                ember.noGravity = true;
            }
            Lighting.AddLight(hand, 1.1f + growth * 0.7f,
                0.42f + growth * 0.35f, 0.08f);
        }

        protected override void OnPierceFlickStarted(Player target)
        {
            if (!target.active || target.dead)
            {
                return;
            }

            Vector2 explosionCenter = target.Center;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.6f }, explosionCenter);
                UsefulFunctions.ScreenShake(explosionCenter, 20f, 30);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int damage = Math.Max(1,
                    (int)Math.Ceiling(target.statLifeMax2 * LordEmbraceMaxHealthDamageFraction));
                target.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), damage,
                    NPC.direction, dodgeable: false, scalingArmorPenetration: 1f);
                target.AddBuff(BuffID.OnFire, 10 * 60);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), explosionCenter + new Vector2(0f, 48f),
                    Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.GwynDescentColumn>(),
                    0, 0f, Main.myPlayer, Projectiles.Enemy.GwynDescentColumn.ExplosionOnlyMode);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true;
            }
        }

        protected override void DoPierceStabFlickDelayTick(Player target, int elapsed)
        {
            if (!target.active || target.dead)
                return;

            var modPlayer = target.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 10;
            modPlayer.ImpaleWorldPosition = LordEmbraceHoldPosition;
            EmitLordEmbraceHandCollapse(1f);
        }

        protected override void OnPierceFlick(Player target)
        {
            var modPlayer = target.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 0;
            if (!target.active || target.dead)
                return;

            // Velocity is resolved by Terraria's ordinary player movement, so every normal solid
            // tile stops the throw. Never move Center directly here: the old 800px displacement
            // skipped tile collision and could place the player through entire walls.
            Vector2 away = new Vector2(NPC.direction, -0.12f)
                .SafeNormalize(new Vector2(NPC.direction, 0f));
            target.velocity = away * LordEmbraceFlickSpeed;
        }

        void TickLordEmbraceRecovery()
        {
            if (Phase != AttackPhase.PierceRecovery)
                return;

            Player target = Main.player[NPC.target];
            if (target == null || !target.active || target.dead)
                return;

            int direction = target.Center.X < NPC.Center.X ? -1 : 1;
            NPC.direction = direction;
            NPC.spriteDirection = direction;
            NPC.velocity.X = direction * TopSpeed * 0.35f;
        }

        // ── Flash Step (sunlight teleport — connective pressure) ─────────────────
        // Only from a free (non-attacking) phase: a burst of sunlight, and he reappears just behind
        // the player so back-turning is punished. Flows straight into the base combo AI from there.
        int _flashStepCd = 420;

        void TickFlashStep()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_flashStepCd > 0)
            {
                _flashStepCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return; // never mid-attack
            }
            Player player = Main.player[NPC.target];
            float dist = NPC.Distance(player.Center);
            if (!player.dead && player.active && dist > 300f && dist < 1000f && Main.rand.NextBool(90))
            {
                _flashStepCd = 420 + Main.rand.Next(240);
                SetAttackLabel("Flash Step", 40);
                FlashBurst(NPC.Center);
                float destX = player.Center.X - player.direction * 70f; // reappear just behind the player
                NPC.Bottom = new Vector2(destX, player.Bottom.Y);
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
                NPC.spriteDirection = NPC.direction;
                FlashBurst(NPC.Center);
                NPC.netUpdate = true;
            }
        }

        void FlashBurst(Vector2 pos)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item8 with { Pitch = 0.3f }, pos);
            for (int i = 0; i < 24; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(7f, 7f);
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                Dust d = Dust.NewDustPerfect(pos, type, vel, 40, default, 1.6f);
                d.noGravity = true;
            }
        }

        // ── Greatsword Boomerang (full-lane reach — the edge-camper punish) ──────
        // Attack spec — Greatsword Boomerang / PuppetNPC Boomerang phases + GwynGreatswordBoomerang.
        // Weapon: EnemySwordOfGwyn, raised for 26t then thrown halfway through a 28t overhead chop.
        // Birth/life/death: launches from the forward hand at 15px/t, spins with a gold/fire dust
        // wake, passes through terrain deliberately, then homes to Gwyn and ends with the catch sound.
        // Reach: selectable from 200-2850px (3x the old 950px maximum); aim locks on release and the
        // outbound leg ends 300px beyond that player position before the return begins.
        // VFX: pixel-filtered cinder copies behind the crisp sword; additive, behind the projectile.
        // Selection: 7% eligible idle roll, 420t cooldown; 60t recovery leaves Gwyn punishable.
        // Fairness: 26t raise + 14t of visible chop before release; rollable, terrain-piercing.
        // Multiplayer: the server locks and sends outbound distance; projectile damage remains hostile.
        protected override bool  CanBoomerang               => true;
        protected override float BoomerangMinRange          => 200f;
        protected override float BoomerangMaxRange          => 2850f;
        protected override int   BoomerangChance            => 7;
        protected override int   BoomerangCooldownAfterUse  => 420;
        protected override int   BoomerangSwingTelegraphTicks => 26;
        protected override int   BoomerangSwingTicks        => 28;
        protected override float BoomerangFireProgress      => 0.5f;
        protected override int   BoomerangRecoveryTicks     => 60;

        const int BoomerangDamage = 65;

        protected override void DoBoomerangSwingTick(int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }
            float swingT = total > 0 ? elapsed / (float)total : 1f;
            float angle = MathHelper.Lerp(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f), swingT);
            Vector2 bladePos = NPC.Center + new Vector2(NPC.direction, 0f).RotatedBy(angle) * 48f;
            int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
            Dust d = Dust.NewDustPerfect(bladePos + Main.rand.NextVector2Circular(6f, 6f), type, Vector2.Zero, 70, default, Main.rand.NextFloat(1.1f, 1.6f));
            d.noGravity = true;
        }

        protected override void DoBoomerangFire()
        {
            SetAttackLabel("Greatsword Boomerang", 130);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.5f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 26f, -14f);
            Vector2 vel = (target.Center - origin).SafeNormalize(new Vector2(NPC.direction, 0f)) * 15f;
            float outboundDistance = Vector2.Distance(origin, target.Center)
                + Projectiles.Enemy.GwynGreatswordBoomerang.TargetOvershoot;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                ModContent.ProjectileType<Projectiles.Enemy.GwynGreatswordBoomerang>(), BoomerangDamage,
                6f, Main.myPlayer, NPC.whoAmI, outboundDistance);
        }

        // ── Judgment from Behind (the back-turn punish) ──────────────────────────
        // Attack spec — Judgment from Behind / bespoke teleport -> Judgment Guillotine.
        // Weapon: EnemySwordOfGwyn is drawn throughout the post-teleport 60t tell; it moves for 40t
        // then holds fully settled for 20t before the Whip-shaped swing re-accelerates.
        // Tell: 45t, locked origin and destination. Exactly 250 gold motes are attempted at EACH
        // point over the tell, beginning outside Gwyn's silhouette and collapsing into its centre.
        // Teleport: the marked destination erupts outward in a separate 250-mote three-layer burst.
        // Reach: destination is locked 90px behind the target; the follow-up has a 131px swept reach
        // against Gwyn's ~88px visible blade, deliberately generous for the blind-side execution.
        // Fairness: both endpoints are visible for 45t, then the sword gives a separate 60t tell;
        // its 19t live interval is fully covered by the base 22t roll. Recovery is 48t.
        // Multiplayer: the server locks and executes the warp; timer and endpoints are synchronized,
        // while every client emits its own cosmetic dust from those shared coordinates.
        int _judgmentCd = 600;
        int _judgmentPending;
        int _judgmentTeleportTimer;
        Vector2 _judgmentOriginCenter;
        Vector2 _judgmentDestinationBottom;

        const int JudgmentTeleportTelegraphTicks = 45;
        const int JudgmentEndpointDustCount = 250;
        const int JudgmentExitBurstDustCount = 250;

        void TickJudgment()
        {
            if (_judgmentTeleportTimer > 0)
            {
                RunJudgmentTeleport();
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_judgmentCd > 0)
            {
                _judgmentCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return;
            }
            Player player = Main.player[NPC.target];
            float dist = NPC.Distance(player.Center);
            bool facingAway = System.Math.Sign(NPC.Center.X - player.Center.X) != player.direction;
            if (!player.dead && player.active && facingAway && dist > 380f && dist < 1100f && Main.rand.NextBool(100))
            {
                _judgmentCd = 700 + Main.rand.Next(300);
                SetAttackLabel("Judgment from Behind", 200);
                _judgmentOriginCenter = NPC.Center;
                float destX = player.Center.X - player.direction * 90f; //their blind side
                _judgmentDestinationBottom = new Vector2(destX, player.Bottom.Y);
                _judgmentTeleportTimer = 1;
                NPC.velocity = Vector2.Zero;
                EnterPhase(AttackPhase.NovaRecovery, JudgmentTeleportTelegraphTicks + 2);
                NPC.netUpdate = true;
            }
        }

        void RunJudgmentTeleport()
        {
            NPC.velocity = Vector2.Zero;
            Vector2 destinationCenter = _judgmentDestinationBottom - Vector2.UnitY * NPC.height * 0.5f;

            if (!Main.dedServ)
            {
                SpawnJudgmentCollapse(_judgmentOriginCenter, _judgmentTeleportTimer);
                SpawnJudgmentCollapse(destinationCenter, _judgmentTeleportTimer);
                float progress = _judgmentTeleportTimer / (float)JudgmentTeleportTelegraphTicks;
                Lighting.AddLight(_judgmentOriginCenter, 0.65f * progress, 0.5f * progress, 0.12f);
                Lighting.AddLight(destinationCenter, 0.9f * progress, 0.72f * progress, 0.18f);

                if (_judgmentTeleportTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.75f, Pitch = -0.4f }, NPC.Center);
                }
            }

            if (_judgmentTeleportTimer < JudgmentTeleportTelegraphTicks)
            {
                _judgmentTeleportTimer++;
                return;
            }

            // The collapse lands first; the new silhouette then punches the same gold back outward.
            if (!Main.dedServ)
            {
                SpawnJudgmentExitBurst(destinationCenter);
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 1f, Pitch = 0.2f }, destinationCenter);
            }

            // Clients predict the already server-authored endpoint so Gwyn appears inside the burst
            // immediately; the server's net update confirms the same coordinates.
            NPC.Bottom = _judgmentDestinationBottom;
            Player player = Main.player[NPC.target];
            NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            NPC.spriteDirection = NPC.direction;
            EnterPhase(AttackPhase.Idle, 0);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _judgmentPending = 120;
                NPC.netUpdate = true;
            }

            _judgmentTeleportTimer = 0;
        }

        void SpawnJudgmentCollapse(Vector2 center, int elapsed)
        {
            // Integer cumulative emission makes the attempted total exactly 250 across all 45 ticks
            // without a lopsided final-frame dump (the global Terraria dust budget may still thin it).
            int throughThisTick = JudgmentEndpointDustCount * elapsed / JudgmentTeleportTelegraphTicks;
            int throughPreviousTick = JudgmentEndpointDustCount * (elapsed - 1) / JudgmentTeleportTelegraphTicks;
            int count = throughThisTick - throughPreviousTick;
            float progress = elapsed / (float)JudgmentTeleportTelegraphTicks;
            float eased = progress * progress * (3f - 2f * progress);
            float radiusX = MathHelper.Lerp(Math.Max(86f, NPC.width * 1.15f), 8f, eased);
            float radiusY = MathHelper.Lerp(Math.Max(72f, NPC.height * 0.8f), 10f, eased);
            int remaining = Math.Max(1, JudgmentTeleportTelegraphTicks - elapsed + 1);

            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 offset = new Vector2((float)Math.Cos(angle) * radiusX,
                    (float)Math.Sin(angle) * radiusY);
                Vector2 position = center + offset;
                Vector2 inward = (center - position) / remaining * 1.35f;
                if (inward.LengthSquared() > 13f * 13f)
                {
                    inward = inward.SafeNormalize(Vector2.Zero) * 13f;
                }

                int type = Main.rand.NextBool(5) ? DustID.GoldCoin : DustID.GoldFlame;
                Dust dust = Dust.NewDustPerfect(position, type, inward, 45, default,
                    Main.rand.NextFloat(0.55f, 1.25f));
                dust.noGravity = true;
            }
        }

        void SpawnJudgmentExitBurst(Vector2 center)
        {
            for (int i = 0; i < JudgmentExitBurstDustCount; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                float spawnRadius;
                float speed;
                float scale;
                int alpha;
                int type;

                if (i < 160) // midground body: the mass of the sunlight eruption
                {
                    spawnRadius = Main.rand.NextFloat(3f, 28f);
                    speed = Main.rand.NextFloat(2.5f, 8f);
                    scale = Main.rand.NextFloat(0.75f, 1.45f);
                    alpha = 55;
                    type = DustID.GoldFlame;
                }
                else if (i < 220) // foreground sparks: smaller, faster leading edge
                {
                    spawnRadius = Main.rand.NextFloat(8f, 38f);
                    speed = Main.rand.NextFloat(8f, 13f);
                    scale = Main.rand.NextFloat(0.45f, 0.85f);
                    alpha = 30;
                    type = DustID.GoldCoin;
                }
                else // background glow: slow motes billow after the sharp burst has passed
                {
                    spawnRadius = Main.rand.NextFloat(4f, 24f);
                    speed = Main.rand.NextFloat(0.6f, 2.2f);
                    scale = Main.rand.NextFloat(0.5f, 0.75f);
                    alpha = 100;
                    type = DustID.GoldFlame;
                }

                Dust dust = Dust.NewDustPerfect(center + direction * spawnRadius, type,
                    direction * speed, alpha, default, scale);
                dust.noGravity = true;
                if (i >= 220)
                {
                    dust.fadeIn = Main.rand.NextFloat(1.25f, 1.6f);
                }
            }
        }

        // ── Riposte Stance (reactive guard -> melee punish OR projectile return) ──
        // Attack spec
        // Weapon / pose: EnemySwordOfGwyn remains visibly raised at -1.62 rad for the full 60t guard.
        // Entry: a far player's shot on a collision course starts it deterministically; a close melee hit
        // has a 1-in-3 chance to provoke it, while neutral bait is only a 1-in-180 roll. Every entry opens
        // the same 480t (8 second) cooldown.
        // Melee response: the first hit during guard is halved, clangs, then starts the 18t -> 24t
        // Riposte Counter clip. Its live 15t Whip cut is covered by a normal 22t dodge; 40t recovery is
        // the reward for respecting it. It uses the real tracked blade hitbox, never a recovery-only swing.
        // Projectile response: non-melee, non-minion shots are swallowed at the guard, held in the existing
        // Gigas Holy Shield vortex, then released 400px behind the target after that vortex's 60t tell as a
        // hostile shot aimed back at its owner. One guard reflects at most one projectile.
        // Multiplayer: guard/cooldown/counter state is extra-AI synchronized; spawn, capture, damage and
        // melee selection are server-authoritative. Each client generates only its local dust and lighting.
        const int RiposteGuardTicks = 60;
        const int RiposteCooldownTicks = 8 * 60;
        const float RiposteMeleeTriggerRange = 180f;
        const float RiposteReflectMinimumRange = 340f;
        const float RiposteProjectileThreatRange = 920f;
        const int RipostePreemptiveChance = 180;
        int _riposteCd = RiposteCooldownTicks;
        int _riposteTimer;
        bool _riposteCounterPending;

        bool RiposteGuardActive => _riposteTimer > 0 && Phase == AttackPhase.Custom;

        void TickRiposte()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && _riposteCd > 0)
            {
                _riposteCd--;
            }

            if (_riposteTimer > 0)
            {
                // A different bespoke state interrupted the guard. Do not leave an invisible 50%
                // reduction or a stale timer behind it.
                if (Phase != AttackPhase.Custom)
                {
                    _riposteTimer = 0;
                    return;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && NPC.target >= 0 && NPC.target < Main.maxPlayers)
                {
                    Player player = Main.player[NPC.target];
                    if (player.active && !player.dead)
                    {
                        AbsorbRiposteProjectiles(player);
                    }
                }

                _riposteTimer--;
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || _riposteCd > 0
                || (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
                || NPC.target < 0 || NPC.target >= Main.maxPlayers)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                return;
            }

            float distance = NPC.Distance(target.Center);
            // Long-range pressure is the reliable reactive entry: commit to a visibly incoming shot,
            // then make the player decide whether to stop firing or watch their own projectile return.
            if (distance >= RiposteReflectMinimumRange && HasIncomingRiposteThreat(target))
            {
                StartRiposteGuard(target, "Projectile Guard");
            }
            // At close range it is only a rare bait. The response-to-a-real-melee-hit path below is
            // deliberately more likely, so it feels reactive rather than a random invulnerability roll.
            else if (distance <= RiposteMeleeTriggerRange && Main.rand.NextBool(RipostePreemptiveChance))
            {
                StartRiposteGuard(target, "Riposte Stance");
            }
        }

        void StartRiposteGuard(Player player, string label)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || _riposteTimer > 0 || _riposteCd > 0
                || (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll) || !player.active || player.dead)
            {
                return;
            }

            NPC.target = player.whoAmI;
            NPC.direction = NPC.spriteDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
            _riposteTimer = RiposteGuardTicks;
            _riposteCd = RiposteCooldownTicks;
            SetAttackLabel(label, RiposteGuardTicks + 20);
            StartCustomAttack(RiposteGuardTicks, MeleeWeaponItemType, swingPose: true);
            NPC.netUpdate = true;
        }

        protected override void DoCustomAttack()
        {
            if (!RiposteGuardActive)
            {
                return;
            }

            // StartCustomAttack is called on the authority; multiplayer clients instead receive the
            // Custom state and play this cue on their first synchronized Custom tick below.
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.58f, Pitch = 0.48f }, NPC.Center);
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (!RiposteGuardActive)
            {
                return;
            }

            float progress = MathHelper.Clamp(1f - ticksRemaining / (float)RiposteGuardTicks, 0f, 1f);
            float bladeReach = ComboReachBase * 0.7f;
            Vector2 bladeTip = PuppetWeaponTipPosition(bladeReach);
            Lighting.AddLight(bladeTip, 0.75f + 0.35f * progress, 0.58f + 0.25f * progress, 0.12f);

            if (Main.netMode == NetmodeID.MultiplayerClient && ticksRemaining == RiposteGuardTicks)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.58f, Pitch = 0.48f }, NPC.Center);
            }

            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Vector2 offset = Main.rand.NextVector2Circular(7f, 24f);
                int dustType = Main.rand.NextBool() ? DustID.GoldCoin : DustID.GoldFlame;
                Dust dust = Dust.NewDustPerfect(bladeTip + offset, dustType,
                    -offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.3f, 1.3f), 50, default,
                    MathHelper.Lerp(0.72f, 1.18f, progress));
                dust.noGravity = true;
            }
        }

        bool HasIncomingRiposteThreat(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile incoming = Main.projectile[i];
                if (!IsRiposteReflectable(incoming, player))
                {
                    continue;
                }

                float speed = incoming.velocity.Length();
                if (speed < 2f)
                {
                    continue;
                }

                Vector2 toGwyn = NPC.Center - incoming.Center;
                float forwardDistance = Vector2.Dot(toGwyn, incoming.velocity / speed);
                if (forwardDistance < 0f || forwardDistance > RiposteProjectileThreatRange)
                {
                    continue;
                }

                float collisionRadius = Math.Max(NPC.width, NPC.height) * 0.65f
                    + Math.Max(incoming.width, incoming.height) * 0.5f;
                float lateralDistanceSquared = Math.Max(0f, toGwyn.LengthSquared() - forwardDistance * forwardDistance);
                if (lateralDistanceSquared > collisionRadius * collisionRadius)
                {
                    continue;
                }

                float impactTicks = forwardDistance / speed;
                if (impactTicks >= 16f && impactTicks <= 55f)
                {
                    return true;
                }
            }
            return false;
        }

        static bool IsRiposteReflectable(Projectile projectile, Player target)
        {
            return projectile.active && projectile.friendly && !projectile.hostile && projectile.damage > 0
                && projectile.owner == target.whoAmI && !projectile.IsMinionOrSentryRelated
                && !projectile.DamageType.CountsAsClass(DamageClass.Melee);
        }

        void AbsorbRiposteProjectiles(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile incoming = Main.projectile[i];
                if (!IsRiposteReflectable(incoming, player))
                {
                    continue;
                }

                Vector2 toGwyn = NPC.Center - incoming.Center;
                float catchRadius = Math.Max(NPC.width, NPC.height) * 0.65f
                    + Math.Max(incoming.width, incoming.height) * 0.5f;
                if (!incoming.Hitbox.Intersects(NPC.Hitbox)
                    && toGwyn.LengthSquared() > catchRadius * catchRadius)
                {
                    continue;
                }

                // A passing shot that has already moved away is no longer a guard interaction.
                if (Vector2.Dot(toGwyn, incoming.velocity) < -8f)
                {
                    continue;
                }

                ReflectRiposteProjectile(player, incoming);
                return; // One clearly readable return per guard.
            }
        }

        void ReflectRiposteProjectile(Player player, Projectile incoming)
        {
            Vector2 impact = incoming.Center;
            Projectile.NewProjectile(NPC.GetSource_FromAI(), impact, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.GigasHolyShieldVortex>(), 0, 0f, Main.myPlayer, -1f, 0f);

            if (TryFindRiposteVortexPosition(player, impact, out Vector2 output))
            {
                int damage = Math.Max(1, (int)(player.statLifeMax2 * 0.20f));
                float returnSpeed = Math.Max(incoming.velocity.Length(), 14f);
                // Preserve the player's original projectile and visuals, but make its later release
                // hostile, wall-piercing, and owned by its obvious 60-tick output telegraph.
                incoming.friendly = false;
                incoming.hostile = false;
                incoming.hide = true;
                incoming.tileCollide = false;
                incoming.ignoreWater = true;
                incoming.velocity = Vector2.Zero;
                incoming.damage = damage;
                incoming.timeLeft = Math.Max(incoming.timeLeft, 180);
                incoming.netUpdate = true;

                Projectile outputVortex = Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), output, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GigasHolyShieldVortex>(), 0, 0f, player.whoAmI,
                    incoming.whoAmI, Projectiles.Enemy.GigasHolyShieldVortex.OutputMode);
                outputVortex.localAI[1] = returnSpeed;
                outputVortex.netUpdate = true;
            }
            else
            {
                // The intake vortex still plays for 20 ticks, making this rare slot-pressure fallback
                // a visible absorption rather than a silent projectile deletion.
                incoming.Kill();
            }

            _riposteTimer = 0;
            SetAttackLabel("PROJECTILE RETURN", 70);
            EnterPhase(AttackPhase.Idle, 0);
            NPC.netUpdate = true;
        }

        bool TryFindRiposteVortexPosition(Player player, Vector2 impact, out Vector2 position)
        {
            int facing = player.direction == 0 ? NPC.direction : player.direction;
            Vector2 behindPlayer = player.Center - new Vector2(facing * 400f, 0f);
            float heightT = MathHelper.Clamp((impact.Y - NPC.Top.Y) / NPC.height, 0f, 1f);
            float desiredY = behindPlayer.Y + MathHelper.Lerp(-90f, 90f, heightT);
            float bestScore = float.MaxValue;
            position = Vector2.Zero;

            float[] verticalSlots = { -90f, -30f, 30f, 90f };
            float[] lateralSlots = { -30f, 30f };
            int vortexType = ModContent.ProjectileType<Projectiles.Enemy.GigasHolyShieldVortex>();
            foreach (float yOffset in verticalSlots)
            {
                foreach (float xOffset in lateralSlots)
                {
                    Vector2 candidate = behindPlayer + new Vector2(xOffset, yOffset);
                    bool occupied = false;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile portal = Main.projectile[i];
                        if (portal.active && portal.type == vortexType && portal.owner == player.whoAmI
                            && portal.ai[1] == Projectiles.Enemy.GigasHolyShieldVortex.OutputMode
                            && Vector2.DistanceSquared(portal.Center, candidate) < 54f * 54f)
                        {
                            occupied = true;
                            break;
                        }
                    }
                    if (!occupied)
                    {
                        float score = Math.Abs(candidate.Y - desiredY) + Math.Abs(xOffset) * 0.1f;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            position = candidate;
                        }
                    }
                }
            }
            return bestScore < float.MaxValue;
        }

        void TryStartRiposteFromMeleeHit(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || _riposteTimer > 0 || _riposteCd > 0
                || (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
                || !player.active || player.dead || NPC.Distance(player.Center) > RiposteMeleeTriggerRange)
            {
                return;
            }

            if (Main.rand.NextBool(3))
            {
                StartRiposteGuard(player, "Riposte Stance");
            }
        }

        void TriggerMeleeRiposte(Player player)
        {
            if (!RiposteGuardActive)
            {
                return;
            }

            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.9f, Pitch = 0.5f }, NPC.Center);
                for (int i = 0; i < 26; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                    int dustType = Main.rand.NextBool() ? DustID.GoldCoin : DustID.GoldFlame;
                    Dust dust = Dust.NewDustPerfect(NPC.Center, dustType, velocity, 40, default, 1.5f);
                    dust.noGravity = true;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            NPC.target = player.whoAmI;
            NPC.direction = NPC.spriteDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
            _riposteTimer = 0;
            _riposteCounterPending = true;
            SetAttackLabel("RIPOSTE!", 70);
            EnterPhase(AttackPhase.Idle, 0);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.4f }, NPC.Center);

            // The protected base picker keeps all V2 timing, swept-blade collision and net snapshots
            // intact, while ReactiveComboIndex forces the internal Riposte Counter entry this instant.
            if (!TryStartMeleeCombo(NPC.Distance(player.Center)))
            {
                _riposteCounterPending = false;
                EnterPhase(AttackPhase.NovaRecovery, 40);
            }
            NPC.netUpdate = true;
        }

        ///<summary>The guard only halves the deliberate melee trade. Eligible ranged shots are instead
        ///blocked in <see cref="CanBeHitByProjectile"/> and converted into a visible return vortex.</summary>
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            if (RiposteGuardActive)
            {
                modifiers.FinalDamage *= 0.5f;
            }
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (RiposteGuardActive && NPC.target >= 0 && NPC.target < Main.maxPlayers)
            {
                Player target = Main.player[NPC.target];
                if (target.active && !target.dead && IsRiposteReflectable(projectile, target))
                {
                    return false;
                }
            }
            return base.CanBeHitByProjectile(projectile);
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            if (RiposteGuardActive)
            {
                TriggerMeleeRiposte(player);
            }
            else
            {
                TryStartRiposteFromMeleeHit(player);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            if (!projectile.DamageType.CountsAsClass(DamageClass.Melee)
                || projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            {
                return;
            }

            Player player = Main.player[projectile.owner];
            if (RiposteGuardActive)
            {
                TriggerMeleeRiposte(player);
            }
            else
            {
                TryStartRiposteFromMeleeHit(player);
            }
        }

        // ── Sunlight Spear Storm (the airborne bullet-hell escalation of the volley) ──
        // He ascends on his wings (angel wings; flame wings once the Wrath ignites) and hangs aloft
        // while a dozen spear-nodes ring the player in three sequenced waves — each node telegraphs,
        // fires its spear, and dissipates. Landing exhausts him: the recovery is the reward.
        int _stormCd = 420;
        int _stormTimer;
        const int StormNodeDamage = 40;

        void TickSpearStorm()
        {
            if (_stormTimer > 0)
            {
                _stormTimer++;
                Player player = Main.player[NPC.target];

                //Sequenced waves of 4 nodes ringing the player, offset per wave
                if (Main.netMode != NetmodeID.MultiplayerClient && !player.dead
                    && (_stormTimer == 60 || _stormTimer == 105 || _stormTimer == 150))
                {
                    int wave = _stormTimer == 60 ? 0 : _stormTimer == 105 ? 1 : 2;
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f, Pitch = 0.3f }, player.Center);
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = MathHelper.ToRadians(wave * 30f) + MathHelper.PiOver2 * i;
                        Vector2 pos = player.Center + ang.ToRotationVector2() * 340f;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.Enemy.GwynSolarSpearNode>(), StormNodeDamage, 2f, Main.myPlayer, 22f);
                    }
                }
                //Radiance while he hangs aloft
                if (Main.rand.NextBool(2) && Main.netMode != NetmodeID.Server)
                {
                    int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                    Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(30f, 30f), type, new Vector2(0f, -1f), 60, default, 1.2f);
                    d.noGravity = true;
                }
                Lighting.AddLight(NPC.Center, 0.9f, 0.8f, 0.35f);

                if (_stormTimer == 200)
                {
                    Flight?.RequestLand();
                }
                if (_stormTimer >= 240 || player.dead)
                {
                    Flight?.RequestLand();
                    _stormTimer = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        _stormCd = _wrathActive
                            ? 660 + Main.rand.Next(240)
                            : 900 + Main.rand.Next(300);
                        NPC.netUpdate = true;
                    }
                }
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_stormCd > 0)
            {
                _stormCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            float dist = NPC.Distance(target.Center);
            int stormRoll = _wrathActive ? 65 : 90;
            if (!target.dead && target.active && dist > 250f && dist < 1000f && Main.rand.NextBool(stormRoll))
            {
                _stormTimer = 1;
                SetAttackLabel("Sunlight Spear Storm", 240);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.9f, Pitch = -0.3f }, NPC.Center);
                EnterPhase(AttackPhase.NovaRecovery, 250); //park the ground machine for the whole storm
                Flight?.RequestTakeoff();
                NPC.netUpdate = true;
            }
        }

        // ── Gravity of the Sun (#13 — the keystone anti-kite) ────────────────────
        // He plants and a golden singularity forms at his chest: 40t of light spiralling inward (the
        // read), then a GwynGravityWell drags every player radially toward him for ~4s. The cast
        // releases Gwyn as soon as the well opens, so the field can feed directly into his next
        // attack. Resistible by holding away or rolling — but a stationary caster gets reeled into
        // his melee, where the reactive hook answers with the pressure string (_pullComboNudge).
        int _gravityCd = 500;
        int _gravityTimer;
        int _pullComboNudge;
        const int GravityChargeTicks = 40;
        const int GravityWellTicks = 240;
        const int GravitySequenceTicks = GravityChargeTicks + GravityWellTicks;

        void TickGravity()
        {
            if (_gravityTimer > 0)
            {
                _gravityTimer++;

                if (_gravityTimer <= GravityChargeTicks)
                {
                    NPC.velocity.X *= 0.75f; //planted only while the singularity forms
                    //The singularity forming: gold spiralling tightly inward to his chest
                    float progress = _gravityTimer / (float)GravityChargeTicks;
                    int count = 1 + (int)(progress * 3f);
                    for (int i = 0; i < count; i++)
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        float radius = MathHelper.Lerp(120f, 15f, progress) + Main.rand.NextFloat(15f);
                        Vector2 pos = NPC.Center + ang.ToRotationVector2() * radius;
                        int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.GoldCoin;
                        Dust d = Dust.NewDustPerfect(pos, type, (NPC.Center - pos) * 0.1f, 60, default, 1.2f);
                        d.noGravity = true;
                    }
                    if (_gravityTimer == GravityChargeTicks)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f, Pitch = -0.6f }, NPC.Center);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                                ModContent.ProjectileType<Projectiles.Enemy.GwynGravityWell>(), 0, 0f, Main.myPlayer,
                                NPC.whoAmI, GravityWellTicks);
                            _pullComboNudge = GravityWellTicks;
                        }

                        //The field is now autonomous. Free the combat machine for the next tick and
                        //stop this helper from damping movement during whatever attack follows.
                        if (Phase == AttackPhase.NovaRecovery)
                        {
                            EnterPhase(AttackPhase.Idle, 0);
                        }
                        NPC.netUpdate = true;
                    }
                }
                Lighting.AddLight(NPC.Center, 1f, 0.85f, 0.35f);

                if (_gravityTimer >= GravitySequenceTicks)
                {
                    _gravityTimer = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        _gravityCd = 800 + Main.rand.Next(300);
                        NPC.netUpdate = true;
                    }
                }
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_gravityCd > 0)
            {
                _gravityCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return;
            }
            Player gPlayer = Main.player[NPC.target];
            float gDist = NPC.Distance(gPlayer.Center);
            if (!gPlayer.dead && gPlayer.active && gDist > 200f && gDist < 1100f && Main.rand.NextBool(130))
            {
                _gravityTimer = 1;
                //The named cast ends when the autonomous well opens; subsequent attacks should get
                //their own telemetry identity even though the field remains active behind them.
                SetAttackLabel("Gravity of the Sun", GravityChargeTicks + 10);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.7f }, NPC.Center);
                EnterPhase(AttackPhase.NovaRecovery, GravityChargeTicks + 5); //only park him for the cast
                NPC.netUpdate = true;
            }
        }

        // ── Unbroken Advance (#15 — anti-knockback / anti-stunlock) ──────────────
        // Not a dash: a slow, relentless, ARMORED march (999 defense — see TickDefenseRing) straight
        // at the player for 3 seconds, fire boiling off him, contact damage live (see the NPC.damage
        // line in AI). No winded recovery: the moment the march ends the combat machine is free, so
        // it flows straight into whatever attack the player's position deserves.
        int _advanceCd = 600;
        int _advanceTimer;
        int _advanceWallTicks;

        void TickAdvance()
        {
            if (_advanceTimer > 0)
            {
                _advanceTimer--;
                Player player = Main.player[NPC.target];
                int dir = player.Center.X > NPC.Center.X ? 1 : -1;
                NPC.direction = dir;
                NPC.spriteDirection = dir;
                NPC.velocity.X = dir * 2.2f;

                //Fire boiling off him + the scorch line his blade drags
                if (Main.netMode != NetmodeID.Server)
                {
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                        int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                        Dust d = Dust.NewDustPerfect(pos, type, new Vector2(0f, -1.8f), 60, default, 1.4f);
                        d.noGravity = true;
                    }
                    Vector2 scorch = NPC.Bottom + new Vector2(-dir * 20f, -4f);
                    Dust s = Dust.NewDustPerfect(scorch, DustID.Torch, new Vector2(0f, -0.6f), 100, default, 1f);
                    s.noGravity = true;
                }
                if (_advanceTimer % 20 == 0)
                {
                    SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.35f, Pitch = 0.3f }, NPC.Bottom);
                }
                Lighting.AddLight(NPC.Center, 0.9f, 0.5f, 0.15f);

                //Walked into the arena wall long enough — the march ends early
                if (NPC.collideX)
                {
                    if (++_advanceWallTicks > 30)
                    {
                        _advanceTimer = 0;
                    }
                }
                else
                {
                    _advanceWallTicks = 0;
                }

                if (_advanceTimer == 0 || player.dead)
                {
                    _advanceTimer = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        _advanceCd = 900 + Main.rand.Next(300);
                        NPC.netUpdate = true;
                    }
                }
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_advanceCd > 0)
            {
                _advanceCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return;
            }
            Player aPlayer = Main.player[NPC.target];
            float aDist = NPC.Distance(aPlayer.Center);
            if (!aPlayer.dead && aPlayer.active && aDist > 250f && aDist < 900f && Main.rand.NextBool(140))
            {
                _advanceTimer = 180; //3 seconds of relentless
                _advanceWallTicks = 0;
                _cowardRingCompressionTimer = CowardRingCompressionTotalTicks;
                SetAttackLabel("Unbroken Advance", 190);
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                        Dust d = Dust.NewDustPerfect(NPC.Center, DustID.Torch, vel, 40, default, 1.7f);
                        d.noGravity = true;
                    }
                }
                //The march + a beat: expiring lands him in Idle right as the march ends, so he can
                //combo into ANY attack immediately — no winded window.
                EnterPhase(AttackPhase.NovaRecovery, 182);
                NPC.netUpdate = true;
            }
        }

        // ── Winged Plunge (the wings' second act) ────────────────────────────────
        // He spreads his wings and rises (dome-aware: the ascent caps itself below any ceiling it
        // finds), hangs a beat to aim, then DIVES at the player's marked position trailing golden
        // echoes — ending in a flaming greatsword slash where they stood. The wings only ever show
        // while airborne (ShowWingsWhenGrounded is false — his cape keeps the grounded silhouette).
        int _plungeCd = 360;
        int _plungeTimer;
        int _plungePhase;
        Vector2 _plungeTarget;
        float _plungeApexY;

        const float PlungeRiseHeight = 380f;  //default ascent — clears the dome's center, not its edges
        const float PlungeCeilingPad = 70f;   //stay this far under whatever ceiling the check finds
        const int PlungeGroundWaveReachTiles = 60;
        const float PlungeExplosionDamageMult = 0.75f;

        // Attack spec — Winged Plunge / bespoke _plungePhase sequence.
        // Weapon: EnemySwordOfGwyn, held by the existing two-handed puppet rig throughout the move.
        // Tell: dome-aware rise (<=70t), then a 20t hover/aim lock with gold dust and a release sound.
        // Dive: 17px/t toward the locked point for <=55t; twelve cached body echoes plus dense
        // GoldFlame body dust stream backward along the committed path.
        // Impact: instant sword hit, an 8t rollable 240px circular blast for 0.75x melee damage,
        // and two 7px/t ground waves travelling 60 tiles (960px) per side. The blast's bright core
        // is 300px wide, exactly 25% larger than its hitbox; smoke and rays are decorative residue.
        // Selection: Idle/CasualStroll, 150-900px, own 480-719t cooldown after use.
        // Multiplayer: selection, damage, and projectile spawning are server-authoritative; plunge
        // phase/target are synchronized so client-only afterimages and dust follow the same dive.

        void TickWingedPlunge()
        {
            if (_plungeTimer > 0)
            {
                RunWingedPlunge();
                return;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_plungeCd > 0)
            {
                _plungeCd--;
                return;
            }
            if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll)
            {
                return;
            }
            Player player = Main.player[NPC.target];
            float dist = NPC.Distance(player.Center);
            if (!player.dead && player.active && dist > 150f && dist < 900f && Main.rand.NextBool(75))
            {
                _plungeTimer = 1;
                _plungePhase = 0;
                //Dome-aware apex: rise the default height, but never within the pad of a ceiling
                float ceiling = FindCeilingY(NPC.Center, 30);
                _plungeApexY = NPC.Center.Y - PlungeRiseHeight;
                if (ceiling > 0f)
                {
                    _plungeApexY = System.Math.Max(_plungeApexY, ceiling + PlungeCeilingPad);
                }
                SetAttackLabel("Winged Plunge", 200);
                SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center); //wingbeat whoosh
                EnterPhase(AttackPhase.NovaRecovery, 210); //park the ground machine for the flight
                Flight?.RequestTakeoff();
                NPC.netUpdate = true;
            }
        }

        void RunWingedPlunge()
        {
            _plungeTimer++;
            Player player = Main.player[NPC.target];
            if (player.dead || !player.active)
            {
                EndPlunge();
                return;
            }

            switch (_plungePhase)
            {
                case 0: //Rise on the wings (overriding the flight controller's own intent)
                    NPC.velocity = new Vector2(NPC.velocity.X * 0.8f, -6.5f);
                    if (Main.rand.NextBool(2) && Main.netMode != NetmodeID.Server)
                    {
                        Dust d = Dust.NewDustPerfect(NPC.Bottom + Main.rand.NextVector2Circular(14f, 6f), DustID.GoldFlame, new Vector2(0f, 2f), 80, default, 1.2f);
                        d.noGravity = true;
                    }
                    if (NPC.Center.Y <= _plungeApexY || _plungeTimer > 70)
                    {
                        _plungePhase = 1;
                        _plungeTimer = 1;
                        NPC.netUpdate = true;
                    }
                    break;

                case 1: //Hang and aim — the read
                    NPC.velocity *= 0.85f;
                    NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
                    NPC.spriteDirection = NPC.direction;
                    if (_plungeTimer >= 20)
                    {
                        _plungeTarget = player.Center; //locked — reposition NOW
                        _plungePhase = 2;
                        _plungeTimer = 1;
                        SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f, Pitch = 0.4f }, NPC.Center);
                        NPC.netUpdate = true;
                    }
                    break;

                case 2: //The dive, trailing golden echoes
                {
                    Vector2 dir = (_plungeTarget - NPC.Center).SafeNormalize(Vector2.UnitY);
                    NPC.velocity = dir * 17f;
                    AfterimageTicks = Math.Max(AfterimageTicks, 3);
                    if (Main.netMode != NetmodeID.Server)
                    {
                        // Dense, mostly-gold motes cover the body while their low reverse velocity
                        // leaves a readable wake between the twelve cached puppet silhouettes.
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                            int type = Main.rand.NextBool(5) ? DustID.Torch : DustID.GoldFlame;
                            Dust d = Dust.NewDustPerfect(pos, type,
                                -dir * Main.rand.NextFloat(0.5f, 1.8f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                                70, new Color(255, 205, 86), Main.rand.NextFloat(0.75f, 1.3f));
                            d.noGravity = true;
                        }
                    }
                    Lighting.AddLight(NPC.Center, 1f, 0.8f, 0.3f);

                    bool arrived = Vector2.Distance(NPC.Center, _plungeTarget) < 48f;
                    if (arrived || NPC.collideX || NPC.collideY || _plungeTimer > 55)
                    {
                        //The flaming slash at the marked position
                        SetAttackLabel("Winged Plunge — Slash", 60);
                        SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.4f }, NPC.Center);
                        SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.1f }, NPC.Center);
                        Vector2 impactPosition = NPC.Bottom - new Vector2(0f, 20f);
                        UsefulFunctions.ScreenShake(impactPosition, 7f, 14);
                        if (Main.netMode != NetmodeID.Server)
                        {
                            for (int i = 0; i < 22; i++)
                            {
                                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                                Dust d = Dust.NewDustPerfect(impactPosition, type, vel, 40, default, 1.6f);
                                d.noGravity = true;
                            }
                        }
                        TryMeleeHit(reach: 140f);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 spawn = NPC.Center + new Vector2(NPC.direction * 24f, -8f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, Vector2.Zero,
                                ModContent.ProjectileType<Projectiles.Enemy.GwynFireArc>(), (int)(MeleeDamage * 0.7f), 4f, Main.myPlayer, NPC.direction, 2f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), impactPosition, Vector2.Zero,
                                ModContent.ProjectileType<Projectiles.Enemy.GwynDescentColumn>(),
                                Math.Max(1, (int)(MeleeDamage * PlungeExplosionDamageMult)), 6f, Main.myPlayer,
                                Projectiles.Enemy.GwynDescentColumn.WingedPlungeExplosionMode);
                            for (int direction = -1; direction <= 1; direction += 2)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), impactPosition, Vector2.Zero,
                                    ModContent.ProjectileType<Projectiles.Enemy.GwynGroundFireWave>(), (int)(MeleeDamage * 0.55f), 5f,
                                    Main.myPlayer, direction, PlungeGroundWaveReachTiles);
                            }
                        }
                        Flight?.RequestLand();
                        _plungePhase = 3;
                        _plungeTimer = 1;
                        NPC.netUpdate = true;
                    }
                    break;
                }

                case 3: //Landing recovery
                    NPC.velocity.X *= 0.8f;
                    if (_plungeTimer >= 40)
                    {
                        EndPlunge();
                    }
                    break;
            }
        }

        void EndPlunge()
        {
            Flight?.RequestLand();
            _plungeTimer = 0;
            _plungePhase = 0;
            NPC.noGravity = false;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _plungeCd = 480 + Main.rand.Next(240);
                NPC.netUpdate = true;
            }
        }

        ///<summary>World Y of the first solid tile BOTTOM above the point (scanning up), or -1 if none
        ///in range — the dome-clearance check for the Winged Plunge's ascent.</summary>
        static float FindCeilingY(Vector2 worldPos, int maxTilesUp)
        {
            int tx = (int)(worldPos.X / 16f);
            int ty = (int)(worldPos.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5)
            {
                return -1f;
            }
            for (int d = 2; d <= maxTilesUp; d++)
            {
                int y = ty - d;
                if (y <= 5)
                {
                    break;
                }
                Tile tile = Main.tile[tx, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return (y + 1) * 16f;
                }
            }
            return -1f;
        }

        // ── Firestorm (summoned rain — the anti-heal / anti-camp pressure) ───────
        // Not a swing: it's channeled magic that rains fireballs over the player's area for a couple
        // seconds while Gwyn keeps pursuing, so standing still to heal is answered. Own cooldown.
        int _firestormCd = 480;
        const int FirestormDamage = 45;

        void TickFirestorm()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_firestormCd > 0)
            {
                _firestormCd--;
                return;
            }
            Player player = Main.player[NPC.target];
            if (!player.dead && player.active && NPC.Distance(player.Center) > 400f && Main.rand.NextBool(160))
            {
                _firestormCd = 900 + Main.rand.Next(300);
                SetAttackLabel("Firestorm", 190);
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.RainsDeath"), 235, 130, 40);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GwynFirestorm>(), 0, 0f, Main.myPlayer, FirestormDamage, 150f);
            }
        }

        // ── Descent of the Sun (the epic set-piece) ──────────────────────────────
        // Summoned magic: a meteor of sunlight hangs high over the player, marks its impact, then
        // crashes with a huge explosion + crater fireballs. Long cooldown — it's the "oh no" moment.
        int _descentCd = 900;
        const int DescentDamage = 80;

        void TickDescent()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (_descentCd > 0)
            {
                _descentCd--;
                return;
            }
            Player player = Main.player[NPC.target];
            if (!player.dead && player.active && Main.rand.NextBool(220))
            {
                _descentCd = 1500 + Main.rand.Next(600);
                SetAttackLabel("Descent of the Sun", 130);
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Fury"), 255, 200, 60);
                Vector2 spawn = new Vector2(player.Center.X, player.Center.Y - 640f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GwynDescentMeteor>(), DescentDamage, 0f,
                    Main.myPlayer, DescentDamage, player.Center.X, ArenaCenter.Y);
            }
        }

        // ── Weapon-swing hooks. Phase 2 drives the bespoke reactive greatsword moveset above; the
        //    magic set-pieces (lightning spear etc.) layer on in Phase 3. ──────────────────────────
        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
            SpawnGwynSwordArc(ComboMotion.HorizontalSweep,
                Math.Max(1, GetMeleeSwingTicks(MeleeAttackTicks)));
        }

        // Gwyn's sword art reaches about 88px from the authored 14%/86% grip after its 0.75 draw
        // scale. VanillaSwordArc's 94px reference radius and 1.1 internal scale put its outer edge
        // at this same distance, so the crescent overlaps the blade rather than reading half-sized.
        const float GwynSwordArcRadius = 88f;
        const int LandingSwordArcTicks = 10;
        bool _swordArcSpawnedForStep;

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            _swordArcSpawnedForStep = false;
        }

        static bool IsGwynSwordSlashMotion(ComboMotion motion)
            => motion == ComboMotion.OverheadArc || motion == ComboMotion.UnderhandArc
            || motion == ComboMotion.HorizontalSweep || motion == ComboMotion.VerticalChop
            || motion == ComboMotion.GroundSlam || motion == ComboMotion.IaidoDraw
            || motion == ComboMotion.DoubleSpinSlam || motion == ComboMotion.Spin
            || motion == ComboMotion.LeapSlam || motion == ComboMotion.RisingUppercutLeap
            || motion == ComboMotion.ApexDiveCleave;

        void SpawnGwynSwordArc(ComboMotion motion, int duration, bool forceReverse = false)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            duration = Math.Max(1, duration);
            bool reverse = forceReverse || motion == ComboMotion.UnderhandArc
                || motion == ComboMotion.RisingUppercutLeap;
            int sweepDirection = (reverse ? -1 : 1) * NPC.direction;
            VanillaSwordArcSettings settings = new VanillaSwordArcSettings
            {
                Texture = VanillaSwordArcTexture.NightsEdge,
                Easing = VanillaSwordArcEasing.Linear,
                Duration = duration,
                StartAngle = PuppetWeaponDirection.ToRotation(),
                SweepAngle = sweepDirection * MathHelper.Pi,
                Radius = GwynSwordArcRadius,
                Opacity = 0.62f,
                FadeInFraction = Math.Min(0.12f, 2f / duration),
                FadeOutFraction = Math.Min(0.35f, 5f / duration),
                AfterimageLag = MathHelper.PiOver4,
                AfterimageOpacity = 0.58f,
                BodyOpacity = 0.82f,
                CoreOpacity = 0.18f,
                DrawTipSparkle = false,
                TintWithWorldLighting = true,
                DarkColor = new Color(72, 8, 3),
                BodyColor = new Color(225, 61, 8),
                CoreColor = new Color(255, 178, 68),
                DrawCinderOverlay = true,
                CinderOverlayOpacity = 0.62f,
                CinderOverlayDarkColor = new Color(64, 8, 2),
                CinderOverlayFlameColor = new Color(255, 116, 14),
                CinderOverlayCoreColor = new Color(255, 236, 172),
                DustType = -1,
                DustCount = 0,
                TrackPuppetBlade = true,
                EnableCollision = false,
            };

            VanillaSwordArc.SpawnForNPC(NPC.GetSource_FromAI(), NPC, 0, 0f, Main.myPlayer,
                settings, Vector2.Zero, hostile: false);
        }

        protected override void OnMeleeComboTelegraphTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboTelegraphTick(combo, step, elapsed, total);
            if (combo.Name != CinderfallName || Main.dedServ)
            {
                return;
            }

            float progress = MathHelper.Clamp(elapsed / (float)Math.Max(1, total - 1), 0f, 1f);
            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            Vector2 bladeTip = PuppetWeaponTipPosition(bladeReach);
            Lighting.AddLight(bladeTip, 1.15f * progress, 0.35f * progress, 0.06f);

            // Ember charge converges onto the raised sword tip throughout the 44-tick tell. The
            // tighter, brighter final frames make the release readable without extending the timer.
            if (elapsed % 2 == 0)
            {
                float radius = MathHelper.Lerp(30f, 8f, progress);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                    int dustType = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                    Dust dust = Dust.NewDustPerfect(bladeTip + offset, dustType,
                        -offset * MathHelper.Lerp(0.06f, 0.16f, progress), 70, default,
                        MathHelper.Lerp(0.85f, 1.45f, progress));
                    dust.noGravity = true;
                }
            }

            if (elapsed == 0)
            {
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.65f, Pitch = -0.35f }, bladeTip);
            }
            else if (elapsed == total - 10)
            {
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = 0.15f }, bladeTip);
            }
        }

        ///<summary>Each swing trails cinder-fire; the heavier ones (reach ≥ 1.15) fling a fire
        ///crescent forward so melee reaches a tile or two past the blade.</summary>
        protected override void OnMeleeComboAttackTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            float progress = elapsed / (float)Math.Max(1, total - 1);
            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;

            // The pursuit run is a CARRY, not a swing: the blade is held low and trailing and deals
            // no damage. Drawing the slash crescent across it would advertise a hit that does not
            // exist. It still shows embers, which is the whole point of dragging a burning
            // greatsword — they just stream off the tip instead of leading it.
            if (step.Motion == ComboMotion.LowAxeRun)
            {
                EmitPursuitDragEmbers(bladeReach, elapsed);
                return;
            }

            bool landingTimedSlash = step.Motion == ComboMotion.LeapSlam
                && (WrathFlurrySwinging || CinderingLeapSwinging);
            if (!_swordArcSpawnedForStep && !landingTimedSlash && elapsed == 0
                && step.DamageMult > 0f && IsGwynSwordSlashMotion(step.Motion))
            {
                int duration = step.Motion == ComboMotion.RisingUppercutLeap
                    ? Math.Min(total, Math.Max(1, GetMeleeSwingTicks(0)))
                    : total;
                SpawnGwynSwordArc(step.Motion, duration, combo.Name == "Backhand Step");
                _swordArcSpawnedForStep = true;
            }

            // Past the step's hit window the blade is harmless. The tracked sprite and its cinder
            // material still finish the real visual swing, but damaging-looking ember spray stops.
            bool followingThrough = step.HitWindowEnd > 0f && progress > step.HitWindowEnd;
            if (!followingThrough)
                EmitSwingFireDust(bladeReach, elapsed);

            // Leap hits resolve on landing; emitting their fire at takeoff would contradict the
            // telegraph. The rising uppercut is the opposite case: its blade sweep is over within a
            // single weapon animation and the rest of its 150-tick step is the ascent and the
            // falling hold, so the mid-phase default would throw the crescent from mid-air long
            // after the hit had already resolved.
            if (step.Motion == ComboMotion.RisingUppercutLeap)
            {
                if (elapsed == 4)
                {
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.1f }, NPC.Center);
                    EmitComboCinders(step);
                }
            }
            else if (step.Motion != ComboMotion.LeapSlam
                && elapsed == (step.Ease == SwingEaseStyle.Weighted ? step.EaseInTicks : total / 2))
            {
                EmitComboCinders(step);
            }
        }

        protected override void OnLandingTimedLeapSlamSwingTick(MeleeComboStep step, float progress)
        {
            if (_swordArcSpawnedForStep || step.DamageMult <= 0f)
                return;

            int remainingSwingTicks = Math.Max(5,
                (int)Math.Ceiling((1f - MathHelper.Clamp(progress, 0f, 1f)) * LandingSwordArcTicks));
            SpawnGwynSwordArc(step.Motion, remainingSwingTicks);
            _swordArcSpawnedForStep = true;
        }

        void EmitSwingFireDust(float bladeReach, int elapsed)
        {
            if (Main.netMode == NetmodeID.Server || elapsed % 2 != 0)
                return;

            Vector2 hand = PuppetHandPosition;
            Vector2 tip = PuppetWeaponTipPosition(bladeReach);
            Vector2 direction = (tip - hand).SafeNormalize(new Vector2(NPC.direction, 0f));
            for (int i = 0; i < 3; i++)
            {
                Vector2 position = Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.38f, 1f))
                    + Main.rand.NextVector2Circular(6f, 6f);
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                Dust dust = Dust.NewDustPerfect(position, type,
                    direction * Main.rand.NextFloat(1.2f, 2.8f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    60, default, Main.rand.NextFloat(1.05f, 1.55f));
                dust.noGravity = true;
            }
        }

        ///<summary>Embers shed by the greatsword dragged low and behind during Sunlight Pursuit.
        ///They fall off the blade and drift BACKWARD out of his run, so the trail marks where he has
        ///been — the opposite read from a swing's dust, which leads the edge toward where it will
        ///hit. Emitted every 3rd tick so a run of arbitrary length stays cheap.</summary>
        void EmitPursuitDragEmbers(float bladeReach, int elapsed)
        {
            if (Main.netMode == NetmodeID.Server || elapsed % 3 != 0)
                return;

            Vector2 hand = PuppetHandPosition;
            Vector2 tip = PuppetWeaponTipPosition(bladeReach);
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.55f, 1f))
                    + Main.rand.NextVector2Circular(5f, 5f);
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                Vector2 velocity = new Vector2(-NPC.direction * Main.rand.NextFloat(0.8f, 2.2f),
                    Main.rand.NextFloat(-1.1f, -0.2f));
                Dust dust = Dust.NewDustPerfect(position, type, velocity,
                    60, default, Main.rand.NextFloat(0.9f, 1.45f));
                dust.noGravity = true;
            }

            Lighting.AddLight(tip, 0.9f, 0.45f, 0.12f);
        }

        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            base.OnComboStepCompleted(step);
            // Only a real landing gets the leap's impact crescent. A timed-out airborne leap keeps
            // its normal recovery without producing a disconnected ground effect.
            if (step.Motion == ComboMotion.LeapSlam && NPC.velocity.Y == 0f)
            {
                EmitComboCinders(step);
            }
            _swordArcSpawnedForStep = false;
        }

        void EmitComboCinders(MeleeComboStep step)
        {

            // Fire dust follows the same authored hand-to-tip line as collision and slash VFX.
            if (Main.netMode != NetmodeID.Server)
            {
                float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
                Vector2 bladeTip = PuppetWeaponTipPosition(bladeReach);
                for (int i = 0; i < 5; i++)
                {
                    int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                    int dust = Dust.NewDust(bladeTip - new Vector2(8f, 20f), 16, 40, type, NPC.direction * 1.5f, 0f, 60, default, 1.4f);
                    Main.dust[dust].noGravity = true;
                }
            }

            // Heavy swings throw a reach crescent (vertical bias tilts it for overhead/rising arcs).
            if (step.ReachMult >= 1.15f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float vBias = step.Motion switch
                {
                    ComboMotion.OverheadArc => 3f,
                    ComboMotion.GroundSlam => 3f,
                    ComboMotion.UnderhandArc => -3f,
                    //The pursuit finisher tears upward through the target, so its crescent rises too.
                    ComboMotion.RisingUppercutLeap => -3f,
                    _ => 0f,
                };
                float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
                Vector2 bladeTip = PuppetWeaponTipPosition(bladeReach);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), bladeTip, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GwynFireArc>(), (int)(MeleeDamage * step.DamageMult * 0.6f), 2f, Main.myPlayer, NPC.direction, vBias);
            }
        }

        protected override void DoRangedAttack()
        {
            // No ranged weapon; the fire/lightning magic will be its own state-machine attacks.
        }

        // ── Spear of the First Sun (the marquee lightning attack) ────────────────
        // Mapped onto the base HomingVolley template. Gwyn launches into a distance-scaled forward
        // jump during the raised telegraph and releases at its end, near the apex. The two-stage
        // payload (contact explosion → delayed ground bolt → floor electricity) lives entirely in
        // the GwynLightningSpear → GwynLightningStrike → GwynFloorSpark projectile chain.
        protected override bool  CanHomingVolley             => true;
        protected override float HomingVolleyMinRange         => 200f;
        protected override float HomingVolleyMaxRange         => 1400f;   // works at nearly any range — it's a signature
        protected override int   HomingVolleyChance           => 8;
        protected override int   HomingVolleyCooldownAfterUse => 420;
        protected override int   HomingVolleyDodgebackTicks   => 1;       // state handoff only; the telegraph owns the jump
        protected override float HomingVolleyDodgebackSpeed   => 0f;
        protected override int   HomingVolleySwingTelegraphTicks => 32;
        protected override int   HomingVolleySwingTicks       => 8;
        protected override float HomingVolleyFireProgress     => 0f;      // release exactly as the telegraph ends
        protected override int   HomingVolleyRecoveryTicks    => 45;
        protected override bool  UseRaisedHomingVolleyHoldoutPose => true;

        const int LightningSpearDamage = 50;
        const int SpearFollowupTelegraphTicks = 14;
        const float SpearMinimumStandoff = 340f;
        bool _spearJumpActive;
        int _spearThrowsThisJump;
        int _spearFollowupsRemaining;
        float _spearAirTargetSpeed;

        void TickSpearJumpChoreography()
        {
            if (Phase == AttackPhase.HomingVolleyDodgeback)
            {
                _spearJumpActive = false;
                _spearThrowsThisJump = 0;
                _spearFollowupsRemaining = 0;
                _spearAirTargetSpeed = 0f;
                return;
            }

            bool telegraphing = Phase == AttackPhase.HomingVolleySwingTelegraph;
            bool throwing = Phase == AttackPhase.HomingVolleySwing;
            if (!telegraphing && !throwing)
            {
                // The base state enters recovery at the end of the throw. Redirect immediately to
                // the shorter second telegraph so there is no recovery frame between releases.
                if (Phase == AttackPhase.HomingVolleyRecovery && _spearJumpActive
                    && _spearFollowupsRemaining > 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    _spearFollowupsRemaining--;
                    EnterPhase(AttackPhase.HomingVolleySwingTelegraph, SpearFollowupTelegraphTicks);
                    NPC.netUpdate = true;
                }
                else if (Phase == AttackPhase.HomingVolleyRecovery)
                {
                    _spearJumpActive = false;
                }
                return;
            }

            Player target = Main.player[NPC.target];
            if (target == null || !target.active || target.dead)
                return;

            if (!_spearJumpActive && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float distance = Math.Abs(target.Center.X - NPC.Center.X);
                float distanceFactor = MathHelper.Clamp((distance - 240f) / 760f, 0f, 1f);
                float launchSpeedY = MathHelper.Lerp(8.8f, 10.4f, distanceFactor);
                float approachFactor = MathHelper.Clamp((distance - SpearMinimumStandoff) / 520f, 0f, 1f);
                int direction = target.Center.X < NPC.Center.X ? -1 : 1;

                _spearAirTargetSpeed = direction * MathHelper.Lerp(0f, 4.2f, approachFactor);
                NPC.noGravity = false;
                NPC.velocity = new Vector2(_spearAirTargetSpeed * 0.55f, -launchSpeedY);
                _spearJumpActive = true;
                NPC.netUpdate = true;
            }

            if (_spearJumpActive && NPC.velocity.Y != 0f)
            {
                float horizontalGap = Math.Abs(target.Center.X - NPC.Center.X);
                float desiredSpeed = horizontalGap > SpearMinimumStandoff ? _spearAirTargetSpeed : 0f;
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desiredSpeed, 0.12f);
            }

            // Aim remains live through each telegraph and locks only when DoHomingVolleyFire runs.
            if (telegraphing)
            {
                int direction = target.Center.X < NPC.Center.X ? -1 : 1;
                NPC.direction = direction;
                NPC.spriteDirection = direction;
                EmitSpearTelegraphVFX(target);
            }
        }

        void EmitSpearTelegraphVFX(Player target)
        {
            if (Main.dedServ)
                return;

            Vector2 hand = PuppetHandPosition;
            Vector2 aim = (target.Center - hand).SafeNormalize(new Vector2(NPC.direction, 0f));
            Vector2 bladePos = hand + aim * 42f;
            if (Main.GameUpdateCount % 2UL == 0UL)
            {
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                Dust dust = Dust.NewDustPerfect(bladePos + Main.rand.NextVector2Circular(8f, 8f),
                    type, Vector2.Zero, 60, default, Main.rand.NextFloat(1.2f, 1.8f));
                dust.noGravity = true;
            }
            Lighting.AddLight(bladePos, 0.7f, 0.6f, 0.25f);
        }

        protected override bool DrawSpecialHeldWeapon(ref PlayerDrawSet drawInfo)
        {
            // Lord's Embrace is deliberately bare-handed. Returning true tells PuppetNPC that this
            // phase supplied its own held-weapon presentation, so the greatsword is not drawn over
            // the outstretched grabbing arm.
            if (IsDashGrabSequence)
                return true;

            bool bareHandGrasp = Phase == AttackPhase.TendrilTelegraph
                || Phase == AttackPhase.TendrilReach;
            if (bareHandGrasp)
                return true;

            bool spearPhase = Phase == AttackPhase.HomingVolleyDodgeback
                || Phase == AttackPhase.HomingVolleySwingTelegraph
                || Phase == AttackPhase.HomingVolleySwing;
            if (!spearPhase)
                return false;

            if (Phase == AttackPhase.HomingVolleySwing)
            {
                float release = HomingVolleySwingTicks > 0
                    ? 1f - PhaseTimer / (float)HomingVolleySwingTicks
                    : 1f;
                if (release >= HomingVolleyFireProgress)
                    return true;
            }

            Player target = Main.player[NPC.target];
            Vector2 hand = PuppetHandPosition;
            Vector2 aim = target != null && target.active && !target.dead
                ? (target.Center - hand).SafeNormalize(new Vector2(NPC.direction, 0f))
                : new Vector2(NPC.direction, 0f);

            Texture2D texture = ModContent.Request<Texture2D>(
                "tsorcRevamp/Projectiles/Enemy/Gwyn/GwynLightningSpear").Value;
            int frameHeight = texture.Height / GwynLightningSpearFrames.FrameCount;
            int frameIndex = (int)(Main.GameUpdateCount / 5UL) % GwynLightningSpearFrames.FrameCount;
            Rectangle frame = new Rectangle(0, frameIndex * frameHeight, texture.Width, frameHeight);
            Vector2 origin = GwynLightningSpearFrames.GetVisualOrigin(frameIndex);
            //Rotation already points the authored spear toward either aim direction. Mirroring it
            //on right-facing holds reverses the spearhead, so the held version never needs a flip.
            SpriteEffects effects = SpriteEffects.None;

            drawInfo.DrawDataCache.Add(new DrawData(
                texture, hand - Main.screenPosition, frame, Color.White, aim.ToRotation(), origin,
                NPC.scale * 0.6f, effects, 0));
            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);

            if (!IsDashGrabSequence || LordEmbraceOrbActive || Main.dedServ)
                return;

            float scale;
            if (Phase == AttackPhase.PierceTelegraph)
            {
                float progress = 1f - PhaseTimer / (float)Math.Max(1, PierceTelegraphTicks);
                scale = MathHelper.Lerp(0.86f, 1.18f, MathHelper.SmoothStep(0f, 1f, progress));
            }
            else
            {
                scale = 1.22f;
            }

            Vector2 handPosition = PuppetHandPosition + new Vector2(NPC.direction * 4f, -2f);
            float rotation = NPC.direction > 0 ? 0f : MathHelper.Pi;
            Projectiles.Enemy.GwynFlameGrasp.DrawHandAura(handPosition, rotation,
                scale * NPC.scale, 1f, drawUnblockableOutline: true);
            Lighting.AddLight(handPosition, 1.15f, 0.24f, 0.06f);
        }

        ///<summary>Lightning gathers on the raised blade through the windup — the telegraph read.</summary>
        protected override void DoHomingVolleySwingTick(int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }
            if (total > 0 && elapsed > total * HomingVolleyFireProgress)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            Vector2 hand = PuppetHandPosition;
            Vector2 aim = target != null && target.active && !target.dead
                ? (target.Center - hand).SafeNormalize(new Vector2(NPC.direction, 0f))
                : new Vector2(NPC.direction, 0f);
            Vector2 bladePos = hand + aim * 42f;
            for (int i = 0; i < 2; i++)
            {
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                Dust d = Dust.NewDustPerfect(bladePos + Main.rand.NextVector2Circular(8f, 8f), type, Vector2.Zero, 60, default, Main.rand.NextFloat(1.2f, 1.8f));
                d.noGravity = true;
            }
            Lighting.AddLight(bladePos, 0.7f, 0.6f, 0.25f);
        }

        ///<summary>Hurl the lightning spear at the player's locked position.</summary>
        protected override void DoHomingVolleyFire()
        {
            SetAttackLabel("Spear of the First Sun", 120);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f, Pitch = 0.1f }, NPC.Center); // lightning cast
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetHandPosition;
            Vector2 vel = (target.Center - origin).SafeNormalize(new Vector2(NPC.direction, 0f)) * 15f;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                ModContent.ProjectileType<Projectiles.Enemy.GwynLightningSpear>(), LightningSpearDamage, 4f, Main.myPlayer);

            _spearThrowsThisJump++;
            if (_spearThrowsThisJump == 1)
            {
                //Below half health, the signature cast is a guaranteed 3-4 throw sequence. Before
                //that breakpoint it retains the lighter 1-2 throw pattern.
                _spearFollowupsRemaining = NPC.life <= NPC.lifeMax * 0.50f
                    ? Main.rand.Next(2, 4)
                    : (Main.rand.NextBool(2) ? 1 : 0);
            }
            NPC.netUpdate = true;
        }

        // ── Cinder Nova (point-blank space-maker + greed punish) ─────────────────
        // Base Nova template: root, gather fire inward over the charge, then detonate the expanding
        // ring. Triggers at three HP thresholds (one-shot each) and, otherwise, occasionally when the
        // player is point-blank so hugging him during a slow moment is answered.
        protected override bool CanNova           => true;
        protected override int  NovaChargeTicks    => 60;   // shorter than Artorias' — a snappier boss beat
        protected override int  NovaBlastHoldTicks => 20;
        protected override int  NovaRecoveryTicks  => 45;

        bool _nova75, _nova45, _nova20;
        int _novaPointBlankCd;

        protected override bool ShouldTriggerNova()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return false;
            }
            float frac = (float)NPC.life / NPC.lifeMax;
            if (frac <= 0.75f && !_nova75) { _nova75 = true; return true; }
            if (frac <= 0.45f && !_nova45) { _nova45 = true; return true; }
            if (frac <= 0.20f && !_nova20) { _nova20 = true; return true; }
            //Point-blank greed punish, on its own cooldown
            if (_novaPointBlankCd > 0) { _novaPointBlankCd--; return false; }
            if (NPC.Distance(Main.player[NPC.target].Center) < 140f && Main.rand.NextBool(240))
            {
                _novaPointBlankCd = 600;
                return true;
            }
            return false;
        }

        protected override void DoNovaChargeTick(int elapsed, int total)
        {
            if (elapsed == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GwynCinderNovaTelegraph>(), 0, 0f,
                    Main.myPlayer, NPC.whoAmI, total);
            }
            if (Main.dedServ)
            {
                return;
            }
            //Fire spiralling INWARD to his blade — the classic "get away" read
            float progress = total > 0 ? elapsed / (float)total : 1f;
            int count = 2 + (int)(progress * 4f);
            for (int i = 0; i < count; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = MathHelper.Lerp(170f, 20f, progress) + Main.rand.NextFloat(20f);
                Vector2 pos = NPC.Center + ang.ToRotationVector2() * radius;
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                Dust d = Dust.NewDustPerfect(pos, type, Vector2.Zero, 60, default, 1.3f);
                d.noGravity = true;
                d.velocity = (NPC.Center - pos) * 0.07f;
            }
            Lighting.AddLight(NPC.Center, progress * 1.2f, progress * 0.6f, progress * 0.2f);
        }

        protected override void DoNovaBlast()
        {
            SetAttackLabel("Cinder Nova", 90);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.3f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 12f, 22);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.GwynCinderNova>(), NovaDamage, 10f, Main.myPlayer, 840f);
        }

        const int NovaDamage = 60;

        // ── Sunlight Spears Volley (mid-range answer) ────────────────────────────
        // Base AbyssShard template: a short raise, then a sequence of "fire events". Each event drops
        // a couple of GwynSolarSpearNode orbs in an arc above the player — each orb telegraphs, then
        // fires a spear at the player. The staggered events + per-orb telegraph make a readable,
        // rolling rhythm of spears rather than a wall.
        protected override bool  CanAbyssShard              => true;
        protected override float AbyssShardMinRange         => 220f;
        protected override float AbyssShardMaxRange         => 1200f;
        protected override int   AbyssShardChance           => 8;
        protected override int   AbyssShardCooldownAfterUse => 360;
        protected override int   AbyssShardTelegraphTicks   => 25;

        const int SunlightSpearDamage = 40;
        const int SpearVolleyEvents = 3;

        protected override int NextAbyssShardDelay(int completedFireIndex)
            => completedFireIndex < SpearVolleyEvents - 1 ? 14 : -1;

        protected override void DoAbyssShardFire(int fireIndex)
        {
            SetAttackLabel("Sunlight Spears Volley", 80);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = 0.2f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            //Two orbs per event, arced above the player and spread horizontally by event index
            for (int i = 0; i < 2; i++)
            {
                float spreadX = (fireIndex - 1) * 90f + (i == 0 ? -55f : 55f);
                Vector2 pos = target.Center + new Vector2(spreadX, -220f - Main.rand.NextFloat(40f));
                Projectile.NewProjectile(NPC.GetSource_FromThis(), pos, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.GwynSolarSpearNode>(), SunlightSpearDamage, 2f, Main.myPlayer, 20f);
            }
        }

        // ── Lord's Grasp (grab — the shield-turtle / spacing punish) ─────────────
        // Attack spec — Lord's Grasp / TendrilTelegraph / GwynFlameGrasp.
        // Tell: bare hand and gathering gold flame for 60t; true line of sight is required both to
        // select and release. Reach: 0-1,200px; launch speed scales 13->26px/t with distance, so a
        // maximum-range target is reached in 47 ticks. Solid tiles intercept the hand and send it
        // back empty. The 180t reach phase covers a max-range flight, 44t grasp, and full retraction.
        // Player effect: on-hit fire plus the existing 44t pull into 72px finisher range; tile impact
        // only retracts, never damages through the wall. Multiplayer: authoritative launch/collision.
        protected override bool CanTendrilGrab
        {
            get
            {
                if (!NPC.HasValidTarget)
                {
                    return false;
                }

                Player target = Main.player[NPC.target];
                return target.active && !target.dead && Collision.CanHitLine(
                    GraspHandPosition, 1, 1, target.Center, 1, 1);
            }
        }
        protected override float TendrilMinRange         => 0f;
        protected override float TendrilMaxRange         => 1200f;
        protected override int   TendrilChance            => 5;
        protected override int   TendrilCooldownAfterUse  => 480;
        protected override int   TendrilTelegraphTicks    => 60;
        protected override int   TendrilReachTicks        => 180;
        protected override int   TendrilSwingArcTicks     => 22;
        protected override int   TendrilSwingHoldTicks    => 6;
        protected override int   TendrilSwingTicks        => 20;
        protected override int   TendrilRecoveryTicks     => 38;

        const int GraspDamage = 70;
        internal Vector2 GraspHandPosition => PuppetHandPosition;

        protected override void DoTendrilTelegraphTick(int elapsed)
        {
            if (elapsed == 0)
            {
                SetAttackLabel("Lord's Grasp", TendrilTelegraphTicks + TendrilReachTicks
                    + TendrilSwingArcTicks + TendrilSwingHoldTicks + TendrilSwingTicks);
            }
            if (Main.dedServ)
            {
                return;
            }
            Vector2 handPos = GraspHandPosition;
            float progress = MathHelper.Clamp(elapsed / (float)Math.Max(1, TendrilTelegraphTicks - 1), 0f, 1f);
            float collapse = MathHelper.SmoothStep(0f, 1f, progress);
            float radius = MathHelper.Lerp(46f, 2f, collapse);
            int count = 2 + (int)(progress * 3f);
            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 position = handPos + angle.ToRotationVector2() * radius
                    + Main.rand.NextVector2Circular(2f, 2f);
                int type = Main.rand.NextBool(4) ? DustID.GoldCoin : DustID.GoldFlame;
                Dust d = Dust.NewDustPerfect(position, type, (handPos - position) * 0.12f,
                    45, new Color(255, 214, 92), Main.rand.NextFloat(0.85f, 1.25f));
                d.noGravity = true;
            }
            Lighting.AddLight(handPos, 0.8f, 0.4f, 0.1f);
        }

        protected override void DoTendrilLaunch()
        {
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            Vector2 origin = GraspHandPosition;
            if (!target.active || target.dead || !Collision.CanHitLine(origin, 1, 1, target.Center, 1, 1))
            {
                return;
            }

            const float graspMaxRange = 1200f;
            const float baseGraspSpeed = 13f;
            float distanceFactor = MathHelper.Clamp(Vector2.Distance(origin, target.Center) / graspMaxRange, 0f, 1f);
            float speed = MathHelper.Lerp(baseGraspSpeed, baseGraspSpeed * 2f, distanceFactor);
            Vector2 vel = UsefulFunctions.Aim(origin, target.Center, speed);
            int graspIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                ModContent.ProjectileType<Projectiles.Enemy.GwynFlameGrasp>(), GraspDamage, 8f, Main.myPlayer, NPC.whoAmI);
            Projectiles.tsorcGlobalProjectile.SetDefenseTraits(
                graspIndex, AttackDefenseTraits.BypassesActiveShield);
        }

        protected override void DoTendrilSwing()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: 110f);
            SpawnGwynSwordArc(ComboMotion.UnderhandArc, TendrilSwingTicks, forceReverse: true);
        }

        ///<summary>KEPT (1000px): the First Flame shields him from cowards — full defense lock beyond
        ///the blue ring, the old fightable value inside it.</summary>
        void TickDefenseRing()
        {
            UsefulFunctions.DustRing(NPC.Center, (int)DefenseRingRadius, DustID.BlueTorch, 20, 1f);

            if (protectedHoldTimer > 0)
            {
                protectedHoldTimer--;
            }
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > DefenseRingRadius)
            {
                NPC.defense = 9999;
                if (protectedHoldTimer <= 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Protected"), 175, 75, 255);
                    protectedHoldTimer = 200;
                }
            }
            else if (protectedHoldTimer <= 0) // preserve the 9999 penalty until the hold expires
            {
                //Unbroken Advance: the march itself is armored — 999 defense until the 3s ends
                NPC.defense = _advanceTimer > 0 ? 999 : FightableDefense;
            }
        }

        ///<summary>KEPT (2000px): the spawn-anchored outer flame wall. Flight is torn down anywhere
        ///inside it; fleeing beyond it brings the Coward's Affliction after a 90-tick grace. Unbroken
        ///Advance temporarily compresses the radius, but never moves the safe-zone centre.</summary>
        void TickCowardRing()
        {
            Player player = Main.player[NPC.target];
            float ringRadius = CurrentCowardRingRadius;
            float playerDistance = Vector2.Distance(player.Center, ArenaCenter);

            if (playerDistance < ringRadius)
            {
                player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);
            }

            UsefulFunctions.DustRing(ArenaCenter, (int)ringRadius, DustID.RedsWingsRun, 1, 1f);
            UsefulFunctions.DustRing(ArenaCenter, (int)ringRadius, DustID.Torch, 10, 1f);
            UsefulFunctions.DustRing(ArenaCenter, (int)ringRadius, DustID.RedTorch, 5, 2f);
            UsefulFunctions.DustRing(ArenaCenter, (int)ringRadius, DustID.Firefly, 100, -3f);

            if (playerDistance > ringRadius)
            {
                cowardGraceTimer--;
                if (cowardGraceTimer <= 0)
                {
                    player.AddBuff(ModContent.BuffType<CowardsAffliction>(), 1 * 30, false);
                    if (!announcedCoward)
                    {
                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Coward"), 235, 199, 23); //deep yellow
                        announcedCoward = true;
                    }
                }
            }
            else
            {
                cowardGraceTimer = 90;
                announcedCoward = false;
            }

            if (_cowardRingCompressionTimer > 0)
            {
                _cowardRingCompressionTimer--;
            }
        }

        ///<summary>Leave the arena and it starts raining. A steady, even drip of death orbs across a
        ///1000px band — ranged builds don't get a free fight, but every individual orb is dodgeable.
        ///
        ///This replaced three overlapping random-chance tiers that each dumped a burst of 3, 6 or 8
        ///orbs at once (1-in-400, 1-in-180 and 1-in-120 per tick). Rolling a burst gave the hazard a
        ///lumpy rhythm — long empty stretches, then an undodgeable wall arriving in a single frame —
        ///and because the tiers overlapped, two could fire on the same tick for 14 orbs at once. A
        ///fixed interval produces the same average pressure with none of the spikes.</summary>
        void TickRainOfDeath()
        {
            Player player = Main.player[NPC.target];
            if (player.dead || !player.active)
            {
                return;
            }

            if (Vector2.Distance(player.Center, ArenaCenter) <= RainOfDeathRange)
            {
                //Reset rather than freeze, so re-crossing the boundary always gets a full interval
                //of grace instead of an orb the instant a player clips the edge.
                _rainTimer = 0;
                _rainAnnounced = false;
                return;
            }

            if (!_rainAnnounced)
            {
                _rainAnnounced = true;
                string key = _wrathActive ? "NPCs.Gwyn.TidalWave" : "NPCs.Gwyn.RainsDeath";
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue(key), 175, 75, 255);
            }

            int interval = _wrathActive ? RainIntervalTicksWrath : RainIntervalTicks;
            if (++_rainTimer < interval)
            {
                return;
            }

            _rainTimer = 0;
            SpawnDeathRainOrb(player);
        }

        ///<summary>One orb, at a random point across the full band. The old helper took an offset
        ///plus a range and every caller but the first passed a negative offset equal to the range
        ///(-600/600, -800/800), which put the entire spread on the player's LEFT — the rain only
        ///ever fell to one side and running right was strictly safer. The band here is symmetric by
        ///construction.</summary>
        void SpawnDeathRainOrb(Player player)
        {
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Vortex, NPC.velocity.X, NPC.velocity.Y);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float spawnX = RainBandCenterX + Main.rand.NextFloat(-RainHalfWidth, RainHalfWidth);
                float spawnY = player.position.Y + RainSpawnHeight;
                //Slight horizontal drift so the column is not a perfectly vertical line of pixels.
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), 0.5f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(spawnX, spawnY), velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>(),
                    RainOfDeathDamage, 1f, Main.myPlayer);
            }

            Lighting.AddLight(NPC.Center, Color.White.ToVector3());
            SoundEngine.PlaySound(SoundID.Zombie53 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center);
        }

        ///<summary>KEPT (700px): standing in the Lord's presence — no wings, no grapple.</summary>
        void TickProximityDebuffs()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }
                if (NPC.Distance(player.Center) < ProximityDebuffRange)
                {
                    player.AddBuff(ModContent.BuffType<TornWings>(), 1 * 60, false);
                    player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 1 * 60, false);
                }
            }
        }

        //Phase 2: the 12-attack state machine, boss bag + SwordOfGwyn drop, BossChecklist entry,
        //and the spawn gating land once the attack proposal is approved.

#region Gore
        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gwyn Gore 1").Type, 1.5f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gwyn Gore 2").Type, 1.5f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gwyn Gore 3").Type, 1.5f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gwyn Gore 2").Type, 1.5f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gwyn Gore 3").Type, 1.5f);
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.Orange));

            }
            tsorcRevampWorld.InitiateTheEnd();
        }
        #endregion
    }
}
