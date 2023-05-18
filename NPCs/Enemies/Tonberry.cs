using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies
{
    class Tonberry : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = 28;
            NPC.knockBackResist = 0.2f;
            NPC.aiStyle = 3;
            NPC.damage = 0;
            NPC.defense = 30;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 1500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 25000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.TonberryBanner>();

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 3360;
                NPC.defense = 57;
                NPC.value = 70000;
                NPC.damage = 0;
            }
        }
        // (O_O;) //I haven't played that game so wtf is this
        int throwingKnifeDamage = 9999;


        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;
            bool InGrayLayer = spawnInfo.SpawnTileY >= Main.rockLayer && spawnInfo.SpawnTileY < (Main.maxTilesY - 200) * 16;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);

            if (spawnInfo.Water) return 0f;

            if (Main.hardMode && !FrozenOcean && Main.rand.NextBool(200)) return 1;

            if (tsorcRevampWorld.SuperHardMode && P.ZoneDungeon && Main.rand.NextBool(30)) return 1;

            if (tsorcRevampWorld.SuperHardMode && P.ZoneJungle && Main.rand.NextBool(75)) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && InGrayLayer && Main.rand.NextBool(100)) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && Main.rand.NextBool(100)) return 1;

            return 0;
        }
        #endregion

        float knifeTimer = 0;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1f, 0.07f, 0.5f, lavaJumping: true);

            bool clearShot = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height) && Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) <= 500;
            tsorcRevampAIs.SimpleProjectile(NPC, ref knifeTimer, 180, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnifeSmall>(), throwingKnifeDamage, 8, clearShot, shootSound: SoundID.Item17);

            //play creature sounds
            if (Main.rand.NextBool(1000))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit55 with { Volume = 0.3f, Pitch = -0.7f }, NPC.Center); // cultist
            }

            Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.5f, 0.4f, 0.4f);
        }

        #region Gore
        public override void OnKill()
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tonberry Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tonberry Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tonberry Gore 3").Type, 1f);
                }
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            IItemDropRule drop = ItemDropRule.Common(ModContent.ItemType<Items.RedTitanite>(), 1, 5, 10);
            IItemDropRule drop2 = ItemDropRule.Common(ModContent.ItemType<Items.WhiteTitanite>(), 1, 5, 10);
            IItemDropRule drop3 = ItemDropRule.Common(ModContent.ItemType<Items.BlueTitanite>(), 1, 5, 10);

            SuperHardmodeRule SHM = new();
            IItemDropRule condition = new LeadingConditionRule(SHM);

            condition.OnSuccess(drop);
            condition.OnSuccess(drop2);
            condition.OnSuccess(drop3);
            npcLoot.Add(condition);

            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofLight, 1, 3, 6));
            npcLoot.Add(hmCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 1, 2));
        }
        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null || spearTexture.IsDisposed)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemyThrowingKnifeSmall");
            }
            if (knifeTimer >= 120)
            {
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(17, 18), NPC.scale, effects, 0); //was 24, 48
                }
                else
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(-7, 18), NPC.scale, effects, 0); //was -4, 48
                }
            }
        }
    }
}