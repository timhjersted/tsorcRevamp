using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class GhostoftheForgottenKnight : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Ghost of the Forgotten Knight");

		}
		public override void SetDefaults()
		{
			npc.npcSlots = 3;
			Main.npcFrameCount[npc.type] = 16;
			animationType = 28;
			npc.aiStyle = 3;
			npc.height = 40;
			npc.width = 20;
			npc.damage = 60;
			npc.defense = 26;
			npc.lifeMax = 300;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.lavaImmune = true;
			npc.value = 650;
			npc.knockBackResist = 0;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			spearDamage = (int)(spearDamage / 2);
		}

		int spearDamage = 30;

		float customAi1;

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!Main.hardMode && spawnInfo.player.ZoneDungeon && Main.rand.Next(2) == 0) return 1;
			if (Main.hardMode && spawnInfo.player.ZoneDungeon && Main.rand.Next(20) == 0) return 1;
			if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneDungeon && Main.rand.Next(100) == 0) return 1;

			return 0;
		}

		#endregion

		#region debuff
		public override void OnHitPlayer(Player player, int damage, bool crit)
		{
			if (Main.rand.Next(2) == 0)
			{
				player.AddBuff(33, 3600, false); //weak
			}
		}
		#endregion

		#region AI
		public override void AI()
		{
			int num3 = 60;
			bool flag2 = false;
			int num5 = 60;
			bool flag3 = true;
			if (npc.velocity.Y == 0f && ((npc.velocity.X > 0f && npc.direction < 0) || (npc.velocity.X < 0f && npc.direction > 0)))
			{
				flag2 = true;
			}
			if (npc.position.X == npc.oldPosition.X || npc.ai[3] >= (float)num5 || flag2)
			{
				npc.ai[3] += 1f;
			}
			else
			{
				if ((double)Math.Abs(npc.velocity.X) > 0.9 && npc.ai[3] > 0f)
				{
					npc.ai[3] -= 1f;
				}
			}
			if (npc.ai[3] > (float)(num5 * 10))
			{
				npc.ai[3] = 0f;
			}
			if (npc.justHit)
			{
				npc.ai[3] = 0f;
			}
			if (npc.ai[3] == (float)num5)
			{
				npc.netUpdate = true;
			}
			if ((!Main.dayTime || (double)npc.position.Y > Main.worldSurface * 16.0 || npc.type == 26 || npc.type == 27 || npc.type == 28 || npc.type == 31 || npc.type == 47 || npc.type == 67 || npc.type == 73 || npc.type == 77 || npc.type == 78 || npc.type == 79 || npc.type == 80 || npc.type == 110 || npc.type == 111 || npc.type == 120) && npc.ai[3] < (float)num5)
			{
				if ((npc.type == 3 || npc.type == 21 || npc.type == 31 || npc.type == 77 || npc.type == 110 || npc.type == 132) && Main.rand.Next(1000) == 0)
				{
					Main.PlaySound(14, (int)npc.position.X, (int)npc.position.Y, 1);
				}
				if ((npc.type == 78 || npc.type == 79 || npc.type == 80) && Main.rand.Next(500) == 0)
				{
					Main.PlaySound(26, (int)npc.position.X, (int)npc.position.Y, 1);
				}
				npc.TargetClosest(true);
			}
			else
			{
				if (npc.velocity.X == 0f)
				{
					if (npc.velocity.Y == 0f)
					{
						npc.ai[0] += 1f;
						if (npc.ai[0] >= 2f)
						{
							npc.direction *= -1;
							npc.spriteDirection = npc.direction;
							npc.ai[0] = 0f;
						}
					}
				}
				else
				{
					npc.ai[0] = 0f;
				}
				if (npc.direction == 0)
				{
					npc.direction = 1;
				}
			}
			if (npc.velocity.X < -1.5f || npc.velocity.X > 1.5f)
			{
				if (npc.velocity.Y == 0f)
				{
					npc.velocity *= 0.8f;
				}
			}
			else
			{
				if (npc.velocity.X < 1.5f && npc.direction == 1)
				{
					npc.velocity.X = npc.velocity.X + 0.07f;
					if (npc.velocity.X > 1.5f)
					{
						npc.velocity.X = 1.5f;
					}
				}
				else
				{
					if (npc.velocity.X > -1.5f && npc.direction == -1)
					{
						npc.velocity.X = npc.velocity.X - 0.07f;
						if (npc.velocity.X < -1.5f)
						{
							npc.velocity.X = -1.5f;
						}
					}
				}
			}
			bool flag4 = false;
			if (npc.velocity.Y == 0f)
			{
				int num29 = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
				int num30 = (int)npc.position.X / 16;
				int num31 = (int)(npc.position.X + (float)npc.width) / 16;
				for (int l = num30; l <= num31; l++)
				{
					if (Main.tile[l, num29] == null)
					{
						return;
					}
					if (Main.tile[l, num29].active() && Main.tileSolid[(int)Main.tile[l, num29].type])
					{
						flag4 = true;
						break;
					}
				}
			}
			if (flag4)
			{
				int num32 = (int)((npc.position.X + (float)(npc.width / 2) + (float)(15 * npc.direction)) / 16f);
				int num33 = (int)((npc.position.Y + (float)npc.height - 15f) / 16f);
				if (npc.type == 109)
				{
					num32 = (int)((npc.position.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 16) * npc.direction)) / 16f);
				}
				if (Main.tile[num32, num33] == null)
				{
					Main.tile[num32, num33] = new Tile();
				}
				if (Main.tile[num32, num33 - 1] == null)
				{
					Main.tile[num32, num33 - 1] = new Tile();
				}
				if (Main.tile[num32, num33 - 2] == null)
				{
					Main.tile[num32, num33 - 2] = new Tile();
				}
				if (Main.tile[num32, num33 - 3] == null)
				{
					Main.tile[num32, num33 - 3] = new Tile();
				}
				if (Main.tile[num32, num33 + 1] == null)
				{
					Main.tile[num32, num33 + 1] = new Tile();
				}
				if (Main.tile[num32 + npc.direction, num33 - 1] == null)
				{
					Main.tile[num32 + npc.direction, num33 - 1] = new Tile();
				}
				if (Main.tile[num32 + npc.direction, num33 + 1] == null)
				{
					Main.tile[num32 + npc.direction, num33 + 1] = new Tile();
				}
				if (Main.tile[num32, num33 - 1].active() && Main.tile[num32, num33 - 1].type == 10 && flag3)
				{
					npc.ai[2] += 1f;
					npc.ai[3] = 0f;
					if (npc.ai[2] >= 60f)
					{
						npc.velocity.X = 0.5f * (float)(-(float)npc.direction);
						npc.ai[1] += 1f;
						npc.ai[2] = 0f;
						bool flag5 = false;
						if (npc.ai[1] >= 10f)
						{
							flag5 = true;
							npc.ai[1] = 10f;
						}
						WorldGen.KillTile(num32, num33 - 1, true, false, false);
						if ((Main.netMode != 1 || !flag5) && flag5 && Main.netMode != 1)
						{
							if (npc.type == 26)
							{
								WorldGen.KillTile(num32, num33 - 1, false, false, false);
								if (Main.netMode == 2)
								{
									NetMessage.SendData(17, -1, -1, null, 0, (float)num32, (float)(num33 - 1), 0f, 0);
								}
							}
							else
							{
								bool flag6 = WorldGen.OpenDoor(num32, num33, npc.direction);
								if (!flag6)
								{
									npc.ai[3] = (float)num5;
									npc.netUpdate = true;
								}
								if (Main.netMode == 2 && flag6)
								{
									NetMessage.SendData(19, -1, -1, null, 0, (float)num32, (float)num33, (float)npc.direction, 0);
								}
							}
						}
					}
				}
				else
				{
					if ((npc.velocity.X < 0f && npc.spriteDirection == -1) || (npc.velocity.X > 0f && npc.spriteDirection == 1))
					{
						if (Main.tile[num32, num33 - 2].active() && Main.tileSolid[(int)Main.tile[num32, num33 - 2].type])
						{
							if (Main.tile[num32, num33 - 3].active() && Main.tileSolid[(int)Main.tile[num32, num33 - 3].type])
							{
								npc.velocity.Y = -8f;
								npc.netUpdate = true;
							}
							else
							{
								npc.velocity.Y = -7f;
								npc.netUpdate = true;
							}
						}
						else
						{
							if (Main.tile[num32, num33 - 1].active() && Main.tileSolid[(int)Main.tile[num32, num33 - 1].type])
							{
								npc.velocity.Y = -6f;
								npc.netUpdate = true;
							}
							else
							{
								if (Main.tile[num32, num33].active() && Main.tileSolid[(int)Main.tile[num32, num33].type])
								{
									npc.velocity.Y = -5f;
									npc.netUpdate = true;
								}
								else
								{
									if (npc.directionY < 0 && npc.type != 67 && (!Main.tile[num32, num33 + 1].active() || !Main.tileSolid[(int)Main.tile[num32, num33 + 1].type]) && (!Main.tile[num32 + npc.direction, num33 + 1].active() || !Main.tileSolid[(int)Main.tile[num32 + npc.direction, num33 + 1].type]))
									{
										npc.velocity.Y = -8f;
										npc.velocity.X = npc.velocity.X * 1.5f;
										npc.netUpdate = true;
									}
									else
									{
										if (flag3)
										{
											npc.ai[1] = 0f;
											npc.ai[2] = 0f;
										}
									}
								}
							}
						}
					}
				}
			}
			else
			{
				if (flag3)
				{
					npc.ai[1] = 0f;
					npc.ai[2] = 0f;
				}
			}
			#endregion
			#region Projectiles
			customAi1 += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
			if (customAi1 >= 10f)
			{
				npc.TargetClosest(true);
				if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
				{
					if (Main.rand.Next(100) == 1)
					{
						float num48 = 8f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							//int damage = 30;//(int) (14f * npc.scale);
							int type = ModContent.ProjectileType<Projectiles.Enemy.BlackKnightsSpear>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, spearDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 300;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}
				}
			}
		}
		#endregion

		#region Gore
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 1"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 3"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 3"), 1f);

			if (Main.rand.Next(99) < 25) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Ranged.RoyalThrowingSpear>(), 41 + Main.rand.Next(10));
			if (Main.rand.Next(99) < 40) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.EphemeralThrowingSpear>(), 41 + Main.rand.Next(10));
			if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.HealingElixir>(), 1);
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.EphemeralDust>(), 3 + Main.rand.Next(6));
			if (Main.rand.Next(99) < 4) Item.NewItem(npc.getRect(), ItemID.IronskinPotion, 1);
			if (Main.rand.Next(99) < 4) Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion, 1);
			if (Main.rand.Next(99) < 8) Item.NewItem(npc.getRect(), ItemID.HunterPotion, 1);
			if (Main.rand.Next(99) < 6) Item.NewItem(npc.getRect(), ItemID.RegenerationPotion, 1);
			if (Main.rand.Next(99) < 30) Item.NewItem(npc.getRect(), ItemID.ShinePotion, 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ItemID.BattlePotion, 1);
		}
		#endregion
	}
}