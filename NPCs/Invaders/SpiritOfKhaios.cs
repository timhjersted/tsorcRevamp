using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Content.Items.Weapons.Ranged.Bows;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Sounds.CenterOfTheUniverse;
using tsorcRevamp.Content.Projectiles.Enemy.Gwyn;
using tsorcRevamp.Content.Projectiles.Enemy.Khaios;
using tsorcRevamp.Content.Projectiles.Ranged.Ammo;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>
    /// Phase 2 of the SHM Khaios Great Invader encounter — Spirit / Angel of Khaios.
    /// Ascended winged storm-archer wielding the Forgotten Thunder Bow (full Owl Father bow-draw,
    /// 4-mode stand/walk/run/jump movement + height-solved jump shots + airborne firing), paired
    /// with Celestial Star Ring (expanding, projectile-blocking, sequential-launching orbiters),
    /// Halo of Judgment (seraphic Sky Fracture crown), Seraphic Afterimage Echoes, Aegis of Light
    /// (Paladin's Shield frontal guard-counter), and Seraph's Judgment (&lt;35% HP aerial storm + dive).
    /// </summary>
    [AutoloadBossHead]
    public class SpiritOfKhaios : PuppetNPC
    {
        private const string SeraphCutName = "Seraph Cut";
        private const string CelestialReversalName = "Celestial Reversal";
        private const string RadiantAdvanceName = "Radiant Advance";
        private const string SkyPursuitLeapName = "Sky Pursuit Leap";
        private const string ThunderDiveName = "Thunder Dive";

        // Same ~180 deg live / ~224 deg envelope policy as Phase 1, slightly brisker tempo.
        private const float HighArcPose = -1.55f;
        private const float LowArcPose = 2.36f;
        private const int LightEaseInTicks = 6;
        private const int LightEaseOutTicks = 26;
        private const float LightDecayK = 7f;
        private const int HeavyEaseInTicks = 8;
        private const int HeavyEaseOutTicks = 30;
        private const float HeavyDecayK = 7f;
        private const int LeapSlamCrescentTicks = 10;

        // Bow jump-arc physics (matching Owl Father's RangedMoveStyle solver).
        private const float ForcedJumpHeightAbove = 3f * 16f;
        private const float ForcedJumpBlockedHeightAbove = 1f * 16f;
        private const float ForcedJumpMinRise = 3f * 16f;
        private const float ForcedJumpMaxRise = 7f * 16f;
        private const float RangedJumpUpSpeed = 8.5f;
        private const float RangedJumpGravity = 0.3f;
        private const float RangedJumpForwardSpeedMult = 1.65f;
        private const float ArrowVerticalLeadFraction = 0.5f;
        private const int ArrowInterceptIterations = 4;

        // Phase 2 custom set-piece timings & cooldowns.
        private const int SplitShotDurationTicks = 84;
        private const int SplitShotFireArrowsTick = 50;
        private const int SplitShotFireLanceTick = 62;
        private const int SplitShotCooldownTicks = 9 * 60;

        private const int StarRingSummonTicks = 46;
        private const int StarRingCooldownTicks = 18 * 60;

        private const int AegisGuardTicks = 56;
        private const int AegisCooldownTicks = 7 * 60;
        private const int AegisHitWindowTicks = 120;
        private const int AegisBeamCooldownTicks = 9 * 60;

        private const int SeraphJudgmentTicks = 112;
        private const int SeraphJudgmentCooldownTicks = 22 * 60;
        private const float SeraphJudgmentHealthThreshold = 0.35f;

        private static readonly SoundStyle[] SwordSwingSounds =
        {
            new SoundStyle("tsorcRevamp/Sounds/DST/DarkSwordSwing1") { Volume = 0.52f, Pitch = 0.14f, PitchVariance = 0.08f },
            new SoundStyle("tsorcRevamp/Sounds/DST/DarkSwordSwing2") { Volume = 0.52f, Pitch = 0.14f, PitchVariance = 0.08f },
            new SoundStyle("tsorcRevamp/Sounds/DST/DarkSwordSwing3") { Volume = 0.52f, Pitch = 0.14f, PitchVariance = 0.08f },
        };

        private static readonly string StarSoundBase = UsefulFunctions.RefactorableFilepath(typeof(CenterOfTheUniverseSound));

        private enum RangedMoveStyle : byte
        {
            Stand = 0,
            Walk = 1,
            Run = 2,
            Jump = 3,
        }

        private enum CustomMoveKind : byte
        {
            None = 0,
            SplitShot = 1,
            StarRing = 2,
            AegisGuard = 3,
            SeraphJudgment = 4,
            AegisBeam = 5,
        }

        private RangedMoveStyle rangedMoveStyle;
        private bool lastRangedMoveWasJump;
        private float rangedJumpVx;
        private float rangedJumpUpSpeed = RangedJumpUpSpeed;

        private CustomMoveKind activeCustomMove;
        private int customMoveElapsed;
        private Vector2 lockedSplitShotDir = Vector2.UnitX;
        private int splitShotCooldown = 180;
        private int starRingCooldown = 260;
        private int aegisCooldown = 140;
        private int aegisBeamCooldown = 110;
        private int seraphJudgmentCooldown = 180;
        private int recentHitCount;
        private int recentHitWindowTimer;
        private bool aegisCounterCharged;
        private int aegisBlockSoundCooldown;
        private int echoSpawnCooldown;
        private bool landingCrescentSpawned;
        private bool deathBurstPlayed;

        private readonly AttackBag<CustomMoveKind> customMoveBag;

        public SpiritOfKhaios()
        {
            customMoveBag = new AttackBag<CustomMoveKind>(cards =>
            {
                cards.Add(CustomMoveKind.AegisBeam);
                cards.Add(CustomMoveKind.AegisBeam);
                cards.Add(CustomMoveKind.AegisGuard);
                cards.Add(CustomMoveKind.SplitShot);
                cards.Add(CustomMoveKind.StarRing);
                cards.Add(CustomMoveKind.SeraphJudgment);
            });
        }

        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(SpiritOfKhaios)) + "_Head_Boss";

        protected override string InvaderTitle => "Spirit of Khaios";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => true;
        protected override bool IsGreatInvader => true;
        protected override int AbandonedDespawnTicks => 600;
        protected override Color PuppetSkinColor => new Color(0xF4, 0x86, 0x67);

        protected override int HeadArmorItemType => ItemID.SpookyHelmet;
        protected override int BodyArmorItemType => ItemID.GhostShirt;
        protected override int LegsArmorItemType => ItemID.None;
        protected override int HeadArmorDyeItemType => ItemID.ReflectiveGoldDye;
        protected override int BodyArmorDyeItemType => ItemID.SilverDye;

        protected override bool HasWings => true;
        protected override bool ShowWingsWhenGrounded => true;
        protected override int WingsAccessoryItemType => ItemID.WingsSolar;
        protected override int WingsDyeItemType => ItemID.BlackDye;

        protected override int[] AccessoryItemTypes => new int[]
        {
            ItemID.GlassSlipper,
            ItemID.AngelHalo,
            ItemID.CrossNecklace,
            ItemID.PaladinsShield,
        };
        protected override int[] AccessoryDyeItemTypes => new int[]
        {
            ItemID.ReflectiveGoldDye,
            ItemID.ReflectiveGoldDye,
            ItemID.ReflectiveGoldDye,
            ItemID.ReflectiveGoldDye,
        };

        // 1x same-size puppet scale (per locked design) with golden-white spirit afterimages on commits.
        protected override bool HasSpectralOverlay => true;
        protected override bool SpectralKeepsArmorDyes => true;
        protected override float SpectralOverlayScale => 1f;
        protected override Color SpectralOverlayColor => new Color(255, 232, 130);
        protected override float SpectralCoreTintStrength => 0.10f;
        protected override float SpectralCoreOpacity => 0.92f;
        protected override Color SpectralHaloColor => new Color(255, 224, 110);
        protected override int SpectralHaloCopyCount => 6;
        protected override float SpectralHaloRadius => 6f;
        protected override float SpectralHaloOpacity => 0.16f;
        protected override float SpectralTrailOpacity => 0.24f;
        protected override bool SpectralExcludeDownwardCopies => true;
        protected override bool ShowSpectralGhostCopies =>
            Phase == AttackPhase.Custom
            || ActiveMeleeComboName == SkyPursuitLeapName
            || ActiveMeleeComboName == ThunderDiveName
            || (Flight != null && Flight.IsAirborne);

        protected override EnemyFlightConfig FlightConfig => new EnemyFlightConfig
        {
            HoverAltitude = 215f,
            HoverSideOffset = 155f,
            HoverTopSpeed = 4.6f,
            TakeOffSpeed = 7.5f,
            DiveAcceleration = 0.48f,
            DiveTopSpeed = 12f,
            LandSpeed = 5.8f,
            MaxFlightTicks = 560,
            CooldownTicks = 175,
            TakeOffTicks = 30,
            HoverDwellTicks = 115,
            StrafeTicks = 60,
            DiveAttackTicks = 42,
            LandTicks = 75,
            WingFlapSpeed = 0.072f,
        };
        protected override int RandomTakeoffChance => 16;
        protected override float FlightHpEscalationFrac => 0.55f;
        protected override bool AllowRangedPatternsAirborne => true;
        protected override int AerialDiveCooldownTicks => 260;

        // Close-range melee (Wyrmkiller) + Signature SHM Ranged (Forgotten Thunder Bow) + Angelic Magic (Sky Fracture).
        protected override int MeleeWeaponItemType => ModContent.ItemType<Wyrmkiller>();
        protected override int RangedWeaponItemType => ModContent.ItemType<ForgottenThunderBow>();
        protected override RangedStyle RangedAnimStyle => RangedStyle.Bow;
        protected override int MagicWeaponItemType => ItemID.SkyFracture;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override Vector2 MeleeHandleNorm => new Vector2(0.20f, 0.78f);
        protected override float MeleeWeaponDrawScale => 1.65f;
        protected override float ComboReachBase => 98f;
        protected override float MeleeRange => 86f;
        protected override float MeleeEngageRange => 80f;
        protected override float ComboMaxStartRange => 440f;

        protected override int MeleeDamage => EnemyDamage.Projectile(300);
        protected override int RangedDamage => EnemyDamage.Projectile(240);
        protected override int MagicDamage => EnemyDamage.Projectile(230);

        // Forgotten Thunder Bow grip & string texels measured from ForgottenThunderBow.png (30x78).
        // Blue riser grip centered at (24, 39) -> (0.80, 0.50); pale-gold string column at x 6..7, y 8..69.
        protected override Vector2 GetHeldRangedGripNorm(int itemType) => new Vector2(0.80f, 0.50f);
        protected override float GetHeldRangedDrawScale(int itemType) => 0.88f;
        protected override Color BowStringColor => new Color(255, 240, 150);
        protected override string NockedBowArrowTexture => UsefulFunctions.RefactorableFilepath(typeof(LightningBoltProjectile));
        protected override bool UseBowStringDrawPose => true;

        private bool IsRangedBowPosePhase =>
            Phase == AttackPhase.RangedTelegraph
            || Phase == AttackPhase.RangedAttack
            || Phase == AttackPhase.CrossbowBurstPause
            || (Phase == AttackPhase.Custom && activeCustomMove == CustomMoveKind.SplitShot);

        protected override bool UseCompositeArmForAdditionalPhase => IsRangedBowPosePhase;

        protected override Rectangle GetBowStringTexels(int itemType)
        {
            if (itemType != ModContent.ItemType<ForgottenThunderBow>())
            {
                return Rectangle.Empty;
            }

            return new Rectangle(6, 8, 2, 62);
        }

        // Bow ranged band & cadence (3-shot early, 3- or 5-shot below 50% HP at 28t cadence — all rollable).
        protected override float RangedRange => 940f;
        protected override float MinRangedRange => 200f;
        protected override int RangedTelegraphTicks => 38;
        protected override int RangedRecoveryTicks => 32;
        protected override Color RangedTelegraphFlashColor => new Color(120, 235, 255);
        protected override int StandingRangedChance => rangedMoveStyle == RangedMoveStyle.Stand ? 100 : 0;

        protected override int[][] PrimaryRangedBurstPatterns
        {
            get
            {
                if (rangedMoveStyle == RangedMoveStyle.Jump)
                {
                    return null;
                }

                bool belowHalf = NPC.lifeMax > 0 && NPC.life <= NPC.lifeMax / 2;
                if (belowHalf)
                {
                    return new[] { new[] { 28, 28 }, new[] { 28, 28, 28, 28 } };
                }

                return new[] { new[] { 32, 32 } };
            }
        }

        protected override int[] PrimaryRangedBurstChances
        {
            get
            {
                bool belowHalf = NPC.lifeMax > 0 && NPC.life <= NPC.lifeMax / 2;
                return belowHalf ? new[] { 55, 45 } : null;
            }
        }

        protected override int SingleRangedBurstChance => rangedMoveStyle == RangedMoveStyle.Jump ? 100 : 0;

        // Halo of Judgment magic cast.
        protected override float MagicRange => 760f;
        protected override float MinMagicRange => 150f;
        protected override int MagicTelegraphTicks => 36;
        protected override int MagicRecoveryTicks => 30;
        protected override int MagicCooldownAfterUse => 380;
        protected override Color MagicTelegraphFlashColor => new Color(135, 240, 255);

        // Owl Father-tier swing stack.
        protected override bool UseCompositeArmSwing => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool UseLandingTimedLeapSlam => true;
        protected override bool UseAlternateFlip => false;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 8;
        protected override float LogicalWindupSettleFraction => 0.45f;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.88f;
        protected override float ComboTelegraphAdvanceStopDistance => MeleeRange * 0.58f;
        protected override float LeapLandingStandoff => 24f;
        protected override float LeapAttackForwardSpeed => TopSpeed * 2.2f;
        protected override float LeapAttackTargetLeadTicks => 10f;
        protected override float LeapAttackAscentTrackingStrength => 0.10f;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 55;
        protected override int MeleeRecoveryTicks => 20;
        protected override int CasualStrollChance => 0;
        protected override bool CanStab => false;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => new Color(140, 230, 255);
        protected override float SlashVFXOpacity => 0.60f;

        protected override int EstusChargesMax => 0;
        protected override float TopSpeed => 3.45f;
        protected override float Acceleration => 0.13f;
        protected override int MeleeTelegraphTicks => 28;
        protected override bool SlowDownBeforeMelee => false;
        protected override Color MeleeTelegraphFlashColor => new Color(255, 228, 120);
        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 6.8f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 6.8f;
        protected override bool EvadesProjectiles => true;
        protected override int PreemptiveQuickStepChance => 40;
        protected override int QuickStepRecoveryTicks => 16;

        protected override bool SlowDownDuringCustom => activeCustomMove != CustomMoveKind.SeraphJudgment;

        // Seraph's Judgment's first 40t aerial flare is staggerable; after that (and during Aegis/SplitShot/StarRing), hyper-armor holds.
        protected override bool CustomAttackCommitted
        {
            get
            {
                if (activeCustomMove == CustomMoveKind.SeraphJudgment)
                {
                    return customMoveElapsed >= 40;
                }

                return activeCustomMove != CustomMoveKind.None;
            }
        }

        // Ghost dust on kill and despawn (consistent with GhostShirt identity).
        protected override int OnKillDustType => DustID.Ghost;
        protected override Color OnKillDustColor => Color.White;
        protected override int DespawnDustType => DustID.Ghost;
        protected override Color DespawnDustColor => Color.White;

        private static MeleeComboStep WeightedSpiritCut(
            ComboMotion motion,
            int telegraphTicks,
            bool light,
            int pauseAfter = 0,
            float damageMult = 1f,
            float forwardPushMult = 0.50f,
            float reachMult = 1.08f,
            float stepInDistance = 40f)
        {
            int easeIn = light ? LightEaseInTicks : HeavyEaseInTicks;
            int easeOut = light ? LightEaseOutTicks : HeavyEaseOutTicks;
            float decay = light ? LightDecayK : HeavyDecayK;
            int totalTicks = easeIn + easeOut;
            float liveTicks = easeIn + easeOut * MathF.Log(1f / 0.3f) / decay;
            float hitWindowEnd = MathHelper.Clamp(liveTicks / totalTicks, 0f, 1f);

            return new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = totalTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = reachMult,
                ForwardPushMult = forwardPushMult,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Weighted,
                EaseInTicks = easeIn,
                EaseOutTicks = easeOut,
                EaseOutDecay = decay,
                HitWindowEnd = hitWindowEnd,
                StepInDistance = stepInDistance,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };
        }

        private static readonly MeleeComboStep CelestialReversalFollowUpStep = WeightedSpiritCut(
            ComboMotion.UnderhandArc,
            telegraphTicks: 0,
            light: false,
            damageMult: 1.18f,
            forwardPushMult: 0.65f,
            reachMult: 1.10f,
            stepInDistance: 46f);

        private static readonly MeleeCombo[] SpiritMeleeCombos =
        {
            new MeleeCombo
            {
                Name = SeraphCutName,
                BaseWeight = 75,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(255, 228, 120),
                CooldownAfterUse = 60,
                RecoveryTicks = 20,
                MoveBrake = 0.16f,
                Steps = new[]
                {
                    WeightedSpiritCut(ComboMotion.OverheadArc, telegraphTicks: 22, light: true, damageMult: 1.0f, forwardPushMult: 0.45f),
                },
            },
            new MeleeCombo
            {
                Name = CelestialReversalName,
                BaseWeight = 95,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(130, 235, 255),
                CooldownAfterUse = 120,
                RecoveryTicks = 24,
                MoveBrake = 0.12f,
                Steps = new[]
                {
                    WeightedSpiritCut(ComboMotion.OverheadArc, telegraphTicks: 24, light: true, pauseAfter: 10, damageMult: 0.92f, forwardPushMult: 0.52f),
                    CelestialReversalFollowUpStep,
                },
            },
            new MeleeCombo
            {
                Name = RadiantAdvanceName,
                BaseWeight = 70,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(255, 215, 95),
                CooldownAfterUse = 150,
                RecoveryTicks = 26,
                MoveBrake = 0.08f,
                Steps = new[]
                {
                    WeightedSpiritCut(ComboMotion.UnderhandArc, telegraphTicks: 26, light: false, pauseAfter: 10, damageMult: 0.95f, forwardPushMult: 0.75f, stepInDistance: 52f),
                    WeightedSpiritCut(ComboMotion.OverheadArc, telegraphTicks: 0, light: false, damageMult: 1.24f, forwardPushMult: 0.82f, reachMult: 1.12f, stepInDistance: 56f),
                },
            },
            new MeleeCombo
            {
                Name = SkyPursuitLeapName,
                BaseWeight = 80,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(130, 235, 255),
                CooldownAfterUse = 145,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                RecoveryTicks = 22,
                MoveBrake = 0.06f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.LeapSlam,
                        TelegraphTicks = 30,
                        AttackTicks = 80,
                        DamageMult = 1.25f,
                        ReachMult = 1.16f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Whip,
                        HitWindowEnd = 1f,
                        LeapHeightMult = 0.65f,
                        LeapForwardSpeedMult = 1.75f,
                        LeapStrikeRange = 120f,
                    },
                },
            },
            new MeleeCombo
            {
                Name = ThunderDiveName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Any,
                InitialFlashColor = new Color(255, 225, 110),
                CooldownAfterUse = 240,
                HeavyCommit = true,
                HyperArmor = true,
                AirborneStartOnly = true,
                RecoveryTicks = 26,
                MoveBrake = 0f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.LeapSlam,
                        TelegraphTicks = 32,
                        AttackTicks = 88,
                        DamageMult = 1.38f,
                        ReachMult = 1.20f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = 1f,
                        LeapStrikeRange = 145f,
                        LeapHeightMult = 1f,
                        LeapForwardSpeedMult = 1f,
                    },
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => SpiritMeleeCombos;

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            base.ModifyMeleeArcEndpoints(motion, ref startRotation, ref endRotation);

            if (motion == ComboMotion.OverheadArc)
            {
                startRotation = HighArcPose;
                endRotation = LowArcPose;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = LowArcPose;
                endRotation = HighArcPose;
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            bool isRangedMovePhase = Phase == AttackPhase.RangedTelegraph
                || Phase == AttackPhase.RangedAttack
                || Phase == AttackPhase.CrossbowBurstPause;

            if (isRangedMovePhase && rangedMoveStyle == RangedMoveStyle.Walk)
            {
                speedMult *= 0.5f;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 85;
            globalNPC.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(NPC, TopSpeed * speedMult, Acceleration, 4, 720f);
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 100000;
            NPC.defense = 62;
            NPC.damage = 0;
            NPC.knockBackResist = 0.10f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            // Expert Dark Souls = NPC.value / 10 -> 500,000 = 50,000 Expert Dark Souls.
            NPC.value = 500000f;
            NPC.boss = true;
            NPC.npcSlots = 10f;
            Music = MusicID.Boss5;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 90f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanDoubleJump = true;
            globalNPC.DoubleJumpPower = PuppetDoubleJumpPower;
            globalNPC.Agility = 0.78f;
            globalNPC.CanDodgeroll = true;
            globalNPC.CanJumpToEvade = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.MagicIllusion;
            globalNPC.TeleportMaxCharges = -1;
            globalNPC.PatrolMode = PatrolMode.ReturnToSpawn;
            globalNPC.RouteReaggroRange = 60f * 16f;
            EvasiveProfile.Khaios(globalNPC);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ForgottenThunderBowScroll>()));
        }

        public override void AI()
        {
            TickCooldowns();

            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
            if (neutral && Main.netMode != NetmodeID.MultiplayerClient && NPC.HasValidTarget)
            {
                Player target = Main.player[NPC.target];
                if (target.active && !target.dead)
                {
                    TryStartPhaseTwoCustomMove(target);
                }
            }

            base.AI();
            UpdateDebugAttackLabel();
            EmitSpiritMotes();
        }

        private void UpdateDebugAttackLabel()
        {
            if (Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause
                || Phase == AttackPhase.MeleeComboRecovery)
            {
                DebugAttackLabel = !string.IsNullOrEmpty(ActiveMeleeComboName)
                    ? ActiveMeleeComboName
                    : "Seraph Cut";
                return;
            }

            if (Phase == AttackPhase.RangedTelegraph
                || Phase == AttackPhase.RangedAttack
                || Phase == AttackPhase.CrossbowBurstPause
                || Phase == AttackPhase.RangedRecovery)
            {
                if (rangedMoveStyle == RangedMoveStyle.Jump)
                {
                    DebugAttackLabel = "Jump Thunder Bow Shot";
                }
                else if (rangedMoveStyle == RangedMoveStyle.Run)
                {
                    DebugAttackLabel = "Run Thunder Bow Volley";
                }
                else if (rangedMoveStyle == RangedMoveStyle.Walk)
                {
                    DebugAttackLabel = "Walk Thunder Bow Volley";
                }
                else
                {
                    DebugAttackLabel = "Stand Thunder Bow Volley";
                }

                return;
            }

            if (Phase == AttackPhase.MagicTelegraph
                || Phase == AttackPhase.MagicAttack
                || Phase == AttackPhase.MagicRecovery)
            {
                DebugAttackLabel = "Halo of Judgment";
                return;
            }

            if (Phase == AttackPhase.Custom)
            {
                if (activeCustomMove == CustomMoveKind.SplitShot)
                {
                    DebugAttackLabel = "Forgotten Thunder Split-Shot";
                }
                else if (activeCustomMove == CustomMoveKind.StarRing)
                {
                    DebugAttackLabel = "Celestial Star Ring";
                }
                else if (activeCustomMove == CustomMoveKind.AegisGuard)
                {
                    DebugAttackLabel = aegisCounterCharged ? "Aegis of Light (Charged)" : "Aegis of Light";
                }
                else if (activeCustomMove == CustomMoveKind.AegisBeam)
                {
                    DebugAttackLabel = customMoveElapsed < KhaiosShieldLightBeam.ChargeTicks
                        ? "Aegis of Light: Consecrated Beam (Charge)"
                        : "Aegis of Light: Consecrated Beam";
                }
                else if (activeCustomMove == CustomMoveKind.SeraphJudgment)
                {
                    DebugAttackLabel = "Seraph's Judgment";
                }
                else
                {
                    DebugAttackLabel = null;
                }

                return;
            }

            DebugAttackLabel = null;
        }

        private void TickCooldowns()
        {
            if (splitShotCooldown > 0)
            {
                splitShotCooldown--;
            }

            if (starRingCooldown > 0)
            {
                starRingCooldown--;
            }

            if (aegisCooldown > 0)
            {
                aegisCooldown--;
            }

            if (aegisBeamCooldown > 0)
            {
                aegisBeamCooldown--;
            }

            if (seraphJudgmentCooldown > 0)
            {
                seraphJudgmentCooldown--;
            }

            if (aegisBlockSoundCooldown > 0)
            {
                aegisBlockSoundCooldown--;
            }

            if (echoSpawnCooldown > 0)
            {
                echoSpawnCooldown--;
            }

            if (recentHitWindowTimer > 0)
            {
                recentHitWindowTimer--;
                if (recentHitWindowTimer <= 0)
                {
                    recentHitCount = 0;
                }
            }
        }

        private void TryStartPhaseTwoCustomMove(Player target)
        {
            // Reactive Aegis of Light guard trigger after taking 2+ hits within 120t.
            if (recentHitCount >= 2 && aegisCooldown <= 0 && NPC.Distance(target.Center) < 640f)
            {
                recentHitCount = 0;
                recentHitWindowTimer = 0;
                aegisCooldown = AegisCooldownTicks;
                aegisCounterCharged = false;
                BeginCustomMove(CustomMoveKind.AegisGuard, AegisGuardTicks, poseWeaponItemType: -1);
                return;
            }

            // Don't interrupt every idle tick — roll a brisk cadence check when drawing from the set-piece bag.
            if (!Main.rand.NextBool(18))
            {
                return;
            }

            float dist = NPC.Distance(target.Center);
            float healthFrac = NPC.lifeMax > 0 ? NPC.life / (float)NPC.lifeMax : 1f;
            bool hasActiveStars = HasActiveOrbitStars();

            bool canDraw = customMoveBag.TryDraw(move =>
            {
                if (move == CustomMoveKind.AegisBeam)
                {
                    return aegisBeamCooldown <= 0 && dist <= 1350f;
                }

                if (move == CustomMoveKind.AegisGuard)
                {
                    return aegisCooldown <= 0 && dist <= 640f;
                }

                if (move == CustomMoveKind.SplitShot)
                {
                    return splitShotCooldown <= 0 && dist >= 180f && dist <= 920f;
                }

                if (move == CustomMoveKind.StarRing)
                {
                    return starRingCooldown <= 0 && !hasActiveStars && dist <= 780f;
                }

                if (move == CustomMoveKind.SeraphJudgment)
                {
                    return seraphJudgmentCooldown <= 0 && healthFrac <= SeraphJudgmentHealthThreshold && dist <= 880f;
                }

                return false;
            }, out CustomMoveKind chosen);

            if (!canDraw)
            {
                return;
            }

            if (chosen == CustomMoveKind.AegisBeam)
            {
                aegisBeamCooldown = AegisBeamCooldownTicks;
                BeginCustomMove(CustomMoveKind.AegisBeam, KhaiosShieldLightBeam.TotalDurationTicks, poseWeaponItemType: -1);
            }
            else if (chosen == CustomMoveKind.AegisGuard)
            {
                aegisCooldown = AegisCooldownTicks;
                aegisCounterCharged = false;
                BeginCustomMove(CustomMoveKind.AegisGuard, AegisGuardTicks, poseWeaponItemType: -1);
            }
            else if (chosen == CustomMoveKind.SplitShot)
            {
                splitShotCooldown = SplitShotCooldownTicks;
                lockedSplitShotDir = (target.Center - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));
                ConfigureCustomRangedWeapon(ModContent.ItemType<ForgottenThunderBow>(), RangedStyle.Bow);
                BeginCustomMove(CustomMoveKind.SplitShot, SplitShotDurationTicks, ModContent.ItemType<ForgottenThunderBow>());
            }
            else if (chosen == CustomMoveKind.StarRing)
            {
                starRingCooldown = StarRingCooldownTicks;
                BeginCustomMove(CustomMoveKind.StarRing, StarRingSummonTicks, poseWeaponItemType: -1);
            }
            else if (chosen == CustomMoveKind.SeraphJudgment)
            {
                seraphJudgmentCooldown = SeraphJudgmentCooldownTicks;
                ConfigureCustomRangedWeapon(ModContent.ItemType<ForgottenThunderBow>(), RangedStyle.Bow);
                BeginCustomMove(CustomMoveKind.SeraphJudgment, SeraphJudgmentTicks, ModContent.ItemType<ForgottenThunderBow>());
            }
        }

        private void BeginCustomMove(CustomMoveKind kind, int duration, int poseWeaponItemType)
        {
            activeCustomMove = kind;
            customMoveElapsed = 0;
            StartCustomAttack(duration, poseWeaponItemType, swingPose: false);
            NPC.netUpdate = true;
        }

        private bool HasActiveOrbitStars()
        {
            int starType = ModContent.ProjectileType<KhaiosOrbitStar>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.type == starType && (int)proj.ai[0] == NPC.whoAmI)
                {
                    return true;
                }
            }

            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)rangedMoveStyle);
            writer.Write(rangedJumpVx);
            writer.Write(rangedJumpUpSpeed);
            writer.Write((byte)activeCustomMove);
            writer.Write(customMoveElapsed);
            writer.WriteVector2(lockedSplitShotDir);
            writer.Write(aegisCounterCharged);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            rangedMoveStyle = (RangedMoveStyle)reader.ReadByte();
            rangedJumpVx = reader.ReadSingle();
            rangedJumpUpSpeed = reader.ReadSingle();
            activeCustomMove = (CustomMoveKind)reader.ReadByte();
            customMoveElapsed = reader.ReadInt32();
            lockedSplitShotDir = reader.ReadVector2();
            aegisCounterCharged = reader.ReadBoolean();
        }

        // ── Custom Set-Piece Execution (Split-Shot, Star Ring, Aegis, Seraph's Judgment) ──

        protected override void DoCustomAttack()
        {
            customMoveElapsed = 0;

            if (activeCustomMove == CustomMoveKind.SplitShot && !Main.dedServ)
            {
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/DarkSouls/harmonic-magic") with { Volume = 0.45f, Pitch = -0.04f },
                    NPC.Center);
            }
            else if (activeCustomMove == CustomMoveKind.StarRing && !Main.dedServ)
            {
                SoundEngine.PlaySound(
                    new SoundStyle(StarSoundBase + "_CirclingProjectileCast1") with { Volume = 0.48f },
                    NPC.Center);
            }
            else if (activeCustomMove == CustomMoveKind.AegisGuard && !Main.dedServ)
            {
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/Custom/BlindingPulse") with { Volume = 0.45f, Pitch = 0.14f },
                    NPC.Center);
            }
            else if (activeCustomMove == CustomMoveKind.AegisBeam)
            {
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(
                        new SoundStyle("tsorcRevamp/Sounds/DarkSouls/harmonic-magic") with { Volume = 0.48f, Pitch = -0.08f },
                        NPC.Center);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 shieldOrigin = NPC.Center + new Vector2((NPC.direction >= 0 ? 1 : -1) * 22f, -2f);
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        shieldOrigin,
                        Vector2.Zero,
                        ModContent.ProjectileType<KhaiosShieldLightBeam>(),
                        EnemyDamage.Projectile(250),
                        4f,
                        Main.myPlayer,
                        NPC.whoAmI);
                }
            }
            else if (activeCustomMove == CustomMoveKind.SeraphJudgment)
            {
                Flight?.RequestTakeoff();
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(
                        new SoundStyle("tsorcRevamp/Sounds/HollowKnight/dream_enter_pt_2") with { Volume = 0.65f, Pitch = 0.16f },
                        NPC.Center);
                }
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            customMoveElapsed++;
            Player target = (NPC.HasValidTarget && Main.player[NPC.target].active && !Main.player[NPC.target].dead)
                ? Main.player[NPC.target]
                : null;

            if (activeCustomMove == CustomMoveKind.SplitShot)
            {
                RunSplitShotTick(target, ticksRemaining);
            }
            else if (activeCustomMove == CustomMoveKind.StarRing)
            {
                RunStarRingTick(ticksRemaining);
            }
            else if (activeCustomMove == CustomMoveKind.AegisGuard)
            {
                RunAegisGuardTick(target, ticksRemaining);
            }
            else if (activeCustomMove == CustomMoveKind.SeraphJudgment)
            {
                RunSeraphJudgmentTick(target, ticksRemaining);
            }

            if (ticksRemaining <= 1)
            {
                CustomMoveKind finished = activeCustomMove;
                activeCustomMove = CustomMoveKind.None;

                // If frontal hits charged Aegis of Light, immediately punish with the shield's Consecrated Light beam!
                if (finished == CustomMoveKind.AegisGuard && aegisCounterCharged && target != null && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    aegisCounterCharged = false;
                    aegisBeamCooldown = AegisBeamCooldownTicks;
                    BeginCustomMove(CustomMoveKind.AegisBeam, KhaiosShieldLightBeam.TotalDurationTicks, poseWeaponItemType: -1);
                }
            }
        }

        private void RunSplitShotTick(Player target, int ticksRemaining)
        {
            // Track target up to tick 36, then lock the aim line for 14t so rolling perpendicular is clean & fair.
            if (customMoveElapsed <= 36 && target != null)
            {
                NPC.direction = target.Center.X >= NPC.Center.X ? 1 : -1;
                Vector2 origin = PuppetBowGripPosition != Vector2.Zero ? PuppetBowGripPosition : NPC.Center;
                lockedSplitShotDir = (target.Center - origin).SafeNormalize(new Vector2(NPC.direction, 0f));
            }

            if (!Main.dedServ && customMoveElapsed <= SplitShotFireLanceTick)
            {
                Vector2 bowOrigin = PuppetBowGripPosition != Vector2.Zero ? PuppetBowGripPosition : NPC.Center;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(36f, 36f);
                    int dustType = i == 0 ? DustID.GoldFlame : DustID.Electric;
                    Color tint = i == 0 ? new Color(255, 228, 105) : Color.Cyan;
                    Dust charge = Dust.NewDustPerfect(
                        bowOrigin + offset,
                        dustType,
                        -offset * 0.14f,
                        20,
                        tint,
                        Main.rand.NextFloat(0.95f, 1.5f));
                    charge.noGravity = true;
                }
            }

            if (customMoveElapsed == SplitShotFireArrowsTick)
            {
                Vector2 bowOrigin = PuppetBowGripPosition != Vector2.Zero ? PuppetBowGripPosition : NPC.Center;
                Vector2 perp = new Vector2(-lockedSplitShotDir.Y, lockedSplitShotDir.X);

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.58f, Pitch = -0.06f }, bowOrigin);
                    SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.30f, Pitch = 0.30f }, bowOrigin);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 flankPos = bowOrigin + perp * (side * 22f);
                        Vector2 flankVel = lockedSplitShotDir.RotatedBy(side * 0.08f) * KhaiosThunderArrow.LaunchSpeed;
                        Projectile.NewProjectile(
                            NPC.GetSource_FromAI(),
                            flankPos,
                            flankVel,
                            ModContent.ProjectileType<KhaiosThunderArrow>(),
                            RangedDamage,
                            2.5f,
                            Main.myPlayer);
                    }

                    SpawnSeraphEchoAtCurrentPosition();
                }
            }
            else if (customMoveElapsed == SplitShotFireLanceTick)
            {
                Vector2 bowOrigin = PuppetBowGripPosition != Vector2.Zero ? PuppetBowGripPosition : NPC.Center;
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.52f, Pitch = 0.24f }, bowOrigin);
                    SoundEngine.PlaySound(
                        new SoundStyle("tsorcRevamp/Sounds/Custom/SoulCrashCut") with { Volume = 0.38f, Pitch = 0.10f },
                        bowOrigin);
                    UsefulFunctions.ScreenShake(bowOrigin, 3.8f, 12, distanceFalloff: 650f);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        bowOrigin,
                        lockedSplitShotDir * 19.5f,
                        ModContent.ProjectileType<KhaiosThunderLance>(),
                        EnemyDamage.Projectile(280),
                        4f,
                        Main.myPlayer);
                }
            }
        }

        private void RunStarRingTick(int ticksRemaining)
        {
            if (!Main.dedServ && customMoveElapsed % 2 == 0)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(54f, 54f);
                Dust dust = Dust.NewDustPerfect(
                    NPC.Center + offset,
                    customMoveElapsed % 4 == 0 ? DustID.GoldFlame : DustID.AncientLight,
                    -offset * 0.10f,
                    40,
                    Color.DeepSkyBlue,
                    Main.rand.NextFloat(1.0f, 1.55f));
                dust.noGravity = true;
            }

            if (customMoveElapsed == 18 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                bool belowHalf = NPC.lifeMax > 0 && NPC.life <= NPC.lifeMax / 2;
                int starCount = belowHalf ? 4 : 3;
                int starDamage = EnemyDamage.Projectile(250);

                for (int i = 0; i < starCount; i++)
                {
                    float baseAngle = MathHelper.TwoPi * i / starCount;
                    Vector2 spawnPos = NPC.Center + baseAngle.ToRotationVector2() * 105f;
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        spawnPos,
                        Vector2.Zero,
                        ModContent.ProjectileType<KhaiosOrbitStar>(),
                        starDamage,
                        3f,
                        Main.myPlayer,
                        NPC.whoAmI,
                        baseAngle,
                        i);
                }
            }
        }

        private void RunAegisGuardTick(Player target, int ticksRemaining)
        {
            // Keep facing locked at activation so the player can cleanly roll BEHIND him for a back-punish!
            if (customMoveElapsed == 1 && target != null)
            {
                NPC.direction = target.Center.X >= NPC.Center.X ? 1 : -1;
            }

            if (!Main.dedServ && customMoveElapsed % 2 == 0)
            {
                Vector2 shieldFront = NPC.Center + new Vector2(NPC.direction * 24f, Main.rand.NextFloat(-24f, 24f));
                Dust shimmer = Dust.NewDustPerfect(
                    shieldFront,
                    DustID.GoldFlame,
                    new Vector2(NPC.direction * 0.6f, Main.rand.NextFloat(-0.8f, 0.8f)),
                    40,
                    new Color(255, 228, 100),
                    Main.rand.NextFloat(0.95f, 1.4f));
                shimmer.noGravity = true;
            }
        }

        private void RunSeraphJudgmentTick(Player target, int ticksRemaining)
        {
            // Hover smoothly above the arena during the aerial halo flare & Tempest Rain.
            if (target != null)
            {
                Vector2 hoverGoal = target.Center + new Vector2(-NPC.direction * 140f, -240f);
                Vector2 toHover = hoverGoal - NPC.Center;
                NPC.velocity = Vector2.Lerp(NPC.velocity, toHover.SafeNormalize(Vector2.Zero) * Math.Min(6.5f, toHover.Length() * 0.06f), 0.12f);
            }

            if (!Main.dedServ && customMoveElapsed <= 60 && customMoveElapsed % 2 == 0)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(72f, 72f);
                Dust flare = Dust.NewDustPerfect(
                    NPC.Center + offset,
                    customMoveElapsed % 4 == 0 ? DustID.Electric : DustID.GoldFlame,
                    -offset * 0.12f,
                    30,
                    new Color(255, 225, 95),
                    Main.rand.NextFloat(1.1f, 1.75f));
                flare.noGravity = true;
            }

            // Spawn 5 staggered ground-snapped GwynLightningStrike columns (Tempest Rain) starting at tick 56.
            if (target != null && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int strikeStep = customMoveElapsed - 56;
                if (strikeStep >= 0 && strikeStep <= 40 && strikeStep % 10 == 0)
                {
                    int index = strikeStep / 10;
                    float[] offsets = { 0f, 110f, -110f, 220f, -220f };
                    float strikeX = target.Center.X + target.velocity.X * 14f + offsets[index];
                    Vector2 strikePos = new Vector2(strikeX, target.Bottom.Y - 40f);

                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        strikePos,
                        Vector2.Zero,
                        ModContent.ProjectileType<GwynLightningStrike>(),
                        EnemyDamage.Projectile(270),
                        4f,
                        Main.myPlayer);
                }
            }
        }

        private void SpawnSeraphEchoAtCurrentPosition()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || echoSpawnCooldown > 0 || !NPC.HasValidTarget)
            {
                return;
            }

            echoSpawnCooldown = 140;
            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                Vector2.Zero,
                ModContent.ProjectileType<KhaiosSeraphEcho>(),
                RangedDamage,
                2f,
                Main.myPlayer,
                NPC.target);
        }

        // ── Owl Father Ranged Bow Movement Variety & Gravity-Intercept Aim ───────────

        private int RangedJumpTicksToApex => Math.Max(1, (int)Math.Round(rangedJumpUpSpeed / RangedJumpGravity));
        protected override bool HasRangedJumpOverride => rangedMoveStyle == RangedMoveStyle.Jump;

        private RangedMoveStyle PickRangedMoveStyle()
        {
            const int standWeight = 32;
            const int walkWeight = 28;
            const int runWeight = 22;
            int jumpWeight = lastRangedMoveWasJump ? 0 : 18;

            int roll = Main.rand.Next(standWeight + walkWeight + runWeight + jumpWeight);
            if (roll < standWeight)
            {
                return RangedMoveStyle.Stand;
            }

            roll -= standWeight;
            if (roll < walkWeight)
            {
                return RangedMoveStyle.Walk;
            }

            roll -= walkWeight;
            if (roll < runWeight)
            {
                return RangedMoveStyle.Run;
            }

            return RangedMoveStyle.Jump;
        }

        protected override void ModifyRangedBurst(
            bool secondary,
            ref int itemType,
            ref RangedStyle style,
            ref Color flashColor,
            ref int telegraphTicks,
            ref int attackTicks,
            ref int recoveryTicks)
        {
            bool grounded = Flight == null || !Flight.IsAirborne;
            rangedJumpUpSpeed = RangedJumpUpSpeed;
            bool forcedJump = false;

            if (grounded && NPC.HasValidTarget)
            {
                Player shotTarget = Main.player[NPC.target];
                Vector2 bowGrip = PuppetBowGripPosition != Vector2.Zero ? PuppetBowGripPosition : NPC.Center;
                float heightAbove = bowGrip.Y - shotTarget.Center.Y;
                bool shotLineClear = Collision.CanHitLine(bowGrip, 1, 1, shotTarget.Center, 1, 1);
                bool farAbove = heightAbove >= ForcedJumpHeightAbove;
                bool aboveAndBlocked = heightAbove >= ForcedJumpBlockedHeightAbove && !shotLineClear;

                if (farAbove || aboveAndBlocked)
                {
                    forcedJump = true;
                    float rise = MathHelper.Clamp(heightAbove, ForcedJumpMinRise, ForcedJumpMaxRise);
                    rangedJumpUpSpeed = MathF.Sqrt(2f * RangedJumpGravity * rise);
                }
            }

            if (forcedJump)
            {
                rangedMoveStyle = RangedMoveStyle.Jump;
            }
            else if (grounded)
            {
                rangedMoveStyle = PickRangedMoveStyle();
            }
            else
            {
                rangedMoveStyle = RangedMoveStyle.Stand;
            }

            lastRangedMoveWasJump = rangedMoveStyle == RangedMoveStyle.Jump;
            NPC.netUpdate = true;

            if (rangedMoveStyle != RangedMoveStyle.Jump || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            int jumpDir = target.Center.X < NPC.Center.X ? -1 : 1;
            rangedJumpVx = jumpDir * TopSpeed * RangedJumpForwardSpeedMult;
            telegraphTicks = RangedJumpTicksToApex;
            attackTicks = RangedJumpTicksToApex * 2;
        }

        protected override void TickRangedJumpOverride()
        {
            bool inTelegraph = Phase == AttackPhase.RangedTelegraph;
            int ticksToApex = RangedJumpTicksToApex;
            int elapsed = inTelegraph ? ticksToApex - PhaseTimer : ticksToApex * 2 - PhaseTimer;

            if (inTelegraph && elapsed < 0)
            {
                NPC.velocity.X *= 0.8f;
                return;
            }

            if (!inTelegraph && elapsed > 2 && NPC.velocity.Y == 0f)
            {
                NPC.velocity.X *= 0.7f;
                return;
            }

            NPC.velocity.X = rangedJumpVx;
            NPC.velocity.Y = inTelegraph
                ? -rangedJumpUpSpeed + RangedJumpGravity * elapsed
                : RangedJumpGravity * elapsed;
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetBowGripPosition != Vector2.Zero ? PuppetBowGripPosition : NPC.Center;
            Vector2 aimAt = target.Center;

            for (int i = 0; i < ArrowInterceptIterations; i++)
            {
                float flightTicks = Vector2.Distance(origin, aimAt) / KhaiosThunderArrow.LaunchSpeed;
                Vector2 lead = new Vector2(target.velocity.X, target.velocity.Y * ArrowVerticalLeadFraction) * flightTicks;
                float fallingTicks = Math.Max(0f, flightTicks - KhaiosThunderArrow.GravityDelayTicks);
                float gravityDrop = KhaiosThunderArrow.GravityAccel * fallingTicks * (fallingTicks + 1f) * 0.5f;
                aimAt = target.Center + lead - new Vector2(0f, gravityDrop);
            }

            Vector2 aimDirection = (aimAt - origin).SafeNormalize(new Vector2(NPC.direction, 0f));

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.56f, Pitch = -0.08f, PitchVariance = 0.08f }, origin);
                SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.24f, Pitch = 0.36f }, origin);
            }

            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                origin,
                aimDirection * KhaiosThunderArrow.LaunchSpeed,
                ModContent.ProjectileType<KhaiosThunderArrow>(),
                RangedDamage,
                2.5f,
                Main.myPlayer);
        }

        // ── Halo of Judgment (Telegraphed Seraphic Sky Fracture Crown) ───────────────

        protected override void DoMagicAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            _magicAttackTicksOverride = 36;
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/DST/DarkSwordSummon1") with { Volume = 0.44f, Pitch = 0.28f },
                    NPC.Center);
            }

            int castFacingSide = NPC.direction >= 0 ? 1 : -1;
            if (NPC.HasValidTarget)
            {
                Player target = Main.player[NPC.target];
                if (target.active && !target.dead)
                {
                    castFacingSide = target.Center.X >= NPC.Center.X ? 1 : -1;
                    NPC.direction = castFacingSide;
                }
            }

            // Spawn 5 Halo of Judgment blades arrayed in a seraphic crown behind his wings (68t between each launch).
            // Encode slot (1..5) * castFacingSide in ai[1] so every blade locks its horizontal firing side on spawn.
            const int bladeCount = 5;
            for (int i = 0; i < bladeCount; i++)
            {
                float fanFrac = (i - 2f) / 2f;
                float arcAngle = -MathHelper.PiOver2 + fanFrac * 1.15f;
                Vector2 spawnPos = NPC.Center + arcAngle.ToRotationVector2() * 64f;
                int launchTick = 46 + i * 68;
                float signedSlot = (i + 1) * castFacingSide;

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    spawnPos,
                    Vector2.Zero,
                    ModContent.ProjectileType<KhaiosHaloBlade>(),
                    MagicDamage,
                    3f,
                    Main.myPlayer,
                    NPC.whoAmI,
                    signedSlot,
                    launchTick);
            }

            NPC.netUpdate = true;
        }

        // ── Melee & Aerial Thunder Dive Callbacks ────────────────────────────────────

        protected override void DoMeleeAttack()
        {
            PlaySwordSwingSound();
            TryMeleeHit();
        }

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            landingCrescentSpawned = false;
        }

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (step.Motion != ComboMotion.LeapSlam)
            {
                if (elapsed == step.EaseInTicks)
                {
                    PlaySwordSwingSound();
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && elapsed == 0)
                {
                    float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
                    SpawnSpiritSwordCrescent(step, Math.Max(8, (int)(total * step.HitWindowEnd) + 4), bladeReach);
                }
            }
        }

        protected override void OnLandingTimedLeapSlamSwingTick(MeleeComboStep step, float progress)
        {
            base.OnLandingTimedLeapSlamSwingTick(step, progress);

            if (landingCrescentSpawned || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            landingCrescentSpawned = true;
            PlaySwordSwingSound();
            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            int remainingTicks = Math.Max(5, (int)Math.Ceiling((1f - MathHelper.Clamp(progress, 0f, 1f)) * LeapSlamCrescentTicks));
            SpawnSpiritSwordCrescent(step, remainingTicks, bladeReach);
        }

        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_strike_ground") with { Volume = 0.48f, Pitch = 0.12f },
                    NPC.Bottom);
                UsefulFunctions.ScreenShake(NPC.Bottom, 4.2f, 12, distanceFalloff: 620f);

                for (int i = 0; i < 36; i++)
                {
                    Dust burst = Dust.NewDustPerfect(
                        NPC.Bottom + new Vector2(Main.rand.NextFloat(-18f, 18f), -3f),
                        i % 3 == 0 ? DustID.GoldFlame : DustID.Electric,
                        new Vector2(Main.rand.NextFloat(-5.5f, 5.5f), Main.rand.NextFloat(-4.8f, -1.0f)),
                        40,
                        Color.Cyan,
                        Main.rand.NextFloat(1.0f, 1.65f));
                    burst.noGravity = true;
                }
            }

            // Thunder Dive calls twin floor sparks outward on touchdown.
            if (ActiveMeleeComboName == ThunderDiveName && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int sparkDamage = EnemyDamage.Projectile(240);
                for (int dir = -1; dir <= 1; dir += 2)
                {
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Bottom + new Vector2(dir * 20f, -16f),
                        Vector2.Zero,
                        ModContent.ProjectileType<GwynFloorSpark>(),
                        sparkDamage,
                        3f,
                        Main.myPlayer,
                        dir,
                        28,
                        0f);
                }
            }
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == SkyPursuitLeapName && distance > ReliableJumpStartRange(combo.Steps[0]))
            {
                return false;
            }

            return true;
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == CelestialReversalName && nextStepIndex == 1)
            {
                float reach = ComboReachBase * 0.7f * CelestialReversalFollowUpStep.ReachMult;
                float pushDist = ComboForwardPushTopSpeed * CelestialReversalFollowUpStep.ForwardPushMult * CelestialReversalFollowUpStep.AttackTicks;
                return previousStepHit || NPC.Distance(target.Center) <= reach + pushDist;
            }

            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        // ── Aegis of Light Frontal Guard & Evasive Gating ────────────────────────────

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitByItem(player, item, ref modifiers);
            ApplyAegisFrontalReduction(player.Center, ref modifiers);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitByProjectile(projectile, ref modifiers);
            ApplyAegisFrontalReduction(projectile.Center, ref modifiers);
        }

        private void ApplyAegisFrontalReduction(Vector2 attackerPos, ref NPC.HitModifiers modifiers)
        {
            if (Phase != AttackPhase.Custom || activeCustomMove != CustomMoveKind.AegisGuard)
            {
                return;
            }

            float relativeX = attackerPos.X - NPC.Center.X;
            bool hitFromFront = (relativeX >= 0f && NPC.direction >= 0) || (relativeX <= 0f && NPC.direction < 0);
            if (!hitFromFront)
            {
                return;
            }

            // Frontal hits during Aegis of Light take 75% less damage and charge the Split-Shot counter.
            modifiers.FinalDamage *= 0.25f;
            aegisCounterCharged = true;
            NPC.netUpdate = true;

            if (!Main.dedServ && aegisBlockSoundCooldown <= 0)
            {
                aegisBlockSoundCooldown = 14;
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/Custom/MetalShatter") with { Volume = 0.36f, Pitch = 0.25f },
                    NPC.Center);

                for (int i = 0; i < 14; i++)
                {
                    Dust spark = Dust.NewDustPerfect(
                        NPC.Center + new Vector2(NPC.direction * 24f, Main.rand.NextFloat(-16f, 16f)),
                        DustID.GoldFlame,
                        Main.rand.NextVector2Circular(5f, 5f),
                        30,
                        new Color(255, 230, 105),
                        Main.rand.NextFloat(1.0f, 1.5f));
                    spark.noGravity = true;
                }
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            RecordHitPressure();

            if (CanTriggerEvasiveReaction())
            {
                SpawnSeraphEchoAtCurrentPosition();
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            RecordHitPressure();

            if (CanTriggerEvasiveReaction())
            {
                SpawnSeraphEchoAtCurrentPosition();
                tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
            }
        }

        private void RecordHitPressure()
        {
            if (Phase == AttackPhase.Custom && activeCustomMove == CustomMoveKind.AegisGuard)
            {
                return;
            }

            recentHitCount++;
            recentHitWindowTimer = AegisHitWindowTicks;
        }

        private bool CanTriggerEvasiveReaction()
        {
            return Phase == AttackPhase.Idle
                || Phase == AttackPhase.CasualStroll
                || Phase == AttackPhase.ClosingDistance;
        }

        // ── Visuals: Telegraph Aim Lines, Golden Aegis Arc, Spirit Motes, Death Burst ──

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);

            // Aegis of Light frontal golden barrier arc (shown during both Aegis guard and Consecrated Light beam).
            bool showingShieldArc = Phase == AttackPhase.Custom
                && (activeCustomMove == CustomMoveKind.AegisGuard || activeCustomMove == CustomMoveKind.AegisBeam);
            if (showingShieldArc)
            {
                bool brightGold = aegisCounterCharged || activeCustomMove == CustomMoveKind.AegisBeam;
                Color arcColor = brightGold
                    ? new Color(255, 240, 130) * 0.92f
                    : new Color(255, 210, 80) * 0.72f;
                float centerAngle = NPC.direction >= 0 ? 0f : MathF.PI;
                const int segments = 10;
                const float radius = 34f;
                Vector2 prev = NPC.Center + (centerAngle - 1.05f).ToRotationVector2() * radius;

                for (int i = 1; i <= segments; i++)
                {
                    float angle = centerAngle - 1.05f + 2.10f * (i / (float)segments);
                    Vector2 next = NPC.Center + angle.ToRotationVector2() * radius;
                    Utils.DrawLine(spriteBatch, prev, next, arcColor, arcColor, 3.2f);
                    prev = next;
                }
            }
        }

        private void EmitSpiritMotes()
        {
            if (Main.dedServ || Main.gamePaused)
            {
                return;
            }

            Lighting.AddLight(NPC.Center, 0.72f, 0.65f, 0.40f);

            if (Main.rand.NextBool(4))
            {
                int dustType = Main.rand.NextBool(3) ? DustID.Electric : DustID.GoldFlame;
                Dust mote = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(18f, 24f),
                    dustType,
                    new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(-1.4f, -0.35f)),
                    80,
                    dustType == DustID.Electric ? Color.Cyan : new Color(255, 225, 100),
                    Main.rand.NextFloat(0.75f, 1.2f));
                mote.noGravity = true;
            }
        }

        private void SpawnSpiritSwordCrescent(MeleeComboStep step, int duration, float bladeReach)
        {
            bool reverse = step.Motion == ComboMotion.UnderhandArc || step.Motion == ComboMotion.RisingUppercutLeap;
            float sweep = (reverse ? -1f : 1f) * NPC.direction * MathHelper.Pi;
            VanillaSwordArcSettings settings = new VanillaSwordArcSettings
            {
                Texture = VanillaSwordArcTexture.NightsEdge,
                Easing = VanillaSwordArcEasing.Linear,
                Duration = Math.Max(1, duration),
                StartAngle = PuppetWeaponDirection.ToRotation(),
                SweepAngle = sweep,
                Radius = Math.Max(54f, bladeReach * 1.06f),
                Opacity = 0.60f,
                FadeInFraction = Math.Min(0.15f, 2f / Math.Max(1, duration)),
                FadeOutFraction = 0.26f,
                AfterimageLag = MathHelper.PiOver4,
                AfterimageOpacity = 0.45f,
                BodyOpacity = 0.74f,
                CoreOpacity = 0.30f,
                DrawTipSparkle = false,
                TintWithWorldLighting = true,
                DarkColor = new Color(36, 48, 86),
                BodyColor = new Color(110, 220, 255),
                CoreColor = new Color(255, 238, 150),
                DrawCinderOverlay = false,
                DustType = -1,
                DustCount = 0,
                TrackPuppetBlade = true,
                EnableCollision = false,
            };

            VanillaSwordArc.SpawnForNPC(NPC.GetSource_FromAI(), NPC, 0, 0f, Main.myPlayer, settings, Vector2.Zero, hostile: false);
        }

        private void PlaySwordSwingSound()
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundStyle chosen = SwordSwingSounds[Main.rand.Next(SwordSwingSounds.Length)];
            SoundEngine.PlaySound(chosen, NPC.Center);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                SpawnSpiritDeathBurst();
            }
        }

        public override void OnKill()
        {
            base.OnKill();
            SpawnSpiritDeathBurst();
        }

        private void SpawnSpiritDeathBurst()
        {
            if (deathBurstPlayed || Main.dedServ)
            {
                return;
            }

            deathBurstPlayed = true;
            SoundEngine.PlaySound(
                new SoundStyle("tsorcRevamp/Sounds/HollowKnight/flamebearer_death_and_absorb") with { Volume = 0.68f, Pitch = 0.08f },
                NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 7f, 22, distanceFalloff: 900f);

            for (int i = 0; i < 160; i++)
            {
                int dustType = DustID.Ghost;
                Color tint = Color.White;
                if (i % 3 == 0)
                {
                    dustType = DustID.GoldFlame;
                    tint = new Color(255, 225, 95);
                }
                else if (i % 5 == 0)
                {
                    dustType = DustID.Electric;
                    tint = Color.Cyan;
                }

                Dust burst = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(18f, 26f),
                    dustType,
                    Main.rand.NextVector2Circular(10f, 10f),
                    25,
                    tint,
                    Main.rand.NextFloat(1.1f, 2.3f));
                burst.noGravity = i % 4 != 0;
            }
        }
    }
}
