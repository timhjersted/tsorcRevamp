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

        //Super high because they seem to like spawning, rolling out of range, and instantly despawning
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.player;

            if (player.ZoneDungeon && tsorcRevampWorld.SuperHardMode) return 1;

            if (player.ZoneDungeon && Main.hardMode) return 0.5f;

            if ((player.ZoneCorrupt || player.ZoneCrimson) && tsorcRevampWorld.SuperHardMode) return 1;

            if (player.ZoneUnderworldHeight && tsorcRevampWorld.SuperHardMode) return 0.5f;


            return 0;
        }

        public override void AI()
        {

            float red = 1.0f;
            float green = 0.0f;
            float blue = 1.0f;

            Lighting.AddLight((int)((npc.position.X + (float)(npc.width / 2)) / 16f), (int)((npc.position.Y + (float)(npc.height / 2)) / 16f), red, green, blue);

        }

    }
}