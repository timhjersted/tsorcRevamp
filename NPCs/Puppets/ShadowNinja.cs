using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Melee;
using tsorcRevamp.Content.Items.Armor.Melee.ShadowNinjaSet;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.ShadowSickle;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Content.Items.Weapons.Ranged.Bows;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;
using EnemyCaltrop = tsorcRevamp.Content.Items.Weapons.Enemy.EnemyCaltrop;

namespace tsorcRevamp.NPCs.Puppets
{
    [AutoloadBossHead]
    public class ShadowNinja : PuppetNPC
    {
        private const string ThreeHitStandardName = "3-Hit Standard";
        private const float ThreeHitOverhandStart = -1.55f;
        private const float ThreeHitOverhandEnd = 2.36f;
        private const int ThreeHitSwingTicks = 36;
        private const int ThreeHitEaseInTicks = 10;
        private const int ThreeHitEaseOutTicks = 26;
        private const float ThreeHitEaseOutDecay = 7f;
        private const float ThreeHitWindowEnd = 0.35f;
        private const int ThreeHitResetTicks = 30;
        private const int ThreeHitReverseTicks = 3;

        // Anti-whiff. The sickle is short: hitbox tip = ComboReachBase * 0.7 = 45px past the hand (visual
        // grip->tip is 47px at 1.25 scale), so contact ends ~75px centre to centre. The default engage
        // range (89px) started cuts that could only whiff; 70px starts them inside reach.
        private const float CloseEngageRange = 70f;
        // Presses in during the tell (only while the target stays on the committed side) and stops 40px out.
        private const float TellAdvanceSpeedMult = 0.75f;
        private const float TellAdvanceStopDistance = 40f;
        // Raised from the engine's 0.25: the wind-up flicked 71° at 23.5°/t (near strike speed, 31°/t) in
        // its first 9t, then stopped dead. 0.45 spreads that move so the strike stays the fastest beat.
        private const float WindupSettleFraction = 0.45f;

        // Step-in per cut (MeleeComboStep.StepInDistance): px travelled across the step, peaking on the strike
        // and decaying with the blade, so the body drives into the cut instead of planting. Quickslash
        // (7/28 k7) peaks at 36px / 6.3 = 5.7 px/t. Follow-up cuts are re-solved from the gap at the pause's
        // end, aiming to finish 34px from the target; 56px caps a 3-Hit cut (10/26 k7) at ~8 px/t.
        // A roll away (~110px) still clears every one.
        private const float OpeningCutStepIn = 36f;
        private const float FollowUpStepInMin = 12f;
        private const float FollowUpStepInMax = 56f;
        private const float FollowUpCloseTo = 34f;
        // Hand sits ~10px ahead of centre, plus half the 18px blade width: added to the hitbox reach when
        // judging whether a whiffed cut's follow-up can still connect.
        private const float FollowUpContactMargin = 19f;

        // ── Shadow Leap ──────────────────────────────────────────────────────────
        // Mid-range gap-closer: a landing-timed LeapSlam carried at the 3-Hit's raised pose (-1.55) that cuts
        // down to its low end (2.36, a 224° downswing) in 10 fully armed ticks, mid-air once past the apex with
        // the target inside ShadowLeapStrikeRange, otherwise on landing. After the planted beat it sometimes
        // leaps again from wherever it landed.
        // Tell:    20t authored x 1.75 = 35t on screen, raised sickle + grey smoke kick at the feet.
        // Leap:    up 9.5 px/t, forward cap TopSpeed x 1.7 = 5.7 px/t, ~63t flat airtime -> ~360px max travel.
        // Strike:  ShadowLeapStrikeRange 105 = hitbox 55px (ReachMult 1.2) + ~50px travelled in the downswing.
        // Planted: ShadowLeapLandingPauseTicks 36 holding the low pose (smoke wisps = "he can go again").
        //          The second leap's downswing is >= 36 + ~30t ascent after the first live window ends, far
        //          past the 30t post-roll gap, so each leap is rolled separately.
        // Select:  RangedStartOnly at 110-300px (CanSelectMeleeCombo); follow-up is a 45% server roll that
        //          also needs the target inside ReliableJumpStartRange.
        private const string ShadowLeapName = "Shadow Leap";
        private const int ShadowLeapTelegraphTicks = 20;
        private const int ShadowLeapTimeoutTicks = 90;
        private const int ShadowLeapLandingPauseTicks = 36;
        private const float ShadowLeapStrikeRange = 105f;
        private const float ShadowLeapDamageMult = 1.25f;
        private const float ShadowLeapReachMult = 1.2f;
        private const float ShadowLeapMinStartRange = 110f;
        private const float ShadowLeapMaxStartRange = 300f;
        private const int ShadowLeapFollowUpChance = 45;
        private static readonly Color ShadowLeapSmokeTint = new Color(70, 70, 80);

        private static MeleeComboStep ShadowLeapStep(int telegraphTicks, int postStepPause)
        {
            // AttackTicks is only the airborne safety timeout; the landing or in-range trigger ends the step.
            return new MeleeComboStep
            {
                Motion = ComboMotion.LeapSlam,
                TelegraphTicks = telegraphTicks,
                AttackTicks = ShadowLeapTimeoutTicks,
                PostStepPause = postStepPause,
                DamageMult = ShadowLeapDamageMult,
                ReachMult = ShadowLeapReachMult,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Smooth,
                HitWindowEnd = 1f,
                LeapStrikeRange = ShadowLeapStrikeRange,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };
        }

        // The shared Broadsword pool plus the leap. Step 2 is the conditional follow-up (ShouldContinueMeleeCombo).
        private static readonly MeleeCombo[] ShadowNinjaCombos = WeaponArchetypeTables.Broadsword.Concat(new[]
        {
            new MeleeCombo
            {
                Name = ShadowLeapName,
                BaseWeight = 70,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(150, 120, 255),
                CooldownAfterUse = 260,
                RecoveryTicks = 34,
                RangedStartOnly = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    ShadowLeapStep(ShadowLeapTelegraphTicks, ShadowLeapLandingPauseTicks),
                    ShadowLeapStep(0, 0),
                },
            },
        }).ToArray();

        // ── Flip set-pieces (Custom phase stages) ────────────────────────────────
        // The body really somersaults: PuppetBodySpinRotation turns every draw piece about the visual centre, and
        // ModifyFrontBladeCapsule turns the blade hitbox the same way, so the live blade is where it is drawn.
        // Front Flip (anti-air). Picked when the target is airborne 56px+ above him, within 260px across.
        //   Tell 20t: sickle cocked behind the head (-1.55) + smoke kick at the feet.
        //   Leap: solved to rise to the target's height 10t ahead (+24px), up to 12 px/t; forward capped 8 px/t.
        //   Flip: within 96px of the target, or once past the apex, the blade snaps level-forward over 6t and the
        //         body turns one forward somersault in 26t (fast start, settling finish). Live for all 26t.
        //   Land: 26t planted, blade easing back to the carry. Cooldown 300t.
        // Back Flip (opener). Picked when the target is on the ground within 80px and there is room behind.
        //   Tell 22t: blade dips to the low pose (2.36) + smoke kick.
        //   Cut: weighted underswing 2.36 -> -1.55 (in 7, out 16, k 6; peak on tick 7), live from tick 0.
        //   Launch on cut tick 9: back 5 px/t, up 8.5 px/t (~57t airborne, ~280px back). The underswing finishes in the
        //         air while the body turns one backward somersault in 32t; the blade stays live until it ends.
        //   Smoke: at air tick 14 he tosses 3 smoke caltrops at the target -> 12 smoke clouds (Darkness + Slow, 4s).
        //   Land: 30t planted. Cooldown 420t.
        // Fairness: the live windows (26t / 41t) outlast a 22t roll, but the blade is carried away from the target on
        // both - the back flip retreats ~5 px/t, the front flip passes through - so a roll away or under clears it.
        // MP: the server picks, launches and decides the front flip's somersault start (snapshot); every peer runs the
        // stage clock, velocities (from synced _flipVx) and the spin, and only the server arms and resolves the blade.
        private enum FlipStage : byte
        {
            None,
            FrontTell,
            FrontLeap,
            BackTell,
            BackCut,
            BackAir,
            Landing,
            ShadeVanish,
            ShadeDive,
        }

