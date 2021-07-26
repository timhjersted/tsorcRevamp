using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class MarilithSpiritTwin : ModNPC
	{
		public override void SetDefaults()
		{
			npc.npcSlots = 200;
			Main.npcFrameCount[npc.type] = 8;
			npc.width = 120;
			npc.height = 160;
			npc.timeLeft = 22500;
			npc.aiStyle = 22;
			npc.damage = 60;
			npc.defense = 23;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath6;
			npc.lifeMax = 20000;
			npc.alpha = 100;
			npc.knockBackResist = 0;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.value = 50000;

			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			lightningDamage = (int)(lightningDamage / 2);
			antiMatterBlastDamage = (int)(antiMatterBlastDamage / 2);
			crazedPurpleCrushDamage = (int)(crazedPurpleCrushDamage / 2);
		}

		int lightningDamage = 38;
		int antiMatterBlastDamage = 55;
		int crazedPurpleCrushDamage = 45;

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player P = spawnInfo.player;

			if (tsorcRevampWorld.SuperHardMode) return 0;

			if (Main.hardMode && !Main.dayTime && P.ZoneJungle && P.ZoneOverworldHeight && Main.rand.Next(20005) == 1) return 1;

			if (Main.hardMode && Main.bloodMoon && P.ZoneJungle && Main.rand.Next(12525) == 1) return 1;

			if (Main.hardMode && !Main.dayTime && P.ZoneUnderworldHeight && Main.rand.Next(7030) == 1) return 1;

			if (Main.hardMode && Main.bloodMoon && P.ZoneBeach && Main.rand.Next(8020) == 1) return 1;

			if (Main.hardMode && Main.bloodMoon && P.ZoneSkyHeight && Main.rand.Next(8020) == 1) return 1;


				return 0;
		}
		#endregion

		#region AI
		public override void AI()
		{
			bool flag25 = false;
			npc.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
			if (npc.ai[1] >= 10f)
			{
				npc.TargetClosest(true);
				if ((Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height)) || !(ModContent.GetInstance<tsorcRevampConfig>().LegacyMode))
				{
					if (Main.rand.Next(70) == 1)
					{
						float num48 = 9f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 21);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 21);

						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning3Ball>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, lightningDamage, 0f);//, Main.myPlayer);
							Main.projectile[num54].timeLeft = 60;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;
						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(220) == 1)
					{
						float num48 = 8f;
						Vector2 vector8 = new Vector2((npc.position.X + ((((npc.width + 50) * 5f) * (npc.direction * 2)) / 20f) + 130), npc.position.Y + (npc.height - 75));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 21);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 21);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							//(int) (14f * npc.scale);
							int type = ModContent.ProjectileType<Projectiles.Enemy.Okiku.AntiMatterBlast>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, antiMatterBlastDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 150;
							//Main.projectile[num54].aiStyle = 9;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;
						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(20) == 1)
					{
						float num48 = 11f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-50, 50) / 100;
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-50, 50) / 100;
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							//(int) (14f * npc.scale);
							int type = ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, crazedPurpleCrushDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 150;
							//Main.projectile[num54].aiStyle = 19;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;
						}
						npc.netUpdate = true;
					}
				}
			}
			if (npc.justHit)
			{
				npc.ai[2] = 0f;
			}
			if (npc.ai[2] >= 0f)
			{
				int num258 = 16;
				bool flag26 = false;
				bool flag27 = false;
				if (npc.position.X > npc.ai[0] - (float)num258 && npc.position.X < npc.ai[0] + (float)num258)
				{
					flag26 = true;
				}
				else
				{
					if ((npc.velocity.X < 0f && npc.direction > 0) || (npc.velocity.X > 0f && npc.direction < 0))
					{
						flag26 = true;
					}
				}
				num258 += 24;
				if (npc.position.Y > npc.ai[1] - (float)num258 && npc.position.Y < npc.ai[1] + (float)num258)
				{
					flag27 = true;
				}
				if (flag26 && flag27)
				{
					npc.ai[2] += 1f;
					if (npc.ai[2] >= 30f && num258 == 16)
					{
						flag25 = true;
					}
					if (npc.ai[2] >= 60f)
					{
						npc.ai[2] = -200f;
						npc.direction *= -1;
						npc.velocity.X = npc.velocity.X * -1f;
						npc.collideX = false;
					}
				}
				else
				{
					npc.ai[0] = npc.position.X;
					npc.ai[1] = npc.position.Y;
					npc.ai[2] = 0f;
				}
				npc.TargetClosest(true);
			}
			else
			{
				npc.ai[2] += 1f;
				if (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) > npc.position.X + (float)(npc.width / 2))
				{
					npc.direction = -1;
				}
				else
				{
					npc.direction = 1;
				}
			}
			int num259 = (int)((npc.position.X + (float)(npc.width / 2)) / 16f) + npc.direction * 2;
			int num260 = (int)((npc.position.Y + (float)npc.height) / 16f);
			bool flag28 = true;
			//bool flag29; //What is this? It doesn't seem to do anything, so i'm commenting it out for now.
			int num261 = 3;
			for (int num269 = num260; num269 < num260 + num261; num269++)
			{
				if (Main.tile[num259, num269] == null)
				{
					Main.tile[num259, num269] = new Tile();
				}
				if ((Main.tile[num259, num269].active() && Main.tileSolid[(int)Main.tile[num259, num269].type]) || Main.tile[num259, num269].liquid > 0)
				{
					//	if (num269 <= num260 + 1)
					//{
					//		flag29 = true;
					//	}
					flag28 = false;
					break;
				}
			}
			if (flag25)
			{
				//	flag29 = false;
				flag28 = true;
			}
			if (flag28)
			{
				npc.velocity.Y = npc.velocity.Y + 0.1f;
				if (npc.velocity.Y > 3f)
				{
					npc.velocity.Y = 3f;
				}
			}
			else
			{
				if (npc.directionY < 0 && npc.velocity.Y > 0f)
				{
					npc.velocity.Y = npc.velocity.Y - 0.1f;
				}
				if (npc.velocity.Y < -4f)
				{
					npc.velocity.Y = -4f;
				}
			}
			if (npc.collideX)
			{
				npc.velocity.X = npc.oldVelocity.X * -0.4f;
				if (npc.direction == -1 && npc.velocity.X > 0f && npc.velocity.X < 1f)
				{
					npc.velocity.X = 1f;
				}
				if (npc.direction == 1 && npc.velocity.X < 0f && npc.velocity.X > -1f)
				{
					npc.velocity.X = -1f;
				}
			}
			if (npc.collideY)
			{
				npc.velocity.Y = npc.oldVelocity.Y * -0.25f;
				if (npc.velocity.Y > 0f && npc.velocity.Y < 1f)
				{
					npc.velocity.Y = 1f;
				}
				if (npc.velocity.Y < 0f && npc.velocity.Y > -1f)
				{
					npc.velocity.Y = -1f;
				}
			}
			float num270 = 2f;
			if (npc.direction == -1 && npc.velocity.X > -num270)
			{
				npc.velocity.X = npc.velocity.X - 0.1f;
				if (npc.velocity.X > num270)
				{
					npc.velocity.X = npc.velocity.X - 0.1f;
				}
				else
				{
					if (npc.velocity.X > 0f)
					{
						npc.velocity.X = npc.velocity.X + 0.05f;
					}
				}
				if (npc.velocity.X < -num270)
				{
					npc.velocity.X = -num270;
				}
			}
			else
			{
				if (npc.direction == 1 && npc.velocity.X < num270)
				{
					npc.velocity.X = npc.velocity.X + 0.1f;
					if (npc.velocity.X < -num270)
					{
						npc.velocity.X = npc.velocity.X + 0.1f;
					}
					else
					{
						if (npc.velocity.X < 0f)
						{
							npc.velocity.X = npc.velocity.X - 0.05f;
						}
					}
					if (npc.velocity.X > num270)
					{
						npc.velocity.X = num270;
					}
				}
			}
			if (npc.directionY == -1 && (double)npc.velocity.Y > -1.5)
			{
				npc.velocity.Y = npc.velocity.Y - 0.04f;
				if ((double)npc.velocity.Y > 1.5)
				{
					npc.velocity.Y = npc.velocity.Y - 0.05f;
				}
				else
				{
					if (npc.velocity.Y > 0f)
					{
						npc.velocity.Y = npc.velocity.Y + 0.03f;
					}
				}
				if ((double)npc.velocity.Y < -1.5)
				{
					npc.velocity.Y = -1.5f;
				}
			}
			else
			{
				if (npc.directionY == 1 && (double)npc.velocity.Y < 1.5)
				{
					npc.velocity.Y = npc.velocity.Y + 0.04f;
					if ((double)npc.velocity.Y < -1.5)
					{
						npc.velocity.Y = npc.velocity.Y + 0.05f;
					}
					else
					{
						if (npc.velocity.Y < 0f)
						{
							npc.velocity.Y = npc.velocity.Y - 0.03f;
						}
					}
					if ((double)npc.velocity.Y > 1.5)
					{
						npc.velocity.Y = 1.5f;
					}
				}
			}
			Lighting.AddLight((int)npc.position.X / 16, (int)npc.position.Y / 16, 0.4f, 0f, 0.25f);

			//The "despawn if the player dies" code was unreachable in legacy.
			if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
			{
				if (Main.player[npc.target].dead)
				{
					npc.velocity.Y += 0.20f;
					if (npc.timeLeft > 10)
					{
						npc.timeLeft = 0;
						return;
					}
				}
			}
		}
		#endregion

		#region Frames
		public override void FindFrame(int currentFrame)
			{
				int num = 1;
				if (!Main.dedServ)
				{
					num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
				}
				if (npc.velocity.X < 0)
				{
					npc.spriteDirection = -1;
				}
				else
				{
					npc.spriteDirection = 1;
				}
				npc.rotation = npc.velocity.X * 0.08f;
				npc.frameCounter += 1.0;
				if (npc.frameCounter >= 4.0)
				{
					npc.frame.Y = npc.frame.Y + num;
					npc.frameCounter = 0.0;
				}
				if (npc.frame.Y >= num * Main.npcFrameCount[npc.type])
				{
					npc.frame.Y = 0;
				}
				if (npc.ai[3] == 0)
				{
					npc.alpha = 0;
				}
				else
				{
					npc.alpha = 200;
				}
			}
		#endregion

		#region Gore
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 1"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 3"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 4"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 5"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 6"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 7"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 8"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 9"), 1f);

			if(Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.Ice3Tome>(), 1);

			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), 1);
		}
		#endregion
	}
}

