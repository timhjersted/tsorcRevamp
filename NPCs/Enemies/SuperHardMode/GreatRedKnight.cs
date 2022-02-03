using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class GreatRedKnight : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Great Red Knight");
        }
        public override void SetDefaults()
        {
            npc.npcSlots = 5;
            Main.npcFrameCount[npc.type] = 16;
            animationType = 28;
            npc.height = 40;
            npc.width = 20;
            npc.damage = 105;
            npc.defense = 61; //was 211
            npc.lifeMax = 17000; //was 35k
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.value = 81870;
            npc.knockBackResist = 0.36f;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.GreatRedKnightOfTheAbyssBanner>();
        }

        int poisonStrikeDamage = 80;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
            poisonStrikeDamage = (int)(poisonStrikeDamage / 2);
        }


        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.player; //this shortens our code up from writing this line over and over.
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHoly;
            bool AboveEarth = spawnInfo.spawnTileY < Main.worldSurface;
            bool InBrownLayer = spawnInfo.spawnTileY >= Main.worldSurface && spawnInfo.spawnTileY < Main.rockLayer;
            bool InGrayLayer = spawnInfo.spawnTileY >= Main.rockLayer && spawnInfo.spawnTileY < (Main.maxTilesY - 200) * 16;
            bool InHell = spawnInfo.spawnTileY >= (Main.maxTilesY - 200) * 16;
            bool Ocean = spawnInfo.spawnTileX < 3600 || spawnInfo.spawnTileX > (Main.maxTilesX - 100) * 16;
            bool BeforeFourAfterSix = spawnInfo.spawnTileX < Main.maxTilesX * 0.4f || spawnInfo.spawnTileX > Main.maxTilesX * 0.6f; //Before 3/10ths or after 7/10ths width (a little wider than ocean bool?)

            // these are all the regular stuff you get , now lets see......

            if (tsorcRevampWorld.SuperHardMode && BeforeFourAfterSix && Main.bloodMoon && AboveEarth && Main.rand.Next(200) == 1)

            {
                //Main.NewText("A portal from The Abyss has been opened! ", 175, 75, 255);
                Main.NewText("A Great Red Knight of the Abyss is now hunting you...", 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && Dungeon && !Corruption && InGrayLayer && Main.rand.Next(400) == 1)

            {
                //Main.NewText("A portal from The Abyss has been opened! ", 175, 75, 255);
                Main.NewText("A Great Red Knight of the Abyss is now hunting you...", 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && Dungeon && Main.rand.Next(700) == 1)

            {
                //Main.NewText("A portal from The Abyss has been opened! ", 175, 75, 255);
                Main.NewText("A Great Red Knight of the Abyss is now hunting you...", 175, 75, 255);
                return 1;
            }

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && BeforeFourAfterSix && InHell && Main.rand.Next(200) == 1)

            {
                //Main.NewText("A portal from The Abyss has been opened!", 175, 75, 255);
                Main.NewText("A Great Red Knight of the Abyss has come to destroy you..", 175, 75, 255);
                return 1;
            }

            //if(tsorcRevampWorld.SuperHardMode && Main.rand.Next(1800)==1) 

            //	{
            //		Main.NewText("A portal from The Abyss has been opened! ", 175, 75, 255);
            //		Main.NewText("The Great Red Knight of Artorias is now hunting you...", 175, 75, 255);
            //		return true;
            //	}


            return 0;
        }

        float poisonTimer = 0;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(npc, 3, canTeleport: true, enragePercent: 0.4f, enrageTopSpeed: 6);
            tsorcRevampAIs.LeapAtPlayer(npc, 7, 4, 1.5f, 128);
            tsorcRevampAIs.SimpleProjectile(npc, ref poisonTimer, 100, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssPoisonStrikeBall>(), poisonStrikeDamage, 9, Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0));

            //TELEGRAPH DUSTS
            if (poisonTimer >= 70)
            {
                Lighting.AddLight(npc.Center, Color.YellowGreen.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.Next(6) == 1)
                {
                    Dust.NewDust(npc.position, npc.width / 2, npc.height / 2, DustID.CrystalSerpent, npc.velocity.X, npc.velocity.Y);
                    Dust.NewDust(npc.position, npc.width / 2, npc.height / 2, DustID.CursedTorch, npc.velocity.X, npc.velocity.Y); //CrystalPulse
                }
            }
        }



        public override void NPCLoot()
        {            
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 1"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 2"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 3"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 2"), 1f);
            Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Knight Gore 3"), 1f);
            
            if (Main.rand.Next(99) < 50) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.FlameOfTheAbyss>(), 1 + Main.rand.Next(1));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.RedTitanite>(), 3 + Main.rand.Next(3));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.PurgingStone>(), 1 + Main.rand.Next(1));
        }
    }
}