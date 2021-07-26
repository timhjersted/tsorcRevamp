using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class WaterSpirit : ModNPC
	{
		public override void SetDefaults()
		{
			npc.npcSlots = 20;
			Main.npcFrameCount[npc.type] = 4;
			animationType = 60;
			npc.width = 50;
			npc.height = 50;
			npc.damage = 75;
			npc.defense = 18;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath6;
			npc.lifeMax = 1200;
			npc.scale = 1;
			npc.friendly = false;
			npc.noTileCollide = true;
			npc.lavaImmune = true;
			npc.noGravity = true;
			npc.knockBackResist = 0;
			npc.alpha = 100;
			npc.value = 1600;


			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.Confused] = true;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{

			/**
			//if(Y >= Main.rockLayer) return false; //this is for being above the grey background
			if (Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].type != 53) return 0; //this means 'if the tile you spawn on is not sand , dont spawn'
			if (Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY - 1].liquid == 0) return 0; //this means if there is no water above , don't spawn
			if (Main.rand.Next(15) == 0)
			{
				NPC.NewNPC(spawnInfo.spawnTileX * 16 + 8, spawnInfo.spawnTileY * 16, 65, 0);
			}
			if (Main.rand.Next(10) == 0)
			{
				NPC.NewNPC(spawnInfo.spawnTileX * 16 + 8, spawnInfo.spawnTileY * 16, 67, 0);
			}
			else
			{
				NPC.NewNPC(spawnInfo.spawnTileX * 16 + 8, spawnInfo.spawnTileY * 16, 64, 0);
			}
			**/
			return 0;
		}

        public override void NPCLoot()
        {
			if (Main.rand.Next(100) < 3) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.WallTome>());
		}
    }
}