        private const float FlipGravity = 0.3f;
        private const float CarryPose = -0.3f;
        private const float FlipCockedPose = ThreeHitOverhandStart;
        private const float FlipLowPose = ThreeHitOverhandEnd;
        private const float FlipExtendedPose = MathHelper.PiOver4;   // level forward
        private const int FlipAirTimeoutTicks = 100;
        private const int FlipLandingTicks = 26;
        private const int BackFlipLandingTicks = 30;
        private const float FlipBladeReachMult = 1.1f;

        private const int FrontFlipTellTicks = 20;
        private const float FrontFlipMinHeight = 56f;
        private const float FrontFlipMaxRange = 260f;
        private const float FrontFlipLeadTicks = 10f;
        private const float FrontFlipRiseOvershoot = 24f;
        private const float FrontFlipMinRise = 64f;
        private const float FrontFlipMaxRise = 240f;
        private const float FrontFlipMaxUpSpeed = 12f;
        private const float FrontFlipMaxForwardSpeed = 8f;
        private const float FrontFlipTriggerRange = 96f;
        private const int FrontFlipSpinTicks = 26;
        private const int FlipBladeSnapTicks = 6;
        private const int FrontFlipCooldownTicks = 300;
        private const int FrontFlipPickDenominator = 3;

        private const int BackFlipTellTicks = 22;
        // Picked out to 150px now that the opener closes in: walk-in during the tell, then a shaped step-in on the cut.
        private const float BackFlipMaxRange = 150f;
        private const float BackFlipTellAdvanceSpeedMult = 0.8f;   // x TopSpeed 3.35 = 2.7 px/t
        private const float BackFlipTellStopDistance = 44f;
        private const float BackFlipCutCloseTo = 28f;
        private const float BackFlipCutStepInMin = 10f;
        private const float BackFlipCutStepInMax = 44f;
        // Integral of the step-in speed shape over the 9 pre-launch cut ticks: in/3 + (1 - e^(-k*2/out)) * out/k
        // = 7/3 + (1 - e^-0.75) * 16/6 = 3.74 ticks at peak, so the 44px cap peaks at 11.8 px/t (a short lunge into the cut).
        private const float BackFlipCutStepCurveTicks = 3.74f;
        private const int BackFlipCutEaseIn = 7;
        private const int BackFlipCutEaseOut = 16;
        private const float BackFlipCutDecay = 6f;
        private const int BackFlipLaunchTick = 9;
        private const float BackFlipBackSpeed = 5f;
        private const float BackFlipUpSpeed = 8.5f;
        private const int BackFlipSpinTicks = 32;
        private const int BackFlipCaltropTick = 14;
        private const int BackFlipCaltropCount = 3;
        private const float BackFlipCaltropSpread = 64f;
        private const float BackFlipClearance = 140f;
        private const int BackFlipCooldownTicks = 420;
        private const int BackFlipPickDenominator = 4;

        private FlipStage _flipStage;
        private int _flipStageTicks;
        private int _flipDir = 1;
        private float _flipVx;
        private int _flipSpinStart = -1;       // stage tick the front flip's somersault began; -1 = still rising
        private int _flipCutTicks;             // back flip underswing clock; runs across BackCut and BackAir
        private bool _flipCaltropsThrown;
        private float _backCutStepIn;          // px the back flip's cut steps in, solved by the server at the tell's end
        private float _flipWeaponRotation = CarryPose;
        private float _flipBodySpin;           // derived every tick on every peer, never synced
        private int _frontFlipCooldown;
        private int _backFlipCooldown;

        // ── Shadow Self (below 50% health) ───────────────────────────────────────
        // A Phase-dyed copy of the ninja (ShadowNinjaShade, bottom of this file): its own 15% HP pool, the boss's
        // damage, melee only - every combo, the leap and both flips. Hitting it never hurts the boss. Summoned
        // the moment he drops to 50%, then ShadeRespawnTicks (20s) after the last one dies; never two at once.
        // Its back flip ends in a smoke step instead of caltrops:
        //   Vanish: when the somersault completes (~apex) it bursts into smoke and is gone for 15t (no gravity,
        //           untouchable, not drawn).
        //   Reappear: on the target's far side from where it vanished (flank), 9 tiles across and 15 tiles up -
        //           falling back to 12/9 tiles up, then the near side, when that air is blocked or has no line to them.
        //   Dive: falls with the sickle cocked overhead, drifting to land on the target (fall time from the height,
        //           drift capped 8 px/t). Within 110px it cuts: weighted overhead -1.55 -> 2.36 (in 6, out 20, k 7),
        //           live ~10t. A dive that lands without getting close cuts on the landing instead.
        // MP: the server picks the reappear spot and moves the NPC (netUpdate); every peer runs the hide and dive clocks.
        protected const float ShadeHealthFraction = 0.15f;
        private const float ShadeSummonHealthFraction = 0.5f;
        private const int ShadeRespawnTicks = 20 * 60;
        private const int ShadowStepVanishTicks = 15;
        private const float ShadowStepAcross = 144f;
        private static readonly float[] ShadowStepHeights = { 240f, 192f, 144f };
        private const float ShadowDiveMaxDrift = 8f;
        private const float ShadowDiveStrikeRange = 110f;
        private const int ShadowDiveEaseIn = 6;
        private const int ShadowDiveEaseOut = 20;
        private const float ShadowDiveDecay = 7f;
        private const int ShadowDiveTimeoutTicks = 160;

        /// <summary>True on the Shadow Self copy: melee only, smoke-step back flip, no summoning of its own.</summary>
        protected virtual bool IsShadowShade => false;

        private int _shadeIndex = -1;          // server only: the boss's live Shadow Self, or -1
        private int _shadeRespawnTimer;        // server only

        // ── Sky Volley (bow) ─────────────────────────────────────────────────────
        // A secondary bow burst variant: hop, loose an arrow at the apex, hop again off the air - double or triple
        // jump - backpedalling 1.4 px/t. Each hop rises at 6.75 px/t under 0.3 gravity: apex 22.5t later, ~76px up.
        // Shot k fires at hop k's apex and hop k+1 launches on that same tick, so the shots are 22t apart (2t attack
        // + 20t pause pattern). 2 or 3 shots (50/50); the last hop falls during the 58t recovery. 45% of grounded bow
        // bursts, only with 240px of headroom. Like Ulhan's bow hops, the velocity is scripted in TickRangedJumpOverride.
        private const int SkyVolleyChance = 45;
        private const float SkyVolleyHeadroom = 240f;
        private const float SkyHopUpSpeed = 6.75f;
        private const int SkyHopApexTicks = 22;
        private const int SkyShotAttackTicks = 2;
        private const int SkyShotGapTicks = SkyHopApexTicks - SkyShotAttackTicks;
        private const float SkyBackpedalSpeed = 1.4f;
        private const float SkyMaxFallSpeed = 10f;
        private static readonly Color SkyVolleyFlashColor = new Color(120, 230, 140);

        private bool _skyVolley;
        private int _skyHopsTotal;
        private int _skyHopIndex;
        private int _skyHopElapsed;
        private float _skyVx;

        public override string BossHeadTexture => "tsorcRevamp/NPCs/Puppets/ShadowNinja_Head_Boss";

        protected override string InvaderTitle => "Shadow Ninja";

