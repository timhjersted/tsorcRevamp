using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic.Tomes;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Sounds.CenterOfTheUniverse;
using tsorcRevamp.Content.Projectiles.Enemy.Khaios;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>
    /// Phase 1 of the SHM Khaios Great Invader encounter.
    /// Agile emerald duelist wielding Wyrmkiller at close range with Owl Father-tier swing curves,
    /// Frozen Tear III geometric bank-shot ricochets off arena surfaces, and a below-66%-HP
    /// StarForger cosmic dragon companion that detonates in stardust on Phase 1 defeat.
    /// </summary>
    [AutoloadBossHead]
    public class Khaios : PuppetNPC
    {
        private const string EmeraldCutName = "Emerald Cut";
        private const string VortexReversalName = "Vortex Reversal";
        private const string AdvancingCrossName = "Advancing Cross";
        private const string BlinkStepFeintName = "Blink-Step Feint";
        private const string VenomousPursuitLeapName = "Venomous Pursuit Leap";
        private const string GaleDiveName = "Gale Dive";

        // Swing arc poses: -1.55 rad (~-89 deg) to +2.36 rad (+135 deg) = 224 deg envelope (~180 deg live sweep).
        private const float HighArcPose = -1.55f;
        private const float LowArcPose = 2.36f;

        // Weighted curve timing: 8t ease-in + 32t exponential decay (k=7) = 40t total, ~13.5t live (< 22t roll i-frames).
        private const int LightEaseInTicks = 7;
        private const int LightEaseOutTicks = 28;
        private const float LightDecayK = 7f;
        private const int HeavyEaseInTicks = 8;
        private const int HeavyEaseOutTicks = 32;
        private const float HeavyDecayK = 7f;
        private const int LeapSlamCrescentTicks = 10;

        // Dragon summon threshold & Frozen Tear III ricochet parameters.
        private const float DragonSummonHealthFraction = 0.66f;
        private const int DragonSummonCeremonyTicks = 48;
        private const float FrozenTearSpeed = 14.5f;
        private const int FrozenTearSalvoIntervalTicks = 24;
        private const int FrozenTearSightlineTicks = 22;

        private static readonly SoundStyle[] SwordSwingSounds =
        {
            new SoundStyle("tsorcRevamp/Sounds/DST/DarkSwordSwing1") { Volume = 0.50f, Pitch = 0.04f, PitchVariance = 0.08f },
            new SoundStyle("tsorcRevamp/Sounds/DST/DarkSwordSwing2") { Volume = 0.50f, Pitch = 0.04f, PitchVariance = 0.08f },
            new SoundStyle("tsorcRevamp/Sounds/DST/DarkSwordSwing3") { Volume = 0.50f, Pitch = 0.04f, PitchVariance = 0.08f },
        };

        private static readonly string DragonSoundBase = UsefulFunctions.RefactorableFilepath(typeof(CenterOfTheUniverseSound));

        private enum CustomMoveKind : byte
        {
            None = 0,
            SummonDragon = 1,
        }

        private enum FrozenTearPattern : byte
        {
            BankedSalvo = 0,
            PrismCrossfire = 1,
        }

        private CustomMoveKind activeCustomMove;
        private bool dragonSummoned;
        private int dragonNpcIndex = -1;
        private FrozenTearPattern activeFrozenTearPattern;
        private int frozenTearDurationTicks;
        private int frozenTearShotOrdinal;
        private Vector2 sightlineStart;
        private Vector2 sightlineEnd;
        private int sightlineTicksRemaining;
        private bool landingCrescentSpawned;

        // Shuffled grab bag for ranged-start combos so leap/gap-closer openers stay varied.
        private readonly AttackBag<int> rangedComboBag;

        public Khaios()
        {
            rangedComboBag = new AttackBag<int>(cards =>
            {
                for (int i = 0; i < KhaiosMeleeCombos.Length; i++)
                {
                    if (KhaiosMeleeCombos[i].RangedStartOnly)
                    {
                        cards.Add(i);
                    }
                }
            });
        }

        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(Khaios)) + "_Head_Boss";

        protected override string InvaderTitle => "Khaios";
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool IsGreatInvader => true;
        protected override int AbandonedDespawnTicks => 600;

        protected override int HeadArmorItemType => ItemID.SpookyHelmet;
        protected override int BodyArmorItemType => ItemID.VortexBreastplate;
        protected override int LegsArmorItemType => ItemID.JimsLeggings;
        protected override int HeadArmorDyeItemType => ItemID.GreenandBlackDye;
        protected override int BodyArmorDyeItemType => ItemID.GreenandBlackDye;
        protected override int LegsArmorDyeItemType => ItemID.GreenandBlackDye;

        protected override bool HasWings => true;
        protected override bool ShowWingsWhenGrounded => true;
        protected override int WingsAccessoryItemType => ItemID.JimsWings;
        protected override int WingsDyeItemType => ItemID.AcidDye;
        protected override EnemyFlightConfig FlightConfig => new EnemyFlightConfig
        {
            HoverAltitude = 195f,
            HoverSideOffset = 135f,
            HoverTopSpeed = 4.4f,
            TakeOffSpeed = 8f,
            DiveAcceleration = 0.5f,
            DiveTopSpeed = 11.5f,
            LandSpeed = 6f,
            MaxFlightTicks = 480,
            CooldownTicks = 210,
            TakeOffTicks = 32,
            HoverDwellTicks = 100,
            StrafeTicks = 65,
            DiveAttackTicks = 42,
            LandTicks = 75,
            WingFlapSpeed = 0.085f,
        };
        protected override int RandomTakeoffChance => 10;
        protected override float FlightHpEscalationFrac => 0.50f;
        protected override int AerialDiveCooldownTicks => 270;

        // Close-range melee weapon: Wyrmkiller sprite (40x40 diagonal, hilt grip at (8, 31) = (0.20, 0.78)).
        // Grip-to-tip distance = 43.8px * 1.65 scale = 72.3px vs hitbox 98 * 0.7 * 1.05 = 72.0px.
        protected override int MeleeWeaponItemType => ModContent.ItemType<Wyrmkiller>();
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => ModContent.ItemType<FreezeBolt3>();
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override Vector2 MeleeHandleNorm => new Vector2(0.20f, 0.78f);
        protected override float MeleeWeaponDrawScale => 1.65f;
        protected override float ComboReachBase => 98f;
        protected override float MeleeRange => 84f;
        protected override float MeleeEngageRange => 78f;
        protected override float ComboMaxStartRange => 420f;
        protected override int MeleeDamage => EnemyDamage.Projectile(260);
        protected override int RangedDamage => 0;
        protected override int MagicDamage => EnemyDamage.Projectile(220);

        // Magic: Frozen Tear III bank-shot ricochets (ground & mid-far range).
        protected override float MagicRange => 780f;
        protected override float MinMagicRange => 165f;
        protected override int MagicTelegraphTicks => 38;
        protected override int MagicRecoveryTicks => 36;
        protected override int MagicCooldownAfterUse => 200;
        protected override Color MagicTelegraphFlashColor => new Color(90, 220, 255);

        // User-edited Estus values preserved verbatim.
        protected override int EstusChargesMax => 6;
        protected override float EstusHealFraction => 0.10f;
        protected override int HealAnimationTicks => 160;
        protected override int HealCooldownTicks => 10 * 60;

        // Owl Father-tier swing polish opt-ins.
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
        protected override float ComboTelegraphAdvanceSpeedMult => 0.85f;
        protected override float ComboTelegraphAdvanceStopDistance => MeleeRange * 0.60f;
        protected override float LeapLandingStandoff => 24f;
        protected override float LeapAttackForwardSpeed => TopSpeed * 2.15f;
        protected override float LeapAttackTargetLeadTicks => 10f;
        protected override float LeapAttackAscentTrackingStrength => 0.10f;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 65;
        protected override int MeleeRecoveryTicks => 22;
        protected override int CasualStrollChance => 0;
        protected override bool CanStab => false;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => new Color(75, 255, 130);
        protected override float SlashVFXOpacity => 0.55f;

        protected override float TopSpeed => 3.25f;
        protected override float Acceleration => 0.12f;
        protected override int MeleeTelegraphTicks => 30;
        protected override bool SlowDownBeforeMelee => false;
        protected override Color MeleeTelegraphFlashColor => new Color(80, 255, 120);
        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 6.5f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 6.5f;
        protected override bool EvadesProjectiles => true;
        protected override int PreemptiveQuickStepChance => 35;
        protected override int QuickStepRecoveryTicks => 18;
        protected override bool CustomAttackCommitted => activeCustomMove == CustomMoveKind.SummonDragon;

        private static MeleeComboStep WeightedCut(
            ComboMotion motion,
            int telegraphTicks,
            bool light,
            int pauseAfter = 0,
            float damageMult = 1f,
            float forwardPushMult = 0.45f,
            float reachMult = 1.05f,
            float stepInDistance = 36f)
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

        private static readonly MeleeComboStep VortexReversalFollowUpStep = WeightedCut(
            ComboMotion.UnderhandArc,
            telegraphTicks: 0,
            light: false,
            damageMult: 1.15f,
            forwardPushMult: 0.62f,
            reachMult: 1.08f,
            stepInDistance: 44f);

        private static readonly MeleeCombo[] KhaiosMeleeCombos =
        {
            new MeleeCombo
            {
                Name = EmeraldCutName,
                BaseWeight = 75,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(80, 255, 130),
                CooldownAfterUse = 65,
                RecoveryTicks = 22,
                MoveBrake = 0.18f,
                Steps = new[]
                {
                    WeightedCut(ComboMotion.OverheadArc, telegraphTicks: 24, light: true, damageMult: 1.0f, forwardPushMult: 0.42f),
                },
            },
            new MeleeCombo
            {
                Name = VortexReversalName,
                BaseWeight = 95,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(60, 240, 145),
                CooldownAfterUse = 125,
                RecoveryTicks = 26,
                MoveBrake = 0.14f,
                Steps = new[]
                {
                    WeightedCut(ComboMotion.OverheadArc, telegraphTicks: 26, light: true, pauseAfter: 10, damageMult: 0.90f, forwardPushMult: 0.50f),
                    VortexReversalFollowUpStep,
                },
            },
            new MeleeCombo
            {
                Name = AdvancingCrossName,
                BaseWeight = 70,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(90, 255, 165),
                CooldownAfterUse = 155,
                RecoveryTicks = 28,
                MoveBrake = 0.08f,
                Steps = new[]
                {
                    WeightedCut(ComboMotion.UnderhandArc, telegraphTicks: 28, light: false, pauseAfter: 10, damageMult: 0.95f, forwardPushMult: 0.72f, stepInDistance: 50f),
                    WeightedCut(ComboMotion.OverheadArc, telegraphTicks: 0, light: false, damageMult: 1.22f, forwardPushMult: 0.80f, reachMult: 1.10f, stepInDistance: 54f),
                },
            },
            new MeleeCombo
            {
                Name = BlinkStepFeintName,
                BaseWeight = 58,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(110, 255, 200),
                CooldownAfterUse = 195,
                HeavyCommit = true,
                HyperArmor = true,
                RecoveryTicks = 30,
                MoveBrake = 0f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.BackstepRaise,
                        TelegraphTicks = 22,
                        AttackTicks = 36,
                        PostStepPause = 8,
                        DamageMult = 0f,
                        ReachMult = 1f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Smooth,
                        LeapHeightMult = 1f,
                        LeapForwardSpeedMult = 1f,
                    },
                    WeightedCut(ComboMotion.OverheadArc, telegraphTicks: 0, light: false, damageMult: 1.32f, forwardPushMult: 1.0f, reachMult: 1.12f, stepInDistance: 64f),
                },
            },
            new MeleeCombo
            {
                Name = VenomousPursuitLeapName,
                BaseWeight = 85,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(70, 255, 130),
                CooldownAfterUse = 145,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                RecoveryTicks = 24,
                MoveBrake = 0.06f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.LeapSlam,
                        TelegraphTicks = 32,
                        AttackTicks = 82,
                        DamageMult = 1.25f,
                        ReachMult = 1.15f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Whip,
                        HitWindowEnd = 1f,
                        LeapHeightMult = 0.65f,
                        LeapForwardSpeedMult = 1.72f,
                        LeapStrikeRange = 115f,
                    },
                },
            },
            new MeleeCombo
            {
                Name = GaleDiveName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Any,
                InitialFlashColor = new Color(100, 255, 170),
                CooldownAfterUse = 270,
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
                        TelegraphTicks = 34,
                        AttackTicks = 88,
                        DamageMult = 1.32f,
                        ReachMult = 1.18f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = 1f,
                        LeapStrikeRange = 135f,
                        LeapHeightMult = 1f,
                        LeapForwardSpeedMult = 1f,
                    },
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => KhaiosMeleeCombos;

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
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 80;
            globalNPC.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(NPC, TopSpeed * speedMult, Acceleration, 4, 650f);
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
            NPC.defense = 55;
            NPC.damage = 0;
            NPC.knockBackResist = 0.14f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 0f;
            NPC.boss = true;
            NPC.npcSlots = 10f;
            Music = MusicID.Boss5;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 75f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanDoubleJump = true;
            globalNPC.DoubleJumpPower = PuppetDoubleJumpPower;
            globalNPC.Agility = 0.72f;
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

        public override void AI()
        {
            if (sightlineTicksRemaining > 0)
            {
                sightlineTicksRemaining--;
            }

            // Below 66% HP: one-time ceremony to summon the Spirit of the Universe (StarForger) dragon.
            bool readyForDragonCeremony = !dragonSummoned
                && NPC.lifeMax > 0
                && NPC.life <= (int)(NPC.lifeMax * DragonSummonHealthFraction)
                && (Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance);

            if (readyForDragonCeremony && Main.netMode != NetmodeID.MultiplayerClient)
            {
                dragonSummoned = true;
                activeCustomMove = CustomMoveKind.SummonDragon;
                StartCustomAttack(DragonSummonCeremonyTicks, poseWeaponItemType: -1, swingPose: false);
                NPC.netUpdate = true;
            }

            base.AI();
            UpdateDebugAttackLabel();

            if (!Main.dedServ && Main.rand.NextBool(6))
            {
                Dust aura = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(16f, 22f),
                    DustID.TerraBlade,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.1f, -0.3f)),
                    100,
                    new Color(80, 255, 130),
                    Main.rand.NextFloat(0.65f, 1.05f));
                aura.noGravity = true;
            }
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
                    : EmeraldCutName;
                return;
            }

            if (Phase == AttackPhase.MagicTelegraph
                || Phase == AttackPhase.MagicAttack
                || Phase == AttackPhase.MagicRecovery)
            {
                if (activeFrozenTearPattern == FrozenTearPattern.PrismCrossfire)
                {
                    DebugAttackLabel = "Frozen Tear: Prism Crossfire";
                }
                else
                {
                    DebugAttackLabel = "Frozen Tear: Banked Salvo";
                }

                return;
            }

            if (Phase == AttackPhase.Custom && activeCustomMove == CustomMoveKind.SummonDragon)
            {
                DebugAttackLabel = "Summon Spirit of the Universe";
                return;
            }

            if (Phase == AttackPhase.FleeToHeal || Phase == AttackPhase.Healing)
            {
                DebugAttackLabel = "Estus Flask";
                return;
            }

            DebugAttackLabel = null;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)activeCustomMove);
            writer.Write(dragonSummoned);
            writer.Write(dragonNpcIndex);
            writer.Write((byte)activeFrozenTearPattern);
            writer.Write(frozenTearDurationTicks);
            writer.Write(frozenTearShotOrdinal);
            writer.WriteVector2(sightlineStart);
            writer.WriteVector2(sightlineEnd);
            writer.Write(sightlineTicksRemaining);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            activeCustomMove = (CustomMoveKind)reader.ReadByte();
            dragonSummoned = reader.ReadBoolean();
            dragonNpcIndex = reader.ReadInt32();
            activeFrozenTearPattern = (FrozenTearPattern)reader.ReadByte();
            frozenTearDurationTicks = reader.ReadInt32();
            frozenTearShotOrdinal = reader.ReadInt32();
            sightlineStart = reader.ReadVector2();
            sightlineEnd = reader.ReadVector2();
            sightlineTicksRemaining = reader.ReadInt32();

            if (frozenTearDurationTicks > 0 && Phase == AttackPhase.MagicAttack)
            {
                _magicAttackTicksOverride = frozenTearDurationTicks;
            }
        }

        protected override void DoCustomAttack()
        {
            if (activeCustomMove != CustomMoveKind.SummonDragon)
            {
                return;
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(
                    new SoundStyle(DragonSoundBase + "_DragonCast") with { Volume = 0.65f },
                    NPC.Center);
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.48f, Pitch = -0.08f },
                    NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 4f, 18, distanceFalloff: 700f);
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (activeCustomMove != CustomMoveKind.SummonDragon)
            {
                return;
            }

            Vector2 dragonSpawn = NPC.Center + new Vector2(-NPC.direction * 90f, -150f);

            if (!Main.dedServ)
            {
                // Rising cosmic ritual stream linking Khaios to the overhead dragon summon rift.
                for (int i = 0; i < 4; i++)
                {
                    int dustType = DustID.PurpleTorch;
                    Color tint = Color.MediumPurple;
                    if (i == 0)
                    {
                        dustType = DustID.GoldFlame;
                        tint = new Color(255, 220, 95);
                    }
                    else if (i == 2)
                    {
                        dustType = DustID.BlueTorch;
                        tint = Color.DeepSkyBlue;
                    }
                    else if (i == 3)
                    {
                        dustType = DustID.AncientLight;
                        tint = Color.White;
                    }

                    Vector2 offset = Main.rand.NextVector2CircularEdge(72f, 72f);
                    Vector2 streamVel = -offset * 0.085f + new Vector2(0f, -1.8f);
                    Dust ritual = Dust.NewDustPerfect(
                        NPC.Center + offset,
                        dustType,
                        streamVel,
                        25,
                        tint,
                        Main.rand.NextFloat(0.95f, 1.6f));
                    ritual.noGravity = true;
                }

                if (ticksRemaining == 16)
                {
                    // Synchronized summon flare radiating off Khaios as the dragon bursts in above.
                    for (int i = 0; i < 42; i++)
                    {
                        int burstType = DustID.ShadowbeamStaff;
                        if (i % 3 == 0)
                        {
                            burstType = DustID.GoldFlame;
                        }
                        else if (i % 3 == 1)
                        {
                            burstType = DustID.IceTorch;
                        }

                        Vector2 burstVel = new Vector2(Main.rand.NextFloat(-7.5f, 7.5f), Main.rand.NextFloat(-8.5f, -1.5f));
                        Dust flare = Dust.NewDustPerfect(
                            NPC.Center + Main.rand.NextVector2Circular(24f, 34f),
                            burstType,
                            burstVel,
                            20,
                            Color.White,
                            Main.rand.NextFloat(1.0f, 1.75f));
                        flare.noGravity = i % 2 == 0;
                    }
                }
            }

            if (ticksRemaining == 16 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                dragonNpcIndex = NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)dragonSpawn.X,
                    (int)dragonSpawn.Y,
                    ModContent.NPCType<KhaiosStarForger>(),
                    ai0: NPC.whoAmI);
                NPC.netUpdate = true;
            }

            if (ticksRemaining <= 1)
            {
                activeCustomMove = CustomMoveKind.None;
            }
        }

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
            if (step.Motion == ComboMotion.BackstepRaise && elapsed == 2 && !Main.dedServ)
            {
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/Runeterra/Shunpo1") with { Volume = 0.42f, Pitch = -0.05f },
                    NPC.Center);
            }

            if (step.Motion != ComboMotion.LeapSlam && step.Motion != ComboMotion.BackstepRaise)
            {
                if (elapsed == step.EaseInTicks)
                {
                    PlaySwordSwingSound();
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && elapsed == 0)
                {
                    float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
                    SpawnEmeraldSwordCrescent(step, Math.Max(8, (int)(total * step.HitWindowEnd) + 4), bladeReach);
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
            SpawnEmeraldSwordCrescent(step, remainingTicks, bladeReach);
        }

        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(
                new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time") with { Volume = 0.44f, Pitch = 0.14f },
                NPC.Bottom);
            UsefulFunctions.ScreenShake(NPC.Bottom, 3.2f, 10, distanceFalloff: 520f);

            for (int i = 0; i < 28; i++)
            {
                Dust puff = Dust.NewDustPerfect(
                    NPC.Bottom + new Vector2(Main.rand.NextFloat(-16f, 16f), -3f),
                    i % 3 == 0 ? DustID.TerraBlade : DustID.GreenTorch,
                    new Vector2(Main.rand.NextFloat(-4.5f, 4.5f), Main.rand.NextFloat(-4.2f, -0.8f)),
                    60,
                    new Color(80, 255, 130),
                    Main.rand.NextFloat(0.95f, 1.55f));
                puff.noGravity = true;
            }
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == VenomousPursuitLeapName && distance > ReliableJumpStartRange(combo.Steps[0]))
            {
                return false;
            }

            return true;
        }

        protected override int ReactiveComboIndex(float distance, ComboRangeBand band, int[] ready)
        {
            bool choosingRangedStart = false;
            for (int i = 0; i < KhaiosMeleeCombos.Length && i < ready.Length; i++)
            {
                if (ready[i] > 0 && KhaiosMeleeCombos[i].RangedStartOnly)
                {
                    choosingRangedStart = true;
                    break;
                }
            }

            if (choosingRangedStart && rangedComboBag.TryDraw(index => index < ready.Length && ready[index] > 0, out int picked))
            {
                return picked;
            }

            return base.ReactiveComboIndex(distance, band, ready);
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == VortexReversalName && nextStepIndex == 1)
            {
                return previousStepHit || WouldForwardPushedSwingConnect(VortexReversalFollowUpStep, target);
            }

            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        private bool WouldForwardPushedSwingConnect(MeleeComboStep step, Player target)
        {
            float reach = ComboReachBase * 0.7f * step.ReachMult;
            float pushDistance = step.ForwardPushMult > 0f
                ? ComboForwardPushTopSpeed * step.ForwardPushMult * step.AttackTicks
                : 0f;
            return NPC.Distance(target.Center) <= reach + pushDistance;
        }

        // ── Frozen Tear III (FreezeBolt3) Geometric Bank-Shot Magic ───────────────────

        protected override void DoMagicAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool belowSixtyPercent = NPC.lifeMax > 0 && NPC.life < (int)(NPC.lifeMax * 0.60f);
            bool chooseCrossfire = belowSixtyPercent && Main.rand.NextBool(3);

            activeFrozenTearPattern = chooseCrossfire
                ? FrozenTearPattern.PrismCrossfire
                : FrozenTearPattern.BankedSalvo;

            int shotCount = belowSixtyPercent ? 4 : 3;
            frozenTearDurationTicks = activeFrozenTearPattern == FrozenTearPattern.PrismCrossfire
                ? 44
                : shotCount * FrozenTearSalvoIntervalTicks + 12;
            frozenTearShotOrdinal = 0;
            _magicAttackTicksOverride = frozenTearDurationTicks;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/DarkSouls/harmonic-magic") with { Volume = 0.40f, Pitch = 0.24f },
                    NPC.Center);
            }

            NPC.netUpdate = true;
        }

        protected override void DoMagicTick(int ticksRemaining)
        {
            int elapsed = frozenTearDurationTicks - ticksRemaining;
            Vector2 castOrigin = PuppetHandPosition != Vector2.Zero
                ? PuppetHandPosition
                : NPC.Center + new Vector2(NPC.direction * 14f, -6f);

            if (!Main.dedServ && elapsed % 3 == 0)
            {
                Dust iceMote = Dust.NewDustPerfect(
                    castOrigin + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.IceTorch,
                    Main.rand.NextVector2Circular(1.4f, 1.4f),
                    60,
                    Color.Cyan,
                    Main.rand.NextFloat(0.9f, 1.4f));
                iceMote.noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                return;
            }

            if (activeFrozenTearPattern == FrozenTearPattern.PrismCrossfire)
            {
                if (elapsed == 10)
                {
                    FirePrismCrossfire(castOrigin, target);
                }

                return;
            }

            if (elapsed % FrozenTearSalvoIntervalTicks == 0)
            {
                FireSingleBankedFrozenTear(castOrigin, target, frozenTearShotOrdinal);
                frozenTearShotOrdinal++;
            }
        }

        private void FireSingleBankedFrozenTear(Vector2 castOrigin, Player target, int patternOrdinal)
        {
            Vector2 predictedTarget = target.Center + target.velocity * 16f;
            Vector2 launchDir = SolveBankShotDirection(castOrigin, predictedTarget, patternOrdinal, out Vector2 firstBouncePoint);

            sightlineStart = castOrigin;
            sightlineEnd = firstBouncePoint;
            sightlineTicksRemaining = FrozenTearSightlineTicks;
            NPC.netUpdate = true;

            SpawnFrozenTearBolt(castOrigin, launchDir * FrozenTearSpeed, bounceBudget: 4);
        }

        private void FirePrismCrossfire(Vector2 castOrigin, Player target)
        {
            Vector2 predictedTarget = target.Center + target.velocity * 18f;
            Vector2 firstBouncePreview = predictedTarget;

            for (int i = 0; i < 3; i++)
            {
                Vector2 launchDir = SolveBankShotDirection(castOrigin, predictedTarget, i, out Vector2 bouncePoint);
                if (i == 0)
                {
                    firstBouncePreview = bouncePoint;
                }

                SpawnFrozenTearBolt(castOrigin, launchDir * FrozenTearSpeed, bounceBudget: 4);
            }

            sightlineStart = castOrigin;
            sightlineEnd = firstBouncePreview;
            sightlineTicksRemaining = FrozenTearSightlineTicks + 6;
            NPC.netUpdate = true;
        }

        private void SpawnFrozenTearBolt(Vector2 origin, Vector2 velocity, int bounceBudget)
        {
            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                origin,
                velocity,
                ModContent.ProjectileType<KhaiosFrozenTearBolt>(),
                MagicDamage,
                3f,
                Main.myPlayer,
                bounceBudget);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.42f, Pitch = 0.14f, PitchVariance = 0.08f }, origin);
            }
        }

        /// <summary>
        /// Probes the arena's solid floor, ceiling, and side walls around the fight and evaluates
        /// mirror-image reflection targets for 2-bounce and 3-bounce bank shots (floor-&gt;ceiling,
        /// floor-&gt;wall, wall-&gt;floor, floor-&gt;ceiling-&gt;floor, etc.). Falls back to a clean 1-bounce
        /// floor skip when fighting under open sky.
        /// </summary>
        private Vector2 SolveBankShotDirection(Vector2 origin, Vector2 targetPos, int preferredVariant, out Vector2 firstBouncePoint)
        {
            float maxVerticalScan = 34f * 16f;
            float maxHorizontalScan = 42f * 16f;

            bool hasFloor = TryFindSolidBoundary(targetPos, Vector2.UnitY, maxVerticalScan, out float floorY);
            bool hasCeiling = TryFindSolidBoundary(targetPos, -Vector2.UnitY, maxVerticalScan, out float ceilingY);
            bool hasLeftWall = TryFindSolidBoundary(targetPos, -Vector2.UnitX, maxHorizontalScan, out float leftWallX);
            bool hasRightWall = TryFindSolidBoundary(targetPos, Vector2.UnitX, maxHorizontalScan, out float rightWallX);

            List<Vector2> candidateMirrors = new List<Vector2>(8);

            // 2-bounce combinations (preferred for dramatic arena ricochets).
            if (hasFloor && hasCeiling)
            {
                float mirrorFloorY = 2f * floorY - targetPos.Y;
                float mirrorFloorThenCeilY = 2f * ceilingY - mirrorFloorY;
                candidateMirrors.Add(new Vector2(targetPos.X, mirrorFloorThenCeilY));

                float mirrorCeilY = 2f * ceilingY - targetPos.Y;
                float mirrorCeilThenFloorY = 2f * floorY - mirrorCeilY;
                candidateMirrors.Add(new Vector2(targetPos.X, mirrorCeilThenFloorY));

                // 3-bounce: floor -> ceiling -> floor.
                float mirrorThreeBounceY = 2f * floorY - mirrorFloorThenCeilY;
                candidateMirrors.Add(new Vector2(targetPos.X, mirrorThreeBounceY));
            }

            if (hasFloor && hasRightWall)
            {
                candidateMirrors.Add(new Vector2(2f * rightWallX - targetPos.X, 2f * floorY - targetPos.Y));
            }

            if (hasFloor && hasLeftWall)
            {
                candidateMirrors.Add(new Vector2(2f * leftWallX - targetPos.X, 2f * floorY - targetPos.Y));
            }

            if (hasCeiling && hasRightWall)
            {
                candidateMirrors.Add(new Vector2(2f * rightWallX - targetPos.X, 2f * ceilingY - targetPos.Y));
            }

            if (hasCeiling && hasLeftWall)
            {
                candidateMirrors.Add(new Vector2(2f * leftWallX - targetPos.X, 2f * ceilingY - targetPos.Y));
            }

            if (hasLeftWall && hasRightWall)
            {
                float rightThenLeftX = 2f * leftWallX - (2f * rightWallX - targetPos.X);
                candidateMirrors.Add(new Vector2(rightThenLeftX, targetPos.Y));
            }

            // 1-bounce floor skip fallback (always available on ground arenas even under open sky).
            if (hasFloor)
            {
                candidateMirrors.Add(new Vector2(targetPos.X, 2f * floorY - targetPos.Y));
            }

            int count = candidateMirrors.Count;
            if (count > 0)
            {
                for (int offset = 0; offset < count; offset++)
                {
                    int index = (preferredVariant + offset) % count;
                    Vector2 virtualTarget = candidateMirrors[index];
                    Vector2 candidateDir = (virtualTarget - origin).SafeNormalize(Vector2.UnitX);

                    if (TryTraceFirstWallHit(origin, candidateDir, 760f, out Vector2 hitPos))
                    {
                        float firstLegDist = Vector2.Distance(origin, hitPos);
                        if (firstLegDist >= 48f)
                        {
                            firstBouncePoint = hitPos;
                            return candidateDir;
                        }
                    }
                }
            }

            // Ultimate open-air fallback: angled downward skip or direct aimed bolt.
            Vector2 fallbackDir = (targetPos - origin).SafeNormalize(new Vector2(NPC.direction, 0f));
            firstBouncePoint = origin + fallbackDir * 240f;
            return fallbackDir;
        }

        private static bool TryFindSolidBoundary(Vector2 origin, Vector2 stepDir, float maxDistance, out float boundaryCoord)
        {
            const float stepPixels = 16f;
            int maxSteps = (int)(maxDistance / stepPixels);

            for (int step = 2; step <= maxSteps; step++)
            {
                Vector2 sample = origin + stepDir * (step * stepPixels);
                int tileX = (int)(sample.X / 16f);
                int tileY = (int)(sample.Y / 16f);

                if (tileX < 2 || tileX >= Main.maxTilesX - 2 || tileY < 2 || tileY >= Main.maxTilesY - 2)
                {
                    break;
                }

                Tile tile = Framing.GetTileSafely(tileX, tileY);
                if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                {
                    if (stepDir.X > 0f)
                    {
                        boundaryCoord = tileX * 16f;
                    }
                    else if (stepDir.X < 0f)
                    {
                        boundaryCoord = (tileX + 1) * 16f;
                    }
                    else if (stepDir.Y > 0f)
                    {
                        boundaryCoord = tileY * 16f;
                    }
                    else
                    {
                        boundaryCoord = (tileY + 1) * 16f;
                    }

                    return true;
                }
            }

            boundaryCoord = 0f;
            return false;
        }

        private static bool TryTraceFirstWallHit(Vector2 origin, Vector2 dir, float maxDistance, out Vector2 hitPos)
        {
            const float stepPixels = 12f;
            int maxSteps = (int)(maxDistance / stepPixels);

            for (int step = 2; step <= maxSteps; step++)
            {
                Vector2 sample = origin + dir * (step * stepPixels);
                int tileX = (int)(sample.X / 16f);
                int tileY = (int)(sample.Y / 16f);

                if (tileX < 2 || tileX >= Main.maxTilesX - 2 || tileY < 2 || tileY >= Main.maxTilesY - 2)
                {
                    break;
                }

                Tile tile = Framing.GetTileSafely(tileX, tileY);
                if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                {
                    hitPos = sample - dir * stepPixels;
                    return true;
                }
            }

            hitPos = origin + dir * 240f;
            return false;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);

            // Brief icy first-segment sightline so the player reads the ricochet bank surface cleanly.
            if (sightlineTicksRemaining > 0)
            {
                float alpha = MathHelper.Clamp(sightlineTicksRemaining / (float)FrozenTearSightlineTicks, 0f, 1f);
                Color lineStartColor = new Color(110, 235, 255) * (0.72f * alpha);
                Color lineEndColor = new Color(35, 110, 255) * (0.18f * alpha);
                Utils.DrawLine(spriteBatch, sightlineStart, sightlineEnd, lineStartColor, lineEndColor, 2.2f);
            }
        }

        private void SpawnEmeraldSwordCrescent(MeleeComboStep step, int duration, float bladeReach)
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
                Radius = Math.Max(52f, bladeReach * 1.05f),
                Opacity = 0.56f,
                FadeInFraction = Math.Min(0.15f, 2f / Math.Max(1, duration)),
                FadeOutFraction = 0.28f,
                AfterimageLag = MathHelper.PiOver4,
                AfterimageOpacity = 0.42f,
                BodyOpacity = 0.72f,
                CoreOpacity = 0.28f,
                DrawTipSparkle = false,
                TintWithWorldLighting = true,
                DarkColor = new Color(8, 52, 24),
                BodyColor = new Color(40, 215, 105),
                CoreColor = new Color(185, 255, 210),
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

        public override bool CheckDead()
        {
            // Phase one is a real death, but not the completion of the encounter. Clear the flags
            // consumed by GlobalNPC.OnKill so it grants neither progression nor automatic soul loot.
            NPC.boss = false;
            NPC.value = 0f;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().SuppressGlobalOnKillDrops = true;
            DetonateActiveDragon();
            return true;
        }

        public override void OnKill()
        {
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().IsTeleportIllusion)
            {
                return;
            }

            DetonateActiveDragon();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(
                    NPC.GetSource_Death(),
                    (int)NPC.Center.X,
                    (int)NPC.Center.Y,
                    ModContent.NPCType<KhaiosTransitionOrb>());
            }
        }

        private void DetonateActiveDragon()
        {
            if (dragonNpcIndex >= 0 && dragonNpcIndex < Main.maxNPCs)
            {
                NPC dragonNpc = Main.npc[dragonNpcIndex];
                if (dragonNpc.active && dragonNpc.ModNPC is KhaiosStarForger dragon)
                {
                    dragon.TriggerPhaseOneDeathExplosion();
                }
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);

            // Gate evasive reaction to neutral movement so hitting Khaios during his recovery
            // or punish windows never bails him out unfairly.
            if (CanTriggerEvasiveReaction())
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);

            if (CanTriggerEvasiveReaction())
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
            }
        }

        private bool CanTriggerEvasiveReaction()
        {
            return Phase == AttackPhase.Idle
                || Phase == AttackPhase.CasualStroll
                || Phase == AttackPhase.ClosingDistance;
        }

        // Melee + Magic phase (RangedWeaponItemType = -1).
        protected override void DoRangedAttack() { }
    }
}
