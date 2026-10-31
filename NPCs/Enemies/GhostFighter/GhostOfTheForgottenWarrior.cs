using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.ForgottenWarriorSet;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Tools;
using tsorcRevamp.Content.Items.Weapons.Throwing;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.GhostFighter
{
    public class GhostOfTheForgottenWarrior : PuppetNPC
    {
        const int RecallTellTicks = 40;
        bool _axeOut;
        int _axeIndex = -1;
        int _axeIdentity = -1;
        int _recallCooldown;
        Vector2 _recallTarget;
        protected override string InvaderTitle => "Ghost of the Forgotten Warrior";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override int HeadArmorItemType => ModContent.ItemType<ForgottenWarriorHelm>();
        protected override int BodyArmorItemType => ModContent.ItemType<ForgottenWarriorArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<ForgottenWarriorGreaves>();
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyEphemeralAxe>();
        protected override int RangedWeaponItemType => -1;
        public int WarriorDamage => tsorcRevampWorld.SuperHardMode ? 200 : Main.hardMode ? 120 : 80;
        protected override int MeleeDamage => EnemyDamage.Projectile(WarriorDamage);
        protected override int RangedDamage => 0;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Axe;
        protected override float TopSpeed => tsorcRevampWorld.SuperHardMode ? 1.8f : Main.hardMode ? 1.1f : 0.8f;
        protected override float PuppetJumpPower => 9f;
        protected override float PuppetJumpBoost => 5f;
        protected override bool AllowReactiveDefense => true;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override bool WeaponSheathed => _axeOut;
        protected override bool HoldAttackSelection => _axeOut;
        protected override bool SuppressGhostWallTeleport => _axeOut || base.SuppressGhostWallTeleport;
        protected override float MeleeRange => 0f;
        protected override float MeleeEngageRange => 64f;
        protected override float ComboMaxStartRange => 64f;
        protected override float RangedStartComboMaxRange => 144f;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 8;
        // 26x60 axe: butt (6,59), grip (7,52), head tip (8,0). 52px reach, 1:1 draw scale.
        protected override Vector2 MeleeHandleNorm => new Vector2(7f / 26f, 52f / 60f);
        protected override float MeleeWeaponDrawScale => 1f;
        protected override float MeleeWeaponRotationOffset => MathHelper.PiOver4;
        protected override float MeleeNaturalRestAngleDeg => 90f;
        protected override bool PreserveShaftDirectionOnBladeFlip => true;
        protected override float ComboReachBase => 75f;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override int PhaseTelegraphMinimumTicks => 20;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 6;
        protected override float LogicalWindupSettleFraction => 0.4f;
        protected override bool UseLandingTimedLeapSlam => true;
        protected override float LeapAttackUpSpeed => 5.4f;
        protected override float LeapAttackForwardSpeed => 6f;
        protected override float LeapAttackMinimumForwardSpeed => 0f;
        protected override float LeapMinLungeDistance => 0f;
        protected override float LeapLandingStandoff => -40f;
        protected override float LeapSlamCarryRotation => -1.62f;
        protected override float LeapSlamImpactRotation => 2.20f;
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


        // Ghost Hew: 219-degree envelope / ~175 live; tell 26, Weighted 10/40 k6,
        // peak ~22 degrees/t, live 18t < a 22t roll. Step 2 starts 46t after step 1 disarms.
        // Shared endpoints, 14t pause, 3t linger; final harmless tail 32 + recovery 34 = 66t punish.
        static MeleeComboStep Hew(ComboMotion motion, float damage, int pause) => new MeleeComboStep
        {
            Motion = motion, TelegraphTicks = 26, AttackTicks = 50, PostStepPause = pause,
            DamageMult = damage, ReachMult = 1f, ForwardPushMult = 0.5f, SwingSpeedMult = 1f,
            Ease = SwingEaseStyle.Weighted, EaseInTicks = 10, EaseOutTicks = 40, EaseOutDecay = 6f,
            HitWindowEnd = 0.36f,
        };
        static readonly MeleeCombo[] Combos = {
            new MeleeCombo { Name = "Ghost Hew", BaseWeight = 100, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Orange, CooldownAfterUse = 90, RecoveryTicks = 34,
                Steps = new[] { Hew(ComboMotion.OverheadArc, 1f, 14), Hew(ComboMotion.UnderhandArc, 1.15f, 0) } },
            // 36t tell, ~36t flat-ground flight, final 10t descent strike, 42t recovery.
            // 64/104/144px + 40 overshoot requires 2.9/4.0/5.1 px/t (< 6 cap).
            new MeleeCombo { Name = "Phantom Leap Cleave", BaseWeight = 100, Preferred = ComboRangeBand.Mid,
                RangedStartOnly = true, InitialFlashColor = Color.Orange, CooldownAfterUse = 240, RecoveryTicks = 42,
                Steps = new[] { new MeleeComboStep { Motion = ComboMotion.LeapSlam, TelegraphTicks = 36,
                    AttackTicks = 90, DamageMult = 1.3f, ReachMult = 1f, SwingSpeedMult = 1f,
                    LeapHeightMult = 1f, LeapForwardSpeedMult = 1f } } }
        };
        protected override MeleeCombo[] MeleeComboPoolOverride => Combos;
        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float start, ref float end)
        {
            if (motion == ComboMotion.OverheadArc) { start = -1.62f; end = 2.20f; }
            if (motion == ComboMotion.UnderhandArc) { start = 2.20f; end = -1.62f; }
        }
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
            => !_axeOut && (combo.Name == "Ghost Hew" ? distance <= 64f : distance >= 64f && distance <= 144f);
        protected override bool CanDamageWithMeleeStep(MeleeComboStep step, float progress)
            => progress < 0.36f;
        protected override bool ShouldContinueMeleeCombo(string name, int next, Player target, bool previousHit)
            => target.active && !target.dead && Math.Sign(target.Center.X - NPC.Center.X) == NPC.direction
                && NPC.Distance(target.Center) <= 1.5f * ComboReachBase * 0.7f;
        protected override void DoMeleeAttack() { }
        protected override void DoRangedAttack() { }
        public Vector2 RecallHandPosition => PuppetHandPosition;
        Vector2 AxeHeadPosition => PuppetHandPosition + PuppetWeaponDirection * 52f * NPC.scale;
        protected override float? CustomWeaponRotation => MathHelper.SmoothStep(-0.3f, -1.62f,
            (RecallTellTicks - PhaseTimer) / 24f);
        protected override bool SlowDownDuringCustom => true;
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom;

        public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 1;
        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 40;
            NPC.damage = 0;
            NPC.lifeMax = 100;
            NPC.defense = 10;
            NPC.value = 500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0.3f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GhostOfTheForgottenWarriorBanner>();
            if (Main.hardMode)
            {
                NPC.lifeMax = 200;
                NPC.defense = 40;
                NPC.value = 1000;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1000;
                NPC.defense = 90;
                NPC.value = 4000;
            }
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.CanPassThroughWalls = true;
            EvasiveProfile.Ghost(g);
            g.EvasiveRetreatJump = false;
            g.EvasiveRetreatDash = false;
            g.EvasiveOnHitChanceDenominator = 6;
            g.EvasiveOnHitCooldownTicks = 120;
            g.KiteRangeMax = 0;
            g.HasGhostAfterimages = false;
            g.MaxJumpPower = 9f;
            g.MaxJumpBoost = 5f;
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
        protected override void RunMovementAI(float speedMult)
            => SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * speedMult, acceleration: 0.07f,
                doorBreakingDamage: 3, attackRange: 700f);
        public override void AI()
        {
            if (_recallCooldown > 0) _recallCooldown--;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (_axeOut && !HasRecallAxe()) FinishRecall();
                bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
                var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
                if (neutral && !_axeOut && _recallCooldown == 0 && NPC.HasValidTarget && NPC.velocity.Y == 0f
                    && g.StaggerTimer == 0 && g.DodgeTimer == 0 && g.QuickStepTimer == 0 && g.QuickStepRecoveryTimer == 0 && g.TeleportCountdown == 0 && !InFinisherPunishWindow)
                {
                    Player target = Main.player[NPC.target];
                    float distance = NPC.Distance(target.Center);
                    if (distance >= 96f && distance <= 320f && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
                    {
                        NPC.direction = target.Center.X >= NPC.Center.X ? 1 : -1;
                        NPC.spriteDirection = NPC.direction;
                        _recallTarget = target.Center;
                        _recallCooldown = Main.rand.Next(300, 421);
                        StartCustomAttack(RecallTellTicks, MeleeWeaponItemType);
                        RequestNetworkSnapshot();
                    }
                }
            }
            base.AI();
            // This ordinary fighter has no hyper armor, even during its swings. Keep evasion out of attacks.
            var global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            global.AttackCommitted = false;
            global.AttackTelegraphing = Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll && Phase != AttackPhase.ClosingDistance;
            global.SuppressGhostWallTeleport |= _axeOut;
            if (!Main.dedServ && Main.rand.NextBool(3))
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y);
            if (_axeOut) DebugAttackLabel = "Spectral Recall — unarmed";
        }
        protected override void DoCustomTick(int remaining)
        {
            int elapsed = RecallTellTicks - remaining;
            if (elapsed == 0 && !Main.dedServ) SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
            if (elapsed <= 30 && Main.netMode != NetmodeID.MultiplayerClient && NPC.HasValidTarget)
            {
                _recallTarget = Main.player[NPC.target].Center;
                if (elapsed == 30) { tsorcRevampAIs.SpawnTelegraphFlash(NPC, Color.Orange); RequestNetworkSnapshot(); }
            }
            GatherMotes(3);
            if (remaining == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 origin = AxeHeadPosition;
                Vector2 gap = _recallTarget - origin;
                float distance = Math.Min(560f, gap.Length() + 200f);
                _axeIndex = Projectile.NewProjectile(NPC.GetSource_FromAI(), origin,
                    gap.SafeNormalize(new Vector2(NPC.direction, 0f)) * 14f,
                    ModContent.ProjectileType<ForgottenWarriorRecallAxe>(), EnemyDamage.Projectile((int)(WarriorDamage * 0.9f)),
                    3f, Main.myPlayer, NPC.whoAmI, 0f, distance);
                if (_axeIndex < Main.maxProjectiles)
                {
                    _axeOut = true; _axeIdentity = Main.projectile[_axeIndex].identity;
                }
                RequestNetworkSnapshot();
            }
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);
            if (Phase == AttackPhase.Custom && !_axeOut)
            {
                Vector2 origin = AxeHeadPosition;
                Vector2 aim = (_recallTarget - origin).SafeNormalize(new Vector2(NPC.direction, 0f));
                float opacity = PhaseTimer <= 10 ? 0.55f : 0.25f;
                Utils.DrawLine(spriteBatch, origin, origin + aim * 320f, Color.Orange * opacity, Color.Orange * 0f, 2f);
            }
        }
        bool HasRecallAxe() => _axeIndex >= 0 && _axeIndex < Main.maxProjectiles
            && Main.projectile[_axeIndex].active && Main.projectile[_axeIndex].identity == _axeIdentity
            && Main.projectile[_axeIndex].type == ModContent.ProjectileType<ForgottenWarriorRecallAxe>()
            && Main.projectile[_axeIndex].ai[0] == NPC.whoAmI;
        public bool OwnsRecallAxe(Projectile axe) => _axeOut && axe.identity == _axeIdentity && axe.ai[0] == NPC.whoAmI;
        public void FinishRecall()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            _axeOut = false; _axeIndex = -1; _axeIdentity = -1; DebugAttackLabel = null;
            NPC.netUpdate = true; RequestNetworkSnapshot();
        }
        void GatherMotes(int count)
        {
            if (Main.dedServ) return;
            Vector2 tip = AxeHeadPosition;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(10f, 10f);
                Dust mote = Dust.NewDustPerfect(tip + offset, DustID.Torch, -offset * 0.15f, 80, Color.Orange, 1.1f);
                mote.noGravity = true;
            }
        }
        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            GatherMotes(2);
            if (elapsed == 0 && !Main.dedServ) SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
        }
        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < 28; i++)
                Dust.NewDustPerfect(NPC.Bottom, DustID.Smoke, new Vector2(Main.rand.NextFloat(-4f, 4f), -Main.rand.NextFloat(2f)),
                    100, new Color(55, 55, 65), 0.8f);
            UsefulFunctions.ScreenShake(NPC.Bottom, 2f, 8, distanceFalloff: 500f);
        }
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        { base.OnHitByItem(player, item, hit, damageDone); tsorcRevampAIs.EvasiveOnHit(NPC, true); }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        { base.OnHitByProjectile(projectile, hit, damageDone); tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee); }
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer); writer.Write(_axeOut); writer.Write(_axeIndex); writer.Write(_axeIdentity);
            writer.Write(_recallCooldown); writer.Write(_recallTarget.X); writer.Write(_recallTarget.Y);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader); _axeOut = reader.ReadBoolean(); _axeIndex = reader.ReadInt32(); _axeIdentity = reader.ReadInt32();
            _recallCooldown = reader.ReadInt32(); _recallTarget = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }


        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 3").Type, 1f);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ItemID.GoldenKey, 10));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<EphemeralDust>(), 1, 2, 4));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<EphemeralThrowingSpear>(), 10, 15, 26));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<GreatMagicShieldScroll>(), 30));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofNight));
            npcLoot.Add(hmCondition);
        }
    }
}
