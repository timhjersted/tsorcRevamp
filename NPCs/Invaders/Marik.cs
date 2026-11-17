using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Armor;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Cinder;
using tsorcRevamp.Content.Projectiles.Enemy.Okiku;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Invaders
{
    [AutoloadBossHead]
    class Marik : PuppetNPC, IHumanoidMeleeHitEffects
    {
        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(Marik)) + "_Head_Boss";

        protected override string InvaderTitle => "Marik of the Smoldering Bog";

        protected override int HeadArmorItemType => ModContent.ItemType<FirelinkHelm>();
        protected override int BodyArmorItemType => ModContent.ItemType<FirelinkArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<FirelinkLeggings>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<SeveringDusk>();
        protected override int RangedWeaponItemType => UsesRangedMemory ? ModContent.ItemType<PurpleGemStaff>() : -1;
        protected override int SecondaryRangedWeaponItemType => UsesPyromancyMemory ? ModContent.ItemType<PurpleGemStaff>() : -1;
        // Abyss now has staff magic of its own (Abyssal Curtain / Upheaval), so it holds the staff as a magic prop in
        // every phase, which also covers its staff ranged volleys.
        protected override int MagicWeaponItemType => UsesMagicMemory
            ? ModContent.ItemType<PurpleGemStaff>()
            : -1;

        protected override Vector2 MeleeHandleNorm => new Vector2(0.14f, 0.87f);
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override RangedStyle RangedAnimStyle => RangedStyle.Staff;
        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Staff;

        protected override int MeleeDamage => 95;
        protected override int RangedDamage => 58;
        protected override int SecondaryRangedDamage => 68;
        protected override int MagicDamage => 70;

        bool SecondPhase => _secondPhaseStarted;
        bool UsesAshenMemory => _memory == CinderMemory.AshenWarrior;
        bool UsesAbyssMemory => _memory == CinderMemory.Abyss;
        bool UsesPyromancyMemory => _memory == CinderMemory.Pyromancy;
        bool UsesSorceryMemory => _memory == CinderMemory.SorceryMiracle;
        bool UsesElandMemory => _memory == CinderMemory.Eland;
        bool UsesRangedMemory => UsesAbyssMemory || UsesPyromancyMemory;
        bool UsesMagicMemory => UsesPyromancyMemory || UsesSorceryMemory || UsesAbyssMemory || UsesElandMemory;
        bool FinaleUnlocked => SecondPhase && NPC.life <= NPC.lifeMax * 0.40f;

        protected override float TopSpeed => SecondPhase ? 3.05f : 2.65f;
        protected override float Acceleration => 0.12f;
        protected override float RunDistance => 360f;
        protected override float MeleeRange => 112f;
        protected override float StabRange => 190f;
        // SeveringDusk is 78x78; handle (0.14, 0.87) to tip (72, 6) = 87px * 1.15 NPC.scale = 100px visual reach.
        // ComboReachBase * 0.7 = 100.1px at 1.0x reach so the hitbox matches the visible blade.
        protected override float ComboReachBase => 143f;
        protected override float MeleeEngageRange => 118f;
        protected override float ComboMaxStartRange => 420f;
        protected override int MeleeTelegraphTicks => 35 + WeaponReadyLeadTicks;
        protected override int StabTelegraphTicks => 38 + WeaponReadyLeadTicks;
        protected override int MeleeComboChance => UsesAshenMemory ? 100 : SecondPhase ? 55 : 0;
        protected override int RangedStartMeleeComboChance => UsesAshenMemory ? 80 : SecondPhase ? 45 : 0;
        protected override float ComboTelegraphMultiplier => 1.0f;
        protected override int MinComboTelegraphTicks => 24;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.85f;
        protected override float ComboTelegraphAdvanceStopDistance => 72f;
        protected override bool ShowWeaponDuringNeutral => UsesAshenMemory;
        protected override int MeleeRecoveryTicks => 28;
        protected override int MeleeRecoveryLingerTicks => 6;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override bool CanStab => UsesAshenMemory;
        protected override bool UseCompositeArmSwing => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool AuthoredClockCoversJoustDash => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseAlternateFlip => false;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool HasSlashVFX => false;
        protected override bool HasSwordArcVFX => true;
        protected override Color SwordArcVFXColor => SecondPhase
            ? Color.Lerp(MemoryColor, MiasmaPlayer.MiasmaPinkColor, 0.55f)
            : MemoryColor;
        protected override bool HasFireSlashVFX => true;
        protected override Color FireSlashCinderColor => SecondPhase
            ? new Color(46, 8, 42)
            : _memory == CinderMemory.Abyss
                ? new Color(24, 6, 44)
                : _memory == CinderMemory.SorceryMiracle
                    ? new Color(8, 22, 48)
                    : _memory == CinderMemory.Eland
                        ? new Color(16, 36, 12)
                        : new Color(48, 6, 2);
        protected override Color FireSlashFlameColor => SecondPhase
            ? Color.Lerp(MemoryColor, MiasmaPlayer.MiasmaPinkColor, 0.45f)
            : MemoryColor;
        protected override Color FireSlashCoreColor => SecondPhase
            ? MiasmaPlayer.MiasmaPinkColor
            : _memory == CinderMemory.Abyss
                ? new Color(230, 175, 255)
                : _memory == CinderMemory.SorceryMiracle
                    ? new Color(195, 235, 255)
                    : _memory == CinderMemory.Eland
                        ? new Color(185, 255, 130)
                        : new Color(255, 224, 120);
        protected override Color MeleeTelegraphFlashColor => MemoryColor;
        protected override bool HyperArmorDuringTelegraph => SecondPhase
            && (Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeTelegraph
                || Phase == AttackPhase.StabTelegraph);

        protected override int EstusChargesMax => 1;
        protected override float EstusHealFraction => 0.10f;
        protected override float FirstHealThreshold => 0.30f;
        protected override float SecondHealThreshold => -1f;
        protected override float HealLifeCapFraction => 0.499f;
        protected override int HealCooldownTicks => 900;
        protected override int HealAnimationTicks => 110;
        protected override float RecentDamageThreshold => 1f;
        protected override float FleeToHealDistance => 20f * 16f;
        protected override int FleeToHealMaxTicks => 120;

        protected override float RangedRange => 780f;
        protected override float MinRangedRange => 160f;
        protected override int RangedTelegraphTicks => 45 + WeaponReadyLeadTicks;
        protected override int RangedAttackTicks => 10;
        protected override int RangedRecoveryTicks => 54;
        protected override int RangedCooldownAfterUse => 170;
        protected override int MaxRangedBurst => 2;
        protected override int SingleRangedBurstChance => 35;
        protected override Color RangedTelegraphFlashColor => MemoryColor;

        protected override float SecondaryRangedRange => 920f;
        protected override float SecondaryRangedMinRange => 300f;
        protected override int SecondaryRangedTelegraphTicks => 45 + WeaponReadyLeadTicks;
        protected override int SecondaryRangedAttackTicks => 10;
        protected override int SecondaryRangedRecoveryTicks => 70;
        protected override int SecondaryRangedCooldownAfterUse => 210;
        protected override int SecondaryRangedChance => 45;
        protected override Color SecondaryRangedFlashColor => Color.LimeGreen;
        // Twelve rising lob shots, exactly five ticks apart, instead of a single generic staff bolt.
        protected override int[][] SecondaryRangedBurstPatterns => new[] { new[] { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 } };

        protected override float MagicRange => UsesElandMemory ? 650f : 1000f;
        protected override float MinMagicRange => UsesElandMemory ? 0f : 260f;
        protected override int MagicTelegraphTicks => 45 + WeaponReadyLeadTicks;
        protected override int MagicAttackTicks => 16;
        protected override int MagicRecoveryTicks => 82;
        protected override int MagicCooldownAfterUse => 210;
        protected override int MagicPreferenceChance => 55;
        protected override Color MagicTelegraphFlashColor => MemoryColor;
        protected override bool UseAuthoredMagicCastPose => true;
        protected override float MagicCastStartRotation => 0.3f;
        protected override float MagicCastEndRotation => _queuedMagicAttack == CinderMagicAttack.IceStorm
            ? MathHelper.Lerp(-1.15f, 0.65f, _magicShotProgress)
            : -0.9f;
        protected override float MagicWeaponRotationOffset => MathHelper.PiOver4;
        protected override bool MirrorMagicWeaponRotationByFacing => true;
        protected override int MagicWeaponRecoveryHoldTicks => 20;
        // PurpleGemStaff.png is 42x40; shaft runs from butt (0,39) to gem tip (41,0).
        // Grip 30% up from the butt across all staff phases (matching OolacileCultist's staff alignment).
        protected override Vector2 MagicGripNorm => new Vector2(0.30f, 0.70f);
        const float StaffDrawScale = 0.9f;

        protected override float GetHeldRangedDrawScale(int itemType)
        {
            if (itemType == ModContent.ItemType<PurpleGemStaff>())
            {
                return StaffDrawScale;
            }
            return base.GetHeldRangedDrawScale(itemType);
        }

        protected override bool CanBreathe => UsesPyromancyMemory;
        protected override float BreathRange => 680f;
        protected override float MinBreathRange => 120f;
        const int WeaponReadyLeadTicks = 30;
        const int BreathTellTicks = 76;
        protected override int BreathTelegraphTicks => WeaponReadyLeadTicks + BreathTellTicks;
        protected override int BreathDurationTicks => SecondPhase ? 135 : 105;
        protected override int BreathRecoveryTicks => 70;
        protected override int BreathCooldownAfterUse => 360;
        protected override int BreathChance => 5;
        protected override bool UseAuthoredBreathHandPose => true;
        protected override bool UseCompositeArmForAdditionalPhase =>
            Phase == AttackPhase.MagicTelegraph
            || Phase == AttackPhase.MagicAttack
            || IsHoldingMagicWeaponDuringRecovery
            || Phase == AttackPhase.RangedTelegraph
            || Phase == AttackPhase.RangedAttack
            || Phase == AttackPhase.CrossbowBurstPause
            || IsSpiralFanPhase
            || Phase == AttackPhase.BreathTelegraph
            || Phase == AttackPhase.Breathing
            || Phase == AttackPhase.BreathRecovery;

        protected override bool CanPierce => UsesAshenMemory;
        protected override float PierceRange => 760f;
        protected override float MinPierceRange => 230f;
        protected override int PierceChance => 5;
        protected override int PierceTelegraphTicks => 56 + WeaponReadyLeadTicks;
        protected override int PierceDashTicks => 34;
        protected override float PierceDashSpeed => SecondPhase ? 18f : 15.5f;
        protected override int PierceRecoveryTicks => 82;
        protected override int PierceCooldownAfterUse => 300;
        protected override int PierceStabChance => 0;

        protected override bool CanJumpSlash => UsesAshenMemory;
        protected override float JumpSlashMinRange => 160f;
        protected override float JumpSlashMaxRange => 700f;
        protected override float JumpSlashLaunchUpSpeed => 12f;
        protected override float JumpSlashMaxUpSpeed => 16f;
        protected override float JumpSlashLaunchForwardSpeed => 11f;
        protected override float JumpSlashMaxForwardSpeed => 14f;
        protected override float JumpSlashTriggerRange => 112f;
        protected override int JumpSlashChance => 4;
        protected override int JumpSlashRecoveryTicks => 72;
        protected override int JumpSlashCooldownAfterUse => 300;

        protected override bool CanFlipSlash => UsesAshenMemory;
        protected override float FlipSlashMinRange => 130f;
        protected override float FlipSlashMaxRange => 470f;
        protected override int FlipSlashChance => 4;
        protected override int FlipSlashCooldownAfterUse => 360;

        protected override bool CanHomingVolley => false;
        protected override float HomingVolleyMinRange => 280f;
        protected override float HomingVolleyMaxRange => 760f;
        protected override int HomingVolleyChance => 7;
        protected override int HomingVolleyRecoveryTicks => 88;
        protected override int HomingVolleyCooldownAfterUse => 280;

        protected override bool CanBoomerang => UsesAbyssMemory;
        protected override float BoomerangMinRange => 120f;
        protected override float BoomerangMaxRange => 760f;
        protected override int BoomerangChance => 7;
        protected override int BoomerangRecoveryTicks => 92;
        protected override int BoomerangCooldownAfterUse => 320;
        protected override int BoomerangSwingTelegraphTicks => 20 + WeaponReadyLeadTicks;

        protected override bool CanSpiralFan => UsesSorceryMemory;
        protected override float SpiralFanMinRange => 220f;
        protected override float SpiralFanMaxRange => 850f;
        protected override int SpiralFanChance => 7;
        protected override int SpiralFanRecoveryTicks => 95;
        protected override int SpiralFanCooldownAfterUse => 360;
        protected override int SpiralFanSwingTelegraphTicks => 45 + WeaponReadyLeadTicks;
        protected override int SpiralFanWeaponItemType => ModContent.ItemType<PurpleGemStaff>();
        protected override bool UseAuthoredSpiralFanCastPose => true;
        protected override float SpiralFanCastStartRotation => 0.3f;
        protected override float SpiralFanCastEndRotation => -0.9f;

        public static readonly float ARENA_WIDTH = 864;
        public static readonly float ARENA_HEIGHT = 656;
        //ARENA_LOCATION_ADVENTURE removed: this boss is currently a copy of the old Gwyn boss and has no real
        //arena location yet — a proper event location is pending. Re-add (routed through ExpandedWorldTransform,
        //legacy 2000-space) once that's designed.

        const int BaseDefense = 130;
        const float ProtectionRadius = 1000f;
        const float KillRingRadius = 2000f;
        const int CowardGraceTicks = 90;

        const int DarkBeadDamage = 36;
        const int PhantomSeekerDamage = 55;
        const int BioSpitDamage = 68;
        const int FireBreathDamage = 50;
        const int IceStormDamage = 40;
        const int DisruptDamage = 68;
        const int LostSoulDamage = 38;
        const int ShadowShotDamage = 38;
        const int ElandVenomDamage = 48;
        const int ElandStickyDamage = 42;
        const float ElandNovaTriggerRange = 280f;
        const float ElandNovaRadius = 400f;
        const int SwordMiasmaBuildupPerHit = 35;
        const int OrangeProjDamage = 45;
        const int SwordProjectileDamage = 69;

        const float CinderRaisedPose = -1.75f;
        const float CinderLoweredPose = 2.10f;
        const float CinderHorizStartPose = -1.65f;
        const float CinderHorizEndPose = 2.05f;
        const float CinderArmedSpeedShare = 0.30f;
        const float CinderSurgePushMult = 1.65f;
        const float CinderConditionalFollowUpRange = 185f;
        const float CinderLeapForwardSpeedMult = 2.10f;
        const float CinderLeapTargetLeadTicks = 12f;
        const float CinderLeapTrackingStrength = 0.14f;
        const float CinderLeapLandingStandoff = 24f;
        const float CinderLeapStrikeRange = 135f;
        const float SecondPhaseStrikeSpeedMult = 1.18f;
        const float SecondPhaseRecoveryMult = 0.80f;
        const int MinTicksBetweenLiveWindows = 30;

        // Severing Pursuit (JoustDash): distance-solved accelerate-cruise-decay dash with 180px overshoot.
        const int PursuitTellTicks = 42;
        const int PursuitAttackTicks = 30;
        const int PursuitLiveStartTick = 6;
        const int PursuitLiveEndTick = 20;
        const int PursuitLockLeadTicks = 10;
        const float PursuitOvershootPixels = 180f;
        const float PursuitMinimumDistance = 120f;
        const float PursuitMaximumDistance = 480f;
        const float PursuitMinimumRange = 130f;
        const float PursuitMaximumRange = 360f;
        const float PursuitCouchedPose = 1.20f;
        const float PursuitLevelPose = MathHelper.PiOver4;

        const string CinderCleaveName = "Cinder Cleave";
        const string DownUpReversalName = "Down-Up Reversal";
        const string UpDownHeavyName = "Up-Down Heavy";
        const string BorrowedFuryName = "Borrowed Fury";
        const string SeveringPursuitName = "Severing Pursuit";
        const string CinderPursuitSlamName = "Cinder Pursuit Slam";
        const string HighLeapingCinderfallName = "High Leaping Cinderfall";
        const string BackstepRepriseName = "Backstep Reprise";
        const string SoulOfLordsComboName = "Soul of Lords";

        const int CB_REVERSAL = 1;
        const int CB_PURSUIT = 4;
        const int CB_BACKSTEP = 7;

        // Timing sheet (60 ticks = 1s). Budget: 22t roll i-frames, next roll 30t from roll START, 40t post-hit immunity.
        // Poses: CinderRaisedPose -1.75 -> CinderLoweredPose +2.10 (220.6 deg envelope, ~179 deg live sweep).
        // Hitbox: SeveringDusk 78x78 @ 1.15x = 100px visual vs ComboReachBase 143 * 0.7 = 100.1px.
        //
        // Move                 Tell  Strike (in/out, k)     Live    Tail+Recovery   Notes
        // Cinder Cleave         28   8/32, k7 (40t)        ~13.5t  26.5 + 24 = 50t  peak 30.5 deg/t; 1 roll beats; surges if out of reach
        // Down-Up Reversal      32   8/32, pause 10, 7/29  13.5/12 24 + 28 = 52t    step 2 starts 36.5t after live1 ends; conditional hit or <=185px
        // Up-Down Heavy         32   8/32, pause 10, 9/36  13.5/15 29.8 + 32 = 62t  step 2 starts 36.5t after live1 ends; peak 27.0 deg/t
        // Borrowed Fury         34   8/32, 7/29, 10/38 (p10) 13.5-16.5t             3-hit HyperArmor chain; step 2/3 surge 1.65x if out of reach
        // Severing Pursuit      42   30t distance-solved   6..20   10 + 36 = 46t    JoustDash (1.20 -> level); 180px overshoot catches roll-away
        // Cinder Pursuit Slam   34   landing-timed leap    <=10t   28t recovery     low pursuit leap (0.64h), 135px mid-air strike, 24px standoff
        // High Leaping Cinder.  42   landing-timed leap    <=10t   34t recovery     high apex-retarget leap (1.15h, 2.1x descent gravity)
        // Backstep Reprise      24   60t backstep, p10, 7/32 ~12.5t 26.5 + 32 = 58t  backsteps out of pressure then lunges 1.45x across 212 deg sweep
        // Soul of Lords         38   4x alternating + leap 12-15t  78t recovery     <=40% HP Phase 2; p10-p12 (>=34t between live windows), leap ender
        static MeleeComboStep WeightedSwordStep(
            ComboMotion motion,
            int telegraphTicks,
            int easeInTicks,
            int easeOutTicks,
            float easeOutDecay,
            int pauseAfter = 0,
            float damageMult = 1f,
            float reachMult = 1.12f,
            float forwardPushMult = 0.55f)
        {
            int attackTicks = easeInTicks + easeOutTicks;
            float armedSettleTicks = easeOutTicks * MathF.Log(1f / CinderArmedSpeedShare) / easeOutDecay;
            float hitWindowEnd = MathHelper.Clamp((easeInTicks + armedSettleTicks) / attackTicks, 0f, 1f);

            return new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = reachMult,
                ForwardPushMult = forwardPushMult,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Weighted,
                EaseInTicks = easeInTicks,
                EaseOutTicks = easeOutTicks,
                EaseOutDecay = easeOutDecay,
                HitWindowEnd = hitWindowEnd,
            };
        }

        static MeleeComboStep LeapSwordStep(
            int telegraphTicks,
            int timeoutTicks,
            float damageMult,
            float reachMult,
            float leapHeightMult,
            float leapForwardSpeedMult,
            float leapDescentGravityMult = 1f,
            float leapApexRetargetStrength = 0f,
            float leapStrikeRange = CinderLeapStrikeRange)
        {
            return new MeleeComboStep
            {
                Motion = ComboMotion.LeapSlam,
                TelegraphTicks = telegraphTicks,
                AttackTicks = timeoutTicks,
                PostStepPause = 0,
                DamageMult = damageMult,
                ReachMult = reachMult,
                ForwardPushMult = 0f,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Whip,
                HitWindowEnd = 1f,
                LeapHeightMult = leapHeightMult,
                LeapForwardSpeedMult = leapForwardSpeedMult,
                LeapDescentGravityMult = leapDescentGravityMult,
                LeapApexRetargetStrength = leapApexRetargetStrength,
                LeapStrikeRange = leapStrikeRange,
            };
        }

        static readonly MeleeCombo[] CinderCombos =
        {
            new MeleeCombo
            {
                Name = CinderCleaveName,
                BaseWeight = 78,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(255, 128, 52),
                CooldownAfterUse = 50,
                RecoveryTicks = 24,
                MoveBrake = 0.12f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.OverheadArc, 28, 8, 32, 7f, 0, 1.08f, 1.12f, 0.55f),
                },
            },
            new MeleeCombo
            {
                Name = DownUpReversalName,
                BaseWeight = 90,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(190, 75, 255),
                CooldownAfterUse = 110,
                RecoveryTicks = 28,
                MoveBrake = 0.14f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.OverheadArc, 32, 8, 32, 7f, 10, 0.95f, 1.12f, 0.55f),
                    WeightedSwordStep(ComboMotion.UnderhandArc, 0, 7, 29, 7f, 0, 1.28f, 1.16f, 0.65f),
                },
            },
            new MeleeCombo
            {
                Name = UpDownHeavyName,
                BaseWeight = 72,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(255, 156, 62),
                CooldownAfterUse = 135,
                RecoveryTicks = 32,
                MoveBrake = 0.14f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.UnderhandArc, 32, 8, 32, 7f, 10, 0.92f, 1.10f, 0.55f),
                    WeightedSwordStep(ComboMotion.OverheadArc, 0, 9, 36, 7f, 0, 1.35f, 1.20f, 0.70f),
                },
            },
            new MeleeCombo
            {
                Name = BorrowedFuryName,
                BaseWeight = 62,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(255, 92, 36),
                CooldownAfterUse = 180,
                RecoveryTicks = 38,
                HyperArmor = true,
                MoveBrake = 0.08f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.OverheadArc, 34, 8, 32, 7f, 10, 0.85f, 1.10f, 0.60f),
                    WeightedSwordStep(ComboMotion.UnderhandArc, 0, 7, 29, 7f, 10, 0.95f, 1.14f, 0.70f),
                    WeightedSwordStep(ComboMotion.OverheadArc, 0, 10, 38, 7f, 0, 1.40f, 1.24f, 0.85f),
                },
            },
            new MeleeCombo
            {
                Name = SeveringPursuitName,
                BaseWeight = 75,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(205, 70, 255),
                CooldownAfterUse = 150,
                RecoveryTicks = 36,
                RangedStartOnly = true,
                HyperArmor = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash,
                        TelegraphTicks = PursuitTellTicks,
                        AttackTicks = PursuitAttackTicks,
                        PostStepPause = 0,
                        DamageMult = 1.35f,
                        ReachMult = 1.25f,
                        ForwardPushMult = 0f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = PursuitLiveEndTick / (float)PursuitAttackTicks,
                    },
                },
            },
            new MeleeCombo
            {
                Name = CinderPursuitSlamName,
                BaseWeight = 85,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(205, 70, 255),
                CooldownAfterUse = 140,
                RecoveryTicks = 28,
                RangedStartOnly = true,
                HyperArmor = true,
                MoveBrake = 0.06f,
                Steps = new[]
                {
                    LeapSwordStep(34, 85, 1.32f, 1.22f, 0.64f, 1.85f),
                },
            },
            new MeleeCombo
            {
                Name = HighLeapingCinderfallName,
                BaseWeight = 55,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(255, 130, 40),
                CooldownAfterUse = 260,
                RecoveryTicks = 34,
                RangedStartOnly = true,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.12f,
                Steps = new[]
                {
                    LeapSwordStep(42, 115, 1.48f, 1.26f, 1.15f, 1.15f, 2.1f, 1.0f),
                },
            },
            new MeleeCombo
            {
                Name = BackstepRepriseName,
                BaseWeight = 48,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(115, 175, 255),
                CooldownAfterUse = 200,
                RecoveryTicks = 32,
                HyperArmor = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.BackstepRaise,
                        TelegraphTicks = 24,
                        AttackTicks = 60,
                        PostStepPause = 10,
                        DamageMult = 0f,
                        ReachMult = 1f,
                        ForwardPushMult = 0f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Linear,
                    },
                    WeightedSwordStep(ComboMotion.HorizontalSweep, 0, 7, 32, 7f, 0, 1.35f, 1.22f, 1.45f),
                },
            },
            new MeleeCombo
            {
                Name = SoulOfLordsComboName,
                BaseWeight = 42,
                Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.White,
                CooldownAfterUse = 360,
                RecoveryTicks = 78,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.05f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.OverheadArc, 38, 8, 32, 7f, 10, 0.78f, 1.10f, 0.60f),
                    WeightedSwordStep(ComboMotion.UnderhandArc, 0, 8, 32, 7f, 10, 0.78f, 1.12f, 0.70f),
                    WeightedSwordStep(ComboMotion.OverheadArc, 0, 7, 29, 7f, 10, 0.88f, 1.14f, 0.75f),
                    WeightedSwordStep(ComboMotion.UnderhandArc, 0, 9, 35, 7f, 10, 1.00f, 1.18f, 0.85f),
                    LeapSwordStep(0, 90, 1.55f, 1.30f, 0.75f, 2.0f),
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => CinderCombos;

        bool InMeleeComboPhases =>
            Phase == AttackPhase.MeleeComboTelegraph
            || Phase == AttackPhase.MeleeComboAttack
            || Phase == AttackPhase.MeleeComboPause;

        bool LeapSlamComboActive =>
            InMeleeComboPhases
            && (ActiveMeleeComboName == CinderPursuitSlamName
                || ActiveMeleeComboName == HighLeapingCinderfallName
                || ActiveMeleeComboName == SoulOfLordsComboName);

        protected override bool UseLandingTimedLeapSlam => LeapSlamComboActive;
        protected override float LeapSlamCarryRotation => LeapSlamComboActive ? CinderRaisedPose : base.LeapSlamCarryRotation;
        protected override float LeapSlamImpactRotation => LeapSlamComboActive ? CinderLoweredPose : base.LeapSlamImpactRotation;
        protected override float LeapAttackForwardSpeed => LeapSlamComboActive ? TopSpeed * CinderLeapForwardSpeedMult : base.LeapAttackForwardSpeed;
        protected override float LeapAttackTargetLeadTicks => LeapSlamComboActive ? CinderLeapTargetLeadTicks : base.LeapAttackTargetLeadTicks;
        protected override float LeapAttackAscentTrackingStrength => LeapSlamComboActive ? CinderLeapTrackingStrength : base.LeapAttackAscentTrackingStrength;
        protected override float LeapLandingStandoff => LeapSlamComboActive ? CinderLeapLandingStandoff : base.LeapLandingStandoff;

        protected override float LogicalWindupSettleFraction
        {
            get
            {
                if (InMeleeComboPhases && !LeapSlamComboActive)
                {
                    return 0.45f;
                }
                return base.LogicalWindupSettleFraction;
            }
        }

        protected override void ModifyMeleeArcEndpoints(
            ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (motion == ComboMotion.OverheadArc)
            {
                startRotation = CinderRaisedPose;
                endRotation = CinderLoweredPose;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = CinderLoweredPose;
                endRotation = CinderRaisedPose;
            }
            else if (motion == ComboMotion.HorizontalSweep)
            {
                startRotation = CinderHorizStartPose;
                endRotation = CinderHorizEndPose;
            }
            else if (motion == ComboMotion.JoustDash)
            {
                startRotation = PursuitCouchedPose;
                endRotation = PursuitLevelPose;
            }
        }

        NPCDespawnHandler despawnHandler;
        int _protectionTextCooldown;
        readonly int[] _cowardTimers = new int[Main.maxPlayers];
        CinderRangedAttack _queuedRangedAttack = CinderRangedAttack.DarkBead;
        CinderMagicAttack _queuedMagicAttack = CinderMagicAttack.IceStorm;
        int _magicBurstsRemaining;
        int _magicBurstTimer;
        int _magicBurstTotal;
        float _magicShotProgress;
        bool _magicPrepared;
        int _greenRainShotIndex;
        int _pursuitDashDirection = 1;
        float _pursuitDashDistance;
        Vector2 _elandAimAnchor;
        CinderMemory _memory = CinderMemory.AshenWarrior;
        CinderCustomSequence _customSequence;
        AttackPhase _previousPhase = AttackPhase.Idle;
        // One completed attack per memory keeps the five identities cycling instead of letting
        // a lucky ranged roll repeat while another memory stays unseen.
        int _memoryAttacksRemaining = 1;
        bool _memoryShiftPending;
        bool _secondPhaseStarted;

        static readonly float[] ElandVenomSpreadX = { -24f, 0f, 24f };
        static readonly float[] ElandComboCBrackets = { 72f, 144f, 240f, 288f, 144f, 72f };
        static readonly Vector2[] ElandStickyOffsets =
        {
            new Vector2(0f, 0f),
            new Vector2(-48f, 0f),
            new Vector2(48f, 0f),
            new Vector2(-24f, -64f),
            new Vector2(24f, -64f),
            new Vector2(0f, -96f),
            new Vector2(-64f, -32f),
            new Vector2(64f, -32f),
        };

        enum CinderMemory : byte
        {
            AshenWarrior,
            Abyss,
            Pyromancy,
            SorceryMiracle,
            Eland,
        }

        enum CinderCustomSequence : byte
        {
            None,
            MemoryShift,
            SecondPhaseTransition,
        }

        enum CinderRangedAttack
        {
            DarkBead,
            PhantomSeeker,
            BioSpit
        }

        enum CinderMagicAttack
        {
            IceStorm,
            PhasedMatterBlast,
            LostSoulCurse,
            CinderConstellation,
            FirelinkCross,
            AshenOrbit,
            AbyssalCurtain,
            AbyssalUpheaval,
            ElandVenomVolley,
            ElandStickySpit,
            ElandToxicNova
        }

        Color MemoryColor => _memory switch
        {
            CinderMemory.AshenWarrior => new Color(255, 128, 52),
            CinderMemory.Abyss => new Color(180, 70, 255),
            CinderMemory.Pyromancy => new Color(255, 72, 30),
            CinderMemory.SorceryMiracle => new Color(115, 185, 255),
            CinderMemory.Eland => new Color(110, 235, 75),
            _ => Color.White,
        };

        int MemoryDustType => _memory switch
        {
            CinderMemory.AshenWarrior => DustID.Torch,
            CinderMemory.Abyss => DustID.Shadowflame,
            CinderMemory.Pyromancy => DustID.GoldFlame,
            CinderMemory.SorceryMiracle => DustID.IceTorch,
            CinderMemory.Eland => DustID.Poisoned,
            _ => DustID.Smoke,
        };

        string MemoryName => _memory switch
        {
            CinderMemory.AshenWarrior => "Ashen Warrior",
            CinderMemory.Abyss => "Abyss",
            CinderMemory.Pyromancy => "Pyromancy",
            CinderMemory.SorceryMiracle => "Sorcery / Miracle",
            CinderMemory.Eland => "Eland",
            _ => "Unknown",
        };

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.npcSlots = 10;
            NPC.height = 40;
            NPC.width = 30;
            NPC.scale = 1.15f;
            NPC.damage = 0;
            NPC.defense = BaseDefense;
            NPC.lifeMax = 450000;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.netAlways = true;
            NPC.lavaImmune = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 2000000;
            NPC.rarity = 47;
            Music = 12;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Marik.DespawnHandler"), Color.OrangeRed, 6);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.Agility = 0.25f;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = NPCs.TeleportVisualStyle.Fire;
            globalNPC.NavSearchRadius = 80;
            EvasiveProfile.RedKnight(globalNPC);
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (!UsesAshenMemory && !SecondPhase)
            {
                return false;
            }
            if (combo.Name == SoulOfLordsComboName)
            {
                return FinaleUnlocked;
            }
            if (combo.Name == HighLeapingCinderfallName && !SecondPhase)
            {
                return false;
            }
            if (combo.Name == SeveringPursuitName)
            {
                bool inBand = distance >= PursuitMinimumRange && distance <= PursuitMaximumRange;
                return inBand && NPC.HasValidTarget && CanPursuitDashTo(Main.player[NPC.target]);
            }
            if (combo.Steps != null
                && combo.Steps.Length > 0
                && combo.Steps[0].Motion == ComboMotion.LeapSlam
                && distance > ReliableJumpStartRange(combo.Steps[0]))
            {
                return false;
            }
            return true;
        }

        protected override bool CanDamageWithMeleeStep(MeleeComboStep step, float progress)
        {
            if (ActiveMeleeComboName != SeveringPursuitName)
            {
                return true;
            }

            if (_pursuitDashDistance <= 0f)
            {
                return false;
            }
            return progress >= PursuitLiveStartTick / (float)step.AttackTicks;
        }

        bool CanPursuitDashTo(Player target)
        {
            int direction = Math.Sign(target.Center.X - NPC.Center.X);
            if (direction == 0)
            {
                direction = NPC.direction;
            }

            float travel = Math.Abs(target.Center.X - NPC.Center.X) + PursuitOvershootPixels;
            travel = MathHelper.Clamp(travel, PursuitMinimumDistance, PursuitMaximumDistance);
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

        protected override bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == DownUpReversalName && nextStepIndex == 1)
            {
                if (previousStepHit)
                {
                    return true;
                }
                if (target == null || !target.active || target.dead)
                {
                    return false;
                }

                bool inFront = Math.Sign(target.Center.X - NPC.Center.X) == NPC.direction;
                return inFront && NPC.Distance(target.Center) <= CinderConditionalFollowUpRange;
            }

            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        protected override void ModifyOpeningMeleeComboStep(
            string comboName, Player target, ref MeleeComboStep openingStep)
        {
            if (openingStep.Motion == ComboMotion.LeapSlam
                || openingStep.Motion == ComboMotion.BackstepRaise
                || target == null
                || !target.active)
            {
                return;
            }

            float reach = ComboReachBase * 0.7f * openingStep.ReachMult;
            if (NPC.Distance(target.Center) > reach)
            {
                openingStep.ForwardPushMult = Math.Max(openingStep.ForwardPushMult, CinderSurgePushMult);
            }
        }

        protected override void ModifyNextMeleeComboStep(
            string comboName, int nextStepIndex, Player target, ref MeleeComboStep nextStep)
        {
            if (nextStep.Motion == ComboMotion.LeapSlam || target == null || !target.active)
            {
                return;
            }

            float reach = ComboReachBase * 0.7f * nextStep.ReachMult;
            if (NPC.Distance(target.Center) > reach)
            {
                nextStep.ForwardPushMult = Math.Max(nextStep.ForwardPushMult, CinderSurgePushMult);
            }
        }

        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            base.CustomizeMeleeCombo(ref combo, healthFraction);
            if (!SecondPhase || combo.Name == SoulOfLordsComboName || combo.Steps == null)
            {
                return;
            }

            combo.HyperArmor = true;
            for (int i = 0; i < combo.Steps.Length; i++)
            {
                MeleeComboStep step = combo.Steps[i];
                if (step.Ease == SwingEaseStyle.Weighted && step.EaseOutDecay > 0f)
                {
                    step.EaseInTicks = Math.Max(4, (int)Math.Round(step.EaseInTicks / SecondPhaseStrikeSpeedMult));
                    step.EaseOutTicks = Math.Max(10, (int)Math.Round(step.EaseOutTicks / SecondPhaseStrikeSpeedMult));
                    step.AttackTicks = step.EaseInTicks + step.EaseOutTicks;

                    float armedSettleTicks = step.EaseOutTicks * MathF.Log(1f / CinderArmedSpeedShare) / step.EaseOutDecay;
                    step.HitWindowEnd = MathHelper.Clamp((step.EaseInTicks + armedSettleTicks) / step.AttackTicks, 0f, 1f);

                    if (step.PostStepPause > 0)
                    {
                        int liveTicks = (int)Math.Ceiling(step.HitWindowEnd * step.AttackTicks);
                        int tailTicks = step.AttackTicks - liveTicks;
                        int scaledPause = (int)Math.Round(step.PostStepPause * SecondPhaseRecoveryMult);
                        int minimumPause = Math.Max(1, MinTicksBetweenLiveWindows - tailTicks);
                        step.PostStepPause = Math.Max(scaledPause, minimumPause);
                    }
                }
                combo.Steps[i] = step;
            }

            int authoredRecovery = combo.RecoveryTicks > 0 ? combo.RecoveryTicks : MeleeRecoveryTicks;
            combo.RecoveryTicks = Math.Max(14, (int)Math.Round(authoredRecovery * SecondPhaseRecoveryMult));
        }

        protected override void OnMeleeComboTelegraphTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name == SeveringPursuitName
                && elapsed == total - PursuitLockLeadTicks
                && Main.netMode != NetmodeID.MultiplayerClient
                && NPC.HasValidTarget)
            {
                Player target = Main.player[NPC.target];
                _pursuitDashDirection = Math.Sign(target.Center.X - NPC.Center.X);
                if (_pursuitDashDirection == 0)
                {
                    _pursuitDashDirection = NPC.direction;
                }

                _pursuitDashDistance = 0f;
                if (CanPursuitDashTo(target))
                {
                    float travel = Math.Abs(target.Center.X - NPC.Center.X) + PursuitOvershootPixels;
                    _pursuitDashDistance = MathHelper.Clamp(travel, PursuitMinimumDistance, PursuitMaximumDistance);
                }
                NPC.netUpdate = true;
                RequestNetworkSnapshot();
            }

            if (Main.dedServ || elapsed % 2 != 0)
            {
                return;
            }

            float progress = (elapsed + 1f) / Math.Max(1, total);
            Vector2 direction = PuppetWeaponDirection.SafeNormalize(new Vector2(NPC.direction, 0f));
            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            Vector2 position = PuppetHandPosition + direction * bladeReach * Main.rand.NextFloat(0.45f, 1.0f);
            Dust ember = Dust.NewDustPerfect(position, MemoryDustType,
                new Vector2(-NPC.direction * 0.6f, Main.rand.NextFloat(-1.4f, -0.3f)),
                70, MemoryColor, MathHelper.Lerp(0.6f, 1.15f, progress));
            ember.noGravity = true;

            if (SecondPhase)
            {
                Color miasmaColor = (elapsed / 2) % 2 == 0
                    ? MiasmaPlayer.MiasmaPinkColor
                    : MiasmaPlayer.MiasmaColor;
                Dust miasmaSpark = Dust.NewDustPerfect(
                    PuppetHandPosition + direction * bladeReach * Main.rand.NextFloat(0.40f, 1.0f),
                    DustID.RainbowMk2,
                    new Vector2(-NPC.direction * 0.4f, Main.rand.NextFloat(-1.1f, -0.2f)),
                    60,
                    miasmaColor,
                    MathHelper.Lerp(0.75f, 1.25f, progress));
                miasmaSpark.noGravity = true;
            }

            Lighting.AddLight(position, MemoryColor.ToVector3() * (0.25f + progress * 0.45f));
        }

        protected override void OnMeleeComboAttackTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name == SeveringPursuitName)
            {
                if (_pursuitDashDistance > 0f)
                {
                    float progressNow = NPCs.Enemies.SuperHardMode.CrystalKnight.DashProgress(elapsed);
                    float progressNext = NPCs.Enemies.SuperHardMode.CrystalKnight.DashProgress(elapsed + 1);
                    float speed = _pursuitDashDistance * (progressNext - progressNow);
                    Vector2 desired = new Vector2(_pursuitDashDirection * speed, 0f);
                    Vector2 permitted = Collision.TileCollision(NPC.position, desired, NPC.width, NPC.height);
                    NPC.velocity.X = permitted.X;
                    if (permitted.X != 0f)
                    {
                        AfterimageTicks = 3;
                    }
                }

                if (elapsed >= PursuitLiveStartTick && elapsed <= PursuitLiveEndTick)
                {
                    float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
                    float liveProgress = (elapsed - PursuitLiveStartTick)
                        / (float)Math.Max(1, PursuitLiveEndTick - PursuitLiveStartTick);
                    ArmFireSlashVFX(bladeReach, liveProgress);
                    if (SecondPhase)
                    {
                        EmitMiasmaSlashDust(bladeReach);
                    }
                }
                return;
            }

            if (step.DamageMult <= 0f || step.Motion == ComboMotion.LeapSlam)
            {
                return;
            }

            float hitWindowEnd = step.HitWindowEnd > 0f ? step.HitWindowEnd : 1f;
            float progress = elapsed / (float)Math.Max(1, total - 1);
            if (progress <= hitWindowEnd)
            {
                float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
                ArmFireSlashVFX(bladeReach, progress / hitWindowEnd);
                if (SecondPhase)
                {
                    EmitMiasmaSlashDust(bladeReach);
                }
            }
        }

        protected override void OnLandingTimedLeapSlamSwingTick(MeleeComboStep step, float progress)
        {
            base.OnLandingTimedLeapSlamSwingTick(step, progress);
            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            ArmFireSlashVFX(bladeReach, progress);
            if (SecondPhase)
            {
                EmitMiasmaSlashDust(bladeReach);
            }
        }

        void EmitMiasmaSlashDust(float bladeReach)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 hand = PuppetHandPosition;
            Vector2 direction = PuppetWeaponDirection.SafeNormalize(new Vector2(NPC.direction, 0f));
            for (int i = 0; i < 3; i++)
            {
                Vector2 spot = hand + direction * bladeReach * Main.rand.NextFloat(0.35f, 1.05f)
                    + Main.rand.NextVector2Circular(6f, 6f);
                Color dustColor = i == 0 ? MiasmaPlayer.MiasmaColor : MiasmaPlayer.MiasmaPinkColor;
                Dust miasma = Dust.NewDustPerfect(
                    spot,
                    DustID.RainbowMk2,
                    -direction * 0.6f + Main.rand.NextVector2Circular(1.4f, 1.4f),
                    60,
                    dustColor,
                    Main.rand.NextFloat(1.05f, 1.5f));
                miasma.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 tipSpot = hand + direction * bladeReach * Main.rand.NextFloat(0.65f, 1.0f);
                Dust pink = Dust.NewDustPerfect(
                    tipSpot,
                    DustID.PinkTorch,
                    Main.rand.NextVector2Circular(1.2f, 1.2f),
                    80,
                    default,
                    Main.rand.NextFloat(1.1f, 1.55f));
                pink.noGravity = true;
            }
        }

        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = -0.15f }, NPC.Bottom);
            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-4.5f, 4.5f), Main.rand.NextFloat(-3.8f, -0.8f));
                Dust ember = Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-16f, 16f), -4f),
                    MemoryDustType, velocity, 70, MemoryColor, Main.rand.NextFloat(0.9f, 1.45f));
                ember.noGravity = true;
            }
        }

        protected override int ReactiveComboIndex(float distance, ComboRangeBand band, int[] ready)
        {
            Player player = Main.player[NPC.target];
            if (player == null || !player.active || player.dead)
            {
                return -1;
            }

            bool dodging = player.GetModPlayer<tsorcRevampPlayer>().isDodging;
            float awaySign = Math.Sign(player.Center.X - NPC.Center.X);
            bool rollingAway = dodging
                && Math.Sign(player.velocity.X) == awaySign
                && Math.Abs(player.velocity.X) > 2f;
            bool rollingThrough = dodging && distance < MeleeRange + 24f;
            bool airborne = player.velocity.Y < -3f && player.Center.Y < NPC.Center.Y - 24f;

            if (rollingAway && Ready(ready, CB_PURSUIT))
            {
                return CB_PURSUIT;
            }
            if (rollingThrough && Ready(ready, CB_BACKSTEP))
            {
                return CB_BACKSTEP;
            }
            if (airborne && Ready(ready, CB_REVERSAL))
            {
                return CB_REVERSAL;
            }
            return -1;
        }

        static bool Ready(int[] ready, int index)
            => index >= 0 && index < ready.Length && ready[index] > 0;

        public override void AI()
        {
            TryStartSecondPhaseTransition();
            base.AI();
            EnsureCorrectAttackWeapon();
            PrepareMagicTelegraph();
            SpawnStaffSpellPreviews();
            UpdateAttackLabel();
            EmitSpiralStaffDust();
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            NPC.TargetClosest(true);

            TickMemoryCycle();
            TickDistanceRules();
        }

        void EnsureCorrectAttackWeapon()
        {
            if (Phase == AttackPhase.JumpSlashDodgeback
                || Phase == AttackPhase.JumpSlashRise
                || Phase == AttackPhase.JumpSlashAttack
                || Phase == AttackPhase.JumpSlashRecovery
                || Phase == AttackPhase.FlipSlashRise
                || Phase == AttackPhase.FlipSlashLand)
            {
                SetDisplayWeapon(MeleeWeaponItemType, swing: false);
            }
        }

        void EmitSpiralStaffDust()
        {
            if (Main.dedServ || !IsSpiralFanPhase || !Main.rand.NextBool(2))
            {
                return;
            }
            if (Phase == AttackPhase.SpiralFanSwingTelegraph && PhaseTimer > 45)
            {
                return;
            }

            Vector2 tip = StaffTipPosition;
            Dust purple = Dust.NewDustPerfect(tip, DustID.PurpleTorch, Main.rand.NextVector2Circular(0.7f, 0.7f), 100,
                new Color(190, 85, 255), 0.85f);
            purple.noGravity = true;
            if (Main.rand.NextBool(2))
            {
                Dust blue = Dust.NewDustPerfect(tip, DustID.IceTorch, Main.rand.NextVector2Circular(0.65f, 0.65f), 100,
                    new Color(85, 185, 255), 0.8f);
                blue.noGravity = true;
            }
        }

        bool IsSpiralFanPhase => Phase == AttackPhase.SpiralFanSwingTelegraph
            || Phase == AttackPhase.SpiralFanSwing
            || Phase == AttackPhase.SpiralFanBurst
            || Phase == AttackPhase.SpiralFanPause;

        internal Vector2 StaffTipPosition
        {
            get
            {
                if (Main.dedServ)
                {
                    float worldAngle = MagicCastEndRotation;
                    if (NPC.direction != 1)
                    {
                        worldAngle = MathHelper.Pi - MagicCastEndRotation;
                    }

                    Vector2 staffDirection = worldAngle.ToRotationVector2();
                    return NPC.Center + new Vector2(NPC.direction * 4f, -2f) + staffDirection * 44f;
                }

                Vector2 gripPx = MagicGripNorm * new Vector2(42f, 40f);
                float gripToTip = Vector2.Distance(gripPx, new Vector2(41f, 0f)) * NPC.scale * StaffDrawScale;
                return PuppetWeaponTipPosition(gripToTip);
            }
        }

        void UpdateAttackLabel()
        {
            if (Phase == AttackPhase.Custom)
                return;

            DebugAttackLabel = Phase switch
            {
                AttackPhase.RangedTelegraph or AttackPhase.RangedAttack or AttackPhase.RangedRecovery
                    => _queuedRangedAttack == CinderRangedAttack.BioSpit ? "Emerald Rain Staff Volley"
                     : _queuedRangedAttack == CinderRangedAttack.PhantomSeeker ? "Phantom Fire Staff Seeker"
                     : "Dark Bead Staff Volley",
                AttackPhase.MagicTelegraph or AttackPhase.MagicAttack or AttackPhase.MagicRecovery
                    => _queuedMagicAttack == CinderMagicAttack.IceStorm ? "Rising Ice Storm"
                     : _queuedMagicAttack == CinderMagicAttack.PhasedMatterBlast ? "Phased Matter Cast"
                     : _queuedMagicAttack == CinderMagicAttack.CinderConstellation ? "Cinder Constellation"
                     : _queuedMagicAttack == CinderMagicAttack.FirelinkCross ? "Firelink Cross"
                     : _queuedMagicAttack == CinderMagicAttack.AshenOrbit ? "Ashen Orbit"
                     : _queuedMagicAttack == CinderMagicAttack.AbyssalCurtain ? "Abyssal Curtain"
                     : _queuedMagicAttack == CinderMagicAttack.AbyssalUpheaval ? "Abyssal Upheaval"
                     : _queuedMagicAttack == CinderMagicAttack.ElandVenomVolley ? "Venom Salvo of Eland"
                     : _queuedMagicAttack == CinderMagicAttack.ElandStickySpit ? "Sticky Bile of Eland"
                     : _queuedMagicAttack == CinderMagicAttack.ElandToxicNova ? "Toxic Gas Nova of Eland"
                     : "Lost Soul Curse",
                AttackPhase.SpiralFanSwingTelegraph or AttackPhase.SpiralFanSwing or AttackPhase.SpiralFanBurst or AttackPhase.SpiralFanPause or AttackPhase.SpiralFanRecovery => "Azure Wisp Fan",
                AttackPhase.JumpSlashDodgeback or AttackPhase.JumpSlashRise or AttackPhase.JumpSlashAttack or AttackPhase.JumpSlashRecovery => "Leaping Cinderfall",
                AttackPhase.FlipSlashRise or AttackPhase.FlipSlashLand => "Cinder Somersault",
                AttackPhase.BoomerangSwingTelegraph or AttackPhase.BoomerangSwing or AttackPhase.BoomerangRecovery => "Cinder Blade Throw",
                AttackPhase.HomingVolleyDodgeback or AttackPhase.HomingVolleySwingTelegraph or AttackPhase.HomingVolleySwing or AttackPhase.HomingVolleyRecovery => "Skyfall Trident Volley",
                AttackPhase.BreathTelegraph or AttackPhase.Breathing or AttackPhase.BreathRecovery => "First Flame Breath",
                AttackPhase.PierceTelegraph or AttackPhase.PierceDash or AttackPhase.PierceRecovery => "Ashen Piercing Dash",
                _ => null,
            };
        }

        protected override void DoRangedTelegraphVFX(bool secondary, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            progress = MathHelper.Clamp((progress * RangedTelegraphTicks - WeaponReadyLeadTicks) / 45f, 0f, 1f);
            if (progress <= 0f)
                return;

            Color color = secondary ? Color.LimeGreen : new Color(125, 185, 255);
            Vector2 tip = StaffTipPosition;
            if (!secondary && _queuedRangedAttack == CinderRangedAttack.PhantomSeeker)
            {
                float angle = progress * MathHelper.TwoPi * 2.5f * NPC.direction;
                float radius = MathHelper.Lerp(24f, 3f, progress);
                Vector2 offset = angle.ToRotationVector2() * radius;
                Vector2 inward = -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(0.7f, 2.5f, progress);
                int dustType = ((int)(progress * 30f) & 1) == 0 ? DustID.GoldFlame : DustID.OrangeTorch;
                Dust phantomDust = Dust.NewDustPerfect(tip + offset, dustType, inward, 85,
                    dustType == DustID.GoldFlame ? Color.Gold : Color.OrangeRed,
                    MathHelper.Lerp(0.5f, 0.9f, progress));
                phantomDust.noGravity = true;
                return;
            }
            Dust dust = Dust.NewDustPerfect(tip, secondary ? DustID.GreenTorch : DustID.IceTorch,
                Main.rand.NextVector2Circular(0.75f, 0.75f), 100, color, MathHelper.Lerp(0.7f, 1.15f, progress));
            dust.noGravity = true;
        }

        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            progress = MathHelper.Clamp((progress * MagicTelegraphTicks - WeaponReadyLeadTicks) / 45f, 0f, 1f);
            if (progress <= 0f)
                return;

            bool fireRitual = _queuedMagicAttack >= CinderMagicAttack.CinderConstellation
                && _queuedMagicAttack <= CinderMagicAttack.AshenOrbit;
            Vector2 tip = StaffTipPosition;
            if (_queuedMagicAttack == CinderMagicAttack.AbyssalCurtain
                || _queuedMagicAttack == CinderMagicAttack.AbyssalUpheaval)
            {
                // Purple staff-tip swirl plus vertical wisps hinting whether the rifts form above or below.
                bool fromAbove = _queuedMagicAttack == CinderMagicAttack.AbyssalCurtain;
                float verticalBias = fromAbove ? -1f : 1f;
                float angle = progress * MathHelper.TwoPi * 2.2f * NPC.direction;
                Vector2 ringOffset = angle.ToRotationVector2() * MathHelper.Lerp(16f, 4f, progress);
                Dust swirl = Dust.NewDustPerfect(tip + ringOffset, DustID.Shadowflame,
                    -ringOffset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(0.7f, 2.1f, progress),
                    85, new Color(175, 70, 255), MathHelper.Lerp(0.65f, 1.15f, progress));
                swirl.noGravity = true;

                Dust directional = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), verticalBias * MathHelper.Lerp(1.2f, 3.2f, progress)),
                    95, new Color(195, 95, 255), MathHelper.Lerp(0.6f, 1.1f, progress));
                directional.noGravity = true;

                if (progress > 0.72f && Main.rand.NextBool(2))
                {
                    Dust glint = Dust.NewDustPerfect(tip, DustID.AncientLight, Main.rand.NextVector2Circular(0.5f, 0.5f),
                        110, Color.MediumPurple, 0.6f);
                    glint.noGravity = true;
                }
                return;
            }
            if (_queuedMagicAttack >= CinderMagicAttack.ElandVenomVolley
                && _queuedMagicAttack <= CinderMagicAttack.ElandToxicNova)
            {
                float angle = progress * MathHelper.TwoPi * 2.0f * NPC.direction;
                Vector2 ringOffset = angle.ToRotationVector2() * MathHelper.Lerp(16f, 4f, progress);
                Dust tipDust = Dust.NewDustPerfect(tip + ringOffset, DustID.Poisoned,
                    -ringOffset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(0.7f, 2.0f, progress),
                    85, new Color(115, 240, 80), MathHelper.Lerp(0.75f, 1.3f, progress));
                tipDust.noGravity = true;

                if (_queuedMagicAttack == CinderMagicAttack.ElandToxicNova && Main.rand.NextBool(2))
                {
                    Vector2 auraSpot = NPC.Center + Main.rand.NextVector2CircularEdge(
                        MathHelper.Lerp(28f, 96f, progress),
                        MathHelper.Lerp(28f, 96f, progress));
                    Dust novaTell = Dust.NewDustPerfect(auraSpot, DustID.Poisoned,
                        (auraSpot - NPC.Center).SafeNormalize(Vector2.Zero) * MathHelper.Lerp(1.2f, 3.6f, progress),
                        90, new Color(140, 255, 95), MathHelper.Lerp(0.95f, 1.55f, progress));
                    novaTell.noGravity = true;
                }
                return;
            }
            if (fireRitual)
            {
                int ritual = (int)_queuedMagicAttack - (int)CinderMagicAttack.CinderConstellation;
                if (ritual == 0) // Cinder Constellation: diagonal, inward-curling embers.
                {
                    float angle = progress * MathHelper.TwoPi * 1.7f * NPC.direction;
                    Dust dust = Dust.NewDustPerfect(tip + angle.ToRotationVector2() * 6f, DustID.OrangeTorch,
                        -angle.ToRotationVector2() * (0.7f + progress), 90, Color.Orange, 0.55f + progress * 0.35f);
                    dust.noGravity = true;
                    if (progress > 0.75f && Main.rand.NextBool(3))
                    {
                        Dust glint = Dust.NewDustPerfect(tip, DustID.AncientLight, Main.rand.NextVector2Circular(0.5f, 0.5f),
                            100, Color.LightGoldenrodYellow, 0.45f);
                        glint.noGravity = true;
                    }
                }
                else if (ritual == 1) // Firelink Cross: alternating horizontal and vertical red-orange strokes.
                {
                    bool vertical = ((int)(progress * 30f) / 2) % 2 == 0;
                    Vector2 direction = vertical ? Vector2.UnitY : Vector2.UnitX * NPC.direction;
                    Dust dust = Dust.NewDustPerfect(tip - direction * 7f, vertical ? DustID.RedTorch : DustID.OrangeTorch,
                        direction * (1f + progress), 90, Color.OrangeRed, 0.55f + progress * 0.35f);
                    dust.noGravity = true;
                }
                else // Ashen Orbit: a clockwise ember ring that becomes white-hot at release.
                {
                    float angle = progress * MathHelper.TwoPi * 2.2f * NPC.direction;
                    Vector2 tangent = (angle + MathHelper.PiOver2 * NPC.direction).ToRotationVector2();
                    Dust dust = Dust.NewDustPerfect(tip + angle.ToRotationVector2() * 7f, DustID.OrangeTorch,
                        tangent * (0.8f + progress), 90, Color.Orange, 0.52f + progress * 0.3f);
                    dust.noGravity = true;
                    if (progress > 0.76f && Main.rand.NextBool(3))
                    {
                        Dust glint = Dust.NewDustPerfect(tip, DustID.AncientLight, Main.rand.NextVector2Circular(0.45f, 0.45f),
                            100, Color.LightGoldenrodYellow, 0.42f);
                        glint.noGravity = true;
                    }
                }
                return;
            }

            Color color = _queuedMagicAttack == CinderMagicAttack.IceStorm
                ? new Color(115, 195, 255) : new Color(190, 95, 255);
            Dust spellDust = Dust.NewDustPerfect(tip, _queuedMagicAttack == CinderMagicAttack.IceStorm ? DustID.IceTorch : DustID.PurpleTorch,
                Main.rand.NextVector2Circular(0.85f, 0.85f), 90, color, 0.75f + progress * 0.45f);
            spellDust.noGravity = true;
        }

        void SpawnStaffSpellPreviews()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (Phase == AttackPhase.RangedTelegraph && PhaseTimer == 30
                && _queuedRangedAttack == CinderRangedAttack.DarkBead)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<CinderDarkBeadPreview>(), 0, 0f, Main.myPlayer, NPC.whoAmI);
            }
            else if (Phase == AttackPhase.MagicTelegraph && PhaseTimer == 30)
            {
                if (_queuedMagicAttack == CinderMagicAttack.IceStorm)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<CinderIceSpellPreview>(), 0, 0f, Main.myPlayer, NPC.whoAmI);
                }
                else if (_queuedMagicAttack >= CinderMagicAttack.CinderConstellation
                    && _queuedMagicAttack <= CinderMagicAttack.AshenOrbit)
                {
                    SpawnRemoteFirePattern(Main.player[NPC.target], _queuedMagicAttack);
                }
            }
        }

        void SpawnRemoteFirePattern(Player target, CinderMagicAttack pattern)
        {
            int mode = pattern == CinderMagicAttack.CinderConstellation
                ? 0
                : pattern == CinderMagicAttack.FirelinkCross
                    ? 1
                    : 2;
            int count = CinderRemoteFlame.CountForMode(mode);
            float radius = CinderRemoteFlame.RadiusForMode(mode);
            float angleOffset = mode == 0
                ? Main.rand.NextFloat(MathHelper.TwoPi)
                : 0f;

            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count + angleOffset;
                // Pass the pattern angle in the spawn velocity (synced across MP) so every client
                // reads the exact same baseAngle on tick 1 for the dust pattern telegraph.
                Vector2 encodedAngle = angle.ToRotationVector2() * 0.01f;
                int id = Projectile.NewProjectile(NPC.GetSource_FromThis(),
                    target.Center + angle.ToRotationVector2() * radius,
                    encodedAngle,
                    ModContent.ProjectileType<CinderRemoteFlame>(),
                    FireBreathDamage,
                    0f,
                    Main.myPlayer,
                    target.whoAmI,
                    mode,
                    i);
                Main.projectile[id].rotation = angle;
                Main.projectile[id].netUpdate = true;
            }

            SoundEngine.PlaySound(pattern switch
            {
                CinderMagicAttack.CinderConstellation => SoundID.Item29 with { Volume = 0.7f, Pitch = 0.1f },
                CinderMagicAttack.FirelinkCross => SoundID.Item74 with { Volume = 0.7f, Pitch = -0.2f },
                _ => SoundID.Item119 with { Volume = 0.65f, Pitch = 0.05f }
            }, NPC.Center);
        }

        void PrepareMagicTelegraph()
        {
            if (Phase != AttackPhase.MagicTelegraph)
            {
                if (Phase != AttackPhase.MagicAttack)
                {
                    _magicPrepared = false;
                }
                return;
            }

            if (_magicPrepared || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (UsesPyromancyMemory)
            {
                _queuedMagicAttack = (CinderMagicAttack)Main.rand.Next(
                    (int)CinderMagicAttack.CinderConstellation,
                    (int)CinderMagicAttack.AshenOrbit + 1);
            }
            else if (UsesAbyssMemory)
            {
                _queuedMagicAttack = Main.rand.NextBool()
                    ? CinderMagicAttack.AbyssalCurtain
                    : CinderMagicAttack.AbyssalUpheaval;
            }
            else if (UsesElandMemory)
            {
                bool allowNova = NPC.HasValidTarget
                    && NPC.Distance(Main.player[NPC.target].Center) <= ElandNovaTriggerRange;
                int roll = Main.rand.Next(allowNova ? 3 : 2);
                if (roll == 0)
                {
                    _queuedMagicAttack = CinderMagicAttack.ElandVenomVolley;
                }
                else if (roll == 1)
                {
                    _queuedMagicAttack = CinderMagicAttack.ElandStickySpit;
                }
                else
                {
                    _queuedMagicAttack = CinderMagicAttack.ElandToxicNova;
                }
            }
            else if (SecondPhase)
            {
                _queuedMagicAttack = Main.rand.Next(3) switch
                {
                    0 => CinderMagicAttack.IceStorm,
                    1 => CinderMagicAttack.PhasedMatterBlast,
                    _ => CinderMagicAttack.LostSoulCurse,
                };
            }
            else
            {
                _queuedMagicAttack = Main.rand.NextBool()
                    ? CinderMagicAttack.IceStorm
                    : CinderMagicAttack.PhasedMatterBlast;
            }

            if (_queuedMagicAttack == CinderMagicAttack.IceStorm)
            {
                _magicShotProgress = 0f;
            }
            _magicPrepared = true;
            NPC.netUpdate = true;
        }

        void TryStartSecondPhaseTransition()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient
                || _secondPhaseStarted
                || NPC.life > NPC.lifeMax / 2)
            {
                return;
            }

            _secondPhaseStarted = true;
            _memory = CinderMemory.AshenWarrior;
            _memoryAttacksRemaining = 1;
            _memoryShiftPending = false;
            _customSequence = CinderCustomSequence.SecondPhaseTransition;
            StartCustomAttack(90);
            NPC.netUpdate = true;
        }

        void TickMemoryCycle()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                _previousPhase = Phase;
                return;
            }

            bool returnedToNeutral = IsCompletedAttackPhase(_previousPhase) && IsNeutralPhase(Phase);
            if (returnedToNeutral)
            {
                _memoryAttacksRemaining--;
                _memoryShiftPending = _memoryAttacksRemaining <= 0;
            }

            if (_memoryShiftPending && IsNeutralPhase(Phase))
            {
                _memory = (CinderMemory)(((int)_memory + 1) % 5);
                _memoryAttacksRemaining = 1;
                _memoryShiftPending = false;
                _customSequence = CinderCustomSequence.MemoryShift;
                StartCustomAttack(32);
                NPC.netUpdate = true;
            }

            _previousPhase = Phase;
        }

        static bool IsNeutralPhase(AttackPhase phase)
            => phase == AttackPhase.Idle || phase == AttackPhase.CasualStroll;

        static bool IsCompletedAttackPhase(AttackPhase phase)
        {
            return phase == AttackPhase.MeleeRecovery
                || phase == AttackPhase.StabRecovery
                || phase == AttackPhase.RangedRecovery
                || phase == AttackPhase.MagicRecovery
                || phase == AttackPhase.BreathRecovery
                || phase == AttackPhase.PierceRecovery
                || phase == AttackPhase.JumpSlashRecovery
                || phase == AttackPhase.FlipSlashLand
                || phase == AttackPhase.HomingVolleyRecovery
                || phase == AttackPhase.BoomerangRecovery
                || phase == AttackPhase.SpiralFanRecovery
                || phase == AttackPhase.MeleeComboRecovery;
        }

        protected override void DoCustomAttack()
        {
            if (_customSequence == CinderCustomSequence.SecondPhaseTransition)
            {
                DebugAttackLabel = "Second Phase: Soul Rekindled";
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.15f }, NPC.Center);
            }
            else
            {
                DebugAttackLabel = $"Memory Shift: {MemoryName}";
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = 0.1f }, NPC.Center);
            }

            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(5.5f, 5.5f);
                Dust dust = Dust.NewDustPerfect(NPC.Center, MemoryDustType, velocity, 80, MemoryColor, 1.35f);
                dust.noGravity = true;
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (!Main.dedServ)
            {
                int duration = _customSequence == CinderCustomSequence.SecondPhaseTransition ? 90 : 32;
                float progress = 1f - ticksRemaining / (float)duration;
                float radius = MathHelper.Lerp(90f, 18f, progress);

                if (Main.GameUpdateCount % 2 == 0)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                    Dust dust = Dust.NewDustPerfect(
                        NPC.Center + offset,
                        MemoryDustType,
                        -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(1.2f, 4.5f, progress),
                        80,
                        MemoryColor,
                        1.15f);
                    dust.noGravity = true;
                }

                Lighting.AddLight(NPC.Center, MemoryColor.ToVector3() * (0.8f + progress * 1.5f));
            }

            if (ticksRemaining == 1)
            {
                _customSequence = CinderCustomSequence.None;
                DebugAttackLabel = null;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }
        }

        void TickDistanceRules()
        {
            UsefulFunctions.DustRing(NPC.Center, (int)ProtectionRadius, DustID.BlueTorch, 20, 1f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.RedsWingsRun, 1, 1f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.Torch, 10, 1f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.RedTorch, 5, 2f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.Firefly, 100, -3f);

            bool protectedByDistance = NPC.HasValidTarget && NPC.Distance(Main.player[NPC.target].Center) > ProtectionRadius;
            if (protectedByDistance)
            {
                NPC.defense = 9999;
                if (_protectionTextCooldown <= 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Protected"), 175, 75, 255);
                    _protectionTextCooldown = 200;
                }
            }
            else
            {
                NPC.defense = BaseDefense;
            }

            if (_protectionTextCooldown > 0)
            {
                _protectionTextCooldown--;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    _cowardTimers[i] = 0;
                    continue;
                }

                float distance = NPC.Distance(player.Center);
                if (distance < KillRingRadius)
                {
                    player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);
                }
                if (distance < 700f)
                {
                    player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 60, false);
                }

                if (distance > KillRingRadius)
                {
                    _cowardTimers[i]++;
                    if (_cowardTimers[i] >= CowardGraceTicks)
                    {
                        player.AddBuff(ModContent.BuffType<CowardsAffliction>(), 30, false);
                        if (_cowardTimers[i] == CowardGraceTicks)
                        {
                            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Coward"), 235, 199, 23);
                        }
                    }
                }
                else
                {
                    _cowardTimers[i] = 0;
                }
            }
        }

        void ApplyMeleeHitDebuffs(Player target)
        {
            target.AddBuff(BuffID.OnFire, 10 * 60, false);
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 40 * 60, false);
            target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 30 * 60, false);
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 30 * 60, false);
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Weak, 10 * 60, false);
                target.AddBuff(BuffID.BrokenArmor, 3 * 60, false);
            }
        }

        public void OnHumanoidMeleeHit(Player target)
        {
            ApplyMeleeHitDebuffs(target);
            if (SecondPhase || NPC.life <= NPC.lifeMax / 2)
            {
                MiasmaBuildup.Apply(target, SwordMiasmaBuildupPerHit);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            ApplyMeleeHitDebuffs(target);
        }

        protected override void DoMeleeAttack()
        {
            ArmFireSlashVFX(MeleeRange * 0.7f, 0.5f);
            if (SecondPhase)
            {
                EmitMiasmaSlashDust(MeleeRange * 0.7f);
            }
            PlayMeleeSwingSound();
            TryMeleeHit(MeleeRange);
        }

        protected override void DoStabAttack()
        {
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.65f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(StabRange * 0.7f);
        }

        protected override void DoPierceWindup(int elapsed)
        {
            if (Main.dedServ || elapsed <= WeaponReadyLeadTicks || elapsed % 3 != 0)
            {
                return;
            }

            float progress = MathHelper.Clamp((elapsed - WeaponReadyLeadTicks) / 56f, 0f, 1f);
            Vector2 offset = Main.rand.NextVector2CircularEdge(58f, 28f);
            Dust dust = Dust.NewDustPerfect(
                NPC.Center + offset,
                DustID.Shadowflame,
                -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(1.2f, 4f, progress),
                80,
                new Color(205, 70, 255),
                1.1f);
            dust.noGravity = true;
        }

        protected override void DoPierceDashTick()
        {
            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(NPC.Center, new Color(180, 55, 255).ToVector3() * 1.1f);
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(12f, 20f),
                    DustID.Shadowflame,
                    -NPC.velocity * 0.22f,
                    90,
                    new Color(205, 70, 255),
                    1.05f);
                dust.noGravity = true;
            }
        }

        protected override void OnPierceContact(Player target, bool isStab)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                target.Center,
                new Vector2(NPC.direction * 0.01f, 0f),
                ModContent.ProjectileType<PuppetMeleeHitbox>(),
                MeleeDamage + 7,
                6f,
                Main.myPlayer,
                target.width + 12f,
                target.height + 8f);
        }

        protected override void OnRangedBurstStarted(bool secondary)
        {
            if (secondary)
            {
                _queuedRangedAttack = CinderRangedAttack.BioSpit;
                _greenRainShotIndex = 0;
                return;
            }

            if (!secondary && SecondPhase && Main.rand.NextBool(3))
            {
                _queuedRangedAttack = CinderRangedAttack.PhantomSeeker;
            }
            else
            {
                _queuedRangedAttack = CinderRangedAttack.DarkBead;
            }
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            switch (_queuedRangedAttack)
            {
                case CinderRangedAttack.PhantomSeeker:
                    FirePhantomSeeker(target);
                    break;
                case CinderRangedAttack.BioSpit:
                    FireBioSpit(target, BioSpitDamage);
                    break;
                default:
                    FireDarkBead(target);
                    break;
            }
        }

        void FireDarkBead(Player target)
        {
            Vector2 origin = StaffTipPosition;
            Vector2 baseVelocity = UsefulFunctions.Aim(origin, target.Center, 7f);
            for (int i = -1; i <= 1; i++)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), origin,
                    baseVelocity.RotatedBy(MathHelper.ToRadians(i * 9f)),
                    ModContent.ProjectileType<ArtoriasDarkBead>(), DarkBeadDamage, 0f, Main.myPlayer);
            }
            SoundEngine.PlaySound(SoundID.Item80 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center);
        }

        void FirePhantomSeeker(Player target)
        {
            Vector2 origin = StaffTipPosition;
            Vector2 velocity = UsefulFunctions.Aim(origin, target.Center, 7.5f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity,
                ModContent.ProjectileType<PhantomFireStaffSeeker>(),
                PhantomSeekerDamage, 0f, Main.myPlayer, target.whoAmI, Main.rand.NextFloat(MathHelper.TwoPi));
            SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
        }

        void FireBioSpit(Player target, int damage)
        {
            float beyondSign = Math.Sign(target.Center.X - NPC.Center.X);
            if (beyondSign == 0f)
                beyondSign = NPC.direction;
            float landingOffset = _greenRainShotIndex++ % 2 == 0
                ? 0f
                : beyondSign * Main.rand.NextFloat(50f, 100f);
            Vector2 landingPoint = target.Center + new Vector2(landingOffset, 0f);
            float height = Main.rand.NextFloat(9f, 16f) * 16f;
            float verticalSpeed = -MathF.Sqrt(2f * 0.30f * height);
            float airTime = -2f * verticalSpeed / 0.30f;
            float horizontalSpeed = MathHelper.Clamp((landingPoint.X - StaffTipPosition.X) / airTime, -18f, 18f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTipPosition,
                new Vector2(horizontalSpeed, verticalSpeed), ModContent.ProjectileType<CinderGreenRain>(),
                damage, 5f, Main.myPlayer, target.whoAmI, landingOffset);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = 0.5f }, NPC.Center);
        }

        protected override void DoMagicAttack()
        {
            if (!Main.dedServ)
            {
                if (_queuedMagicAttack == CinderMagicAttack.AbyssalCurtain
                    || _queuedMagicAttack == CinderMagicAttack.AbyssalUpheaval)
                {
                    Vector2 tip = StaffTipPosition;
                    for (int i = 0; i < 18; i++)
                    {
                        Dust burst = Dust.NewDustPerfect(tip, i % 2 == 0 ? DustID.Shadowflame : DustID.PurpleTorch,
                            Main.rand.NextVector2CircularEdge(3.5f, 3.5f), 80, new Color(180, 75, 255), 1.2f);
                        burst.noGravity = true;
                    }
                }
                else if (_queuedMagicAttack == CinderMagicAttack.ElandToxicNova)
                {
                    for (int i = 0; i < 48; i++)
                    {
                        Vector2 spot = NPC.Center + Main.rand.NextVector2Circular(ElandNovaRadius, ElandNovaRadius);
                        Dust gas = Dust.NewDustPerfect(spot, DustID.Poisoned,
                            Main.rand.NextVector2Circular(1.2f, 1.2f), 100, default, 1.5f);
                        gas.noGravity = true;
                    }
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f }, NPC.Center);
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // The selection was locked at telegraph entry so the staff, growing spell preview,
            // and release all tell the same attack.
            if (!_magicPrepared)
            {
                PrepareMagicTelegraph();
            }

            if (NPC.HasValidTarget)
            {
                _elandAimAnchor = Main.player[NPC.target].Center;
            }

            if (_queuedMagicAttack == CinderMagicAttack.IceStorm)
            {
                _magicBurstTotal = Main.rand.Next(6, 13);
                _magicBurstsRemaining = _magicBurstTotal - 1;
                _magicBurstTimer = 0;
                _magicShotProgress = 0f;
                _magicAttackTicksOverride = _magicBurstTotal * 12;
            }
            else if (_queuedMagicAttack >= CinderMagicAttack.CinderConstellation
                && _queuedMagicAttack <= CinderMagicAttack.AshenOrbit)
            {
                _magicBurstTotal = 0;
                _magicBurstsRemaining = 0;
                _magicAttackTicksOverride = 30;
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.55f, Pitch = -0.1f }, NPC.Center);
            }
            else if (_queuedMagicAttack == CinderMagicAttack.AbyssalCurtain
                || _queuedMagicAttack == CinderMagicAttack.AbyssalUpheaval)
            {
                _magicBurstTotal = 0;
                _magicBurstsRemaining = 0;
                _magicAttackTicksOverride = 24;
            }
            else if (_queuedMagicAttack == CinderMagicAttack.ElandVenomVolley)
            {
                int interval = SecondPhase ? 18 : 22;
                _magicBurstTotal = SecondPhase ? ElandComboCBrackets.Length : 4;
                _magicBurstsRemaining = _magicBurstTotal - 1;
                _magicBurstTimer = 0;
                _magicAttackTicksOverride = (_magicBurstTotal - 1) * interval + 16;
            }
            else if (_queuedMagicAttack == CinderMagicAttack.ElandStickySpit)
            {
                _magicBurstTotal = ElandStickyOffsets.Length;
                _magicBurstsRemaining = _magicBurstTotal - 1;
                _magicBurstTimer = 0;
                _magicAttackTicksOverride = (_magicBurstTotal - 1) * 6 + 16;
            }
            else if (_queuedMagicAttack == CinderMagicAttack.ElandToxicNova)
            {
                _magicBurstTotal = 0;
                _magicBurstsRemaining = 0;
                _magicAttackTicksOverride = 28;
            }
            else if (_queuedMagicAttack == CinderMagicAttack.LostSoulCurse)
            {
                _magicBurstTotal = SecondPhase ? 3 : 2;
                _magicBurstsRemaining = _magicBurstTotal - 1;
                _magicBurstTimer = 0;
                _magicAttackTicksOverride = (_magicBurstTotal - 1) * 60 + 16;
            }
            else
            {
                _magicBurstTotal = SecondPhase ? 3 : 2;
                _magicBurstsRemaining = _magicBurstTotal - 1;
                _magicBurstTimer = 0;
                _magicAttackTicksOverride = 56;
            }
            FireQueuedMagic(Main.player[NPC.target]);
        }

        protected override void DoMagicTick(int ticksRemaining)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || _magicBurstsRemaining <= 0)
            {
                return;
            }

            _magicBurstTimer++;
            int interval = 18;
            if (_queuedMagicAttack == CinderMagicAttack.IceStorm)
            {
                interval = 12;
            }
            else if (_queuedMagicAttack == CinderMagicAttack.LostSoulCurse)
            {
                interval = 60;
            }
            else if (_queuedMagicAttack == CinderMagicAttack.ElandVenomVolley)
            {
                interval = SecondPhase ? 18 : 22;
            }
            else if (_queuedMagicAttack == CinderMagicAttack.ElandStickySpit)
            {
                interval = 6;
            }

            if (_magicBurstTimer >= interval)
            {
                _magicBurstTimer = 0;
                FireQueuedMagic(Main.player[NPC.target]);
                _magicBurstsRemaining--;
                NPC.netUpdate = true;
            }
        }

        void FireQueuedMagic(Player target)
        {
            switch (_queuedMagicAttack)
            {
                case CinderMagicAttack.PhasedMatterBlast:
                {
                    Vector2 origin = StaffTipPosition;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), origin,
                        UsefulFunctions.BallisticTrajectory(origin, target.Center, 6f, 1.06f, true, true),
                        ModContent.ProjectileType<PhasedMatterBlast>(), DisruptDamage, 5f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.2f, Pitch = 0.4f }, NPC.Center);
                    break;
                }
                case CinderMagicAttack.LostSoulCurse:
                {
                    Vector2 origin = StaffTipPosition;
                    for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++)
                    {
                        Player fanTarget = Main.player[playerIndex];
                        if (!fanTarget.active || fanTarget.dead)
                            continue;

                        Vector2 aim = UsefulFunctions.Aim(origin, fanTarget.Center, 8f);
                        for (int fanIndex = -1; fanIndex <= 1; fanIndex++)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin,
                                aim.RotatedBy(MathHelper.ToRadians(fanIndex * 20f)),
                                ProjectileID.DesertDjinnCurse, LostSoulDamage, 7f, Main.myPlayer);
                        }
                    }
                    SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = 0.5f }, NPC.Center);
                    break;
                }
                case CinderMagicAttack.CinderConstellation:
                case CinderMagicAttack.FirelinkCross:
                case CinderMagicAttack.AshenOrbit:
                    // The brands were created at their remote positions during the last 30 tell ticks.
                    break;
                case CinderMagicAttack.AbyssalCurtain:
                    SpawnShadowRiftPattern(target, fromAbove: true);
                    break;
                case CinderMagicAttack.AbyssalUpheaval:
                    SpawnShadowRiftPattern(target, fromAbove: false);
                    break;
                case CinderMagicAttack.ElandVenomVolley:
                {
                    Vector2 origin = StaffTipPosition;
                    int volleyIndex = _magicBurstTotal - _magicBurstsRemaining - 1;
                    if (SecondPhase)
                    {
                        float bracket = ElandComboCBrackets[Math.Clamp(volleyIndex, 0, ElandComboCBrackets.Length - 1)];
                        for (int side = -1; side <= 1; side += 2)
                        {
                            Vector2 aimPoint = _elandAimAnchor + new Vector2(side * bracket, 0f);
                            Vector2 velocity = UsefulFunctions.BallisticTrajectory(origin, aimPoint, 13.5f, 0.25f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity,
                                ModContent.ProjectileType<PoisonSpitBall>(), ElandVenomDamage, 0f,
                                Main.myPlayer, 0.25f, ElandVenomSplash.ComboBDamageTicks);
                        }
                    }
                    else
                    {
                        Vector2 liveAnchor = target != null && target.active ? target.Center : _elandAimAnchor;
                        for (int i = 0; i < ElandVenomSpreadX.Length; i++)
                        {
                            Vector2 aimPoint = liveAnchor + new Vector2(ElandVenomSpreadX[i], 0f);
                            Vector2 velocity = UsefulFunctions.BallisticTrajectory(origin, aimPoint, 13.5f, 0.25f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity,
                                ModContent.ProjectileType<PoisonSpitBall>(), ElandVenomDamage, 0f,
                                Main.myPlayer, 0.25f, ElandVenomSplash.ComboBDamageTicks);
                        }
                    }
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.1f }, origin);
                    break;
                }
                case CinderMagicAttack.ElandStickySpit:
                {
                    Vector2 origin = StaffTipPosition;
                    int shotIndex = _magicBurstTotal - _magicBurstsRemaining - 1;
                    Vector2 offset = ElandStickyOffsets[Math.Clamp(shotIndex, 0, ElandStickyOffsets.Length - 1)];
                    Vector2 velocity = UsefulFunctions.BallisticTrajectory(origin, _elandAimAnchor + offset, 13f, 0.30f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity,
                        ModContent.ProjectileType<StickySpitBall>(), ElandStickyDamage, 0f,
                        Main.myPlayer, 0.30f);
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.35f, Pitch = -0.1f }, origin);
                    break;
                }
                case CinderMagicAttack.ElandToxicNova:
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<ToxicGasNovaFog>(), 0, 0f, Main.myPlayer);
                    break;
                default:
                {
                    Vector2 iceOrigin = StaffTipPosition;
                    float shotIndex = _magicBurstTotal - _magicBurstsRemaining - 1;
                    _magicShotProgress = _magicBurstTotal <= 1 ? 0.5f : shotIndex / (_magicBurstTotal - 1f);
                    float angle = MathHelper.Lerp(MathHelper.ToRadians(-145f), MathHelper.ToRadians(-35f), _magicShotProgress);
                    Vector2 velocity = angle.ToRotationVector2() * 12.5f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), iceOrigin, velocity,
                        ModContent.ProjectileType<EnemySpellIce3Ball>(), IceStormDamage, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.3f }, NPC.Center);
                    break;
                }
            }
        }

        void SpawnShadowRiftPattern(Player target, bool fromAbove)
        {
            if (target == null || !target.active)
            {
                return;
            }

            // Mirrors Knight of Gwyn's Curtain Call (above) and Bottom Up (below) ShadowShot rows,
            // centered symmetrically on the player and preceded by 40-tick converging rift dust.
            int count = 15;
            float spacing = 100f;
            float verticalOffset = fromAbove ? -400f : 480f;
            float launchSpeed = 1.3f;
            float riftTellTicks = 40f;

            for (int i = 0; i < count; i++)
            {
                float horizontalOffset = (i - (count - 1) * 0.5f) * spacing;
                Vector2 spawnPosition = target.Center + new Vector2(horizontalOffset, verticalOffset);
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    spawnPosition,
                    Vector2.Zero,
                    ModContent.ProjectileType<CinderShadowRift>(),
                    ShadowShotDamage,
                    2f,
                    Main.myPlayer,
                    target.whoAmI,
                    launchSpeed,
                    riftTellTicks);
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = fromAbove ? -0.05f : -0.2f }, NPC.Center);
        }

        protected override void DoBreathWindup(int elapsed)
        {
            if (Main.dedServ || elapsed <= WeaponReadyLeadTicks)
            {
                return;
            }

            // This is Massacre's former Juggernaut flame-breath tell, moved from the mouth to
            // Soul of Cinder's authored outstretched hand. The first 30 ticks show the pose alone.
            float t = MathHelper.Clamp((elapsed - WeaponReadyLeadTicks) / (float)BreathTellTicks, 0f, 1f);
            float scale = t < 0.4f ? 0.45f : MathHelper.Lerp(0.6f, 1.9f, (t - 0.4f) / 0.6f);
            Vector2 hand = PuppetHandPosition;
            int count = t < 0.4f ? 1 : 2;
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(hand + Main.rand.NextVector2Circular(3f, 3f), DustID.RedTorch,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 0, default, scale);
                dust.noGravity = true;
            }
            Lighting.AddLight(hand, 0.6f * scale, 0.05f, 0.05f);
        }

        protected override void OnBreathStart()
        {
            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.5f }, NPC.Center);
        }

        protected override void DoBreathTick(int ticksRemaining)
        {
            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 2f);
            if (Main.netMode == NetmodeID.MultiplayerClient || ticksRemaining % 3 != 0)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 hand = PuppetHandPosition;
            Vector2 breathVel = UsefulFunctions.Aim(hand, target.Center, 9f) + Main.rand.NextVector2Circular(1.5f, 1.5f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), hand, breathVel, ModContent.ProjectileType<FireBreath>(), FireBreathDamage, 0f, Main.myPlayer);
            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.12f, Pitch = 0.2f }, NPC.Center);
        }

        protected override void DoBoomerangFire()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, target.Center, 18f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<SwordOfCinder>(), SwordProjectileDamage, 0.5f, Main.myPlayer, NPC.whoAmI);
        }

        protected override void DoSpiralFanFire(int shotIndex)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = StaffTipPosition;
            int bulletCount = SecondPhase ? 7 : 5;
            float spread = SecondPhase ? 67f : 52.5f;
            Vector2 baseVelocity = UsefulFunctions.Aim(origin, target.Center, 24f);
            for (int i = -(bulletCount / 2); i <= bulletCount / 2; i++)
            {
                Vector2 velocity = baseVelocity.RotatedBy(MathHelper.ToRadians(-((spread / (bulletCount - 1)) * i)));
                Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity,
                    ModContent.ProjectileType<CinderWispTelegraph>(), OrangeProjDamage, 1f, Main.myPlayer);
            }
        }

        protected override int NextSpiralFanDelay(int completedShotIndex)
        {
            return completedShotIndex < (SecondPhase ? 5 : 3) ? 30 : -1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_memory);
            writer.Write((byte)_customSequence);
            writer.Write((byte)Math.Clamp(_memoryAttacksRemaining, 0, byte.MaxValue));
            writer.Write(_memoryShiftPending);
            writer.Write(_secondPhaseStarted);
            writer.Write((byte)_queuedRangedAttack);
            writer.Write((byte)_queuedMagicAttack);
            writer.Write((byte)Math.Clamp(_magicBurstsRemaining, 0, byte.MaxValue));
            writer.Write((byte)Math.Clamp(_magicBurstTotal, 0, byte.MaxValue));
            writer.Write(_magicShotProgress);
            writer.Write((sbyte)_pursuitDashDirection);
            writer.Write(_pursuitDashDistance);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            // The Custom phase, its timer and DoCustomAttack's cue arrive through the PuppetNPC phase snapshot; this only
            // carries the state those read.
            base.ReceiveExtraAI(reader);
            _memory = (CinderMemory)reader.ReadByte();
            _customSequence = (CinderCustomSequence)reader.ReadByte();
            _memoryAttacksRemaining = reader.ReadByte();
            _memoryShiftPending = reader.ReadBoolean();
            _secondPhaseStarted = reader.ReadBoolean();
            _queuedRangedAttack = (CinderRangedAttack)reader.ReadByte();
            _queuedMagicAttack = (CinderMagicAttack)reader.ReadByte();
            _magicBurstsRemaining = reader.ReadByte();
            _magicBurstTotal = reader.ReadByte();
            _magicShotProgress = reader.ReadSingle();
            _pursuitDashDirection = reader.ReadSByte();
            _pursuitDashDistance = reader.ReadSingle();
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 1.1f;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 1.1f;

            if (projectile.minion)
            {
                modifiers.Knockback *= 0f;
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<SoulOfCinderBag>()));
        }

    }
}
