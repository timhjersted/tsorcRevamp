using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors.Magic;
using tsorcRevamp.Items.Weapons.Magic;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// An early-game red mage invader. Kahlrun alternates short, readable sword strings with
    /// several formations of the same Farron Dart spell.
    /// </summary>
    public class Kahlrun : PuppetNPC
    {
        protected override string InvaderTitle => "Kahlrun of Falstrók";

        protected override void RunMovementAI(float speedMult)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 60;
            globalNPC.RemembersLastKnownPos = true;

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 1,
                attackRange: MagicRange);
        }

        protected override int HeadArmorItemType => ModContent.ItemType<RedClothHat>();
        protected override int BodyArmorItemType => ModContent.ItemType<RedClothTunic>();
        protected override int LegsArmorItemType => ModContent.ItemType<RedClothPants>();

        protected override int MeleeWeaponItemType => ItemID.SilverShortsword;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => ModContent.ItemType<FarronDart>();

        protected override int MeleeDamage => 20;
        protected override int RangedDamage => 0;
        protected override int MagicDamage => 20;

        protected override int EstusChargesMax => 2;
        protected override int HealAnimationTicks => 3 * 60;

        protected override float TopSpeed => 2.45f;
        protected override float Acceleration => 0.09f;
        protected override float MeleeRange => 66f;
        protected override float StabRange => 150f;
        protected override float ComboMaxStartRange => 210f;
        // Select melee from mid-range, but do not reveal the combo until Kahlrun has pursued
        // inside the shortsword's real first-swing reach. This mirrors Red Knight's range-gated
        // melee admission while keeping the approach itself neutral and reactable.
        protected override float MeleeEngageRange => 76f;
        protected override float ClosingDistanceSpeedMult => 1.65f;
        protected override int ClosingDistanceMaxTicks => 110;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 72;
        protected override float ComboTelegraphMultiplier => 1.35f;
        protected override int MinComboTelegraphTicks => 26;
        // Red Knight keeps advancing during the readable part of its melee windup. Kahlrun does
        // the same at a restrained speed: it catches backpedaling, but never turns through a dodge.
        protected override float ComboTelegraphAdvanceSpeedMult => 0.8f;
        protected override float ComboTelegraphAdvanceStopDistance => 48f;

        protected override float MagicRange => 620f;
        protected override float MinMagicRange => 105f;
        protected override int MagicTelegraphTicks => 84;
        protected override int MagicAttackTicks => 18;
        protected override int MagicRecoveryTicks => 48;
        protected override int MagicCooldownAfterUse => 95;
        protected override Color MagicTelegraphFlashColor => new Color(55, 145, 255);
        protected override int MagicTelegraphFlashLeadTicks => MagicTelegraphTicks;
        protected override bool UseAuthoredMagicCastPose => true;
        protected override float MagicCastStartRotation => 0.08f;
        protected override float MagicCastEndRotation => -1.12f;
        protected override float MagicWeaponRotationOffset => MathHelper.PiOver4;
        protected override int MagicWeaponRecoveryHoldTicks => 30;
        protected override bool UseCompositeArmForAdditionalPhase =>
            Phase == AttackPhase.MagicTelegraph
            || Phase == AttackPhase.MagicAttack
            || IsHoldingMagicWeaponDuringRecovery;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override Vector2 MagicGripNorm => new Vector2(0.16f, 0.84f);
        protected override float GetHeldRangedDrawScale(int itemType)
            => itemType == MagicWeaponItemType ? 0.9f : base.GetHeldRangedDrawScale(itemType);

        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (Main.dedServ)
                return;

            const float staffTipReach = 43f;
            Vector2 tip = PuppetWeaponTipPosition(staffTipReach);
            float radius = MathHelper.Lerp(24f, 7f, progress);
            int count = 1 + (progress > 0.5f ? 1 : 0) + (progress > 0.82f ? 1 : 0);

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Vector2 inward = (-offset).SafeNormalize(Vector2.Zero)
                    * MathHelper.Lerp(1.2f, 2.8f, progress);
                float scale = Main.rand.NextFloat(
                    MathHelper.Lerp(0.42f, 0.72f, progress),
                    MathHelper.Lerp(0.68f, 1.12f, progress));
                Dust mote = Dust.NewDustPerfect(
                    tip + offset,
                    DustID.TintableDustLighted,
                    inward,
                    80,
                    Color.Lerp(new Color(55, 120, 255), new Color(120, 225, 255), progress),
                    scale);
                mote.noGravity = true;
                mote.fadeIn = scale + 0.28f;
            }

            Lighting.AddLight(tip,
                0.08f + 0.12f * progress,
                0.18f + 0.20f * progress,
                0.38f + 0.22f * progress);
        }

        protected override Vector2 MeleeHandleNorm => new Vector2(0.14f, 0.84f);
        protected override float MeleeWeaponDrawScale => 1.1f;
        protected override float ComboReachBase => 72f;
        protected override float MeleeBladeWidth => 24f;
        protected override bool UseSwingEasing => true;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => new Color(245, 95, 110);
        protected override float SlashVFXOpacity => 0.38f;
        protected override float SlashVFXScale => 0.42f;

        private static MeleeComboStep SwordStep(
            ComboMotion motion,
            int telegraphTicks,
            int attackTicks,
            int pauseAfter = 0,
            float damageMult = 1f,
            float forwardPushMult = 0f,
            float reachMult = 1.15f)
            => new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = reachMult,
                ForwardPushMult = forwardPushMult,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Smooth,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };

        private static readonly PuppetAttackClip QuickCutClip = new PuppetAttackClip(
            name: "Quick Cut",
            pose: PuppetPosePreset.Swing,
            windupTicks: 28,
            activeTicks: 14,
            recoveryTicks: 20,
            oppositeWindupRotation: 0.8f,
            attackStartRotation: -1.05f,
            attackEndRotation: 0.72f);

        private static readonly PuppetAttackClip RisingCutClip = new PuppetAttackClip(
            name: "Rising Cut",
            pose: PuppetPosePreset.Swing,
            windupTicks: 30,
            activeTicks: 15,
            recoveryTicks: 22,
            oppositeWindupRotation: -0.8f,
            attackStartRotation: 0.9f,
            attackEndRotation: -0.78f);

        private static readonly MeleeCombo[] KahlrunSwordCombos = new[]
        {
            new MeleeCombo
            {
                Name = "Quick Cut",
                BaseWeight = 90,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White,
                CooldownAfterUse = 54,
                MoveBrake = 0.12f,
                RuntimeV2Clip = QuickCutClip,
                Steps = new[] { SwordStep(ComboMotion.OverheadArc, 28, 14, damageMult: 0.9f, forwardPushMult: 0.35f) },
            },
            new MeleeCombo
            {
                Name = "Rising Cut",
                BaseWeight = 80,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightPink,
                CooldownAfterUse = 62,
                MoveBrake = 0.14f,
                RuntimeV2Clip = RisingCutClip,
                Steps = new[] { SwordStep(ComboMotion.UnderhandArc, 30, 15, damageMult: 0.95f, forwardPushMult: 0.32f) },
            },
            new MeleeCombo
            {
                Name = "Red Doublet",
                BaseWeight = 78,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(255, 105, 120),
                CooldownAfterUse = 110,
                MoveBrake = 0.1f,
                Steps = new[]
                {
                    SwordStep(ComboMotion.OverheadArc, 28, 14, pauseAfter: 8, damageMult: 0.72f, forwardPushMult: 0.35f),
                    SwordStep(ComboMotion.UnderhandArc, 0, 16, damageMult: 0.88f, forwardPushMult: 0.48f),
                },
            },
            new MeleeCombo
            {
                Name = "Crimson Cross",
                BaseWeight = 58,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.IndianRed,
                CooldownAfterUse = 135,
                MoveBrake = 0.14f,
                Steps = new[]
                {
                    SwordStep(ComboMotion.HorizontalSweep, 32, 16, pauseAfter: 10, damageMult: 0.78f, forwardPushMult: 0.28f),
                    SwordStep(ComboMotion.OverheadArc, 0, 18, damageMult: 1.05f, forwardPushMult: 0.42f),
                },
            },
            new MeleeCombo
            {
                Name = "Silver Needle",
                BaseWeight = 68,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Silver,
                CooldownAfterUse = 120,
                MoveBrake = 0.05f,
                Steps = new[] { SwordStep(ComboMotion.JoustDash, 34, 18, damageMult: 1.05f, forwardPushMult: 1.25f, reachMult: 1.25f) },
            },
            new MeleeCombo
            {
                Name = "False Retreat",
                BaseWeight = 42,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 175,
                HeavyCommit = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    SwordStep(ComboMotion.BackstepRaise, 38, 28, pauseAfter: 8, damageMult: 0f),
                    SwordStep(ComboMotion.JoustDash, 0, 20, damageMult: 1.2f, forwardPushMult: 1.45f, reachMult: 1.28f),
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => KahlrunSwordCombos;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 1500;
            NPC.defense = 7;
            NPC.damage = 0;
            NPC.knockBackResist = 0.3f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 25000f;
            NPC.boss = true;
            NPC.npcSlots = 3f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 18f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 150;
            globalNPC.PursuitLeashRange = 1400f;
            globalNPC.PursuitFallBehindTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.RecoveryOnly;
            globalNPC.RecoveryTeleportMaxRange = 900f;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
            globalNPC.AllowDynamicEventNaturalDespawn = true;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RedClothHat>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RedClothTunic>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RedClothPants>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FarronDart>()));
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0)
                return;

            for (int i = 0; i < 150; i++)
            {
                int dust = Dust.NewDust(
                    NPC.position,
                    NPC.width,
                    NPC.height,
                    DustID.Blood,
                    Main.rand.NextFloat(-6f, 6f),
                    Main.rand.NextFloat(-6f, 6f),
                    100,
                    default,
                    Main.rand.NextFloat(1f, 2.2f));
                Main.dust[dust].noGravity = true;
            }
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.2f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.18f, PitchVariance = 0.12f }, NPC.Center);
            base.DoComboMeleeHit(step);
        }

        protected override void DoMagicAttack()
        {
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.65f, PitchVariance = 0.1f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
                return;

            Vector2 origin = KahlrunStaffTipPosition();
            Vector2 aimPoint = target.Center + target.velocity * 9f;
            Vector2 aimVelocity = (aimPoint - origin).SafeNormalize(new Vector2(NPC.direction, 0f)) * 7f;

            switch (Main.rand.Next(4))
            {
                case 0:
                    SpawnFarronDart(origin, aimVelocity);
                    break;

                case 1:
                    for (int i = -1; i <= 1; i++)
                        SpawnFarronDart(origin, aimVelocity.RotatedBy(MathHelper.ToRadians(i * 11f)) * 0.92f);
                    break;

                case 2:
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 burstVelocity = aimVelocity.RotatedBy(MathHelper.ToRadians((i - 1) * 4f));
                        SpawnFarronDart(origin, burstVelocity, delayTicks: i * 10);
                    }
                    break;

                default:
                    for (int i = -2; i <= 2; i++)
                    {
                        Vector2 rainOrigin = target.Center + new Vector2(i * 46f, -250f - System.Math.Abs(i) * 12f);
                        Vector2 rainTarget = target.Center + target.velocity * (20f + i * 2f);
                        Vector2 rainVelocity = (rainTarget - rainOrigin).SafeNormalize(Vector2.UnitY) * 8.5f;
                        SpawnFarronDart(rainOrigin, rainVelocity, delayTicks: (i + 2) * 5);
                    }
                    break;
            }
        }

        private void SpawnFarronDart(Vector2 position, Vector2 velocity, int delayTicks = 0)
        {
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                position,
                velocity,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.KahlrunFarronDart>(),
                MagicDamage,
                1.5f,
                Main.myPlayer,
                delayTicks);
        }

        private Vector2 KahlrunStaffTipPosition()
        {
            if (!Main.dedServ)
                return PuppetWeaponTipPosition(43f);

            float worldAngle = NPC.direction == 1
                ? MagicCastEndRotation
                : MathHelper.Pi - MagicCastEndRotation;
            Vector2 staffDirection = worldAngle.ToRotationVector2();
            return NPC.Center + new Vector2(NPC.direction * 4f, -2f) + staffDirection * 53f;
        }

        protected override void DoRangedAttack() { }
    }
}
