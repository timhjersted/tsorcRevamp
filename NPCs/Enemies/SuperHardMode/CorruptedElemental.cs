using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class CorruptedElemental : ModNPC
	{
		public override void SetDefaults()
		{
			npc.npcSlots = 1;
			npc.width = 18;
			npc.height = 40;
			animationType = 120;
			Main.npcFrameCount[npc.type] = 15;
			npc.knockBackResist = 0.1f;
			
			npc.aiStyle = 3;
			npc.timeLeft = 750;
			npc.damage = 100;
			npc.defense = 32;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath6;
			npc.lifeMax = 5200;
			npc.value = 1300;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.CorruptedElementalBanner>();

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
			if (tsorcRevampWorld.SuperHardMode)
			{
				if ((spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson) && (spawnInfo.player.position.Y / 16) < Main.rockLayer && (spawnInfo.player.position.Y / 16) < Main.maxTilesY - 200 && !spawnInfo.player.ZoneDungeon)
				{
					return 0.5f;
				}
				else return 0;
			}
			else return 0;
		}

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			target.AddBuff(13, 3600, false); //battle
			target.AddBuff(33, 3600, false); //weak
		}

		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 2.8f, 0.08f, canTeleport: true, enragePercent: 0.2f, enrageTopSpeed: 3.6f);
			tsorcRevampAIs.LeapAtPlayer(npc, 6, 5, 2, 128);
	
		}

		public override void NPCLoot()
		{
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.WhiteTitanite>(), Main.rand.Next(1, 3));
			Dust.NewDust(npc.position, npc.width, npc.height, 4, 0.3f, 0.3f, 200, default(Color), 1f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.width, npc.height, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 3f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.width, npc.height, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
		}
	}
}