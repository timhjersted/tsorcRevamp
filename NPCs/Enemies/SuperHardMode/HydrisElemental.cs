using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class HydrisElemental : ModNPC
	{
		public override void SetDefaults()
		{

			npc.npcSlots = 1;
			npc.width = 18;
			npc.height = 40;
			animationType = 120;
			Main.npcFrameCount[npc.type] = 15;
			npc.knockBackResist = 0.2f;
			
			npc.aiStyle = 3;
			npc.timeLeft = 750;
			npc.damage = 100;
			npc.defense = 42;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath6;
			npc.lifeMax = 1700;
			npc.scale = 1f;
			npc.value = 1200;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.HydrisElementalBanner>();

			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
		}

		//Spawns in the Underground and Cavern before 3.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.


		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.water) return 0f;

			if (tsorcRevampWorld.SuperHardMode)
			{
				if ((spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson) && spawnInfo.player.position.Y > Main.rockLayer && spawnInfo.player.position.Y < Main.maxTilesY - 200 && !spawnInfo.player.ZoneDungeon && Main.rand.Next(1500) == 0)
				{
					return 1;
				}
				else return 0;
			}
			else return 0;
		}





		public override void OnHitPlayer(Player player, int target, bool crit) 
		{
			if (Main.rand.Next(2) == 0)
			{
				player.AddBuff(13, 1800, false); //battle
				player.AddBuff(33, 1800, false); //weak
			}

		}



		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 4.8f, 0.08f, canTeleport: true, enragePercent: 0.4f, enrageTopSpeed: 5.6f);
			tsorcRevampAIs.LeapAtPlayer(npc, 6, 5, 2, 128);
		}

		public override void NPCLoot()
		{

			Dust.NewDust(npc.position, npc.width, npc.height, 4, 0.3f, 0.3f, 200, default(Color), 1f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.width, npc.height, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 3f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.width, npc.height, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);

			if (Main.rand.Next(99) < 40) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DyingWindShard>());
		}
	}
}