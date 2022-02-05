using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class DarkKnight : ModNPC
	{
		public override void SetDefaults()
		{
			npc.npcSlots = 2;
			Main.npcFrameCount[npc.type] = 20;
			animationType = 110;
			npc.width = 18;
			npc.height = 48;

			npc.timeLeft = 750;
			npc.damage = 105;
			npc.lavaImmune = true;
			npc.defense = 30;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.lifeMax = 7000;
			npc.knockBackResist = 0f;
			npc.value = 3680;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.DarkKnightBanner>();
		}

		int stormWaveDamage = 35;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			stormWaveDamage = (int)(stormWaveDamage * tsorcRevampWorld.SubtleSHMScale);
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player player = spawnInfo.player;
			bool FrozenOcean = spawnInfo.spawnTileX > (Main.maxTilesX - 800);
			bool Ocean = spawnInfo.spawnTileX < 800 || FrozenOcean;

			// these are all the regular stuff you get , now lets see......
			float chance = 0;
			if (tsorcRevampWorld.SuperHardMode && player.townNPCs < 1f && (player.ZoneCorrupt || player.ZoneCrimson || player.ZoneDungeon) && !player.ZoneMeteor && !player.ZoneJungle && !player.ZoneUnderworldHeight && !player.ZoneHoly && !Ocean)
			{
				chance = 0.2f;
			}
			if (!Main.dayTime)
			{
				chance *= 2;
			}
			if (Main.bloodMoon)
			{
				chance *= 2;
			}		


			return chance;
		}


		public override void AI()
		{
			tsorcRevampAIs.ArcherAI(npc, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellAbyssStormWave>(), stormWaveDamage, 14, 80, 1.4f, 0.04f, 0.04f, true, lavaJumping: true);
		}

		#region Gore
		public override void NPCLoot()
		{
			Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
			if (npc.life <= 0)
			{
				for (int num36 = 0; num36 < 50; num36++)
				{
					{
						Color color = new Color();
						int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 2f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 2f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 3f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
						Main.dust[dust].noGravity = false;

						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 2f);
						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
					}					
				}
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.WhiteTitanite>(), 3 + Main.rand.Next(2));
			}
		}
		#endregion
	}
}