        protected override void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 80;
            globalNPC.RemembersLastKnownPos = true;
            // Sky Volley scripts its own air hops; navigation must not spend the double jump under it.
            globalNPC.CanDoubleJump = !_skyVolley;

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 3,
                attackRange: SecondaryRangedRange);
        }

        protected override int HeadArmorItemType => ModContent.ItemType<ShadowNinjaMask>();
        protected override int BodyArmorItemType => ModContent.ItemType<ShadowNinjaTop>();
        protected override int LegsArmorItemType => ModContent.ItemType<ShadowNinjaBottoms>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<ShadowSickleItem>();
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyNinjaStar>();
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<EnemyPoisonBow>();
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyCaltrop>();

        protected override int MeleeDamage => 48;
        protected override int RangedDamage => 34;
        protected override int SecondaryRangedDamage => 42;
        protected override int MagicDamage => 28;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override WeaponArchetype RangedArchetype => WeaponArchetype.Throwables;

        protected override bool UseCompositeArmSwing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool AuthoredClockCoversJoustDash => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 6;

        private bool ThreeHitStandardActive => ActiveMeleeComboName == ThreeHitStandardName
            && (Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery);

        // Poses: -1.55 -> 2.36 -> -1.55 rad, a 224-degree envelope for each swipe.
        // Tell: 35t on screen. Strike: 10t rise, 26t decay (k=7); ~184° is armed.
        // Chain: first 30t pause visibly re-raises for overhand 2; second pause holds 3t at
        // the shared overhand/underhand endpoint, producing the quick "1, 1-2" finish.
        // The first reset is an intentional harmless sweep, not an active fourth strike.
        // Every weighted cut (all Broadsword pool cuts; not Joust Poke's thrust) also gets OpeningCutStepIn;
        // ModifyNextMeleeComboStep re-solves the later cuts from the live gap.
        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            bool threeHitStandard = combo.Name == ThreeHitStandardName;

            combo.Steps = (MeleeComboStep[])combo.Steps.Clone();
            for (int i = 0; i < combo.Steps.Length; i++)
            {
                MeleeComboStep step = combo.Steps[i];

                if (threeHitStandard)
                {
                    step.Motion = i == 2 ? ComboMotion.UnderhandArc : ComboMotion.OverheadArc;
                    step.TelegraphTicks = i == 0 ? 20 : 0;
                    step.AttackTicks = ThreeHitSwingTicks;

                    step.PostStepPause = 0;
                    if (i == 0)
                    {
                        step.PostStepPause = ThreeHitResetTicks;
                    }
                    else if (i == 1)
                    {
                        step.PostStepPause = ThreeHitReverseTicks;
                    }

                    step.Ease = SwingEaseStyle.Weighted;
                    step.EaseInTicks = ThreeHitEaseInTicks;
                    step.EaseOutTicks = ThreeHitEaseOutTicks;
                    step.EaseOutDecay = ThreeHitEaseOutDecay;
                    step.HitWindowEnd = ThreeHitWindowEnd;
                }

                if (IsWeightedCut(step))
                {
                    step.StepInDistance = OpeningCutStepIn;
                }

                combo.Steps[i] = step;
            }
        }

        private static bool IsWeightedCut(MeleeComboStep step)
        {
            bool arcMotion = step.Motion == ComboMotion.OverheadArc || step.Motion == ComboMotion.UnderhandArc;
            return arcMotion && step.Ease == SwingEaseStyle.Weighted;
        }

        // A landed cut always earns the next. After a whiff, the next cut only fires if its reach plus its
        // largest step-in still covers the gap; otherwise the combo ends in recovery instead of slashing air.
        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            // Shadow Leap's second leap: a coin-ish roll (server only), and only if a leap can still land on them.
            if (comboName == ShadowLeapName)
            {
                bool rolledFollowUp = Main.rand.Next(100) < ShadowLeapFollowUpChance;
                float leapReach = ReliableJumpStartRange(ShadowLeapStep(0, 0));
                bool reachable = NPC.Distance(target.Center) <= leapReach;
                return rolledFollowUp && reachable;
            }

            if (previousStepHit)
            {
                return true;
            }

            float hitboxReach = ComboReachBase * 0.7f;
            float maximumContact = hitboxReach + FollowUpStepInMax + FollowUpContactMargin;
            float gap = NPC.Distance(target.Center);
            return gap <= maximumContact;
        }

        // Solve each follow-up cut's step-in from the gap the previous cut left, so a close target isn't
        // run through and a backing-off one is still chased (up to FollowUpStepInMax).
        protected override void ModifyNextMeleeComboStep(string comboName, int nextStepIndex, Player target, ref MeleeComboStep nextStep)
        {
            if (!IsWeightedCut(nextStep))
            {
                return;
            }

            float gap = System.Math.Abs(target.Center.X - NPC.Center.X);
            nextStep.StepInDistance = MathHelper.Clamp(gap - FollowUpCloseTo, FollowUpStepInMin, FollowUpStepInMax);
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion,
            ref float startRotation, ref float endRotation)
        {
            if (!ThreeHitStandardActive)
            {
                if (Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                    || Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery)
                {
                    WeaponArchetypeTables.SetBroadswordComboArc(ActiveMeleeComboName, motion,
                        ref startRotation, ref endRotation);
                }
                return;
            }

            if (motion == ComboMotion.OverheadArc)
            {
                startRotation = ThreeHitOverhandStart;
                endRotation = ThreeHitOverhandEnd;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = ThreeHitOverhandEnd;
                endRotation = ThreeHitOverhandStart;
            }
        }

        protected override bool UseCompositeArmForAdditionalPhase =>
            (IsSecondaryRangedActive && (Phase == AttackPhase.RangedTelegraph
                || Phase == AttackPhase.RangedAttack || Phase == AttackPhase.CrossbowBurstPause))
            || FlipSetPieceActive;
        protected override bool UseBowStringDrawPose => true;
        protected override bool ShowRangedWeaponDuringRecovery => true;
        protected override Vector2 GetHeldRangedGripNorm(int itemType) =>
            itemType == SecondaryRangedWeaponItemType ? new Vector2(0.57f, 0.48f)
                : base.GetHeldRangedGripNorm(itemType);
        protected override float GetHeldRangedDrawScale(int itemType) =>
            itemType == RangedWeaponItemType ? 0.5f
                : itemType == SecondaryRangedWeaponItemType ? 0.9f
                : base.GetHeldRangedDrawScale(itemType);
        protected override string GetHeldRangedDrawTexturePath(int itemType) =>
            itemType == SecondaryRangedWeaponItemType
                ? "tsorcRevamp/Content/Items/Weapons/Enemy/PoisonBowDraw"
                : base.GetHeldRangedDrawTexturePath(itemType);
        // PoisonBow's baked string occupies x=7..8, y=16..43 in its 28x58 sprite.
        protected override Rectangle GetBowStringTexels(int itemType) =>
            itemType == SecondaryRangedWeaponItemType ? new Rectangle(7, 16, 2, 28)
                : Rectangle.Empty;
        protected override Color BowStringColor => new Color(55, 170, 115);
        protected override string NockedBowArrowTexture =>
            "tsorcRevamp/Content/Projectiles/Ranged/Ammo/TaintedArrowProjectile";

        protected override RangedStyle RangedAnimStyle => RangedStyle.Throw;
        protected override float RangedRange => 470f;
        protected override float MinRangedRange => 170f;
        protected override int RangedTelegraphTicks => 60;
        protected override int RangedAttackTicks => 8;
        protected override int RangedRecoveryTicks => 42;
        protected override int RangedCooldownAfterUse => 190;
        protected override int MaxRangedBurst => 1;
        protected override int SingleRangedBurstChance => 100;
        protected override int StandingRangedChance => 25;
        protected override Color RangedTelegraphFlashColor => new Color(150, 120, 255);

        protected override int[][] PrimaryRangedBurstPatterns => new int[][]
        {
            new int[] { },
            new int[] { 12 },
            new int[] { 10, 10 },
            new int[] { 8, 8, 18 },
            new int[] { 6, 6, 6, 6 },
        };
        protected override int[] PrimaryRangedBurstTelegraphExtras => new int[] { 0, 4, 8, 14, 22 };
        protected override Color[] PrimaryRangedBurstFlashColors => new Color[]
        {
            Color.White,
            Color.Cyan,
            new Color(150, 120, 255),
            Color.Yellow,
            Color.Red,
        };
        protected override int[] PrimaryRangedBurstChances => new int[] { 80, 65, 45, 25, 10 };

        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Bow;
        protected override float SecondaryRangedRange => 650f;
        protected override float SecondaryRangedMinRange => 310f;
        protected override int SecondaryRangedTelegraphTicks => 54;
        protected override int SecondaryRangedAttackTicks => 8;
        protected override int SecondaryRangedRecoveryTicks => 58;
        protected override int SecondaryRangedCooldownAfterUse => 250;
        protected override int SecondaryMaxRangedBurst => 1;
        protected override int SecondaryRangedChance => 42;
        protected override int SecondaryStandingRangedChance => 85;
        protected override Color SecondaryRangedFlashColor => new Color(90, 220, 100);

        // Read after ModifyRangedBurst, so a rolled Sky Volley swaps in its one hop-timed pattern (one pause per extra hop).
        protected override int[][] SecondaryRangedBurstPatterns
        {
            get
            {
                if (_skyVolley)
                {
                    int[] hopPauses = Enumerable.Repeat(SkyShotGapTicks, _skyHopsTotal - 1).ToArray();
                    return new int[][] { hopPauses };
                }

                return new int[][]
                {
                    new int[] { },
                    new int[] { 18 },
                    new int[] { 20, 20 },
                    new int[] { 12, 12, 28 },
                    new int[] { 8, 8, 8, 8 },
                };
            }
        }

        protected override int[] SecondaryRangedBurstTelegraphExtras => _skyVolley
            ? new int[] { 0 }
            : new int[] { 0, 8, 14, 20, 32 };

        protected override Color[] SecondaryRangedBurstFlashColors
        {
            get
            {
                if (_skyVolley)
                {
                    return new Color[] { SkyVolleyFlashColor };
                }

                return new Color[]
                {
                    Color.White,
                    Color.Cyan,
                    Color.LightYellow,
                    Color.Yellow,
                    Color.Red,
                };
            }
        }

        protected override int[] SecondaryRangedBurstChances => _skyVolley
            ? new int[] { 100 }
            : new int[] { 75, 50, 35, 20, 8 };

        protected override float MagicRange => 430f;
        protected override float MinMagicRange => 210f;
        protected override int MagicTelegraphTicks => 60;
        protected override int MagicAttackTicks => 10;
        protected override int MagicRecoveryTicks => 58;
        protected override int MagicCooldownAfterUse => 520;
        protected override Color MagicTelegraphFlashColor => new Color(95, 95, 95);

        protected override float TopSpeed => 3.35f;
        protected override float Acceleration => 0.12f;
        protected override float BrakingPower => 0.24f;
        protected override float MeleeRange => 65f;
        protected override float MeleeBladeWidth => 18f;
        protected override float MeleeWeaponDrawScale => 1.25f;
        protected override float ComboMaxStartRange => 245f;
        protected override float MeleeEngageRange => CloseEngageRange;
        protected override float ComboTelegraphAdvanceSpeedMult => TellAdvanceSpeedMult;
        protected override float ComboTelegraphAdvanceStopDistance => TellAdvanceStopDistance;
        protected override float LogicalWindupSettleFraction => WindupSettleFraction;
        protected override float RangedStartComboMaxRange => ShadowLeapMaxStartRange;
        protected override MeleeCombo[] MeleeComboPoolOverride => ShadowNinjaCombos;

        // ActiveMeleeComboName persists after recovery, so the leap overrides are scoped to the combo phases.
        private bool ShadowLeapActive => ActiveMeleeComboName == ShadowLeapName
            && (Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause);

        protected override bool UseLandingTimedLeapSlam => ShadowLeapActive;

        protected override float LeapSlamCarryRotation => ShadowLeapActive
            ? ThreeHitOverhandStart
            : base.LeapSlamCarryRotation;

        protected override float LeapSlamImpactRotation => ShadowLeapActive
            ? ThreeHitOverhandEnd
            : base.LeapSlamImpactRotation;

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == ShadowLeapName)
            {
                return distance >= ShadowLeapMinStartRange && distance <= ShadowLeapMaxStartRange;
            }

            return true;
        }

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);

            if (combo.Name != ShadowLeapName || Main.dedServ)
            {
                return;
            }

            // Tell: a low grey smoke kick at the feet, the same smoke as his teleport.
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
            SpawnShadowLeapSmoke(18, 2.4f);
        }

        // Landing burst for each leap: smoke ring at the feet.
        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            base.OnLeapSlamLanded(step);

            if (!ShadowLeapActive || Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.35f }, NPC.Center);
            SpawnShadowLeapSmoke(24, 3.2f);
        }

        // Grey smoke spread sideways from the feet. Called from the tell, both landings and the follow-up wisps.
        private void SpawnShadowLeapSmoke(int count, float spreadSpeed)
        {
            Vector2 feet = new Vector2(NPC.Center.X, NPC.Bottom.Y - 4f);
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-spreadSpeed, spreadSpeed), Main.rand.NextFloat(-1.2f, -0.2f));
                Dust smoke = Dust.NewDustPerfect(feet, DustID.Smoke, velocity, 120, ShadowLeapSmokeTint, Main.rand.NextFloat(1f, 1.5f));
                smoke.noGravity = true;
            }
        }

        // ── Flip set-pieces: engine hooks ────────────────────────────────────────
        private bool FlipSetPieceActive => Phase == AttackPhase.Custom && _flipStage != FlipStage.None;

        protected override float PuppetBodySpinRotation => FlipSetPieceActive ? _flipBodySpin : 0f;
        protected override float? CustomWeaponRotation => FlipSetPieceActive ? _flipWeaponRotation : null;
        protected override bool SlowDownDuringCustom => !FlipSetPieceActive;

        // The blade is tested where it is drawn: the same rigid rotation the draw layer applies to the body.
        protected override void ModifyFrontBladeCapsule(ref Vector2 origin, ref Vector2 tip)
        {
            if (_flipBodySpin == 0f)
            {
                return;
            }

            Vector2 pivot = PuppetBodySpinPivot;
            origin = pivot + (origin - pivot).RotatedBy(_flipBodySpin);
            tip = pivot + (tip - pivot).RotatedBy(_flipBodySpin);
        }

        private void TickFlipCooldowns()
        {
            if (_frontFlipCooldown > 0)
            {
                _frontFlipCooldown--;
            }

            if (_backFlipCooldown > 0)
            {
                _backFlipCooldown--;
            }
        }

        // Server: start a flip from a neutral, grounded state. Front Flip answers a jumping target; Back Flip opens
        // against a grounded one standing close, when there is room to land behind.
        private void TrySelectFlip()
        {
            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.ClosingDistance || Phase == AttackPhase.CasualStroll;
            if (!neutral || !NPC.HasValidTarget || NPC.velocity.Y != 0f)
            {
                return;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool busy = globalNPC.StaggerTimer > 0 || globalNPC.TeleportCountdown > 0 || globalNPC.DodgeTimer > 0;
            if (busy)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            float horizontalGap = System.Math.Abs(target.Center.X - NPC.Center.X);
            float heightAbove = NPC.Center.Y - target.Center.Y;
            bool targetAirborne = target.velocity.Y != 0f;

            int towardTarget = 1;
            if (target.Center.X < NPC.Center.X)
            {
                towardTarget = -1;
            }

            bool frontFlipFits = _frontFlipCooldown <= 0 && targetAirborne
                && heightAbove >= FrontFlipMinHeight && heightAbove <= FrontFlipMaxRise
                && horizontalGap <= FrontFlipMaxRange
                && HasHeadroom(heightAbove + FrontFlipRiseOvershoot);
            if (frontFlipFits && Main.rand.NextBool(FrontFlipPickDenominator))
            {
                _flipDir = towardTarget;
                _frontFlipCooldown = FrontFlipCooldownTicks;
                DebugAttackLabel = "Front Flip";
                StartFlipStage(FlipStage.FrontTell, FrontFlipTellTicks);
                return;
            }

            bool backFlipFits = _backFlipCooldown <= 0 && !targetAirborne
                && horizontalGap <= BackFlipMaxRange && System.Math.Abs(heightAbove) < 40f
                && HasRoomBehind(towardTarget);
            if (backFlipFits && Main.rand.NextBool(BackFlipPickDenominator))
            {
                _flipDir = towardTarget;
                _backFlipCooldown = BackFlipCooldownTicks;
                DebugAttackLabel = "Back Flip";
                StartFlipStage(FlipStage.BackTell, BackFlipTellTicks);
            }
        }

        private bool HasHeadroom(float height)
        {
            Vector2 aboveHead = NPC.position - new Vector2(0f, height);
            return !Collision.SolidCollision(aboveHead, NPC.width, (int)height);
        }

        // A body-sized box 140px behind him, raised 60px for the arc, must be open air.
        private bool HasRoomBehind(int towardTarget)
        {
            Vector2 landingBox = new Vector2(NPC.position.X - towardTarget * BackFlipClearance, NPC.position.Y - 60f);
            return !Collision.SolidCollision(landingBox, NPC.width, NPC.height + 50);
        }

        // Server: every stage is its own Custom phase; the stage and its length reach clients in the snapshot.
        private void StartFlipStage(FlipStage stage, int ticks)
        {
            _flipStage = stage;
            _flipStageTicks = System.Math.Max(1, ticks);
            StartCustomAttack(_flipStageTicks, MeleeWeaponItemType, swingPose: false);
            NPC.netUpdate = true;
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (_flipStage == FlipStage.None)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            int elapsed = _flipStageTicks - ticksRemaining;
            bool lastTick = ticksRemaining <= 1;
            bool server = Main.netMode != NetmodeID.MultiplayerClient;

            NPC.direction = _flipDir;
            NPC.spriteDirection = _flipDir;

            switch (_flipStage)
            {
                case FlipStage.FrontTell:
                {
                    NPC.velocity.X *= 0.8f;
                    float raise = MathHelper.SmoothStep(0f, 1f, elapsed / (float)FrontFlipTellTicks);
                    _flipWeaponRotation = MathHelper.Lerp(CarryPose, FlipCockedPose, raise);

                    if (elapsed == 0 && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.6f, Pitch = 0.1f }, NPC.Center);
                        SpawnShadowLeapSmoke(14, 2f);
                    }

                    if (lastTick && server)
                    {
                        LaunchFrontFlip(target);
                    }
                    break;
                }

                case FlipStage.FrontLeap:
                    TickFrontLeap(target, elapsed, lastTick, server);
                    break;

                case FlipStage.BackTell:
                {
                    // Walks in during the dip while the target stays on the committed side, stopping 44px out.
                    float gapAhead = _flipDir * (target.Center.X - NPC.Center.X);
                    bool keepClosing = gapAhead > BackFlipTellStopDistance;
                    if (keepClosing)
                    {
                        float advanceSpeed = TopSpeed * BackFlipTellAdvanceSpeedMult;
                        NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, _flipDir * advanceSpeed, 0.25f);
                    }
                    else
                    {
                        NPC.velocity.X *= 0.7f;
                    }

                    float dip = MathHelper.SmoothStep(0f, 1f, elapsed / (float)BackFlipTellTicks);
                    _flipWeaponRotation = MathHelper.Lerp(CarryPose, FlipLowPose, dip);

                    if (elapsed == 0 && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.6f, Pitch = -0.1f }, NPC.Center);
                        SpawnShadowLeapSmoke(14, 2f);
                    }

                    if (lastTick && server)
                    {
                        // Step-in solved from the gap left after the walk-in, aiming to cut from 28px out.
                        float gapAtCut = System.Math.Abs(target.Center.X - NPC.Center.X);
                        _backCutStepIn = MathHelper.Clamp(gapAtCut - BackFlipCutCloseTo, BackFlipCutStepInMin, BackFlipCutStepInMax);
                        _flipCutTicks = 0;
                        TryMeleeHit(ComboReachBase * 0.7f * FlipBladeReachMult);
                        StartFlipStage(FlipStage.BackCut, BackFlipLaunchTick);
                    }
                    break;
                }

                case FlipStage.BackCut:
                {
                    if (elapsed == 0)
                    {
                        PlayMeleeSwingSound();
                    }

                    // Drive forward with the blade: (t/in)^2 through the ease-in, e^(-k*p) after, scaled so the
                    // 9 cut ticks before launch cover _backCutStepIn px. Read before TickBackFlipUnderswing advances the clock.
                    float cutTick = _flipCutTicks;
                    float speedFraction;
                    if (cutTick < BackFlipCutEaseIn)
                    {
                        float easeInProgress = cutTick / BackFlipCutEaseIn;
                        speedFraction = easeInProgress * easeInProgress;
                    }
                    else
                    {
                        float easeOutProgress = (cutTick - BackFlipCutEaseIn) / BackFlipCutEaseOut;
                        speedFraction = (float)System.Math.Exp(-BackFlipCutDecay * easeOutProgress);
                    }

                    float peakSpeed = _backCutStepIn / BackFlipCutStepCurveTicks;
                    NPC.velocity.X = _flipDir * peakSpeed * speedFraction;

                    TickBackFlipUnderswing();
                    TickBladeHit();

                    if (lastTick && server)
                    {
                        LaunchBackFlip();
                    }
                    break;
                }

                case FlipStage.BackAir:
                    TickBackAir(target, elapsed, lastTick, server);
                    break;

                case FlipStage.ShadeVanish:
                {
                    // Gone: hangs where it vanished, untouchable and undrawn (PreDraw), while the server picks the spot.
                    NPC.velocity = Vector2.Zero;
                    NPC.noGravity = true;
                    NPC.dontTakeDamage = true;

                    if (elapsed == 0 && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
                        SpawnShadowStepBurst();
                    }

                    if (lastTick && server)
                    {
                        ReappearForShadowDive(target);
                    }
                    break;
                }

                case FlipStage.ShadeDive:
                    TickShadowDive(target, elapsed, lastTick, server);
                    break;

                case FlipStage.Landing:
                {
                    NPC.velocity.X *= 0.6f;
                    _flipBodySpin = 0f;
                    int landingTicks = _flipStageTicks;
                    float settle = MathHelper.SmoothStep(0f, 1f, elapsed / (float)landingTicks);
                    _flipWeaponRotation = MathHelper.Lerp(_flipWeaponRotation, CarryPose, settle * 0.25f);

                    if (elapsed == 0 && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = -0.4f }, NPC.Center);
                        SpawnShadowLeapSmoke(18, 2.8f);
                    }

                    if (lastTick)
                    {
                        _flipStage = FlipStage.None;
                        DebugAttackLabel = null;
                    }
                    break;
                }
            }
        }

        // Server: rise to the target's height 10t ahead (+24px) and cover the horizontal gap by the apex.
        private void LaunchFrontFlip(Player target)
        {
            Vector2 aim = target.Center + target.velocity * FrontFlipLeadTicks;
            float rise = MathHelper.Clamp(NPC.Center.Y - aim.Y + FrontFlipRiseOvershoot, FrontFlipMinRise, FrontFlipMaxRise);
            float upSpeed = System.Math.Min((float)System.Math.Sqrt(2f * FlipGravity * rise), FrontFlipMaxUpSpeed);
            float riseTicks = upSpeed / FlipGravity;
            float horizontalGap = aim.X - NPC.Center.X;

            _flipVx = MathHelper.Clamp(horizontalGap / riseTicks, -FrontFlipMaxForwardSpeed, FrontFlipMaxForwardSpeed);
            _flipSpinStart = -1;
            NPC.velocity = new Vector2(_flipVx, -upSpeed);
            StartFlipStage(FlipStage.FrontLeap, FlipAirTimeoutTicks);
        }

        private void TickFrontLeap(Player target, int elapsed, bool lastTick, bool server)
        {
            NPC.velocity.X = _flipVx;

            // Somersault trigger: close to the target, or past the apex. Every peer predicts it from its own positions;
            // the server's start is flushed in a snapshot and wins. Only the server arms the blade.
            bool rising = _flipSpinStart < 0;
            if (rising)
            {
                _flipWeaponRotation = FlipCockedPose;
                bool close = NPC.Distance(target.Center) <= FrontFlipTriggerRange;
                bool pastApex = NPC.velocity.Y >= 0f;
                if (elapsed > 2 && (close || pastApex))
                {
                    _flipSpinStart = elapsed;
                    PlayMeleeSwingSound();
                    if (server)
                    {
                        TryMeleeHit(ComboReachBase * 0.7f * FlipBladeReachMult);
                        RequestNetworkSnapshot();
                    }
                }
            }

            if (_flipSpinStart >= 0)
            {
                int spinElapsed = elapsed - _flipSpinStart;
                float snap = MathHelper.SmoothStep(0f, 1f, spinElapsed / (float)FlipBladeSnapTicks);
                _flipWeaponRotation = MathHelper.Lerp(FlipCockedPose, FlipExtendedPose, snap);

                // One forward turn, fast out of the snap and settling upright: angle = 2pi * (1 - (1 - t)^2).
                float spinProgress = MathHelper.Clamp(spinElapsed / (float)FrontFlipSpinTicks, 0f, 1f);
                float remaining = 1f - spinProgress;
                bool spinning = spinProgress < 1f;
                if (spinning)
                {
                    _flipBodySpin = _flipDir * MathHelper.TwoPi * (1f - remaining * remaining);
                    TickBladeHit();
                    SpawnFlipTrail();
                }
                else
                {
                    _flipBodySpin = 0f;
                    StopMeleeHit();
                }
            }

            bool landed = elapsed > 3 && NPC.velocity.Y == 0f;
            if (server && (landed || lastTick))
            {
                _flipBodySpin = 0f;
                StopMeleeHit();
                StartFlipStage(FlipStage.Landing, FlipLandingTicks);
            }
        }

        // The underswing runs on its own clock across BackCut and BackAir, so the cut finishes mid-somersault.
        private void TickBackFlipUnderswing()
        {
            int cutTotalTicks = BackFlipCutEaseIn + BackFlipCutEaseOut;
            _flipWeaponRotation = SwingEase.ApplyWeighted(FlipLowPose, FlipCockedPose, _flipCutTicks, cutTotalTicks,
                BackFlipCutEaseIn, BackFlipCutEaseOut, BackFlipCutDecay);

            if (_flipCutTicks < cutTotalTicks)
            {
                _flipCutTicks++;
            }
        }

        // Server: hop backward off the cut; the blade stays armed from the underswing.
        private void LaunchBackFlip()
        {
            _flipVx = -_flipDir * BackFlipBackSpeed;
            _flipCaltropsThrown = false;
            NPC.velocity = new Vector2(_flipVx, -BackFlipUpSpeed);
            StartFlipStage(FlipStage.BackAir, FlipAirTimeoutTicks);
        }

        private void TickBackAir(Player target, int elapsed, bool lastTick, bool server)
        {
            NPC.velocity.X = _flipVx;
            TickBackFlipUnderswing();

            // One backward turn over BackFlipSpinTicks, same ease as the front flip, live until it completes.
            float spinProgress = MathHelper.Clamp(elapsed / (float)BackFlipSpinTicks, 0f, 1f);
            float remaining = 1f - spinProgress;
            bool spinning = spinProgress < 1f;
            if (spinning)
            {
                _flipBodySpin = -_flipDir * MathHelper.TwoPi * (1f - remaining * remaining);
                TickBladeHit();
                SpawnFlipTrail();
            }
            else
            {
                _flipBodySpin = 0f;
                StopMeleeHit();
            }

            // The Shadow Self throws no caltrops: once its somersault completes (near the apex) it smoke-steps instead.
            if (IsShadowShade)
            {
                bool somersaultDone = elapsed >= BackFlipSpinTicks;
                if (somersaultDone && server)
                {
                    _flipBodySpin = 0f;
                    StopMeleeHit();
                    StartFlipStage(FlipStage.ShadeVanish, ShadowStepVanishTicks);
                    return;
                }
            }

            // Smoke caltrops tossed from mid-somersault, spread across the target: 3 bombs x 4 clouds.
            bool throwNow = !IsShadowShade && elapsed == BackFlipCaltropTick && !_flipCaltropsThrown;
            if (throwNow)
            {
                _flipCaltropsThrown = true;
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = 0.3f }, NPC.Center);
                }

                if (server && NPC.HasValidTarget)
                {
                    for (int i = 0; i < BackFlipCaltropCount; i++)
                    {
                        float spreadFraction = i / (float)(BackFlipCaltropCount - 1) - 0.5f;
                        Vector2 landingSpot = target.Bottom + new Vector2(spreadFraction * 2f * BackFlipCaltropSpread, -8f);
                        ShadowSmokeCaltrop.Throw(NPC.GetSource_FromThis(), NPC.Center, landingSpot);
                    }
                }
            }

            bool landed = elapsed > 3 && NPC.velocity.Y == 0f;
            if (server && (landed || lastTick))
            {
                _flipBodySpin = 0f;
                StopMeleeHit();
                StartFlipStage(FlipStage.Landing, BackFlipLandingTicks);
            }
        }

        // Server: the flank spot first (far side from where it vanished), highest first, then the near side. A spot must
        // be open air with a clear line to the target; with none, it reappears where it vanished.
        private void ReappearForShadowDive(Player target)
        {
            int vanishedSide = 1;
            if (NPC.Center.X < target.Center.X)
            {
                vanishedSide = -1;
            }

            int[] sides = { -vanishedSide, vanishedSide };
            Vector2 spot = NPC.Center;
            bool found = false;

            foreach (int side in sides)
            {
                foreach (float height in ShadowStepHeights)
                {
                    Vector2 candidate = target.Center + new Vector2(side * ShadowStepAcross, -height);
                    Vector2 topLeft = candidate - NPC.Size * 0.5f;
                    bool openAir = !Collision.SolidCollision(topLeft, NPC.width, NPC.height);
                    bool clearLine = Collision.CanHitLine(candidate, 1, 1, target.Center, 1, 1);
                    if (openAir && clearLine)
                    {
                        spot = candidate;
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }

            // Drift so the fall ends on the target: fall time from the drop height under 0.3 gravity.
            float drop = System.Math.Max(16f, target.Center.Y - spot.Y);
            float fallTicks = (float)System.Math.Sqrt(2f * drop / FlipGravity);
            float horizontalGap = target.Center.X - spot.X;

            NPC.Center = spot;
            _flipDir = 1;
            if (horizontalGap < 0f)
            {
                _flipDir = -1;
            }

            _flipVx = MathHelper.Clamp(horizontalGap / fallTicks, -ShadowDiveMaxDrift, ShadowDiveMaxDrift);
            _flipSpinStart = -1;
            NPC.velocity = new Vector2(_flipVx, 0f);
            NPC.noGravity = false;
            NPC.dontTakeDamage = false;
            StartFlipStage(FlipStage.ShadeDive, ShadowDiveTimeoutTicks);
        }

        // Dive with the sickle cocked overhead; the cut starts within ShadowDiveStrikeRange, or on landing if it never
        // got close. _flipSpinStart is reused as the stage tick the cut began.
        private void TickShadowDive(Player target, int elapsed, bool lastTick, bool server)
        {
            NPC.noGravity = false;
            NPC.dontTakeDamage = false;

            if (elapsed == 0 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = 0.2f }, NPC.Center);
                SpawnShadowStepBurst();
            }

            bool landed = elapsed > 3 && NPC.velocity.Y == 0f;
            if (landed)
            {
                NPC.velocity.X *= 0.6f;
            }
            else
            {
                NPC.velocity.X = _flipVx;
            }

            bool slashing = _flipSpinStart >= 0;
            if (!slashing)
            {
                _flipWeaponRotation = FlipCockedPose;
                bool close = NPC.Distance(target.Center) <= ShadowDiveStrikeRange;
                if (elapsed > 2 && (close || landed))
                {
                    _flipSpinStart = elapsed;
                    PlayMeleeSwingSound();
                    if (server)
                    {
                        TryMeleeHit(ComboReachBase * 0.7f * FlipBladeReachMult);
                        RequestNetworkSnapshot();
                    }
                }
            }

            if (_flipSpinStart >= 0)
            {
                int slashElapsed = elapsed - _flipSpinStart;
                int slashTotalTicks = ShadowDiveEaseIn + ShadowDiveEaseOut;
                _flipWeaponRotation = SwingEase.ApplyWeighted(FlipCockedPose, FlipLowPose, slashElapsed, slashTotalTicks,
                    ShadowDiveEaseIn, ShadowDiveEaseOut, ShadowDiveDecay);

                // Live while blade speed is >= 30% of peak: in + out * ln(1/0.3) / k ticks.
                float liveTicks = ShadowDiveEaseIn + ShadowDiveEaseOut * 1.204f / ShadowDiveDecay;
                if (slashElapsed <= liveTicks)
                {
                    TickBladeHit();
                }
                else
                {
                    StopMeleeHit();
                }

                bool slashDone = slashElapsed >= slashTotalTicks;
                if (server && landed && slashDone)
                {
                    StartFlipStage(FlipStage.Landing, FlipLandingTicks);
                    return;
                }
            }

            if (server && lastTick)
            {
                StopMeleeHit();
                StartFlipStage(FlipStage.Landing, FlipLandingTicks);
            }
        }

        // Smoke-step burst: a dense charcoal puff with a few shadowflame sparks.
        private void SpawnShadowStepBurst()
        {
            for (int i = 0; i < 30; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                Dust smoke = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(10f, 18f), DustID.Smoke, velocity, 110,
                    ShadowLeapSmokeTint, Main.rand.NextFloat(1.3f, 2f));
                smoke.noGravity = true;
            }

            for (int i = 0; i < 8; i++)
            {
                Dust spark = Dust.NewDustPerfect(NPC.Center, DustID.Shadowflame, Main.rand.NextVector2Circular(4f, 4f), 80, default, 1.1f);
                spark.noGravity = true;
            }
        }

        // Not drawn while smoke-stepping.
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool hidden = Phase == AttackPhase.Custom && _flipStage == FlipStage.ShadeVanish;
            if (hidden)
            {
                return false;
            }

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        // Server, boss only: keep one Shadow Self alive below 50% health, 20s between a death and the next summon.
        private void TickShadeSummon()
        {
            if (IsShadowShade)
            {
                return;
            }

            float healthFraction = NPC.life / (float)NPC.lifeMax;
            if (healthFraction > ShadeSummonHealthFraction)
            {
                return;
            }

            int shadeType = ModContent.NPCType<ShadowNinjaShade>();
            bool shadeAlive = _shadeIndex >= 0 && Main.npc[_shadeIndex].active && Main.npc[_shadeIndex].type == shadeType;
            if (shadeAlive)
            {
                return;
            }

            // A shade that just died starts the 20s timer; the first summon (no shade yet) is immediate.
            if (_shadeIndex >= 0)
            {
                _shadeIndex = -1;
                _shadeRespawnTimer = ShadeRespawnTicks;
                return;
            }

            if (_shadeRespawnTimer > 0)
            {
                _shadeRespawnTimer--;
                return;
            }

            // Steps out of him, on the side away from the target.
            int awayFromTarget = 1;
            if (Main.player[NPC.target].Center.X > NPC.Center.X)
            {
                awayFromTarget = -1;
            }

            int spawnX = (int)(NPC.Center.X + awayFromTarget * 40f);
            int spawnY = (int)NPC.Bottom.Y;
            int index = NPC.NewNPC(NPC.GetSource_FromAI(), spawnX, spawnY, shadeType, 0, 0f, 0f, 0f, 0f, NPC.target);
            if (index < 0 || index >= Main.maxNPCs)
            {
                return;
            }

            _shadeIndex = index;
            if (Main.npc[index].ModNPC is ShadowNinjaShade shade)
            {
                shade.OwnerIndex = NPC.whoAmI;
            }
        }

        // Grey smoke shed from the blade tip while the somersault is live.
        private void SpawnFlipTrail()
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 hand = PuppetHandPosition;
            Vector2 tip = PuppetWeaponTipPosition(ComboReachBase * 0.7f * FlipBladeReachMult);
            ModifyFrontBladeCapsule(ref hand, ref tip);
            Dust trail = Dust.NewDustPerfect(tip, DustID.Smoke, Vector2.Zero, 120, ShadowLeapSmokeTint, 1.2f);
            trail.noGravity = true;
        }

        // ── Sky Volley: burst setup and scripted hops ────────────────────────────
        protected override void ModifyRangedBurst(bool secondary, ref int itemType, ref RangedStyle style,
            ref Color flashColor, ref int telegraphTicks, ref int attackTicks, ref int recoveryTicks)
        {
            // The roll is the server's; a client keeps the synced choice and only mirrors its timings.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _skyVolley = false;
                bool canHop = secondary && NPC.velocity.Y == 0f && NPC.HasValidTarget && HasHeadroom(SkyVolleyHeadroom);
                if (canHop && Main.rand.Next(100) < SkyVolleyChance)
                {
                    _skyVolley = true;
                    _skyHopsTotal = Main.rand.Next(2, 4);
                    _skyHopIndex = 0;
                    _skyHopElapsed = 0;

                    int awayFromTarget = 1;
                    if (Main.player[NPC.target].Center.X > NPC.Center.X)
                    {
                        awayFromTarget = -1;
                    }

                    _skyVx = awayFromTarget * SkyBackpedalSpeed;
                    NPC.netUpdate = true;
                }
            }

            if (_skyVolley)
            {
                // The first hop launches on the telegraph's first tick, so the first shot lands on its apex.
                telegraphTicks = SkyHopApexTicks;
                attackTicks = SkyShotAttackTicks;
            }
        }

        protected override bool HasRangedJumpOverride => _skyVolley;

        // Scripted hop: vy = -6.75 + 0.3t from the hop's start (written each tick, so vanilla gravity can't add to it),
        // backpedalling at _skyVx. A hop restarts on each non-final shot (DoRangedAttack).
        protected override void TickRangedJumpOverride()
        {
            if (!_skyVolley)
            {
                return;
            }

            bool landed = _skyHopElapsed > 2 && NPC.velocity.Y == 0f;
            if (landed)
            {
                NPC.velocity.X *= 0.7f;
                return;
            }

            if (_skyHopElapsed == 0 && !Main.dedServ)
            {
                // Each hop kicks off a puff of smoke: the ground hop and every air jump alike.
                SoundEngine.PlaySound(SoundID.DoubleJump with { Volume = 0.7f }, NPC.Bottom);
                SpawnShadowLeapSmoke(10, 1.8f);
            }

            float verticalSpeed = -SkyHopUpSpeed + FlipGravity * _skyHopElapsed;
            NPC.velocity.X = _skyVx;
            NPC.velocity.Y = System.Math.Min(verticalSpeed, SkyMaxFallSpeed);
            _skyHopElapsed++;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_flipStage);
            writer.Write((short)_flipStageTicks);
            writer.Write((sbyte)_flipDir);
            writer.Write(_flipVx);
            writer.Write((short)_flipSpinStart);
            writer.Write((byte)_flipCutTicks);
            writer.Write(_flipCaltropsThrown);
            writer.Write(_backCutStepIn);
            writer.Write(_skyVolley);
            writer.Write((byte)_skyHopsTotal);
            writer.Write((byte)_skyHopIndex);
            writer.Write((short)_skyHopElapsed);
            writer.Write(_skyVx);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _flipStage = (FlipStage)reader.ReadByte();
            _flipStageTicks = reader.ReadInt16();
            _flipDir = reader.ReadSByte();
            _flipVx = reader.ReadSingle();
            _flipSpinStart = reader.ReadInt16();
            _flipCutTicks = reader.ReadByte();
            _flipCaltropsThrown = reader.ReadBoolean();
            _backCutStepIn = reader.ReadSingle();
            _skyVolley = reader.ReadBoolean();
            _skyHopsTotal = reader.ReadByte();
            _skyHopIndex = reader.ReadByte();
            _skyHopElapsed = reader.ReadInt16();
            _skyVx = reader.ReadSingle();
        }
        protected override int MeleeComboChance => 92;
        protected override float ComboTelegraphMultiplier => 1.75f;
        protected override int MinComboTelegraphTicks => 34;
        protected override int CasualStrollChance => 0;

        protected override float PuppetJumpPower => 10.5f;
        protected override float PuppetJumpBoost => 7f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 7f;

        protected override int TeleportTelegraphTicks => 30;
        protected override int TeleportDustCount => 34;
        protected override int TeleportDustTypeId => DustID.Smoke;
        protected override Color TeleportDustTint => new Color(70, 70, 80);
        protected override float TeleportDustScale => 1.05f;

        protected override Color MeleeTelegraphFlashColor => Color.White;
        protected override Vector2 MeleeHandleNorm => new Vector2(0.16f, 0.84f);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 30000;
            NPC.defense = 34;
            NPC.damage = 0;
            NPC.knockBackResist = 0.12f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 26000f;
            NPC.boss = true;
            NPC.npcSlots = 10f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 32f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 160;
            globalNPC.CanUseRopes = true;
            globalNPC.CanDoubleJump = true;
            globalNPC.DoubleJumpPower = PuppetDoubleJumpPower;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<YellowTail>(), 6));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<TaintedBow>(), 8));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoulItem>(), 1, 900, 1400));
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<ShadowNinja>());
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<global::tsorcRevamp.Content.Items.StaminaVessel>());
                tsorcRevampWorld.NewSlain.Add(definition, 1);

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
        }

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                TickFlipCooldowns();
                TickShadeSummon();
                TrySelectFlip();
            }

            AttackPhase previousPhase = Phase;
            base.AI();
            if (Phase == AttackPhase.RangedRecovery && IsSecondaryRangedActive)
            {
                SetDisplayWeapon(SecondaryRangedWeaponItemType, swing: false);
            }

            // Stagger, a teleport or any other interruption dropped the set-piece: stand back up and disarm.
            if (Phase != AttackPhase.Custom && _flipStage != FlipStage.None)
            {
                _flipStage = FlipStage.None;
                _flipBodySpin = 0f;
                NPC.noGravity = false;
                NPC.dontTakeDamage = false;
                StopMeleeHit();
            }

            // The engine only drives TickRangedJumpOverride in the telegraph and attack phases; the hop keeps
            // flying through the pauses between shots and the airborne part of the recovery (same as Ulhan).
            bool rangedPhase = Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack
                || Phase == AttackPhase.CrossbowBurstPause || Phase == AttackPhase.RangedRecovery;
            if (_skyVolley && !rangedPhase)
            {
                _skyVolley = false;
            }

            bool hopBetweenShots = previousPhase == AttackPhase.CrossbowBurstPause;
            bool fallingInRecovery = previousPhase == AttackPhase.RangedRecovery && NPC.velocity.Y != 0f;
            if (_skyVolley && (hopBetweenShots || fallingInRecovery))
            {
                TickRangedJumpOverride();
            }

            // The pause only runs when the follow-up leap was rolled, so these wisps are its tell.
            bool followUpLeapPending = ShadowLeapActive && Phase == AttackPhase.MeleeComboPause;
            if (followUpLeapPending && !Main.dedServ && Main.GameUpdateCount % 5 == 0)
            {
                SpawnShadowLeapSmoke(3, 1.2f);
            }
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, PitchVariance = 0.25f }, NPC.Center);
            TryMeleeHit(reach: MeleeRange * 0.8f);
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, PitchVariance = 0.28f }, NPC.Center);
            base.DoComboMeleeHit(step);
        }

        protected override void DoRangedAttack()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(IsSecondaryRangedActive
                    ? SoundID.Item5 with { Volume = 0.75f, PitchVariance = 0.1f }
                    : SoundID.Item1 with { Volume = 0.52f, PitchVariance = 0.22f }, NPC.Center);
            }

            // Sky Volley: every peer launches the next air hop on the shot tick (all but the last shot).
            bool anotherHop = _skyVolley && _skyHopIndex < _skyHopsTotal - 1;
            if (anotherHop)
            {
                _skyHopIndex++;
                _skyHopElapsed = 0;

                if (Main.netMode == NetmodeID.Server)
                {
                    NPC.netUpdate = true;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 muzzle = IsSecondaryRangedActive ? PuppetBowGripPosition
                : NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.22f);
            Vector2 aimAt = target.Center + target.velocity * 12f;
            Vector2 toTarget = aimAt - muzzle;
            if (toTarget == Vector2.Zero)
            {
                toTarget = new Vector2(NPC.direction, 0f);
            }
            toTarget.Normalize();

            if (IsSecondaryRangedActive)
            {
                float spread = MathHelper.ToRadians(Main.rand.NextFloat(-4f, 4f));
                Vector2 velocity = toTarget.RotatedBy(spread) * 11.3f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    muzzle,
                    velocity,
                    ModContent.ProjectileType<ShadowNinjaPoisonArrow>(),
                    SecondaryRangedDamage,
                    3f,
                    Main.myPlayer);
                return;
            }

            float starSpread = MathHelper.ToRadians(Main.rand.NextFloat(-7f, 7f));
            Vector2 starVelocity = toTarget.RotatedBy(starSpread) * 11f;
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                muzzle,
                starVelocity,
                ModContent.ProjectileType<ShadowNinjaStarProj>(),
                RangedDamage,
                2f,
                Main.myPlayer);
        }

        protected override void DoRangedTelegraphVFX(bool secondary, float progress)
        {
            if (!secondary || Main.dedServ)
            {
                return;
            }

            Vector2 origin = PuppetBowGripPosition;
            float angle = BowAimAngle;
            Vector2 aim = new Vector2(NPC.direction * (float)System.Math.Cos(angle),
                (float)System.Math.Sin(angle));
            if (Main.GameUpdateCount % 3 == 0)
            {
                Dust spark = Dust.NewDustPerfect(origin + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.Poisoned, -aim * 0.5f, 80, default, 0.65f);
                spark.noGravity = true;
            }
            if (Main.GameUpdateCount % 6 == 0)
            {
                for (int i = 1; i <= 8; i++)
                {
                    Dust marker = Dust.NewDustPerfect(origin + aim * (240f * i / 8f),
                        DustID.Poisoned, Vector2.Zero, 180, default, 0.35f);
                    marker.noGravity = true;
                }
            }
        }

        protected override void DoMagicAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.25f);
            Vector2 throwTarget = target.Center + target.velocity * 18f;
            Content.Projectiles.Enemy.Weapons.EnemyCaltrop.ThrowSpread(NPC.GetSource_FromThis(), origin, throwTarget, MagicDamage, 1.5f, Main.myPlayer);
        }
    }

    // The Shadow Ninja's Shadow Self (see "Shadow Self" in ShadowNinja): the same puppet in Phase-dyed armour, melee
    // only - combos, Shadow Leap and both flips, its back flip ending in a smoke step. Its own HP pool, so damage
    // never reaches the boss; no loot. It is struck dead the moment its summoner is gone.
    public class ShadowNinjaShade : ShadowNinja
    {
        // Server only: set by the summoner right after spawning.
        internal int OwnerIndex = -1;

        // Stands in its arrival smoke this long before acting (attack-quality-pass §7: arrive, then a delay).
        private const int ArrivalTicks = 45;
        private int _arrivalTicks = ArrivalTicks;

        protected override bool IsShadowShade => true;
        protected override string InvaderTitle => "Shadow Self";
        protected override bool AnnounceInvasion => false;
        protected override int EstusChargesMax => 0;

        protected override int RangedWeaponItemType => -1;
        protected override int SecondaryRangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => -1;

        protected override int HeadArmorDyeItemType => ItemID.PhaseDye;
        protected override int BodyArmorDyeItemType => ItemID.PhaseDye;
        protected override int LegsArmorDyeItemType => ItemID.PhaseDye;

        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.lifeMax = (int)(NPC.lifeMax * ShadeHealthFraction);
            NPC.boss = false;
            NPC.value = 0f;
            NPC.npcSlots = 0f;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
        }

        // No Stamina Vessel or first-kill credit: that belongs to the real ninja.
        public override void OnKill()
        {
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit);

            bool died = NPC.life <= 0;
            if (died && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.8f, Pitch = -0.4f }, NPC.Center);
                SpawnShadeSmoke(40);
            }
        }

        public override void AI()
        {
            bool server = Main.netMode != NetmodeID.MultiplayerClient;
            if (server && !OwnerAlive())
            {
                NPC.StrikeInstantKill();
                return;
            }

            if (_arrivalTicks > 0)
            {
                if (_arrivalTicks == ArrivalTicks && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
                    SpawnShadeSmoke(36);
                }

                _arrivalTicks--;
                NPC.velocity.X *= 0.8f;

                if (NPC.HasValidTarget)
                {
                    int towardTarget = 1;
                    if (Main.player[NPC.target].Center.X < NPC.Center.X)
                    {
                        towardTarget = -1;
                    }

                    NPC.direction = towardTarget;
                    NPC.spriteDirection = towardTarget;
                }
                return;
            }

            base.AI();
        }

        private bool OwnerAlive()
        {
            if (OwnerIndex < 0 || OwnerIndex >= Main.maxNPCs)
            {
                return false;
            }

            NPC owner = Main.npc[OwnerIndex];
            return owner.active && owner.type == ModContent.NPCType<ShadowNinja>();
        }

        private void SpawnShadeSmoke(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3.5f);
                Dust smoke = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(10f, 20f), DustID.Smoke, velocity, 110,
                    new Color(70, 60, 95), Main.rand.NextFloat(1.3f, 2f));
                smoke.noGravity = true;
            }
        }
    }
}
