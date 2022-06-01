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
			NPC.npcSlots = 1;
			NPC.width = 18;
			NPC.height = 40;
			AnimationType = 120;
			Main.npcFrameCount[NPC.type] = 15;
			NPC.knockBackResist = 0.1f;
			
			NPC.aiStyle = 3;
			NPC.timeLeft = 750;
			NPC.damage = 100;
			NPC.defense = 32;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath6;
			NPC.lifeMax = 5200;
			NPC.value = 1300;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.CorruptedElementalBanner>();

			NPC.buffImmune[BuffID.Poisoned] = true;
			NPC.buffImmune[BuffID.OnFire] = true;
			NPC.buffImmune[BuffID.CursedInferno] = true;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			NPC.damage = (int)(NPC.damage / 2);
		}
		//Spawns in the Underground and Cavern before 3.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.


		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (tsorcRevampWorld.SuperHardMode)
			{
				if ((spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && (spawnInfo.Player.position.Y / 16) < Main.rockLayer && (spawnInfo.Player.position.Y / 16) < Main.maxTilesY - 200 && !spawnInfo.Player.ZoneDungeon)
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
			tsorcRevampAIs.FighterAI(NPC, 2.8f, 0.08f, canTeleport: true, enragePercent: 0.2f, enrageTopSpeed: 3.6f);
			tsorcRevampAIs.LeapAtPlayer(NPC, 6, 5, 2, 128);
	
		}

		public override void OnKill()
		{
			Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.WhiteTitanite>(), Main.rand.Next(1, 3));
			Dust.NewDust(NPC.position, NPC.width, NPC.height, 4, 0.3f, 0.3f, 200, default(Color), 1f);
			Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(NPC.position, NPC.width, NPC.height, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 3f);
			Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(NPC.position, NPC.width, NPC.height, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
			Dust.NewDust(NPC.position, NPC.height, NPC.width, 4, 0.2f, 0.2f, 200, default(Color), 2f);
		}
	}
}