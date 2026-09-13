using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    /// <summary>
    /// Super Hardmode blood elite rebuilt on the puppet foundation. Its sword and whip are
    /// deliberately dodgeable commitments; its Tendon Bow plays fixed, learnable shot phrases.
    /// </summary>
    public class DarkBloodKnight : PuppetNPC
    {
        private const string ScarletHewName = "Scarlet Hew";
        private const string ButchersReversalName = "Butcher's Reversal";
        private const string BloodMeasureName = "Blood Measure";
        private const string DarkHarvestLashName = "Dark Harvest Lash";

        private const int BleedingDuration = 30 * 60;
        private const int DarkInfernoDuration = 5 * 60;
        // BloodSword.png is 50x58px and its configured grip is (0.10, 0.85), at 1.05 draw scale.
        // Keep the decorative crescent inside this blade-length envelope plus a small collision allowance.
        private const float BloodSwordVisualReach = 66.7f;
        private const float BloodSwordArcVisualAllowance = 1.12f;

        // The executor spends five ticks in RangedAttack after every release, so these stored
        // pauses are five below the audible release-to-release gaps:
        //   3 shots: 34,34 | 4: 7,7,36 | 5: 7,30,7,36
        //   9: 7,7,34,18,7,36,7,7 | 12: 7,30,7,30,7,30,7,30,7,30,7.
        private static readonly int[][] BowPhrases =
        {
            new[] { 29, 29 },
            new[] { 2, 2, 31 },
            new[] { 2, 25, 2, 31 },
            new[] { 2, 2, 29, 13, 2, 31, 2, 2 },
            new[] { 2, 25, 2, 25, 2, 25, 2, 25, 2, 25, 2 },
        };

        private static readonly int[][] PhraseAimSnapshots =
        {
            new[] { 0, 1, 2 },
            new[] { 0, 3 },
            new[] { 0, 2, 4 },
            new[] { 0, 3, 4, 6 },
            new[] { 0, 2, 4, 6, 8, 10 },
        };

        private static readonly int[] BowTelegraphExtras = { 0, 4, 8, 14, 20 };
        private static readonly Color[] BowFlashColors =
        {
            new Color(205, 75, 82),
            new Color(220, 65, 72),
            new Color(226, 54, 65),
            new Color(160, 43, 67),
            new Color(115, 34, 58),
        };

        private static readonly int[] RainOffsets = { 0, -96, 96, -48, 48, -144, 144 };

        private bool _activeComboUsesWhip;
        private Vector2 _lockedWhipAim;
        private bool _bloodWhipProjectileSpawned;
        private int _bowShotIndex;
        private Vector2 _bowAimVelocity;
        private int _leadMissStreak;

        protected override string InvaderTitle => "Demonic Blood Knight";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;

        protected override int HeadArmorItemType => ModContent.ItemType<DarkKnightHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<DarkKnightArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<DarkKnightGreaves>();
        protected override int HeadArmorDyeItemType => ItemID.BloodbathDye;
        protected override int BodyArmorDyeItemType => ItemID.BloodbathDye;
        protected override int LegsArmorDyeItemType => ItemID.BloodbathDye;

        protected override int MeleeWeaponItemType => _activeComboUsesWhip ? ItemID.ScytheWhip : ItemID.BloodButcherer;
        protected override int RangedWeaponItemType => ItemID.TendonBow;
        protected override int SecondaryRangedWeaponItemType => ItemID.TendonBow;

        // Terraria doubles hostile projectile damage on player hit. These are pre-hit values;
        // keeping them near Dark Knight's Dark Wave avoids treating puppet attacks like contact damage.
        protected override int MeleeDamage => (int)(34f * tsorcRevampWorld.SubtleSHMScale);
        protected override int RangedDamage => (int)(23f * tsorcRevampWorld.SubtleSHMScale);
        protected override int SecondaryRangedDamage => (int)(25f * tsorcRevampWorld.SubtleSHMScale);
        protected override int EstusChargesMax => 0;

        protected override float TopSpeed => 2.7f;
        protected override float Acceleration => 0.095f;
        protected override float MeleeRange => 88f;
        protected override float ComboReachBase => 90f;
        protected override float MeleeEngageRange => 104f;
        protected override float ComboMaxStartRange => 330f;
        protected override float ClosingDistanceSpeedMult => 1.5f;
        protected override int ClosingDistanceMaxTicks => 100;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.62f;
        protected override float ComboTelegraphAdvanceStopDistance => 66f;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override string MeleeDrawTexturePath => _activeComboUsesWhip
            ? null
            : "tsorcRevamp/Projectiles/Enemy/Weapons/BloodSword";
        protected override string GetHeldRangedDrawTexturePath(int itemType)
            => itemType == ItemID.TendonBow
                ? "tsorcRevamp/Projectiles/Enemy/Weapons/BloodBow"
                : base.GetHeldRangedDrawTexturePath(itemType);
        protected override Vector2 GetHeldRangedGripNorm(int itemType)
            => itemType == ItemID.TendonBow
                ? new Vector2(15f / 22f, 20f / 40f)
                : base.GetHeldRangedGripNorm(itemType);
        protected override Vector2 MeleeHandleNorm => new Vector2(0.1f, 0.85f);
        protected override float MeleeWeaponDrawScale => 1.05f;
        protected override float MeleeBladeWidth => 28f;
        protected override bool HideHeldMeleeSprite => _activeComboUsesWhip;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        // VanillaSwordArc owns the BloodSword cuts; Dark Harvest has its own projectile VFX.
        protected override bool HasSlashVFX => false;
        protected override Color SlashVFXColor => new Color(185, 35, 52);
        protected override float SlashVFXOpacity => 0.5f;
        protected override float SlashVFXScale => 0.58f;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 100;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 8;
        protected override int MeleeComboInterStepLingerTicks => 5;
        protected override int MeleeRecoveryLingerTicks => 8;

        protected override float RangedRange => 900f;
        protected override float MinRangedRange => 220f;
        protected override int RangedTelegraphTicks => 36;
        protected override int RangedAttackTicks => 5;
        protected override int RangedRecoveryTicks => 42;
        protected override int RangedCooldownAfterUse => 135;
        protected override int StandingRangedChance => 72;
        protected override RangedStyle RangedAnimStyle => RangedStyle.Bow;
        protected override Color RangedTelegraphFlashColor => new Color(210, 55, 65);
        protected override int[][] PrimaryRangedBurstPatterns => BowPhrases;
        protected override int[] PrimaryRangedBurstTelegraphExtras => BowTelegraphExtras;
        protected override Color[] PrimaryRangedBurstFlashColors => BowFlashColors;
        protected override int[] PrimaryRangedBurstChances
        {
            get
            {
                float health = NPC.life / (float)Math.Max(1, NPC.lifeMax);
                if (health > 0.75f)
                    return new[] { 100, 75, 0, 0, 0 };
                if (health > 0.5f)
                    return new[] { 80, 70, 55, 0, 0 };
                if (health > 0.25f)
                    return new[] { 55, 60, 70, 50, 0 };
                return new[] { 35, 45, 65, 70, 35 };
            }
        }

        protected override float SecondaryRangedRange => 950f;
        protected override float SecondaryRangedMinRange => 280f;
        protected override int SecondaryRangedTelegraphTicks => 60;
        protected override int SecondaryRangedAttackTicks => 6;
        protected override int SecondaryRangedRecoveryTicks => 56;
        protected override int SecondaryRangedCooldownAfterUse => 360;
        protected override int SecondaryRangedChance => 22;
        protected override int SecondaryStandingRangedChance => 88;
        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Bow;
        protected override Color SecondaryRangedFlashColor => new Color(120, 22, 42);
        protected override bool SecondaryRangedAvailable => NPC.life <= NPC.lifeMax * 0.75f;

        private bool IsBlackfirePhase => NPC.life <= NPC.lifeMax * 0.5f;
        public Vector2 BloodWhipAnchor => PuppetHandPosition;
        public Vector2 BloodWhipAim => _lockedWhipAim;
        public bool IsBloodWhipActive => _activeComboUsesWhip
            && (Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause
                || Phase == AttackPhase.MeleeComboRecovery);

        private static MeleeComboStep WeightedSwordStep(
            ComboMotion motion,
            int telegraphTicks,
            int easeInTicks,
            int easeOutTicks,
            float easeOutDecay,
            int pauseAfter = 0,
            float damageMult = 1f,
            float reachMult = 1.16f,
            float forwardPushMult = 0f)
        {
            int attackTicks = easeInTicks + easeOutTicks;
            float tailTicks = easeOutTicks * MathF.Log(1f / 0.3f) / easeOutDecay;
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
                HitWindowEnd = MathHelper.Clamp((easeInTicks + tailTicks) / attackTicks, 0f, 1f),
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };
        }

        // Timing sheet (60 ticks = one second):
        // Scarlet Hew       28 tell | 7 in / 26 out k7 | 225-degree envelope | 28 recovery
        // Butcher Reversal  30 tell | 8 in / 28 out k6 | 220-degree envelope | 30 recovery
        // Blood Measure     32 tell | 8/24 k6, 11 pause, 8/32 k6 | 225 each | 38 recovery
        // Dark Harvest      46 tell (last 10 aim-locked) | 30 lash, live 8-19 | 42 recovery
        private static readonly MeleeCombo[] BloodKnightCombos =
        {
            new MeleeCombo
            {
                Name = ScarletHewName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(220, 70, 75),
                CooldownAfterUse = 68,
                RecoveryTicks = 28,
                MoveBrake = 0.24f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.OverheadArc, 28, 7, 26, 7f,
                        damageMult: 1f, reachMult: 1.17f, forwardPushMult: 0.18f),
                },
            },
            new MeleeCombo
            {
                Name = ButchersReversalName,
                BaseWeight = 92,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(185, 45, 62),
                CooldownAfterUse = 74,
                RecoveryTicks = 30,
                MoveBrake = 0.22f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.UnderhandArc, 30, 8, 28, 6f,
                        damageMult: 0.96f, reachMult: 1.16f, forwardPushMult: 0.16f),
                },
            },
            new MeleeCombo
            {
                Name = BloodMeasureName,
                BaseWeight = 78,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(235, 105, 110),
                CooldownAfterUse = 128,
                RecoveryTicks = 38,
                MoveBrake = 0.24f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.OverheadArc, 32, 8, 24, 6f,
                        pauseAfter: 11, damageMult: 0.82f, reachMult: 1.16f,
                        forwardPushMult: 0.14f),
                    WeightedSwordStep(ComboMotion.UnderhandArc, 0, 8, 32, 6f,
                        damageMult: 1.06f, reachMult: 1.18f, forwardPushMult: 0.2f),
                },
            },
            new MeleeCombo
            {
                Name = DarkHarvestLashName,
                BaseWeight = 94,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(110, 24, 55),
                CooldownAfterUse = 150,
                RecoveryTicks = 42,
                RangedStartOnly = true,
                MoveBrake = 0.34f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.HorizontalSweep,
                        TelegraphTicks = 46,
                        AttackTicks = 30,
                        DamageMult = 0f,
                        ReachMult = 3.55f,
                        ForwardPushMult = 0f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Whip,
                        HitWindowEnd = 0f,
                        LeapHeightMult = 1f,
                        LeapForwardSpeedMult = 1f,
                    },
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => BloodKnightCombos;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Bleeding] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 3f;
            NPC.width = 20;
            NPC.height = 48;
            NPC.timeLeft = 750;
            NPC.damage = 0;
            NPC.defense = 67;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            NPC.lifeMax = 3200;
            NPC.knockBackResist = 0.3f;
            NPC.value = 8000;
            NPC.aiStyle = -1;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DarkBloodKnightBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavSearchRadius = 40;
            globalNPC.RemembersLastKnownPos = true;
            globalNPC.CanUseRopes = true;
        }

        protected override void RunMovementAI(float speedMult)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 40;
            globalNPC.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 4,
                attackRange: MeleeEngageRange);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;
            bool frozenOcean = spawnInfo.SpawnTileX > Main.maxTilesX - 800;
            bool ocean = spawnInfo.SpawnTileX < 800 || frozenOcean;

            if (NPC.AnyNPCs(Type))
                return 0f;

            float chance = 0f;
            if (tsorcRevampWorld.SuperHardMode
                && !player.ZoneDungeon
                && (player.ZoneUnderworldHeight || player.ZoneCrimson)
                && !ocean)
            {
                chance = player.ZoneOverworldHeight ? 0.25f : 0.3f;
            }

            if (!Main.dayTime)
                chance *= 2f;
            if (Main.bloodMoon)
                chance *= 2f;
            return chance;
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == DarkHarvestLashName)
                return distance >= 112f && distance <= 340f;
            return true;
        }

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            _activeComboUsesWhip = combo.Name == DarkHarvestLashName;
            _bloodWhipProjectileSpawned = false;

            if (_activeComboUsesWhip && NPC.HasValidTarget)
                _lockedWhipAim = CalculateWhipAim(Main.player[NPC.target]);
        }

        protected override bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == BloodMeasureName && nextStepIndex == 1)
            {
                bool stillInFront = Math.Sign(target.Center.X - NPC.Center.X) == NPC.direction;
                bool inReturnCut = Vector2.Distance(NPC.Center, target.Center) <= ComboReachBase * 1.18f + 36f;
                return previousStepHit || (stillInFront && inReturnCut);
            }
            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        protected override void ModifyMeleeArcEndpoints(
            ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (ActiveMeleeComboName == ScarletHewName && motion == ComboMotion.OverheadArc)
            {
                startRotation = -1.55f;
                endRotation = 2.38f;
            }
            else if (ActiveMeleeComboName == ButchersReversalName && motion == ComboMotion.UnderhandArc)
            {
                startRotation = 2.30f;
                endRotation = -1.54f;
            }
            else if (ActiveMeleeComboName == BloodMeasureName)
            {
                if (motion == ComboMotion.OverheadArc)
                {
                    startRotation = -1.55f;
                    endRotation = 2.38f;
                }
                else if (motion == ComboMotion.UnderhandArc)
                {
                    startRotation = 2.38f;
                    endRotation = -1.55f;
                }
            }
            else if (ActiveMeleeComboName == DarkHarvestLashName && motion == ComboMotion.HorizontalSweep)
            {
                startRotation = -1.35f;
                endRotation = 1.78f;
            }
        }

        protected override void OnMeleeComboTelegraphTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name != DarkHarvestLashName || !NPC.HasValidTarget)
                return;

            if (elapsed < total - 10)
                _lockedWhipAim = CalculateWhipAim(Main.player[NPC.target]);

            if (!_bloodWhipProjectileSpawned && Main.netMode != NetmodeID.MultiplayerClient)
            {
                _bloodWhipProjectileSpawned = true;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), PuppetHandPosition, _lockedWhipAim,
                    ModContent.ProjectileType<Projectiles.Enemy.BloodKnightDarkHarvestWhip>(),
                    MeleeDamage, 4f, Main.myPlayer, NPC.whoAmI);
            }

            if (!Main.dedServ && elapsed % 5 == 0)
            {
                Dust dust = Dust.NewDustPerfect(PuppetHandPosition, DustID.Blood,
                    Main.rand.NextVector2Circular(0.7f, 0.7f), 70, default, 0.85f);
                dust.noGravity = true;
            }
        }

        protected override void OnMeleeComboAttackTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            // The whip projectile owns its own telegraph and damage geometry. Sword arcs instead
            // get a cosmetic crescent that mirrors the live blade's Weighted clock exactly.
            if (combo.Name != DarkHarvestLashName
                && elapsed == 0
                && step.DamageMult > 0f
                && IsBloodSwordArc(step.Motion)
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnBloodSwordArc(combo, step, total);
            }
        }

        private static bool IsBloodSwordArc(ComboMotion motion)
            => motion == ComboMotion.OverheadArc || motion == ComboMotion.UnderhandArc;

        private void SpawnBloodSwordArc(MeleeCombo combo, MeleeComboStep step, int total)
        {
            float authoredSweep = combo.Name switch
            {
                ScarletHewName or BloodMeasureName => MathHelper.ToRadians(225f),
                ButchersReversalName => MathHelper.ToRadians(220f),
                _ => MathHelper.ToRadians(220f),
            };
            if (step.Motion == ComboMotion.UnderhandArc)
                authoredSweep = -authoredSweep;

            float collisionReach = ComboReachBase * 0.7f * step.ReachMult;
            float arcRadius = Math.Min(collisionReach,
                BloodSwordVisualReach * BloodSwordArcVisualAllowance);
            float hitWindowEnd = step.HitWindowEnd > 0f ? step.HitWindowEnd : 0.5f;
            VanillaSwordArcSettings settings = new VanillaSwordArcSettings
            {
                Texture = VanillaSwordArcTexture.NightsEdge,
                Easing = VanillaSwordArcEasing.Weighted,
                WeightedEaseInTicks = step.EaseInTicks,
                WeightedEaseOutTicks = step.EaseOutTicks,
                WeightedEaseOutDecay = step.EaseOutDecay,
                Duration = total,
                StartAngle = PuppetWeaponDirection.ToRotation(),
                SweepAngle = authoredSweep * NPC.direction,
                Radius = arcRadius,
                Opacity = 0.52f,
                FadeInFraction = 0.045f,
                FadeOutFraction = 1f - hitWindowEnd,
                AfterimageLag = MathHelper.PiOver4,
                AfterimageOpacity = 0.66f,
                BodyOpacity = 0.78f,
                CoreOpacity = 0.22f,
                DrawTipSparkle = false,
                DarkColor = new Color(58, 5, 9),
                BodyColor = new Color(192, 30, 38),
                CoreColor = new Color(255, 120, 108),
                DustType = DustID.Blood,
                DustColor = new Color(205, 42, 48),
                DustCount = 1,
                DustAlpha = 60,
                DustScale = 0.82f,
                DustRadiusFraction = 0.96f,
                DustAngularSpread = 0.42f,
                DustRadialSpeed = 0.15f,
                DustTangentialSpeed = 0.8f,
                DustVelocityJitter = 0.3f,
                DustInheritAnchorVelocity = 0.12f,
                DustNoGravity = false,
                TrackPuppetBlade = true,
                EnableCollision = false,
            };

            VanillaSwordArc.SpawnForNPC(NPC.GetSource_FromAI(), NPC, 0, 0f, Main.myPlayer,
                settings, Vector2.Zero, hostile: false);
        }

        private Vector2 CalculateWhipAim(Player target)
        {
            Vector2 delta = target.Center + target.velocity * 6f - PuppetHandPosition;
            float x = NPC.direction * Math.Max(1f, Math.Abs(delta.X));
            float yLimit = Math.Abs(x) * 0.72f;
            return new Vector2(x, MathHelper.Clamp(delta.Y, -yLimit, yLimit))
                .SafeNormalize(new Vector2(NPC.direction, 0f));
        }

        protected override void OnBladeHit(Player player)
        {
            if (!player.immune)
                player.AddBuff(BuffID.Bleeding, BleedingDuration);
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            if (step.DamageMult <= 0f)
                return;

            SoundEngine.PlaySound(SoundID.Item1 with
            {
                Volume = 0.72f,
                Pitch = -0.2f,
                PitchVariance = 0.07f,
            }, NPC.Center);
            base.DoComboMeleeHit(step);
        }

        protected override void OnRangedBurstStarted(bool secondary)
        {
            base.OnRangedBurstStarted(secondary);
            _bowShotIndex = 0;
            _bowAimVelocity = Vector2.Zero;
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
                return;

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
                return;

            if (IsSecondaryRangedActive)
            {
                FireBloodRain(target);
                return;
            }

            int pattern = Math.Clamp(ActiveBurstPatternIndex, 0, PhraseAimSnapshots.Length - 1);
            if (_bowAimVelocity == Vector2.Zero || IsPhraseAimSnapshot(pattern, _bowShotIndex))
                _bowAimVelocity = SolveArrowVelocity(target, 13.5f);

            SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.58f, PitchVariance = 0.06f }, NPC.Center);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), PuppetHandPosition, _bowAimVelocity,
                ModContent.ProjectileType<Projectiles.Enemy.BloodKnightArrow>(),
                RangedDamage, 3f, Main.myPlayer, NPC.whoAmI, NPC.target, IsBlackfirePhase ? 1f : 0f);
            _bowShotIndex++;
        }

        private bool IsPhraseAimSnapshot(int pattern, int shotIndex)
        {
            int[] snapshots = PhraseAimSnapshots[pattern];
            for (int i = 0; i < snapshots.Length; i++)
            {
                if (snapshots[i] == shotIndex)
                    return true;
            }
            return false;
        }

        private Vector2 SolveArrowVelocity(Player target, float speed)
        {
            Vector2 origin = PuppetHandPosition;
            float distance = Vector2.Distance(origin, target.Center);
            float travelTicks = distance / speed;
            float extraLeadTicks = _leadMissStreak * 2f;
            float leadTicks = MathHelper.Clamp(travelTicks + extraLeadTicks, 3f, 70f);
            Vector2 aimPoint = target.Center + target.velocity * leadTicks;
            aimPoint.Y -= 0.5f * Projectiles.Enemy.BloodKnightArrow.Gravity * leadTicks * leadTicks;
            return (aimPoint - origin).SafeNormalize(new Vector2(NPC.direction, 0f)) * speed;
        }

        private void FireBloodRain(Player target)
        {
            SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.72f, Pitch = -0.3f }, NPC.Center);
            Vector2 predictedCenter = target.Center + target.velocity * 18f;

            for (int i = 0; i < RainOffsets.Length; i++)
            {
                float offset = RainOffsets[i];
                Vector2 spawn = predictedCenter + new Vector2(offset, -600f);
                Vector2 destination = predictedCenter + new Vector2(offset * 0.35f, 8f);
                Vector2 velocity = (destination - spawn).SafeNormalize(Vector2.UnitY) * 15f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.BloodKnightRainArrow>(),
                    SecondaryRangedDamage, 3f, Main.myPlayer,
                    NPC.whoAmI, i * 5f, IsBlackfirePhase ? 1f : 0f);
            }
        }

        public void ReportBloodArrowHit()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                _leadMissStreak = 0;
        }

        public void ReportBloodArrowMiss()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                _leadMissStreak = Math.Min(4, _leadMissStreak + 1);
        }

        public static void ApplyBloodArrowDebuffs(Player target, bool blackfire)
        {
            target.AddBuff(BuffID.Bleeding, BleedingDuration);
            if (blackfire)
                target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DarkInferno>(), DarkInfernoDuration);
        }

        public override void OnKill()
        {
            if (Main.dedServ)
                return;

            for (int i = 1; i <= 3; i++)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position,
                    Main.rand.NextVector2Circular(6f, 6f),
                    Mod.Find<ModGore>($"Man Hunter Gore {i}").Type, 1.1f);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 2, 3));
            npcLoot.Add(ItemDropRule.ByCondition(
                tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule,
                ModContent.ItemType<FlameOfTheAbyss>()));
        }
    }
}
