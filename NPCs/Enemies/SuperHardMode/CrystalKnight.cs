using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class CrystalKnight : ModNPC
	{
		public override void SetDefaults()
		{
			npc.npcSlots = 2;
			Main.npcFrameCount[npc.type] = 20;
			animationType = 110;
			npc.width = 18;
			npc.height = 48;
			npc.timeLeft = 750;
			npc.damage = 125;
			npc.defense = 30;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.lavaImmune = true;
			npc.lifeMax = 8000;
			npc.scale = 0.9f;
			npc.knockBackResist = 0;
			npc.value = 7930;

			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.CrystalKnightBanner>();
		}

		int crystalBoltDamage = 43;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player player = spawnInfo.player;
			bool FrozenOcean = spawnInfo.spawnTileX > (Main.maxTilesX - 800);

			// these are all the regular stuff you get , now lets see......
			float chance = 0;

			if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneOverworldHeight && (FrozenOcean || player.ZoneHoly))
			{
				chance = 0.2f; 
			}
			if (tsorcRevampWorld.SuperHardMode && !spawnInfo.player.ZoneOverworldHeight && (FrozenOcean || player.ZoneHoly))
			{
				chance = 0.36f; 
			}
			if (FrozenOcean && player.ZoneHoly)
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
			tsorcRevampAIs.ArcherAI(npc, ModContent.ProjectileType<Projectiles.Enemy.EnemyCrystalKnightBolt>(), crystalBoltDamage, 14, 70, 2, 0.07f, canTeleport: true, lavaJumping: true);
		}


		public override void NPCLoot()
		{
			Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
			if (npc.life <= 0)
			{
				Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Crystal Knight Gore 1"), 1.1f);
				Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Crystal Knight Gore 2"), 1.1f);
				Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Crystal Knight Gore 2"), 1.1f);
				Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Crystal Knight Gore 2"), 1.1f);

				for (int num36 = 0; num36 < 50; num36++)
				{
					{
						Color color = new Color();
						int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 1f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 1f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 1f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 1f);
						Main.dust[dust].noGravity = true;

						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 2f);
						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
						Dust.NewDust(npc.position, npc.height, npc.width, 14, 0.2f, 0.2f, 100, default(Color), 1f);
					}


				}

				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.BlueTitanite>(), 3 + Main.rand.Next(2));
			}
		}
		
	}
}