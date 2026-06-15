using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.NPCs.Enemies
{
    class GhostOfTheForgottenKnight : ModNPC
    {
        public int spearDamage = 15;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            AnimationType = 28;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 30;
            NPC.defense = 22;
            NPC.lifeMax = 150;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            NPC.value = 750; //was 45
            NPC.knockBackResist = 0.0f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GhostOfTheForgottenKnightBanner>();
            if (Main.hardMode)
            {
                NPC.lifeMax = 200;
                NPC.defense = 32;
                NPC.value = 1000; // was 65
                NPC.damage = 50;
                spearDamage = 25;
                topSpeed = 1.5f;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1000;
                NPC.defense = 80;
                NPC.damage = 50;
                NPC.value = 4000; //was 100
                spearDamage = 45;
                topSpeed = 2f;
            }

            UsefulFunctions.AddAttack(NPC, 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), 20, 8, SoundID.Item17, stopBeforeFiring: false);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.CanPassThroughWalls = true;
            globalNPC.HasGhostAfterimages = true;
            globalNPC.MaxJumpPower = 10f;
            globalNPC.MaxJumpBoost = 6f;
            // Step 6 ghost levers: drift (Wander) around where it lost the player when it gives up.
            globalNPC.PatrolMode = NPCs.PatrolMode.Wander;
            globalNPC.PatrolAnchorSource = NPCs.PatrolAnchorSource.GiveUpLocation;
            globalNPC.NavSearchRadius = 40; // Phase 2: SmartFighter4AI movement
            globalNPC.CanUseRopes = true;
            globalNPC.TeleportTelegraphTime = 80;
            globalNPC.TeleportDustType = DustID.Smoke;
            globalNPC.TeleportDustColor = new Color(55, 55, 65);
            globalNPC.TeleportDustScale = 0.65f;
            globalNPC.TeleportDustCount = 20;
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {

            if (!Main.hardMode && NPC.downedBoss3 && spawnInfo.Player.ZoneDungeon)
            {
                return 0.16f; // was .2 : .16 should be 8%
            }
            if (Main.hardMode && spawnInfo.Player.ZoneDungeon)
            {
                return 0.1f; // was 0.17
            }
            if (spawnInfo.Player.ZoneGraveyard)
            {
                return 0.2f; // was 0.17
            }
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon)
            {
                return 0.05f; //.08% is 4.28%
            }

            return 0;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Weak, 60 * 60, false);
            }
        }

        float topSpeed = 1.2f;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, topSpeed, .05f, 0.2f, false, enragePercent: 0.2f, enrageTopSpeed: 2.4f);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            bool hasLos = Collision.CanHit(NPC.position, NPC.width, NPC.height,
                Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
            if (!hasLos && globalNPC.ProjectileTimer > 0f)
            {
                globalNPC.ProjectileTimer = 0f;
                NPC.netUpdate = true;
            }

            if (NPC.justHit && globalNPC.ProjectileTimer <= 149 && Main.rand.NextBool(4))
            {
                globalNPC.ProjectileTimer = 0f;
                NPC.netUpdate = true;
            }

            if (globalNPC.ProjectileTimer >= 150)
            {
                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y);
                }
            }
        }

        #region Draw Spear Texture
        public static Texture2D spearTexture;
        static Texture2D handTexture;
        const float FrameW = 70f;
        const float FrameH = 56f;
        static readonly Vector2[] HandPixel = new Vector2[16]
        {
            new Vector2(47, 30), // 0 idle
            new Vector2(47, 25), // 1 jump
            new Vector2(48, 33), // 2
            new Vector2(50, 31), // 3
            new Vector2(50, 31), // 4
            new Vector2(50, 31), // 5
            new Vector2(50, 33), // 6
            new Vector2(48, 33), // 7
            new Vector2(48, 33), // 8
            new Vector2(48, 33), // 9
            new Vector2(46, 31), // 10
            new Vector2(44, 31), // 11
            new Vector2(44, 31), // 12
            new Vector2(45, 32), // 13
            new Vector2(48, 33), // 14
            new Vector2(48, 33), // 15
        };
        static readonly Vector2 SpearGripOrigin = new Vector2(8f, 38f);

        Vector2 CurrentHandWorld()
        {
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            if (frame < 0 || frame >= HandPixel.Length)
            {
                frame = 0;
            }

            Vector2 fp = HandPixel[frame];
            float x = NPC.Center.X + (fp.X - FrameW / 2f) * NPC.scale * -NPC.spriteDirection;
            float y = NPC.Center.Y + 24f + NPC.gfxOffY + (fp.Y - FrameH) * NPC.scale;
            return new Vector2(x, y);
        }

        Vector2 CurrentSpearWorld()
        {
            Vector2 handWorld = CurrentHandWorld();
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            if (frame == 0)
            {
                handWorld.Y -= 21f;
            }
            return handWorld;
        }

        void DrawHandOverlay(SpriteBatch spriteBatch, Color drawColor)
        {
            if (handTexture == null)
            {
                return;
            }

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH);
            Vector2 drawPosition = NPC.Center + new Vector2(0f, 24f + NPC.gfxOffY) - Main.screenPosition;
            spriteBatch.Draw(handTexture, drawPosition, sourceRectangle, drawColor, NPC.rotation, new Vector2(FrameW / 2f, FrameH), NPC.scale, effects, 0f);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null || spearTexture.IsDisposed)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/BlackKnightGhostSpear");
            }
            if (handTexture == null || handTexture.IsDisposed)
            {
                handTexture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/GhostOfTheForgottenKnight_Hand", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 150)
            {
                Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 0.3f);

                float rotation = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1).ToRotation() + MathHelper.PiOver2;
                spriteBatch.Draw(spearTexture, CurrentSpearWorld() - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, rotation, SpearGripOrigin, NPC.scale * 0.8f, SpriteEffects.None, 0);
                DrawHandOverlay(spriteBatch, drawColor);
            }
        }
        #endregion

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
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<Items.Weapons.Throwing.EphemeralThrowingSpear>(), 5, 25, 30, 2));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<EphemeralDust>(), 1, 3, 9));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ItemID.GoldenKey, 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Throwing.RoyalThrowingSpear>(), 4, 25, 35));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<Items.Potions.HealingElixir>(), 5, 1, 1, 2));
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
