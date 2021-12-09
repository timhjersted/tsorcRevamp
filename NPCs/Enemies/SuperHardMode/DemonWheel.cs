using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class DemonWheel : ModNPC
    {
        public override void SetDefaults()
        {
            npc.width = 34;
            npc.height = 34;
            npc.aiStyle = 21;
            Main.npcFrameCount[npc.type] = 8;
            npc.timeLeft = 750;
            aiType = 72;
            npc.damage = 120;
            npc.defense = 1000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.lifeMax = 1000;
            npc.alpha = 100;
            npc.scale = 1.2f;
            npc.noGravity = true;
            npc.behindTiles = true;
            npc.dontTakeDamage = true;
            	
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
        }
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.damage = (int)(npc.damage / 2);
        }

        #region Spawn
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

            // these are all the regular stuff you get , now lets see......

            if (Dungeon && tsorcRevampWorld.SuperHardMode && Main.rand.Next(10) == 1) return 1;

            if (Corruption && tsorcRevampWorld.SuperHardMode && !AboveEarth && Main.rand.Next(100) == 1) return 1;

            if (InHell && tsorcRevampWorld.SuperHardMode && Main.rand.Next(30) == 1) return 1;


            return 0;
        }
        #endregion

        public override void AI()
        {

            float red = 1.0f;
            float green = 0.0f;
            float blue = 1.0f;

            Lighting.AddLight((int)((npc.position.X + (float)(npc.width / 2)) / 16f), (int)((npc.position.Y + (float)(npc.height / 2)) / 16f), red, green, blue);

        }

    }
}