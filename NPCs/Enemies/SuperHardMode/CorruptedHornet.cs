using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class CorruptedHornet : ModNPC
	{
		public override void SetDefaults()
		{

			Main.npcFrameCount[npc.type] = 3;
			npc.npcSlots = 1;
			animationType = 42;
			aiType = 42;
			npc.width = 34;
			npc.height = 12;
			npc.knockBackResist = .3f;
			npc.value = 1130;
			npc.aiStyle = 5;
			npc.timeLeft = 750;
			npc.damage = 95;
			npc.defense = 100;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.noGravity = true;
			npc.lifeMax = 1811;
			npc.scale = 1;
		}

		int cursedFlameDamage = 60;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			cursedFlameDamage = (int)(cursedFlameDamage / 2);
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (tsorcRevampWorld.SuperHardMode)
			{
				if (spawnInfo.player.position.Y > Main.rockLayer && spawnInfo.player.position.Y < Main.maxTilesY - 200 && (Main.evilTiles > 20 || Main.jungleTiles > 20) && !spawnInfo.player.ZoneDungeon && Main.rand.Next(5) == 0)
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
				player.AddBuff(31, 180, false); //confused!
			}
		}


		public override void AI()
		{
			npc.ai[3]++;
			if (npc.ai[3] >= 200) //200 was 240
			{
				if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
				{
					float num87 = 8f;
					Vector2 vector12 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)(npc.height / 2));
					float num88 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - vector12.X + (float)Main.rand.Next(-20, 21);
					float num89 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - vector12.Y + (float)Main.rand.Next(-20, 21);
					if ((num88 < 0f && npc.velocity.X < 0f) || (num88 > 0f && npc.velocity.X > 0f))
					{
						float num90 = (float)Math.Sqrt((double)(num88 * num88 + num89 * num89));
						num90 = num87 / num90;
						num88 *= num90;
						num89 *= num90;
						int num93 = Projectile.NewProjectile(vector12.X, vector12.Y, num88, num89, ProjectileID.CursedFlameHostile, cursedFlameDamage, 0f, Main.myPlayer);
						Main.projectile[num93].timeLeft = 300;
						npc.ai[3] = 101f;
						npc.netUpdate = true;
					}
					else
					{
						npc.ai[3] = 0f;
					}
				}
				npc.ai[3] = 0;
			}
		}
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Corrupt Hornet Gore 1"), 1.1f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Corrupt Hornet Gore 2"), 1.1f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 100, default(Color), 1f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 100, default(Color), 1f);
			Dust.NewDust(npc.position, npc.height, npc.width, 4, 0.2f, 0.2f, 100, default(Color), 1f);
			if (Main.rand.Next(99) < 50) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.FlameOfTheAbyss>());
		}
	}
}