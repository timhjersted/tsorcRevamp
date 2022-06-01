using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class BlackKnight : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 28;
            NPC.aiStyle = -1;
            //npc.aiStyle = 3;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 95;
            NPC.defense = 21;
            NPC.lifeMax = 900;
            if (Main.hardMode) { NPC.lifeMax = 1400; NPC.defense = 60; }
            if (tsorcRevampWorld.SuperHardMode) { NPC.lifeMax = 3000; NPC.defense = 75; NPC.damage = 120; NPC.value = 6600; }
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            NPC.value = 5000;
            NPC.knockBackResist = 0.15f;
            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.BlackKnightBanner>();
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            spearDamage = (int)(spearDamage / 2);
        }

        public int spearDamage = 50;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.townNPCs > 1f) return 0f;
            if (!spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneDungeon && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && spawnInfo.Player.ZoneOverworldHeight && NPC.downedBoss3 && !Main.dayTime && Main.rand.Next(100) == 1) return 1;
            if (!Main.hardMode && spawnInfo.Player.ZoneMeteor && NPC.downedBoss2 && Main.rand.Next(100) == 1) return 1;
            if (Main.hardMode && spawnInfo.Player.ZoneDungeon && Main.rand.Next(100) == 1) return 1;
            if (Main.hardMode && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && !spawnInfo.Player.ZoneBeach && !Main.dayTime && Main.rand.Next(200) == 1) return 1;
            if (Main.hardMode && spawnInfo.Player.ZoneUnderworldHeight && !Main.dayTime && Main.rand.Next(60) == 1) return 1;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon && Main.rand.Next(50) == 1) return 1;

            return 0;
        }
        #endregion

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 2.6f, 0.05f, enragePercent: 0.3f, enrageTopSpeed: 3.4f);
            bool inRange = NPC.Distance(Main.player[NPC.target].Center) < 300 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0);
            tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.ai[2], 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), spearDamage, 9, inRange, true, 2, 17);

            if (NPC.ai[2] >= 150f && NPC.justHit)
            {
                NPC.ai[2] = 100f; // reset throw countdown when hit, was 150
            }
        }



        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null || spearTexture.IsDisposed)
            {
                //spearTexture = mod.GetTexture("Projectiles/Enemy/BlackKnightsSpear");
                spearTexture = (Texture2D)ModContent.Request<Texture2D>("Terraria/Projectile_508");
            }
            if (NPC.ai[2] >= 165)
            {
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (NPC.spriteDirection == -1)
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 38), NPC.scale, effects, 0); // facing left (8, 38 work)
                }
                else
                {
                    spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 38), NPC.scale, effects, 0); // facing right, first value is height, higher number is higher
                }
            }
        }


        #region Gore
        public override void OnKill()
        {
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Black Knight Gore 1").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Black Knight Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Black Knight Gore 3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Black Knight Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Black Knight Gore 3").Type, 1f);

            if (Main.rand.Next(99) < 30) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Ranged.ThrowingSpear>(), 1 + Main.rand.Next(50));
            if (Main.rand.Next(99) < 30) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Ranged.RoyalThrowingSpear>(), 1 + Main.rand.Next(50));
            if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.BootsOfHaste>(), 1);
            if (Main.rand.Next(99) < 5) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.AncientDragonLance>(), 1);
            if (Main.rand.Next(99) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.OldHalberd>(), 1);
            if (Main.rand.Next(99) < 40) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.IronskinPotion, 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GreaterHealingPotion, 1);
            if (Main.rand.Next(99) < 40) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ArcheryPotion, 1);
            if (Main.rand.Next(99) < 40) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.RegenerationPotion, 1);
        }
        #endregion
    }
}