using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class Tonberry : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 28;
            NPC.knockBackResist = 0.2f;
            NPC.aiStyle = 3;
            NPC.damage = 0;
            NPC.defense = 30;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 3000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 25000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.TonberryBanner>();

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 6660;
                NPC.defense = 57;
                NPC.value = 70000;
                NPC.damage = 295;
            }
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
        }

        // (O_O;)
        int throwingKnifeDamage = 9999;


        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;
            bool InGrayLayer = spawnInfo.SpawnTileY >= Main.rockLayer && spawnInfo.SpawnTileY < (Main.maxTilesY - 200) * 16;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);

            if (spawnInfo.Water) return 0f;

            if (Main.hardMode && !FrozenOcean && Main.rand.Next(200) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && P.ZoneDungeon && Main.rand.Next(30) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && P.ZoneJungle && Main.rand.Next(75) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && InGrayLayer && Main.rand.Next(100) == 1) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && Main.rand.Next(100) == 1) return 1;

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
            if (Main.rand.Next(1000) == 1)
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
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Tonberry Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Tonberry Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Tonberry Gore 3").Type, 1f);
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.RedTitanite>(), 5 + Main.rand.Next(5));
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.WhiteTitanite>(), 5 + Main.rand.Next(5));
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.BlueTitanite>(), 5 + Main.rand.Next(5));
            }
        }
        #endregion

        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null || spearTexture.IsDisposed)
            {
                spearTexture = Mod.GetTexture("Projectiles/Enemy/EnemyThrowingKnifeSmall");
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