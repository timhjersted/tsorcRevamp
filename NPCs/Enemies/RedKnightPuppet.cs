using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Armor.RedKnightSet;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>
    /// The Red Knight family as a PuppetNPC: Red Knight and Great Red Knight share this body. RedKnightAttackController
    /// still authors every special attack; each one runs as a single AttackPhase.Custom so the puppet body, SF4
    /// movement and the phase snapshot carry it, and the spear combos are puppet melee combos. Subclasses supply
    /// stats, spawns, loot and which controller attacks they may start. See tsorcDocs/RedKnightPuppetConversion_Proposal.md.
    /// </summary>
    public abstract class RedKnightPuppet : PuppetNPC
    {
        #region Defaults
        // Declared Expert Mode damage before player defense: KnightAttackStats carries these to the controller, which
        // wraps every spawn in EnemyDamage.Projectile. These defaults are Red Knight's HM values; subclasses override.
        public int redKnightsSpearDamage = 80;  // Spear combos, lunges & spear throws (real expert damage)
        public int redMagicDamage = 60;         // Poison/cinder rain, stormbolts & standards (real expert damage)
        public int redKnightsGreatDamage = 72;  // Great lunges, royal spears & shadow hands (real expert damage)
        public int redKnightsBombDamage = 104;  // Firebombs (real expert damage)
        private protected readonly RedKnightAttackController specialAttacks = new RedKnightAttackController();

        // A controller attack ends itself (Finish/Cancel); this cap only stops a stuck Custom phase from living forever.
        const int ControllerPhaseCapTicks = 3600;

        const string DoubleLungeName = "Double Spear Lunge";
        const string RisingSkewerName = "Rising Skewer";
        const string ChargingThrustName = "Charging Spear Thrust";

        // ── Melee combos (Phase 3) ───────────────────────────────────────────────────────────────────
        // Crystal Knight's spear template: every thrust is a JoustDash held level (start = end pose), the visible reach
        // comes from the authored grip (OverrideSpearGrip: 0.45 -> 0.97 over 6t, hold, retract) and the hitbox is the
        // drawn shaft (ModifyFrontBladeCapsule). No damage before the punch lands (CanDamageWithMeleeStep).
        //
        // Double Spear Lunge — close (<= 120px).
        //   Tell 36t on screen | thrust 24t: punch 6t, live 6..15 (10t, rollable), retract | pause 26 | thrust 2 (0.9x).
        //   Thrust 2 goes live 41t after thrust 1's live window ends (>= 30: a late roller can roll again) and 50t after
        //   thrust 1 went live (>= 40: both can damage). It only follows if the target is still in front within 130px.
        //   Push 1.2 x 1.8 px/t = ~52px per thrust. Recovery 36t. Counter: roll through either thrust, or back off.
        // Rising Skewer — close (<= 120px), the anti-jump follow-up.
        //   Tell 30t | level thrust 24t (live 6..15) | pause 14 | ONLY if the target is airborne or 24px+ above:
        //   an underhand rising sweep, swing 1.13 -> -0.11 (facing 20 degrees down to 52 degrees up, ~71 degrees),
        //   Weighted 4/18 k6 -> peak 71 / (4/3 + 18 * (1 - e^-6) / 6) = ~16 deg/t, live to 0.35 (8t). Its 23t gap is fine because it is position-
        //   conditional (attack-timing-design §2 rule 3). Recovery 34t. Counter: stay grounded, or roll it.
        // Charging Spear Thrust — mid (130-300px), RangedStartOnly.
        //   Tell 45t, ember glints at the tip for the last 30 | the dash direction and length (gap + 150px overshoot,
        //   96-420px) lock 10t before release | 30t dash on Crystal Knight's DashProgress curve (6t cubic in, 14t
        //   cruise, 10t out; ~21 px/t peak at full length) | live 6..19 (13t, rollable) | recovery 42t.
        //   Aborts into a harmless recovery when the floor or a wall breaks the path. Counter: roll through it;
        //   rolling away fails, the dash overshoots ~150px past where the target stood.
        const int ThrustAttackTicks = 24;
        const int ThrustPunchTicksMelee = 6;
        const int ThrustLiveEndTick = 15;
        const int ChargeAttackTicks = 30;
        const int ChargeLiveEndTick = 19;
        const int ChargeLockLeadTicks = 10;
        const float ChargeOvershootPixels = 150f;
        const float ChargeMinimumDistance = 96f;
        const float ChargeMaximumDistance = 420f;
        const float SkewerTargetAbovePixels = 24f;
        const float CloseComboMaximumRange = 120f;
        const float LungeFollowUpRange = 130f;
        const float ChargeMinimumRange = 130f;
        const float ChargeMaximumRange = 300f;
        // Swing-space poses: level thrust (facing 0), and the rising sweep from 20 degrees down to 52 degrees up.
        const float ThrustSwingPose = MathHelper.PiOver4;
        const float SkewerStartSwingPose = 1.13f;
        const float SkewerEndSwingPose = -0.11f;

        static readonly MeleeCombo[] RedKnightCombos =
        {
            new MeleeCombo
            {
                Name = DoubleLungeName, BaseWeight = 100, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed, CooldownAfterUse = 75, RecoveryTicks = 36,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash, TelegraphTicks = 36, AttackTicks = ThrustAttackTicks,
                        PostStepPause = 26, DamageMult = 1f, ReachMult = 1f, ForwardPushMult = 1.2f,
                        SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = ThrustLiveEndTick / (float)ThrustAttackTicks,
                    },
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash, TelegraphTicks = 0, AttackTicks = ThrustAttackTicks,
                        DamageMult = 0.9f, ReachMult = 1f, ForwardPushMult = 1.2f,
                        SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = ThrustLiveEndTick / (float)ThrustAttackTicks,
                    },
                },
            },
            new MeleeCombo
            {
                Name = RisingSkewerName, BaseWeight = 70, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed, CooldownAfterUse = 120, RecoveryTicks = 34,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash, TelegraphTicks = 30, AttackTicks = ThrustAttackTicks,
                        PostStepPause = 14, DamageMult = 1f, ReachMult = 1f, ForwardPushMult = 0.8f,
                        SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = ThrustLiveEndTick / (float)ThrustAttackTicks,
                    },
                    // HitWindowEnd = (in + out * ln(1/0.3) / k) / AttackTicks = (4 + 18 * 1.204 / 6) / 22.
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.UnderhandArc, TelegraphTicks = 0, AttackTicks = 22,
                        DamageMult = 1.1f, ReachMult = 1f, ForwardPushMult = 0.6f, SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Weighted, EaseInTicks = 4, EaseOutTicks = 18, EaseOutDecay = 6f,
                        HitWindowEnd = 0.346f,
                    },
                },
            },
            new MeleeCombo
            {
                Name = ChargingThrustName, BaseWeight = 100, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.OrangeRed, RangedStartOnly = true, CooldownAfterUse = 150, RecoveryTicks = 42,
                Steps = new[]
                {
                    // The dash is authored (OnMeleeComboAttackTick), so no constant push.
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash, TelegraphTicks = 45, AttackTicks = ChargeAttackTicks,
                        DamageMult = 1.3f, ReachMult = 1f, ForwardPushMult = 0f, SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = ChargeLiveEndTick / (float)ChargeAttackTicks,
                    },
                },
            },
        };

        // Charging Spear Thrust's locked dash (server decides at the lock tick; synced).
        float _chargeDistance;
        int _chargeDirection = 1;
        // This tick's tile-clipped dash velocity, re-asserted after the base AI so SF4 can't overwrite it.
        Vector2 _chargeVelocity;

        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        // Pre-hardmode it is a boss with its own despawn handler (AI below); the puppet party-wipe one would double it.
        protected override bool DespawnsOnPartyWipe => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        // The 1-in-6 dodge roll from EvasiveProfile.RedKnight is bounded by its 120t cooldown.
        protected override bool AllowReactiveDefense => true;

        // Pure black under the armour, so any skin pixel the RedKnightSet sheets leave uncovered (neck, wrist gaps)
        //   reads as shadow instead of the base puppet's orange-pink.
        protected override Color PuppetSkinColor => Color.Black;

        protected override int HeadArmorItemType => ModContent.ItemType<RedKnightHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<RedKnightBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<RedKnightGreaves>();

        // ItemID.Spear is only the held-item identity; the drawn shaft is BlackKnightSpear (SpearDrawTexturePath).
        protected override int MeleeWeaponItemType => ItemID.Spear;
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => EnemyDamage.Projectile(redKnightsSpearDamage); // PuppetNPC spawns its blade hitboxes with this raw value
        protected override int RangedDamage => 0;
        protected override void DoMeleeAttack() { } // All melee is the combo pool above.
        protected override void DoRangedAttack() { }

        protected override float TopSpeed => 1.8f;
        protected override float Acceleration => 0.07f;
        protected override float PuppetJumpPower => 12f;
        protected override float PuppetJumpBoost => 6f;

        protected override MeleeCombo[] MeleeComboPoolOverride => RedKnightCombos;
        // No legacy slash path: MeleeRange 0 leaves the combo pool as the only melee.
        protected override float MeleeRange => 0f;
        protected override int MeleeComboChance => 100;
        // Rolled per neutral tick from 130-300px, so the charge comes out after ~25t of walking in, not instantly.
        protected override int RangedStartMeleeComboChance => 4;
        protected override float ComboReachBase => 90f;
        protected override float MeleeEngageRange => CloseComboMaximumRange;
        protected override float ComboMaxStartRange => ChargeMinimumRange;
        protected override float RangedStartComboMaxRange => ChargeMaximumRange;
        // Authored tells play as written (no 1.35x stretch); the 20t floor never binds (shortest tell is 30t).
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override int TelegraphFacingCommitTicks => 10;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override int MeleeComboInterStepLingerTicks => 5;
        protected override int MeleeRecoveryLingerTicks => 8;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Halberd;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool AuthoredClockCoversJoustDash => true;
        protected override bool HasSlashVFX => false;
        protected override float MeleeBladeWidth => 10f;

        protected override bool DrawWeaponAsSpear => true;
        protected override bool UseAuthoredSpearGrip => true;
        protected override bool MirrorSpearRotationByFacing => true;
        protected override string SpearDrawTexturePath => UsefulFunctions.RefactorableFilepath(typeof(BlackKnightSpear));
        // BlackKnightSpear.png measured: 14x62, tip straight up, top opaque row 0 and bottom row 61, both at x 6-7.
        protected override Vector2 SpearTextureSize => new Vector2(14f, 62f);
        protected override Vector2 SpearHeadNorm => new Vector2(0.5f, 0.5f / 62f);
        protected override Vector2 SpearBaseNorm => new Vector2(0.5f, 61.5f / 62f);
        // Swing space puts 0 on the sword's 45-degree up-forward diagonal; a tip-up sprite needs +45 to match it.
        protected override float SpearDrawRotationOffset => MathHelper.PiOver4;
        protected override Vector2 MeleeHandleNorm => new Vector2(0.5f);

        // Controller attacks own velocity (hops, dashes, retreats); the puppet must not brake or path under them.
        protected override bool SlowDownDuringCustom => false;

        // ── Controller attack poses (Phase 2) ────────────────────────────────────────────────────────
        // Every held prop lives in the FRONT hand: no RK attack holds the spear and a bomb at the same time, and
        // the puppet only poses the back arm for a second weapon. A bomb is the spear identity with its sprite
        // hidden (HiddenSpearWeapon), so the composite arm still poses while PostDraw draws the bomb in the fist.
        //
        // Angles are FACING space: 0 = straight ahead, negative = up, mirrored for a left-facing puppet.
        // Swing space (_weaponRotation) = facing + 45 degrees (see SpearDrawRotationOffset).
        //
        // Spear Throw (release R = 45):  appears R-40 | carry until R-30 | 24t javelin draw-back: the arm swings back
        //   over the shoulder (arm offset -2.565 rad) while the spear tilts 0.35 rad above the aim, ember glints at
        //   the tip | 6t cubic whip: arm and spear snap forward onto the aim |
        //   leaves the hand at R from AttackHandWorld. Same windows for the Adder Spear (R 55) and the
        //   retreat-teleport throw (R = its damage tick; the first half of its draw-back is spent hidden).
        // Firebomb Throw (release R = 60, Ember Reversal R = 120): appears R-40 at the hip | fuse lit and
        //   24t wind-up from R-30: overhand (aim - 1.6) when the solved arc climbs steeper than 25 degrees,
        //   otherwise underhand (arm swung back to 2.1 rad, behind the leg) | 6t whip | thrown at R.
        // Thrust (strike S, the lunge-hitbox tick): appears S-24 | 14t cock: grip slides to 0.32 (shaft drawn
        //   back through the fist) and the tip dips 10 degrees | 5t punch to grip 0.97 | 8t hold | 12t retract.
        //   The lunge hitbox reaches 70-84px from the centre; the extended shaft reaches ~70px, so they agree.
        const int PropShowAheadTicks = 10;
        const int ThrowWindupTicks = 24;
        const int ThrowWhipTicks = 6;
        const float SpearCarryFacing = 0.2f;
        const float ThrowCockRadians = 1.6f;
        // The held spear is drawn along the arm's swing pose, so the arm alone can't go back while the spear points
        // forward. MeleeCompositeArmRotationOffset rotates just the arm; the spear follows the hand. 2.6 rad puts the
        // hand ~123 degrees up-back from ahead at full cock (rendered in SwingPreview --knight-poses; 2.6 sat ~2 degrees too far back).
        const float ThrowArmDrawBack = 2.565f;
        const float ThrowSpearLift = 0.35f;
        const float BombHipFacing = 1.0f;
        const float BombUnderhandFacing = 2.1f;
        const float BombOverhandAboveFacing = -0.45f;
        const int ThrustCockTicks = 14;
        const int ThrustPunchTicks = 5;
        const int ThrustHoldTicks = 8;
        const int ThrustRetractTicks = 12;
        const float ThrustTipDip = 0.17f;
        const float SpearGripCarry = 0.55f;
        const float SpearGripCocked = 0.32f;
        const float SpearGripExtended = 0.97f;
        const float ThrowGrip = 0.6f;
        // Keeps the arm from folding through the head (behind) or the back (low and behind), in swing space.
        const float MinimumSwingPose = -1.75f;
        const float MaximumSwingPose = 2.9f;

        // Palm cast (release R = PalmCastReleaseTick: Poison Arc Volley / Rain / Curtain 60, Adder Spear channel 148):
        //   the EMPTY back arm rises from the hang over 24t to 0.45 rad above the cast aim, toxic motes gathering in
        //   the palm | 6t cubic push onto the aim | spell leaves the palm at R | 14t hold | 12t lower to the hang.
        //   Aim: Arc Volley = its solved lob, Rain = straight up, Curtain = the target, Adder channel = the thread.
        // Spectral Hand Barrage (R 60, hands stream until 90): BOTH arms rise overhead, then fling apart onto the
        //   aim (front 0.3 rad above it, back 0.3 below); the hands stream from the back palm; hold to 90 + 14.
        const int PalmRaiseTicks = 24;
        const int PalmPushTicks = 6;
        const int PalmHoldTicks = 14;
        const int PalmLowerTicks = 12;
        const float PalmHangFacing = 1.4f;
        const float PalmReadyLift = 0.45f;
        // Up-forward, not straight up: the back arm draws under the torso, and a vertical palm hides behind the head.
        const float PalmRainFacing = -0.95f;
        const float PalmMinimumFacing = -1.5f;
        const float PalmMaximumFacing = 1.0f;
        // Raised up-forward: much past -0.8 the front arm crosses the face (rendered in SwingPreview --knight-poses).
        const float SpectralFrontOverheadFacing = -0.8f;
        const float SpectralBackOverheadFacing = -0.65f;
        const float SpectralArmSpread = 0.3f;

        // Royal / Crimson Standard heave (release R = StandardReleaseTick, 75): appears R-54 | 36t draw-back, longer
        //   than the javelin, spear tilted 0.5 rad above its flight line | 8t whip | released at R. Great Red Knight
        //   holds it in both hands (UsesGreatKnightPoses); Red Knight heaves one-handed.
        const int HeaveWindupTicks = 36;
        const int HeaveWhipTicks = 8;
        const float HeaveSpearLift = 0.5f;
        // A standard's lob is already steep; past ~63 degrees up the cocked lance stands vertical behind the head.
        const float HeaveHighestCockFacing = -1.1f;
        // Crimson Dominion plant (phase 1, controller Timer; DominionHoldTicks 300): spear point-down in both hands,
        //   raised to the chest over 0-29 | carried through the hop (30) | 8t slam from 52 drives it in | held |
        //   last 50t retract back to the carry. The death finale replants: raise 0-21, slam 22-29, held to death.
        //   Arm offsets are on top of the point-down swing pose (hand 135 degrees, down-back): -2.7 lifts the hands to
        //   ~20 degrees above ahead, -1.45 sets them ~50 degrees below ahead, in front of the knight.
        const float PlantFacing = MathHelper.PiOver2;
        const float PlantRaisedArmOffset = -2.7f;
        const float PlantArmOffset = -1.45f;
        // Fractions of GRK's ~115px lance, head = 0. The arms can't lift the hands far above the chest, so the raise holds
        // the lance high in the fists (0.22, hands at the crossguard, tip near the feet) and the slam slides it down
        // through them (0.33) to sink the blade ~15px into the floor, as the old hand-drawn GRK plant did. Rendered.
        const float PlantGrip = 0.22f;
        const float PlantDeepGrip = 0.33f;
        const int PlantRaiseEndTick = 30;
        const int PlantSlamStartTick = 52;
        const int PlantSlamTicks = 8;
        const int PlantRetractTicks = 50;
        const int FinaleRaiseEndTick = 22;
        // Stormbreaker Edict: after its lunge, the spear is raised straight up (arm ~80 degrees up) over 12t and held
        //   while the lightning lanes resolve.
        // Heralds (Furnace 150t, Storm 210t): the same raised spear for the whole cast, up over 12t and lowered over
        //   the last 16t.
        const int HeraldLowerTicks = 16;
        const int StormRaiseStartTick = 85;
        const int StormRaiseTicks = 12;
        const float StormRaiseArmOffset = -0.6f;

        KnightHeldProp _frontProp;
        float? _backPalmFacing;
        float? _frontFreeFacing;
        bool _bombFuseLit;
        bool _poseTwoHanded;
        float _poseFacing;
        float _poseArmOffset;
        float _poseGrip = SpearGripCarry;
        PuppetWeapon _hiddenSpearWeapon;

        /// <summary>World position of the front hand, captured before each controller tick. The controller throws
        /// spears and bombs from here (SpearLaunchSource / BombLaunchSource).</summary>
        internal Vector2 AttackHandWorld { get; private set; }

        /// <summary>World position of the back palm, captured with AttackHandWorld. Palm spells leave from here.</summary>
        internal Vector2 CastHandWorld { get; private set; }

        PuppetWeapon HiddenSpearWeapon => _hiddenSpearWeapon ??= new PuppetWeapon(MeleeWeaponItemType,
            handleNorm: MeleeHandleNorm, bladeWidth: MeleeBladeWidth, archetype: MeleeArchetype, hideHeldSprite: true);

        /// <summary>Great Red Knight: the heave and the Dominion plant are two-handed.</summary>
        protected virtual bool UsesGreatKnightPoses => false;

        /// <summary>A Custom phase the subclass runs without a controller attack (GRK's death finale) still poses.</summary>
        protected virtual bool HoldsFinalePose => false;

        bool PoseActive => Phase == AttackPhase.Custom && (specialAttacks.Active || HoldsFinalePose);
        bool FrontHandHoldsProp => PoseActive && (_frontProp == KnightHeldProp.Spear || _frontProp == KnightHeldProp.Bomb);
        protected override bool UseCompositeArmForAdditionalPhase => FrontHandHoldsProp;
        // The Custom phase is entered with the spear as its pose weapon; put it away when the hand is empty.
        protected override bool WeaponSheathed => PoseActive && !FrontHandHoldsProp;

        protected override float? FreeBackArmFacingAngle => PoseActive ? _backPalmFacing : null;
        protected override float? FreeFrontArmFacingAngle => PoseActive && !FrontHandHoldsProp ? _frontFreeFacing : null;

        // The javelin draw-back swings the arm off the spear's line (see ThrowArmDrawBack); 0 outside a held prop.
        protected override float MeleeCompositeArmRotationOffset => FrontHandHoldsProp ? _poseArmOffset : 0f;

        // The back hand joins the front on the shaft for the poses that ask for it (PuppetNPC's two-handed solve).
        protected override bool UseTwoHandedCompositeSwing => FrontHandHoldsProp && _poseTwoHanded;

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (!FrontHandHoldsProp)
                {
                    return null;
                }

                return SwingPoseFor(_poseFacing);
            }
        }

        /// <summary>Swing-space weapon rotation for a held prop's facing angle, clamped so the arm never folds
        /// through the head or back. Public so SwingPreview poses the arm exactly as the game does.</summary>
        public static float SwingPoseFor(float facing)
        {
            float swingPose = facing + MathHelper.PiOver4;
            return MathHelper.Clamp(swingPose, MinimumSwingPose, MaximumSwingPose);
        }

        protected override float? OverrideSpearGrip(float swingClockT)
        {
            if (FrontHandHoldsProp)
            {
                return _poseGrip;
            }

            bool inCombo = Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery;
            if (!inCombo)
            {
                return null;
            }

            if (Phase != AttackPhase.MeleeComboAttack)
            {
                // Cocked: the shaft drawn back through the fist, ready to punch.
                return SpearGripCocked;
            }

            // The rising sweep is held extended for its whole arc; a pole doesn't telescope mid-swing.
            if (ActiveMeleeComboMotion == ComboMotion.UnderhandArc)
            {
                return SpearGripExtended;
            }

            return ThrustGrip(swingClockT, ActiveMeleeComboName == ChargingThrustName);
        }

        /// <summary>Spear grip through a thrust step: punch out over 6t, hold to the end of the live window, retract
        /// over the rest. Public and static so SwingPreview renders the exact curve the game plays.</summary>
        public static float ThrustGrip(float progress, bool charge)
        {
            int attackTicks = ThrustAttackTicks;
            int holdEnd = ThrustLiveEndTick;
            if (charge)
            {
                attackTicks = ChargeAttackTicks;
                holdEnd = ChargeLiveEndTick;
            }

            float elapsed = progress * attackTicks;
            if (elapsed < ThrustPunchTicksMelee)
            {
                return MathHelper.SmoothStep(SpearGripCocked, SpearGripExtended, elapsed / ThrustPunchTicksMelee);
            }
            if (elapsed < holdEnd)
            {
                return SpearGripExtended;
            }
            return MathHelper.SmoothStep(SpearGripExtended, SpearGripCarry, (elapsed - holdEnd) / (attackTicks - holdEnd));
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (motion == ComboMotion.JoustDash)
            {
                startRotation = ThrustSwingPose;
                endRotation = ThrustSwingPose;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = SkewerStartSwingPose;
                endRotation = SkewerEndSwingPose;
            }
        }

        // Hit detection follows the drawn shaft to its real tip, so what is seen is what hits.
        protected override void ModifyFrontBladeCapsule(ref Vector2 origin, ref Vector2 tip)
        {
            tip = PuppetSpearTipPosition;
        }

        protected override bool CanDamageWithMeleeStep(MeleeComboStep step, float progress)
        {
            // A charge whose path broke at the lock tick plays out harmlessly into its recovery.
            if (ActiveMeleeComboName == ChargingThrustName && _chargeDistance <= 0f)
            {
                return false;
            }

            // Thrusts only hurt once the punch is out; the cocked shaft brushing a player does nothing.
            if (step.Motion == ComboMotion.JoustDash)
            {
                return progress >= ThrustPunchTicksMelee / (float)step.AttackTicks;
            }

            return true;
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == ChargingThrustName)
            {
                bool inBand = distance >= ChargeMinimumRange && distance <= ChargeMaximumRange;
                return inBand && NPC.HasValidTarget && CanChargeTo(Main.player[NPC.target]);
            }

            return distance <= CloseComboMaximumRange;
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (!target.active || target.dead)
            {
                return false;
            }

            bool inFront = Math.Sign(target.Center.X - NPC.Center.X) == NPC.direction;
            if (comboName == DoubleLungeName)
            {
                return inFront && NPC.Distance(target.Center) <= LungeFollowUpRange;
            }

            if (comboName == RisingSkewerName)
            {
                // Anti-jump: only rises after a target that left the ground or stands on something above.
                bool airborne = target.velocity.Y != 0f;
                bool above = target.Center.Y < NPC.Center.Y - SkewerTargetAbovePixels;
                return inFront && (airborne || above);
            }

            return true;
        }

        // Path check for the charge: open air at body height and floor under every 16px of it, out to the overshoot.
        bool CanChargeTo(Player target)
        {
            int direction = Math.Sign(target.Center.X - NPC.Center.X);
            if (direction == 0)
            {
                direction = NPC.direction;
            }

            float travel = Math.Abs(target.Center.X - NPC.Center.X) + ChargeOvershootPixels;
            travel = MathHelper.Clamp(travel, ChargeMinimumDistance, ChargeMaximumDistance);
            for (float x = 16f; x <= travel; x += 16f)
            {
                Vector2 position = NPC.position + new Vector2(direction * x, 0f);
                bool blocked = Collision.SolidCollision(position, NPC.width, NPC.height);
                bool floored = Collision.SolidCollision(position + new Vector2(0f, NPC.height), NPC.width, 20);
                if (blocked || !floored)
                {
                    return false;
                }
            }
            return true;
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name != ChargingThrustName)
            {
                return;
            }

            // Lock the dash 10t before release; a path that broke during the tell aborts into a harmless recovery.
            if (elapsed == total - ChargeLockLeadTicks && Main.netMode != NetmodeID.MultiplayerClient && NPC.HasValidTarget)
            {
                Player target = Main.player[NPC.target];
                _chargeDirection = Math.Sign(target.Center.X - NPC.Center.X);
                if (_chargeDirection == 0)
                {
                    _chargeDirection = NPC.direction;
                }

                _chargeDistance = 0f;
                if (CanChargeTo(target))
                {
                    float travel = Math.Abs(target.Center.X - NPC.Center.X) + ChargeOvershootPixels;
                    _chargeDistance = MathHelper.Clamp(travel, ChargeMinimumDistance, ChargeMaximumDistance);
                }
                NPC.netUpdate = true;
                RequestNetworkSnapshot();
            }

            // Ember glints gathering on the tip for the last 30t of the tell.
            if (!Main.dedServ && elapsed >= total - 30 && elapsed % 2 == 0)
            {
                Dust glint = Dust.NewDustPerfect(PuppetSpearTipPosition + Main.rand.NextVector2Circular(3f, 3f), DustID.Torch,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 80, default, Main.rand.NextFloat(1f, 1.4f));
                glint.noGravity = true;
            }
        }

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name != ChargingThrustName || _chargeDistance <= 0f)
            {
                return;
            }

            // Distance-solved dash: this tick's share of the locked length on the 30t accelerate-cruise-decay curve.
            float speed = _chargeDistance
                * (SuperHardMode.CrystalKnight.DashProgress(elapsed + 1) - SuperHardMode.CrystalKnight.DashProgress(elapsed));
            Vector2 desired = new Vector2(_chargeDirection * speed, 0f);
            _chargeVelocity = Collision.TileCollision(NPC.position, desired, NPC.width, NPC.height);
            NPC.velocity.X = _chargeVelocity.X;
            if (_chargeVelocity.X != 0f)
            {
                AfterimageTicks = 3;
            }
        }

        #endregion

        #region Hit Logic
        // EvasiveOnHit flags the hit on a client and the hit report carries it to the server, which rolls it.
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);
            // The stagger already dropped the Custom phase; drop the controller attack it was running too.
            if (Main.netMode != NetmodeID.MultiplayerClient && specialAttacks.Active)
            {
                specialAttacks.Cancel(NPC, NPC.GetGlobalNPC<tsorcRevampGlobalNPC>());
                RequestNetworkSnapshot();
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            specialAttacks.Send(writer);
            writer.Write(_chargeDistance);
            writer.Write((sbyte)_chargeDirection);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            specialAttacks.Receive(reader);
            _chargeDistance = reader.ReadSingle();
            _chargeDirection = reader.ReadSByte();
        }
        #endregion

        #region AI
        /// <summary>Server only, from neutral with a valid target: let the controller pick and begin an attack
        /// (TryStartRed / TryStartGreat / TryStartDominion). True when one started.</summary>
        private protected abstract bool TryStartControllerAttack(Player target, tsorcRevampGlobalNPC globalNPC);

        public override void AI()
        {
            specialAttacks.TickCooldowns();
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            _chargeVelocity = Vector2.Zero;

            // Server picks the next controller attack from neutral. The controller's CanStart still reads ai[1] as the
            // old conveyor's neutral clock (60-119); nothing on a puppet writes it, so pin it there while idle.
            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
            if (!specialAttacks.Active)
            {
                NPC.ai[1] = 60f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && neutral && NPC.HasValidTarget && !specialAttacks.Active
                && !InFinisherPunishWindow)
            {
                Player target = Main.player[NPC.target];
                bool started = TryStartControllerAttack(target, globalNPC);
                if (started)
                {
                    // The spear is the pose weapon; WeaponSheathed hides it for the bomb/magic stretches.
                    StartCustomAttack(ControllerPhaseCapTicks, MeleeWeaponItemType, swingPose: false);
                    NPC.netUpdate = true;
                }
            }

            base.AI();

            // The charge set its velocity inside the attack tick; movement code after it in the base tick must not win.
            if (_chargeVelocity != Vector2.Zero)
            {
                NPC.velocity.X = _chargeVelocity.X;
            }

            // A stagger, teleport or the safety cap ended the Custom phase under a live attack: drop the attack.
            if (Main.netMode != NetmodeID.MultiplayerClient && specialAttacks.Active && Phase != AttackPhase.Custom)
            {
                specialAttacks.Cancel(NPC, globalNPC);
                RequestNetworkSnapshot();
            }

            // A bomb pose swapped in the sprite-hidden spear; give the melee combos their visible spear back.
            if (!PoseActive)
            {
                _frontProp = KnightHeldProp.None;
                EquipWeapon(PuppetHandSlot.Front, DefaultFrontWeapon);
            }

            if (specialAttacks.Active && Phase == AttackPhase.Custom)
            {
                // The base cleared both flags for Custom at the end of its tick; the controller's tell/commit split wins.
                specialAttacks.SetCombatFlags(NPC, globalNPC);
                DebugAttackLabel = specialAttacks.DebugAttackName;
                Lighting.AddLight(NPC.Center, new Color(150, 25, 35).ToVector3() * 0.45f);
            }
            else
            {
                DebugAttackLabel = null;
                Lighting.AddLight(NPC.Center, new Color(150, 25, 35).ToVector3() * 0.35f);
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            // The controller drives velocity for the whole attack, including its throw hops and dashes.
            if (Phase == AttackPhase.Custom)
            {
                return;
            }

            // attackRange is SF4's engage/re-aggro radius, not a weapon reach: RK throws and casts out to ~850px.
            SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * speedMult, acceleration: Acceleration,
                doorBreakingDamage: 4, attackRange: 700f);
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (!NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            KnightAttackStats attackStats = new KnightAttackStats(
                redKnightsSpearDamage, redMagicDamage, redKnightsGreatDamage, redKnightsBombDamage);

            // Pose from the same Timer the controller is about to process, so a release tick shows the release pose.
            // The hand position is last tick's (the arm is posed after this, in TickWeaponAnim); one tick is invisible.
            AttackHandWorld = PuppetHandPosition;
            CastHandWorld = PuppetBackHandPosition;
            UpdateControllerPose();
            if (_frontProp == KnightHeldProp.Bomb)
            {
                EquipWeapon(PuppetHandSlot.Front, HiddenSpearWeapon);
            }
            else
            {
                EquipWeapon(PuppetHandSlot.Front, DefaultFrontWeapon);
            }

            // The controller marks netUpdate at its branch points (bomb return dash, teleport snap, every 60t).
            // netSpam can hold a plain netUpdate for up to 30t, so turn each new request into a direct snapshot.
            bool netUpdateBefore = NPC.netUpdate;
            specialAttacks.Tick(NPC, target, attackStats);
            if (!netUpdateBefore && NPC.netUpdate)
            {
                RequestNetworkSnapshot();
            }

            // Finish/Cancel ran inside Tick: end the Custom phase on this same tick (the base decrements to 0).
            if (!specialAttacks.Active)
            {
                PhaseTimer = 1;
            }
        }

        /// <summary>Facing-space angle (0 = ahead, negative = up) of a world direction for a puppet facing
        /// <paramref name="direction"/>; 0 for a zero vector. Public so SwingPreview aims exactly as the game does.</summary>
        public static float FacingAngleOf(Vector2 worldDirection, int direction)
        {
            if (worldDirection.LengthSquared() < 0.01f)
            {
                return 0f;
            }

            return (float)Math.Atan2(worldDirection.Y, worldDirection.X * direction);
        }

        /// <summary>Everything the controller-attack pose reads, as plain values, so SwingPreview drives the same
        /// function offline. Ticks are the controller's pose cues (-1 = none); aims are facing-space radians.</summary>
        public struct ControllerPoseInput
        {
            public KnightSpecialAttack Attack;
            public KnightHeldProp HeldProp;
            public int Timer;
            public int ThrowRelease;
            public int BombRelease;
            public int ThrustStrike;
            public int PalmRelease;
            public int PalmEnd;
            public float ThrowAim;
            public float BombAim;
            public float SpearAim;
            public float CastAim;
            public bool GreatKnight;
            public int StandardRelease;
            public int HeraldEnd;
            public int DominionTimer;
            public bool DominionFinale;
        }

        /// <summary>One tick of the controller-attack pose. Facing is the front arm's facing-space angle while it
        /// holds a prop; the palm angles are null when that arm is not gesturing.</summary>
        public struct ControllerPose
        {
            public KnightHeldProp FrontProp;
            public float Facing;
            public float ArmOffset;
            public float Grip;
            public bool FuseLit;
            public bool ThrowDrawBack;
            public bool TwoHanded;
            public float? BackPalmFacing;
            public float? FrontFreeFacing;
        }

        /// <summary>
        /// World-facing half of the pose: reads the controller's cues and solves this tick's aims, then hands them
        /// to <see cref="ResolveControllerPose"/>. Runs on every machine from synced controller state.
        /// </summary>
        private protected void UpdateControllerPose()
        {
            ControllerPoseInput input = new ControllerPoseInput
            {
                Attack = specialAttacks.Attack,
                HeldProp = specialAttacks.HeldProp,
                Timer = specialAttacks.Timer,
                ThrowRelease = specialAttacks.SpearThrowReleaseTick,
                BombRelease = specialAttacks.BombReleaseTick,
                ThrustStrike = specialAttacks.SpearThrustStrikeTick,
                PalmRelease = specialAttacks.PalmCastReleaseTick,
                PalmEnd = specialAttacks.PalmCastEndTick,
            };

            input.ThrowAim = FacingAngleOf(specialAttacks.LockedVelocity, NPC.direction);
            input.GreatKnight = UsesGreatKnightPoses;
            input.StandardRelease = specialAttacks.StandardReleaseTick;
            input.HeraldEnd = specialAttacks.HeraldEndTick;
            input.DominionTimer = -1;
            if (input.Attack == KnightSpecialAttack.CrimsonDominion)
            {
                input.DominionTimer = input.Timer;
            }

            // A standard flies on its own solved arc; aim the heave down that line from the first tick of the tell.
            if (input.StandardRelease >= 0)
            {
                bool royal = input.Attack == KnightSpecialAttack.RoyalStandard;
                Vector2 flight = RedKnightStandard.InitialFlightDirection(AttackHandWorld, specialAttacks.LockedTarget, royal);
                input.ThrowAim = FacingAngleOf(flight, NPC.direction);
            }

            // Firebomb Throw has its solved velocity from tick 30; Ember Reversal only solves on release, so solve the
            // same arc here for the pose.
            if (input.BombRelease >= 0)
            {
                Vector2 launch = specialAttacks.LockedVelocity;
                if (launch.LengthSquared() < 0.01f)
                {
                    // Furnace Pincer tosses its delayed bomb at the flank point, not the player.
                    Vector2 bombTarget = specialAttacks.LockedTarget;
                    if (input.Attack == KnightSpecialAttack.FurnacePincer)
                    {
                        bombTarget = specialAttacks.AuxiliaryTargetA;
                    }
                    launch = UsefulFunctions.BallisticTrajectory(AttackHandWorld, bombTarget,
                        15f, 0.2f, highAngle: false, fallback: true);
                }
                input.BombAim = FacingAngleOf(launch, NPC.direction);
            }

            // GetSpearRotation is a tip-up sprite rotation (world angle + 90); NPC.Center stands in for the hand so the
            // aim does not feed back through the arm pose it sets.
            float spearSpriteRotation = specialAttacks.GetSpearRotation(NPC.Center, NPC.Center);
            Vector2 spearWorldDirection = (spearSpriteRotation - MathHelper.PiOver2).ToRotationVector2();
            input.SpearAim = FacingAngleOf(spearWorldDirection, NPC.direction);

            // NPC.Center stands in for the palm too. Rain ignores this aim (it casts straight up).
            if (input.PalmRelease >= 0)
            {
                Vector2 castTarget = specialAttacks.LockedTarget;
                if (castTarget == Vector2.Zero && NPC.HasValidTarget)
                {
                    castTarget = Main.player[NPC.target].Center;
                }

                Vector2 castDirection = castTarget - NPC.Center;
                if (input.Attack == KnightSpecialAttack.PoisonArcVolley)
                {
                    // Same lob the volley solves at release (4.6 px/t, gravity 1.1, high angle).
                    castDirection = UsefulFunctions.BallisticTrajectory(NPC.Center, castTarget, 4.6f, 1.1f,
                        highAngle: true, fallback: true);
                }
                else if (input.Attack == KnightSpecialAttack.PoisonSpearTether)
                {
                    castDirection = specialAttacks.AuxiliaryTargetA - NPC.Center;
                }
                input.CastAim = FacingAngleOf(castDirection, NPC.direction);
            }

            ModifyControllerPoseInput(ref input);
            ControllerPose pose = ResolveControllerPose(input);
            _frontProp = pose.FrontProp;
            _poseFacing = pose.Facing;
            _poseArmOffset = pose.ArmOffset;
            _poseGrip = pose.Grip;
            _bombFuseLit = pose.FuseLit;
            _backPalmFacing = pose.BackPalmFacing;
            _poseTwoHanded = pose.TwoHanded;
            _frontFreeFacing = pose.FrontFreeFacing;

            // Ember glint at the raised tip through the throw's draw-back, every other tick: OrangeRed like the trim.
            if (pose.ThrowDrawBack && !Main.dedServ && input.Timer % 2 == 0)
            {
                Dust glint = Dust.NewDustPerfect(PuppetSpearTipPosition, DustID.Torch,
                    Main.rand.NextVector2Circular(0.5f, 0.5f), 80, default, Main.rand.NextFloat(0.9f, 1.3f));
                glint.noGravity = true;
            }
        }

        /// <summary>Last say over this tick's pose input, after the controller's cues are read (GRK's death finale
        /// replants the spear with no controller attack running).</summary>
        protected virtual void ModifyControllerPoseInput(ref ControllerPoseInput input)
        {
        }

        /// <summary>
        /// Picks the front-hand prop with its facing angle and spear grip, plus the empty arms' palm-cast angles, from
        /// the controller's pose cues. Timings are in the block comment above the pose constants. Pure: public and
        /// static so SwingPreview renders exactly the poses the game plays.
        /// </summary>
        public static ControllerPose ResolveControllerPose(ControllerPoseInput input)
        {
            int timer = input.Timer;
            ControllerPose pose = new ControllerPose
            {
                FrontProp = KnightHeldProp.None,
                Facing = SpearCarryFacing,
                Grip = SpearGripCarry,
            };

            // ── Empty-arm palm cast (back arm; Spectral Hand Barrage also the front). Independent of the front-hand
            // prop below, so the Adder Spear's channel palm and its thrust spear overlap. ──
            int palmRelease = input.PalmRelease;
            if (palmRelease >= 0)
            {
                int pushStart = palmRelease - PalmPushTicks;
                int raiseStart = pushStart - PalmRaiseTicks;
                int lowerStart = input.PalmEnd + PalmHoldTicks;
                int lowerEnd = lowerStart + PalmLowerTicks;
                bool inCast = timer >= raiseStart && timer < lowerEnd;
                if (inCast)
                {
                    float castAim = input.CastAim;
                    // Rains from above cast with the palm raised up-forward instead of at the target.
                    bool rainCast = input.Attack == KnightSpecialAttack.PoisonRain
                        || input.Attack == KnightSpecialAttack.CinderRain
                        || input.Attack == KnightSpecialAttack.RedCourtProcession;
                    if (rainCast)
                    {
                        castAim = PalmRainFacing;
                    }
                    castAim = MathHelper.Clamp(castAim, PalmMinimumFacing, PalmMaximumFacing);

                    // Hang -> ready (SmoothStep) | cubic push onto the aim | hold through the cast | SmoothStep back down.
                    float StagePalm(float ready, float aim)
                    {
                        if (timer < pushStart)
                        {
                            float raise = (timer - raiseStart + 1f) / PalmRaiseTicks;
                            return MathHelper.SmoothStep(PalmHangFacing, ready, raise);
                        }
                        if (timer < palmRelease)
                        {
                            float push = (timer - pushStart + 1f) / PalmPushTicks;
                            return MathHelper.Lerp(ready, aim, push * push * push);
                        }
                        if (timer < lowerStart)
                        {
                            return aim;
                        }
                        float lower = (timer - lowerStart + 1f) / PalmLowerTicks;
                        return MathHelper.SmoothStep(aim, PalmHangFacing, lower);
                    }

                    if (input.Attack == KnightSpecialAttack.SpectralHandBarrage)
                    {
                        pose.BackPalmFacing = StagePalm(SpectralBackOverheadFacing, castAim + SpectralArmSpread);
                        pose.FrontFreeFacing = StagePalm(SpectralFrontOverheadFacing, castAim - SpectralArmSpread);
                    }
                    else
                    {
                        pose.BackPalmFacing = StagePalm(castAim - PalmReadyLift, castAim);
                    }
                }
            }

            // ── Heralds: spear raised straight up for the whole cast ──
            if (input.HeraldEnd >= 0)
            {
                float raise = MathHelper.SmoothStep(0f, 1f, (timer + 1f) / StormRaiseTicks);
                float lower = MathHelper.SmoothStep(0f, 1f, (input.HeraldEnd - timer) / (float)HeraldLowerTicks);
                float up = Math.Min(raise, lower);
                pose.FrontProp = KnightHeldProp.Spear;
                pose.Facing = MathHelper.Lerp(SpearCarryFacing, -MathHelper.PiOver2, up);
                pose.ArmOffset = MathHelper.Lerp(0f, StormRaiseArmOffset, up);
                return pose;
            }

            // ── Crimson Dominion plant / finale replant ──
            int plantTimer = input.DominionTimer;
            if (plantTimer >= 0)
            {
                int raiseEnd = PlantRaiseEndTick;
                int slamStart = PlantSlamStartTick;
                int retractStart = RedKnightAttackController.DominionHoldTicks - PlantRetractTicks;
                if (input.DominionFinale)
                {
                    raiseEnd = FinaleRaiseEndTick;
                    slamStart = FinaleRaiseEndTick;
                    retractStart = int.MaxValue;
                }
                int slamEnd = slamStart + PlantSlamTicks;

                pose.FrontProp = KnightHeldProp.Spear;
                pose.TwoHanded = input.GreatKnight;
                pose.Facing = PlantFacing;
                pose.Grip = PlantGrip;

                if (plantTimer < raiseEnd)
                {
                    float raise = MathHelper.SmoothStep(0f, 1f, (plantTimer + 1f) / raiseEnd);
                    pose.ArmOffset = MathHelper.Lerp(PlantArmOffset, PlantRaisedArmOffset, raise);
                }
                else if (plantTimer < slamStart)
                {
                    pose.ArmOffset = PlantRaisedArmOffset;
                }
                else if (plantTimer < slamEnd)
                {
                    // Cubic ease-in: the hands are fastest on the frame the spear bites.
                    float slam = (plantTimer - slamStart + 1f) / PlantSlamTicks;
                    float eased = slam * slam * slam;
                    pose.ArmOffset = MathHelper.Lerp(PlantRaisedArmOffset, PlantArmOffset, eased);
                    pose.Grip = MathHelper.Lerp(PlantGrip, PlantDeepGrip, eased);
                }
                else if (plantTimer < retractStart)
                {
                    pose.ArmOffset = PlantArmOffset;
                    pose.Grip = PlantDeepGrip;
                }
                else
                {
                    // Pull it out and bring it round to the carry: the handoff into phase 2's melee.
                    float retract = MathHelper.SmoothStep(0f, 1f, (plantTimer - retractStart + 1f) / PlantRetractTicks);
                    pose.ArmOffset = MathHelper.Lerp(PlantArmOffset, 0f, retract);
                    pose.Facing = MathHelper.Lerp(PlantFacing, SpearCarryFacing, retract);
                    pose.Grip = MathHelper.Lerp(PlantDeepGrip, SpearGripCarry, retract);
                }
                return pose;
            }

            // ── Spear throw, and the standards' heavier heave ──
            int throwRelease = input.ThrowRelease;
            int windupTicks = ThrowWindupTicks;
            int whipTicks = ThrowWhipTicks;
            float spearLift = ThrowSpearLift;
            bool heave = input.StandardRelease >= 0;
            if (heave)
            {
                throwRelease = input.StandardRelease;
                windupTicks = HeaveWindupTicks;
                whipTicks = HeaveWhipTicks;
                spearLift = HeaveSpearLift;
            }
            if (throwRelease >= 0)
            {
                int whipStart = throwRelease - whipTicks;
                int windupStart = whipStart - windupTicks;
                int appearTick = windupStart - PropShowAheadTicks;
                bool inThrow = timer >= appearTick && timer < throwRelease;
                if (inThrow)
                {
                    float aim = input.ThrowAim;
                    float cocked = aim - spearLift;
                    if (heave)
                    {
                        cocked = Math.Max(cocked, HeaveHighestCockFacing);
                    }
                    pose.FrontProp = KnightHeldProp.Spear;
                    pose.Grip = ThrowGrip;
                    pose.TwoHanded = heave && input.GreatKnight;

                    if (timer < windupStart)
                    {
                        pose.Facing = SpearCarryFacing;
                    }
                    else if (timer < whipStart)
                    {
                        float windup = (timer - windupStart + 1f) / windupTicks;
                        pose.Facing = MathHelper.SmoothStep(SpearCarryFacing, cocked, windup);
                        pose.ArmOffset = MathHelper.SmoothStep(0f, -ThrowArmDrawBack, windup);
                        pose.ThrowDrawBack = true;
                    }
                    else
                    {
                        // Cubic ease-in: the whip is slow out of the cock and fastest on the release frame.
                        float whip = (timer - whipStart + 1f) / whipTicks;
                        float eased = whip * whip * whip;
                        pose.Facing = MathHelper.Lerp(cocked, aim, eased);
                        pose.ArmOffset = MathHelper.Lerp(-ThrowArmDrawBack, 0f, eased);
                    }
                    return pose;
                }
            }

            // ── Firebomb throw ──
            int bombRelease = input.BombRelease;
            if (bombRelease >= 0)
            {
                int whipStart = bombRelease - ThrowWhipTicks;
                int windupStart = whipStart - ThrowWindupTicks;
                int appearTick = windupStart - PropShowAheadTicks;
                bool inThrow = timer >= appearTick && timer < bombRelease;
                if (inThrow)
                {
                    float aim = input.BombAim;
                    bool overhand = aim < BombOverhandAboveFacing;
                    float cocked = BombUnderhandFacing;
                    if (overhand)
                    {
                        cocked = aim - ThrowCockRadians;
                    }

                    pose.FrontProp = KnightHeldProp.Bomb;
                    pose.FuseLit = timer >= windupStart;

                    if (timer < windupStart)
                    {
                        pose.Facing = BombHipFacing;
                    }
                    else if (timer < whipStart)
                    {
                        float windup = (timer - windupStart + 1f) / ThrowWindupTicks;
                        pose.Facing = MathHelper.SmoothStep(BombHipFacing, cocked, windup);
                    }
                    else
                    {
                        float whip = (timer - whipStart + 1f) / ThrowWhipTicks;
                        float eased = whip * whip * whip;
                        pose.Facing = MathHelper.Lerp(cocked, aim, eased);
                    }
                    return pose;
                }
            }

            float spearAim = input.SpearAim;

            // ── Thrust ──
            int thrustStrike = input.ThrustStrike;
            if (thrustStrike >= 0)
            {
                int sinceStrike = timer - thrustStrike;
                int appearOffset = -(ThrustCockTicks + PropShowAheadTicks);
                int endOffset = ThrustPunchTicks + ThrustHoldTicks + ThrustRetractTicks;
                bool inThrust = sinceStrike >= appearOffset && sinceStrike < endOffset;
                if (inThrust)
                {
                    pose.FrontProp = KnightHeldProp.Spear;
                    pose.Facing = spearAim;

                    if (sinceStrike < -ThrustCockTicks)
                    {
                        pose.Grip = SpearGripCarry;
                    }
                    else if (sinceStrike < 0)
                    {
                        // Cock: draw the shaft back through the fist and dip the tip.
                        float cock = MathHelper.SmoothStep(0f, 1f, (sinceStrike + ThrustCockTicks + 1f) / ThrustCockTicks);
                        pose.Facing = spearAim + ThrustTipDip * cock;
                        pose.Grip = MathHelper.Lerp(SpearGripCarry, SpearGripCocked, cock);
                    }
                    else if (sinceStrike < ThrustPunchTicks)
                    {
                        // Punch: quadratic ease-out, so the first tick already covers ~36% of the reach.
                        float punch = (sinceStrike + 1f) / ThrustPunchTicks;
                        float eased = 1f - (1f - punch) * (1f - punch);
                        pose.Facing = MathHelper.Lerp(spearAim + ThrustTipDip, spearAim, eased);
                        pose.Grip = MathHelper.Lerp(SpearGripCocked, SpearGripExtended, eased);
                    }
                    else if (sinceStrike < ThrustPunchTicks + ThrustHoldTicks)
                    {
                        pose.Grip = SpearGripExtended;
                    }
                    else
                    {
                        float retract = (sinceStrike - ThrustPunchTicks - ThrustHoldTicks + 1f) / ThrustRetractTicks;
                        pose.Grip = MathHelper.SmoothStep(SpearGripExtended, SpearGripCarry, retract);
                    }
                    return pose;
                }
            }

            // A throw that has not reached its appear tick (or has already released) keeps the hand empty, rather than
            // falling back to the controller's prop, which is out from tick 0.
            if (throwRelease >= 0 || bombRelease >= 0)
            {
                return pose;
            }

            // ── Everything else: the controller's own prop, held at its aim ──
            if (input.HeldProp == KnightHeldProp.Bomb)
            {
                pose.FrontProp = KnightHeldProp.Bomb;
                pose.Facing = BombHipFacing;
            }
            else if (input.HeldProp == KnightHeldProp.Spear)
            {
                pose.FrontProp = KnightHeldProp.Spear;
                pose.Facing = spearAim;

                // Ember Reversal waits for its bomb to go off before the return dash: hold the thrust cocked,
                // so the dash that follows is already telegraphed.
                if (input.Attack == KnightSpecialAttack.FirebombReversal)
                {
                    pose.Facing = spearAim + ThrustTipDip;
                    pose.Grip = SpearGripCocked;
                }

                // Stormbreaker Edict: the spear goes up and stays up while its lightning lanes resolve.
                if (input.Attack == KnightSpecialAttack.StormbreakerEdict && timer >= StormRaiseStartTick)
                {
                    float raise = MathHelper.SmoothStep(0f, 1f, (timer - StormRaiseStartTick + 1f) / StormRaiseTicks);
                    pose.Facing = MathHelper.Lerp(spearAim, -MathHelper.PiOver2, raise);
                    pose.ArmOffset = MathHelper.Lerp(0f, StormRaiseArmOffset, raise);
                }
            }
            return pose;
        }
        #endregion

        #region Draw
        static Texture2D bombTexture;
        static readonly Vector2 BombGripOrigin = new Vector2(11f, 18f); // EnemyFirebomb is 22x24, hand near the bottom

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Crimson Teleport Ambush hides the knight with alpha 255 between markers; the puppet draw ignores alpha.
            if (NPC.alpha >= 255)
            {
                return false;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (globalNPC.TeleportCountdown <= 0 && globalNPC.TeleportAppearanceTimer <= 0
                && specialAttacks.IsSpectralHandBarrage)
            {
                RedKnightVFX.DrawUltrakillSeal(NPC.Center, specialAttacks.SpectralGatherProgress);
            }

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (NPC.alpha >= 255 || globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0
                || Phase != AttackPhase.Custom || !specialAttacks.Active)
            {
                return;
            }

            // The arm's world angle: facing space mirrored back for a left-facing puppet.
            float armWorldAngle = _poseFacing;
            if (NPC.direction < 0)
            {
                armWorldAngle = MathHelper.Pi - _poseFacing;
            }

            if (_frontProp == KnightHeldProp.Spear && specialAttacks.SpearDamageWake)
            {
                Vector2 forward = armWorldAngle.ToRotationVector2();
                Vector2 wakeCenter = PuppetHandPosition + forward * 30f;
                EnemyVFX.DrawBlackKnightSpearWake(wakeCenter, armWorldAngle, new Vector2(74f, 16f), 0.54f);
            }
            else if (_frontProp == KnightHeldProp.Bomb)
            {
                if (bombTexture == null)
                {
                    bombTexture = ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(EnemyFirebomb)),
                        ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }

                // The bomb sits on the fist, its fuse pointing along the arm (sprite is fuse-up, so +90 degrees).
                Vector2 handWorld = PuppetHandPosition;
                float rotation = armWorldAngle + MathHelper.PiOver2;
                if (_bombFuseLit)
                {
                    Vector2 fusePoint = handWorld + new Vector2(0f, -15f).RotatedBy(rotation);
                    RedKnightVFX.DrawBombFuse(fusePoint, specialAttacks.TelegraphProgress, planted: false);
                }
                spriteBatch.Draw(bombTexture, handWorld - Main.screenPosition, null, drawColor,
                    rotation, BombGripOrigin, 1f, SpriteEffects.None, 0f);
            }

            // Poison channels stay on the empty back hand; the Adder Spear thrust can overlap them in the front.
            if (specialAttacks.HeldProp == KnightHeldProp.Magic)
            {
                if (specialAttacks.Attack == KnightSpecialAttack.CinderRain)
                {
                    RedKnightVFX.DrawCinderMotes(PuppetBackHandPosition, specialAttacks.TelegraphProgress, 16f);
                }
                else
                {
                    RedKnightVFX.DrawToxicMotes(PuppetBackHandPosition, 2, specialAttacks.TelegraphProgress, 16f);
                }
            }
        }
        #endregion
    }

}
