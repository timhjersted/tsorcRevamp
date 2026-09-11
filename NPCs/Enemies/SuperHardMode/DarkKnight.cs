using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    /// <summary>
    /// A regular Super Hardmode elite rebuilt on the puppet combat foundation. Its sword strings
    /// use authored wide arcs, visible follow-through, and distinct punish windows; the old
    /// ArcherAI spell loop survives as a deliberately spaced Dark Wave cast.
    /// </summary>
    public class DarkKnight : PuppetNPC
    {
        private const string BlacksteelCutName = "Blacksteel Cut";
        private const string RisingReversalName = "Rising Reversal";
        private const string KnightsMeasureName = "Knight's Measure";
        private const string UmbralLungeName = "Umbral Lunge";
        private const string FalseRetreatName = "False Retreat";
        private const string EdgeOfNightName = "Edge of Night";

        private const int FalseRetreatIndex = 4;
        private const int PressureWindowTicks = 60;
        private const int PressureQueueLifetimeTicks = 180;

        private int _stormWaveDamage = 35;
        private int _recentPressureHits;
        private int _pressureWindowTimer;
        private int _pressureQueueTimer;
        private bool _shadowCounterQueued;

        private Vector2 _lungeDirection;
        private float _lungeDistance;
        private Vector2 _lungeVelocityThisTick;
        private bool _edgeProjectileReleased;

        protected override string InvaderTitle => "Dark Knight";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;

        protected override int HeadArmorItemType => ModContent.ItemType<DarkKnightHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<DarkKnightArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<DarkKnightGreaves>();

        protected override int MeleeWeaponItemType => ItemID.NightsEdge;
        protected override int RangedWeaponItemType => -1;
        // Dark Wave is cast through the blade, so the Night's Edge remains in the knight's hand.
        protected override int MagicWeaponItemType => ItemID.NightsEdge;

        protected override int MeleeDamage => (int)(105f * tsorcRevampWorld.SubtleSHMScale);
        protected override int RangedDamage => 0;
        protected override int MagicDamage => _stormWaveDamage;

        protected override float TopSpeed => 2.65f;
        protected override float Acceleration => 0.09f;
        protected override float MeleeRange => 84f;
        protected override float StabRange => 230f;
        protected override float ComboReachBase => 88f;
        protected override float MeleeEngageRange => 100f;
        protected override float ComboMaxStartRange => 330f;
        protected override float ClosingDistanceSpeedMult => 1.55f;
        protected override int ClosingDistanceMaxTicks => 100;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.65f;
        protected override float ComboTelegraphAdvanceStopDistance => 62f;

        protected override float MagicRange => 800f;
        protected override float MinMagicRange => 315f;
        protected override int MagicTelegraphTicks => 42;
        protected override int MagicAttackTicks => 8;
        protected override int MagicRecoveryTicks => 40;
        protected override int MagicCooldownAfterUse => 180;
        protected override Color MagicTelegraphFlashColor => new Color(118, 68, 210);
        protected override int MagicTelegraphFlashLeadTicks => 42;
        protected override bool UseAuthoredMagicCastPose => true;
        protected override float MagicCastStartRotation => 0.35f;
        protected override float MagicCastEndRotation => -1.35f;
        protected override int MagicWeaponRecoveryHoldTicks => 18;
        protected override Vector2 MagicGripNorm => new Vector2(0.1f, 0.85f);
        protected override bool UseCompositeArmForAdditionalPhase =>
            Phase == AttackPhase.MagicTelegraph
            || Phase == AttackPhase.MagicAttack
            || IsHoldingMagicWeaponDuringRecovery;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override Vector2 MeleeHandleNorm => new Vector2(0.1f, 0.85f);
        protected override float MeleeWeaponDrawScale => 1.1f;
        protected override float MeleeBladeWidth => 28f;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => new Color(125, 55, 205);
        protected override float SlashVFXOpacity => 0.48f;
        protected override float SlashVFXScale => 0.58f;

        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 100;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 8;
        protected override int MeleeComboInterStepLingerTicks => 5;
        protected override int MeleeRecoveryLingerTicks => 8;

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

        private static MeleeComboStep MotionStep(
            ComboMotion motion,
            int telegraphTicks,
            int attackTicks,
            int pauseAfter,
            float damageMult,
            float reachMult = 1f)
            => new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = reachMult,
                ForwardPushMult = 0f,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Smooth,
                HitWindowEnd = damageMult > 0f ? 0.92f : 0f,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };

        // Timing sheet (60 ticks = 1 second). Weighted hit windows end when angular speed falls
        // below 30% of peak; the harmless tail and recovery remain as the player's punish window.
        //
        // Move              Tell   Strike Strike curve       Envelope/live  Tail  Pause Recovery
        // Blacksteel Cut     28     7 in / 26 out, k7     225° / ~180°    20t    -      26t
        // Rising Reversal    26     8 in / 28 out, k6     220° / ~176°    21t    -      28t
        // Knight's Measure   30     8/24 k6 -> 8/32 k6    225° each       18/24  9t     36t
        // Umbral Lunge       36     6t ramp + 18t decay   thrust pose     while slowing 42t
        // False Retreat       8     24t backstep; 6/30 k7 220° / ~176°    23t   10t     48t
        // Edge of Night      46     6 in / 30 out, k8     240° / ~192°    24t    -      50t
        //
        // Standard cuts extend their END pose instead of folding farther behind the torso. The
        // two Measure cuts share exact endpoints; False Retreat's strike begins at BackstepRaise's
        // -1.30 pose. Both handoffs therefore hold instead of snapping. Edge of Night is the only
        // signature-wide cut and releases its crescent at the six-tick speed peak.
        private static readonly MeleeCombo[] DarkKnightCombos =
        {
            new MeleeCombo
            {
                Name = BlacksteelCutName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(150, 110, 210),
                CooldownAfterUse = 64,
                RecoveryTicks = 26,
                MoveBrake = 0.24f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.OverheadArc, 28, 7, 26, 7f,
                        damageMult: 1f, reachMult: 1.17f, forwardPushMult: 0.18f),
                },
            },
            new MeleeCombo
            {
                Name = RisingReversalName,
                BaseWeight = 92,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(105, 85, 190),
                CooldownAfterUse = 70,
                RecoveryTicks = 28,
                MoveBrake = 0.2f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.UnderhandArc, 26, 8, 28, 6f,
                        damageMult: 0.96f, reachMult: 1.16f, forwardPushMult: 0.16f),
                },
            },
            new MeleeCombo
            {
                Name = KnightsMeasureName,
                BaseWeight = 76,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(175, 150, 225),
                CooldownAfterUse = 122,
                RecoveryTicks = 36,
                MoveBrake = 0.22f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.OverheadArc, 30, 8, 24, 6f,
                        pauseAfter: 9, damageMult: 0.82f, reachMult: 1.16f,
                        forwardPushMult: 0.14f),
                    WeightedSwordStep(ComboMotion.UnderhandArc, 0, 8, 32, 6f,
                        damageMult: 1.05f, reachMult: 1.18f, forwardPushMult: 0.2f),
                },
            },
            new MeleeCombo
            {
                Name = UmbralLungeName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(80, 55, 175),
                CooldownAfterUse = 145,
                RecoveryTicks = 42,
                RangedStartOnly = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    MotionStep(ComboMotion.JoustDash, 36, 24, 0, 1.12f, 1.24f),
                },
            },
            new MeleeCombo
            {
                Name = FalseRetreatName,
                BaseWeight = 42,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(90, 55, 180),
                CooldownAfterUse = 175,
                RecoveryTicks = 48,
                MoveBrake = 0f,
                Steps = new[]
                {
                    MotionStep(ComboMotion.BackstepRaise, 8, 24, 10, 0f),
                    WeightedSwordStep(ComboMotion.OverheadArc, 0, 6, 30, 7f,
                        damageMult: 1.2f, reachMult: 1.2f, forwardPushMult: 0.38f),
                },
            },
            new MeleeCombo
            {
                Name = EdgeOfNightName,
                BaseWeight = 58,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(185, 90, 255),
                CooldownAfterUse = 230,
                RecoveryTicks = 50,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.38f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.OverheadArc, 46, 6, 30, 8f,
                        damageMult: 1.42f, reachMult: 1.25f, forwardPushMult: 0.12f),
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => DarkKnightCombos;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 3f;
            NPC.width = 20;
            NPC.height = 48;
            NPC.timeLeft = 750;
            NPC.damage = 0;
            NPC.lavaImmune = true;
            NPC.defense = 30;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 3000;
            NPC.knockBackResist = 0.35f;
            NPC.value = 8000;
            NPC.aiStyle = -1;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DarkKnightBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavSearchRadius = 40;
            globalNPC.RemembersLastKnownPos = true;
            globalNPC.CanUseRopes = true;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            _stormWaveDamage = (int)(_stormWaveDamage * tsorcRevampWorld.SHMScale);
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
                && player.townNPCs < 1f
                && (player.ZoneCorrupt || player.ZoneDungeon)
                && !player.ZoneMeteor
                && !player.ZoneJungle
                && !player.ZoneUnderworldHeight
                && !player.ZoneHallow
                && !ocean)
            {
                chance = 0.2f;
            }

            if (!Main.dayTime)
                chance *= 2f;
            if (Main.bloodMoon)
                chance *= 2f;
            return chance;
        }

        public override void AI()
        {
            base.AI();
            UpdatePressureCounter();

            // The lunge distance is solved when the tell begins. Reapply its authored velocity after
            // the base phase's movement brake so the real dash matches the timing sheet.
            if (ActiveMeleeComboName == UmbralLungeName
                && Phase == AttackPhase.MeleeComboAttack
                && _lungeVelocityThisTick != Vector2.Zero)
            {
                NPC.velocity.X = _lungeVelocityThisTick.X;
            }
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == EdgeOfNightName)
                return healthFraction <= 0.5f;
            if (combo.Name == UmbralLungeName)
                return distance >= 145f && distance <= ComboMaxStartRange;
            return true;
        }

        protected override int ReactiveComboIndex(float dist, ComboRangeBand band, int[] ready)
        {
            if (_shadowCounterQueued
                && FalseRetreatIndex < ready.Length
                && ready[FalseRetreatIndex] > 0
                && dist <= ComboMaxStartRange)
            {
                return FalseRetreatIndex;
            }
            return -1;
        }

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            _lungeVelocityThisTick = Vector2.Zero;
            _edgeProjectileReleased = false;

            if (combo.Name == FalseRetreatName)
            {
                _shadowCounterQueued = false;
                _pressureQueueTimer = 0;
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.45f, Pitch = -0.45f }, NPC.Center);
                    for (int i = 0; i < 14; i++)
                    {
                        Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
                            DustID.Shadowflame, 0f, 0f, 80, new Color(125, 65, 205), 1.05f);
                        dust.noGravity = true;
                        dust.velocity += Main.rand.NextVector2Circular(2.2f, 2.2f);
                    }
                }
            }

            if (combo.Name == UmbralLungeName && NPC.HasValidTarget)
            {
                Player target = Main.player[NPC.target];
                _lungeDirection = new Vector2(target.Center.X < NPC.Center.X ? -1f : 1f, 0f);
                _lungeDistance = MathHelper.Clamp(Math.Abs(target.Center.X - NPC.Center.X) - 68f, 72f, 238f);
            }

            if (Main.netMode == NetmodeID.Server)
                NPC.netUpdate = true;
        }

        protected override bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == KnightsMeasureName && nextStepIndex == 1)
            {
                bool stillInFront = Math.Sign(target.Center.X - NPC.Center.X) == NPC.direction;
                bool insideReturnCut = Vector2.Distance(NPC.Center, target.Center) <= ComboReachBase * 1.18f + 34f;
                return previousStepHit || (stillInFront && insideReturnCut);
            }
            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        protected override void OnMeleeComboAttackTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name == UmbralLungeName)
            {
                float totalWeight = 0f;
                for (int tick = 0; tick < total; tick++)
                    totalWeight += LungeSpeedWeight(tick, total);

                float speed = _lungeDistance * LungeSpeedWeight(elapsed, total) / Math.Max(0.001f, totalWeight);
                _lungeVelocityThisTick = _lungeDirection * speed;
            }
            else
            {
                _lungeVelocityThisTick = Vector2.Zero;
            }

            if (combo.Name == EdgeOfNightName
                && !_edgeProjectileReleased
                && elapsed >= Math.Max(1, step.EaseInTicks))
            {
                _edgeProjectileReleased = true;
                ReleaseEdgeOfNight();
            }
        }

        private static float LungeSpeedWeight(int elapsed, int total)
        {
            if (elapsed < 6)
            {
                float ramp = (elapsed + 1f) / 6f;
                return MathHelper.SmoothStep(0.18f, 1f, ramp);
            }

            float decayProgress = (elapsed - 6f) / Math.Max(1f, total - 7f);
            return MathHelper.Lerp(1f, 0.2f, MathHelper.SmoothStep(0f, 1f, decayProgress));
        }

        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            _lungeVelocityThisTick = Vector2.Zero;
        }

        protected override void ModifyMeleeArcEndpoints(
            ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            bool comboPoseActive = Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause
                || Phase == AttackPhase.MeleeComboRecovery;
            if (!comboPoseActive)
                return;

            switch (ActiveMeleeComboName)
            {
                case BlacksteelCutName:
                case KnightsMeasureName:
                    if (motion == ComboMotion.OverheadArc)
                    {
                        startRotation = -1.55f;
                        endRotation = 2.38f; // 225 degree envelope, about 180 degrees live.
                    }
                    else if (motion == ComboMotion.UnderhandArc)
                    {
                        startRotation = 2.38f;
                        endRotation = -1.55f;
                    }
                    break;

                case RisingReversalName:
                    if (motion == ComboMotion.UnderhandArc)
                    {
                        startRotation = 2.30f;
                        endRotation = -1.54f; // 220 degree envelope.
                    }
                    break;

                case FalseRetreatName:
                    if (motion == ComboMotion.OverheadArc)
                    {
                        startRotation = -1.30f; // exact BackstepRaise end pose: no inter-step snap.
                        endRotation = 2.54f;    // 220 degree envelope.
                    }
                    break;

                case EdgeOfNightName:
                    if (motion == ComboMotion.OverheadArc)
                    {
                        startRotation = -1.68f;
                        endRotation = 2.51f; // 240 degree signature envelope, about 192 degrees live.
                    }
                    break;
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            RecordPressureHit();
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            RecordPressureHit();
        }

        private void RecordPressureHit()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (_pressureWindowTimer <= 0)
                _recentPressureHits = 0;

            _recentPressureHits++;
            _pressureWindowTimer = PressureWindowTicks;
            if (_recentPressureHits >= 2)
            {
                _recentPressureHits = 0;
                _shadowCounterQueued = true;
                _pressureQueueTimer = PressureQueueLifetimeTicks;
                NPC.netUpdate = true;
            }
        }

        private void UpdatePressureCounter()
        {
            if (_pressureWindowTimer > 0 && --_pressureWindowTimer == 0)
                _recentPressureHits = 0;

            if (_shadowCounterQueued && _pressureQueueTimer > 0 && --_pressureQueueTimer == 0)
            {
                _shadowCounterQueued = false;
                NPC.netUpdate = true;
            }
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, Pitch = -0.12f, PitchVariance = 0.08f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            if (step.DamageMult <= 0f)
                return;

            SoundEngine.PlaySound(SoundID.Item1 with
            {
                Volume = ActiveMeleeComboName == EdgeOfNightName ? 0.9f : 0.68f,
                Pitch = ActiveMeleeComboName == EdgeOfNightName ? -0.35f : -0.14f,
                PitchVariance = 0.08f,
            }, NPC.Center);
            base.DoComboMeleeHit(step);
        }

        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (Main.dedServ)
                return;

            Vector2 tip = PuppetWeaponTipPosition(64f);
            float radius = MathHelper.Lerp(26f, 5f, progress);
            int count = 1 + (progress > 0.55f ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Dust dust = Dust.NewDustPerfect(tip + offset, DustID.Shadowflame,
                    -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(0.8f, 2.6f, progress),
                    70, new Color(130, 70, 220), Main.rand.NextFloat(0.75f, 1.25f));
                dust.noGravity = true;
            }
            Lighting.AddLight(tip, 0.2f, 0.08f, 0.34f);
        }

        protected override void DoMagicAttack()
        {
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.75f, Pitch = -0.28f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
                return;

            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetWeaponTipPosition(64f);
            Vector2 velocity = (target.Center + target.velocity * 8f - origin)
                .SafeNormalize(new Vector2(NPC.direction, 0f)) * 14f;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity,
                ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssStormWave>(),
                MagicDamage, 8f, Main.myPlayer);
        }

        protected override void DoRangedAttack()
        {
        }

        private void ReleaseEdgeOfNight()
        {
            if (!Main.dedServ)
            {
                Vector2 tip = PuppetWeaponTipPosition(72f);
                for (int i = 0; i < 12; i++)
                {
                    Dust dust = Dust.NewDustPerfect(tip, DustID.Shadowflame,
                        Main.rand.NextVector2Circular(3f, 3f), 80,
                        new Color(165, 80, 245), Main.rand.NextFloat(0.85f, 1.35f));
                    dust.noGravity = true;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
                return;

            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetWeaponTipPosition(72f);
            Vector2 velocity = (target.Center + target.velocity * 4f - origin)
                .SafeNormalize(new Vector2(NPC.direction, 0f)) * 12f;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity,
                ModContent.ProjectileType<Projectiles.Enemy.AbyssSlash>(),
                MagicDamage, 5f, Main.myPlayer, NPC.whoAmI + 1);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)Math.Clamp(_recentPressureHits, 0, byte.MaxValue));
            writer.Write((short)Math.Clamp(_pressureWindowTimer, 0, short.MaxValue));
            writer.Write((short)Math.Clamp(_pressureQueueTimer, 0, short.MaxValue));
            writer.Write(_shadowCounterQueued);
            writer.Write(_lungeDirection.X);
            writer.Write(_lungeDistance);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _recentPressureHits = reader.ReadByte();
            _pressureWindowTimer = reader.ReadInt16();
            _pressureQueueTimer = reader.ReadInt16();
            _shadowCounterQueued = reader.ReadBoolean();
            _lungeDirection = new Vector2(reader.ReadSingle(), 0f);
            _lungeDistance = reader.ReadSingle();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WhiteTitanite>(), 1, 2, 3));
        }
    }
}
