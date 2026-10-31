using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.ForgottenKnightSet;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Weapons.Throwing;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.NPCs.Enemies.GhostFighter
{
    public class GhostOfTheForgottenKnight : RedKnightPuppet, IHumanoidMeleeHitEffects
    {
        const string LeapName = "Phantom Leap Thrust";
        static readonly MeleeCombo[] ForgottenCombos = CreateCombos();
        float _leapAim;
        protected override string InvaderTitle => "Ghost of the Forgotten Knight";
        protected override int HeadArmorItemType => ModContent.ItemType<ForgottenKnightHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<ForgottenKnightBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<ForgottenKnightGreaves>();
        protected override Color KnightAccentColor => Color.LightBlue;
        protected override Color KnightLightColor => new Color(120, 130, 160);
        protected override int KnightGlintDust => DustID.IceTorch;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override float TopSpeed => tsorcRevampWorld.SuperHardMode ? 2f : Main.hardMode ? 1.5f : 1.2f;
        protected override float PuppetJumpPower => 13f;
        protected override float PuppetJumpBoost => 6f;
        protected override MeleeCombo[] MeleeComboPoolOverride => ForgottenCombos;
        protected override bool UseLandingTimedLeapSlam => true;
        protected override int LeapSlamDownswingTicks => 16;
        protected override float LeapAttackUpSpeed => 7f;
        protected override float LeapAttackForwardSpeed => 8f;
        protected override float LeapAttackMinimumForwardSpeed => 0f;
        protected override float LeapMinLungeDistance => 0f;
        protected override float LeapLandingStandoff => -40f;
        protected override float LeapSlamCarryRotation => MathHelper.PiOver4 + _leapAim;
        protected override float LeapSlamImpactRotation => LeapSlamCarryRotation;
        protected override int TeleportTelegraphTicks => 80;
        protected override int TeleportDustTypeId => DustID.Smoke;
        protected override Color TeleportDustTint => new Color(55, 55, 65);
        protected override float TeleportDustScale => 0.65f;
        protected override int TeleportDustCount => 20;
        protected override Color PuppetSkinColor => Color.Black;
        protected override bool HasSpectralOverlay => true;
        protected override bool SpectralKeepsArmorDyes => true;
        protected override float SpectralOverlayScale => 1f;
        protected override Color SpectralOverlayColor => new Color(190, 200, 220);
        protected override float SpectralCoreTintStrength => 0.3f;
        protected override float SpectralCoreOpacity => 0.75f;
        protected override Color SpectralHaloColor => new Color(120, 130, 160);
        protected override int SpectralHaloCopyCount => 6;
        protected override float SpectralHaloRadius => 3f;
        protected override float SpectralHaloOpacity => 0.22f;
        protected override float SpectralTrailOpacity => 0.35f;

        bool IsThrustPose => (ActiveMeleeComboName == "Double Spear Lunge"
                || ActiveMeleeComboName == "Charging Spear Thrust" || ActiveMeleeComboName == LeapName)
            && (Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery);

        // All thrusts share the lower 180-degree hand arc and middle-of-shaft ready grip.
        // The spear is perpendicular over the rear half of the arc, level for the latter half of the punch.
        // Double: tell 36, punch 6, live 6..15, return 15..24, pause 26, recovery 36.
        // Charge: tell 45, punch 6, live 6..19, return 19..30, recovery 42; the dash still owns movement.
        // Leap: tell 40, cocked throughout flight, punch 6 on the synced 16t strike clock, live 6..15.
        // Its 40t landing recovery swings back over 12t, then releases to carry over 12t.
        // The spear-tip capsule follows the same hand, grip and angle; no extra network state is needed.
        string ThrustPosePhase => Phase == AttackPhase.MeleeComboTelegraph ? "MeleeComboTelegraph"
            : Phase == AttackPhase.MeleeComboAttack ? "MeleeComboAttack"
            : Phase == AttackPhase.MeleeComboPause ? "MeleeComboPause" : "MeleeComboRecovery";

        protected override float MeleeCompositeArmRotationOffset => IsThrustPose
            ? ThrustArmOffset(ActiveMeleeComboName, ThrustPosePhase, PhaseTimer, LeapSlamSwingProgress)
            : base.MeleeCompositeArmRotationOffset;
        protected override float SpearDrawRotationOffset => base.SpearDrawRotationOffset
            + (IsThrustPose ? ThrustSpearAngle(ActiveMeleeComboName, ThrustPosePhase, PhaseTimer, LeapSlamSwingProgress) : 0f);

        // 16 game pixels = 32 pixels in the 2x preview; measured tip/base centers are y0.5/y61.5.
        public const float ThrustReadyGrip = 0.32f + 16f / 61f;

        public static float ThrustSpearAngle(string combo, string phase, int remaining, float leapProgress)
            => Math.Max(0f, MathHelper.PiOver4 + ThrustArmOffset(combo, phase, remaining, leapProgress) - MathHelper.PiOver2);

        public static float ThrustArmOffset(string combo, string phase, int remaining, float leapProgress)
        {
            const float rear = MathHelper.Pi - MathHelper.PiOver4;
            const float front = -MathHelper.PiOver4;
            bool leap = combo == LeapName;
            bool charge = combo == "Charging Spear Thrust";
            if (phase == "MeleeComboTelegraph")
                return MathHelper.SmoothStep(0f, rear, (24f - remaining) / 24f);
            if (phase == "MeleeComboPause") return rear;
            if (phase == "MeleeComboRecovery")
            {
                float elapsedRecovery = (leap ? 40f : charge ? 42f : 36f) - remaining;
                if (leap)
                    return elapsedRecovery < 12f ? MathHelper.SmoothStep(front, rear, elapsedRecovery / 12f)
                        : MathHelper.SmoothStep(rear, 0f, (elapsedRecovery - 12f) / 12f);
                return MathHelper.SmoothStep(rear, 0f, elapsedRecovery / 12f);
            }
            if (phase != "MeleeComboAttack") return 0f;
            float total = charge ? 30f : 24f;
            float elapsed = leap ? leapProgress * 16f : total - remaining;
            if (elapsed < 6f) return MathHelper.SmoothStep(rear, front, elapsed / 6f);
            float holdEnd = charge ? 19f : 15f;
            if (leap || elapsed <= holdEnd) return front;
            return MathHelper.SmoothStep(front, rear, (elapsed - holdEnd) / (total - holdEnd));
        }

        public static float ThrustGrip(string combo, string phase, int remaining, float progress)
        {
            bool leap = combo == LeapName;
            if (phase != "MeleeComboAttack")
                return leap && phase == "MeleeComboRecovery"
                    ? MathHelper.SmoothStep(0.97f, ThrustReadyGrip, (40f - remaining) / 12f) : ThrustReadyGrip;
            bool charge = combo == "Charging Spear Thrust";
            float total = leap ? 16f : charge ? 30f : 24f;
            float elapsed = progress * total;
            if (elapsed < 6f) return MathHelper.SmoothStep(ThrustReadyGrip, 0.97f, elapsed / 6f);
            float holdEnd = charge ? 19f : 15f;
            if (leap || elapsed <= holdEnd) return 0.97f;
            return MathHelper.SmoothStep(0.97f, ThrustReadyGrip, (elapsed - holdEnd) / (total - holdEnd));
        }

        protected override float? OverrideSpearGrip(float progress) => IsThrustPose
            ? ThrustGrip(ActiveMeleeComboName, ThrustPosePhase, PhaseTimer,
                ActiveMeleeComboName == LeapName ? LeapSlamSwingProgress : progress)
            : base.OverrideSpearGrip(progress);

        static MeleeCombo[] CreateCombos()
        {
            var inherited = BuildKnightCombos(Color.LightBlue);
            // Level spear, 40t tell; 6t punch then 10 live ticks (one roll); no landing hit.
            // 128/192/256px + 40 overshoot in ~47t needs 3.6/5.0/6.3 px/t, below the 8px/t cap.
            // Recovery 40t. Aim and the ballistic landing line lock at release; moving after launch can evade it.
            return new[] { inherited[0], inherited[2], new MeleeCombo
            {
                Name = LeapName, BaseWeight = 100, Preferred = ComboRangeBand.Mid,
                RangedStartOnly = true, InitialFlashColor = Color.LightBlue,
                CooldownAfterUse = 240, RecoveryTicks = 40,
                Steps = new[] { new MeleeComboStep
                {
                    Motion = ComboMotion.LeapSlam, TelegraphTicks = 40, AttackTicks = 90,
                    DamageMult = 1.25f, ReachMult = 1f, SwingSpeedMult = 1f,
                    LeapStrikeRange = 110f, LeapHeightMult = 1f, LeapForwardSpeedMult = 1f,
                } }
            } };
        }

        public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 1;
        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 40;
            NPC.damage = 0;
            NPC.lifeMax = 150;
            NPC.defense = 22;
            NPC.value = 750;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0.3f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GhostOfTheForgottenKnightBanner>();
            redKnightsSpearDamage = 80;
            if (Main.hardMode)
            {
                NPC.lifeMax = 200;
                NPC.defense = 32;
                NPC.value = 1000;
                redKnightsSpearDamage = 100;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1000;
                NPC.defense = 80;
                NPC.value = 4000;
                redKnightsSpearDamage = 180;
            }
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.CanPassThroughWalls = true;
            EvasiveProfile.Ghost(g);
            g.EvasiveRetreatJump = false;
            g.EvasiveRetreatDash = false;
            g.EvasiveOnHitChanceDenominator = 6;
            g.EvasiveOnHitCooldownTicks = 120;
            g.KiteRangeMin = 7f;
            g.KiteRangeMax = 14f;
            g.KiteLooseness = 0.55f;
            g.HasGhostAfterimages = false;
            g.MaxJumpPower = 13f;
            g.MaxJumpBoost = 6f;
            g.PatrolMode = PatrolMode.Wander;
            g.PatrolAnchorSource = PatrolAnchorSource.GiveUpLocation;
            g.NavSearchRadius = 40;
            g.CanUseRopes = true;
            g.TeleportTelegraphTime = 80;
            g.TeleportDustType = DustID.Smoke;
            g.TeleportDustColor = new Color(55, 55, 65);
            g.TeleportDustScale = 0.65f;
            g.TeleportDustCount = 20;
        }

        private protected override bool TryStartControllerAttack(Player target, tsorcRevampGlobalNPC globalNPC)
            => specialAttacks.TryStartForgotten(NPC, target, globalNPC);

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (!NPC.HasValidTarget) return false;
            Player target = Main.player[NPC.target];
            bool leapRange = distance >= 128f && distance <= 256f
                && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            bool preferLeap = leapRange && (target.Center.Y <= NPC.Center.Y - 32f || !CanChargeTo(target));
            if (combo.Name == LeapName) return leapRange;
            if (combo.Name == "Charging Spear Thrust" && preferLeap) return false;
            return base.CanSelectMeleeCombo(combo, distance, healthFraction);
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboTelegraphTick(combo, step, elapsed, total);
            if (combo.Name != LeapName) return;
            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.HasValidTarget)
            {
                Vector2 gap = Main.player[NPC.target].Center - NPC.Center;
                _leapAim = MathHelper.Clamp((float)Math.Atan2(gap.Y, Math.Abs(gap.X)), -0.6f, 0.6f);
                if (elapsed >= total - 1) { NPC.netUpdate = true; RequestNetworkSnapshot(); }
            }
            if (!Main.dedServ && elapsed >= total - 30 && elapsed % 2 == 0)
            {
                var dust = Dust.NewDustPerfect(PuppetSpearTipPosition, KnightGlintDust,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 80, Color.LightBlue, 1.2f);
                dust.noGravity = true;
            }
            if (elapsed == total - 1 && Main.netMode != NetmodeID.MultiplayerClient)
                tsorcRevampAIs.SpawnTelegraphFlash(NPC, Color.LightBlue);
        }

        protected override bool CanDamageWithLeapSlamStep(MeleeComboStep step, float progress)
            => progress >= 6f / 16f && progress < 1f;

        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < 20; i++)
                Dust.NewDustPerfect(NPC.Bottom, DustID.Smoke, new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(2f)),
                    100, new Color(55, 55, 65), 0.8f);
        }
        public override void AI()
        {
            base.AI();
            if (!Main.dedServ && Main.rand.NextBool(3))
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y);
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);
            if (specialAttacks.Active && specialAttacks.Timer >= 15 && specialAttacks.Timer < 45)
            {
                Vector2 aim = specialAttacks.LockedVelocity.SafeNormalize(new Vector2(NPC.direction, 0f));
                Utils.DrawLine(spriteBatch, PuppetSpearTipPosition, PuppetSpearTipPosition + aim * 320f,
                    Color.LightBlue * 0.4f, Color.LightBlue * 0f, 2f);
            }
        }
        public void OnHumanoidMeleeHit(Player target)
        {
            if (Main.rand.NextBool(2)) target.AddBuff(BuffID.Weak, 60 * 60);
        }
        public override void SendExtraAI(BinaryWriter writer) { base.SendExtraAI(writer); writer.Write(_leapAim); }
        public override void ReceiveExtraAI(BinaryReader reader) { base.ReceiveExtraAI(reader); _leapAim = reader.ReadSingle(); }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 3").Type, 1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<EphemeralThrowingSpear>(), 5, 25, 30, 2));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<EphemeralDust>(), 1, 3, 9));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ItemID.GoldenKey, 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RoyalThrowingSpear>(), 4, 25, 35));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<HealingElixir>(), 5, 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 35));
            npcLoot.Add(new CommonDrop(ItemID.HunterPotion, 100, 1, 1, 8));
            npcLoot.Add(new CommonDrop(ItemID.RegenerationPotion, 100, 1, 1, 6));
            npcLoot.Add(new CommonDrop(ItemID.ShinePotion, 100, 1, 1, 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.BattlePotion, 30));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofNight));
            npcLoot.Add(hmCondition);
        }
    }
}
