using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.NetModules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
	[AutoloadBossHead]
	class Gwyn : ModNPC
	{
		public override void SetDefaults()
		{
			npc.npcSlots = 100;
			Main.npcFrameCount[npc.type] = 16;
			animationType = 28;
			npc.aiStyle = 3;
			npc.height = 40;
			npc.width = 20;
			music = 12;
			npc.damage = 105;
			npc.defense = 90;
			npc.lifeMax = 500000;
			npc.boss = true;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.value = 800000;
			bossBag = ModContent.ItemType<Items.BossBags.GwynBag>();
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Gwyn, Lord of Cinder");
		}

		int deathBallDamage = 100;
		int phantomSeekerDamage = 45;
		int armageddonBallDamage = 85;
		int holdBallDamage = 35;
		int fireballBallDamage = 45;
		int blazeBallDamage = 55;
		int blackBreathDamage = 80;
		int purpleCrushDamage = 45;
		int fireBreathDamage = 50;
		int iceStormDamage = 33;
		int gravityBallDamage = 80;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			deathBallDamage = (int)(deathBallDamage / 2);
			phantomSeekerDamage = (int)(phantomSeekerDamage / 2);
			armageddonBallDamage = (int)(armageddonBallDamage / 2);
			holdBallDamage = (int)(holdBallDamage / 2);
			fireballBallDamage = (int)(fireballBallDamage / 2);
			blazeBallDamage = (int)(blazeBallDamage / 2);
			blackBreathDamage = (int)(blackBreathDamage / 2);
			purpleCrushDamage = (int)(purpleCrushDamage / 2);
			fireBreathDamage = (int)(fireBreathDamage / 2);
			iceStormDamage = (int)(iceStormDamage / 2);
			gravityBallDamage = (int)(gravityBallDamage / 2);
		}

		int chargeDamage = 0;
		bool chargeDamageFlag = false;

		float customAi1;
		float customspawn1;
		float customspawn2;
		float customspawn3;

        #region debuffs
        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
			if (Main.rand.Next(2) == 0)
			{
				player.AddBuff(33, 7200, false); //weak
				player.AddBuff(36, 180, false); //broken armor
				player.AddBuff(24, 600, false); //on fire
				player.AddBuff(ModContent.BuffType<Buffs.SlowedLifeRegen>(), 1200, false); //slowed life regen
				player.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800, false); //you lose knockback resistance
				player.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 1200, false); //you lose flight
			}
		}
		#endregion

		#region AI
		bool hasTargeted = false;
		int targetCount = 0;
		Player[] targets = new Player[256];
		bool[] targetAlive = new bool[256];
		float closestPlayerDistance = 999999;
		int despawnTime = -1;
		public override void AI()
		{
			//When despawning, we set timeLeft to 240. If that's been done, we don't need to check for players or target anyone anymore.
			if (despawnTime < 0)
			{
				//Only run this once. Gets all active players and throws them into these arrays so we can track their status.
				if (!hasTargeted)
				{
					foreach (Player player in Main.player)
					{
						//For some reason, Main.player always has 255 entries. This ensures we're only pulling real players from it.
						if (player.name != "")
						{
							targets[targetCount] = player;
							targetAlive[targetCount] = true;
							targetCount++;
						}
					}
					hasTargeted = true;
				}
				else
				{
					//Aka, "is there a player who hasn't been killed yet?"
					bool viableTarget = false;
					//Iterate through all tracked players in the array
					for (int i = 0; i < targetCount; i++)
					{
						//For each of them, check if they're dead. If so, mark it down in targetAlive.
						if (targets[i].dead)
						{
							targetAlive[i] = false;
							//Setting this makes it so the dead player's distance won't persist, and it has to check again.
							closestPlayerDistance = 999999;
						}
						else if (targetAlive[i])
						{
							//If it found a player that hasn't been killed yet, then don't despawn
							viableTarget = true;
							//Check if they're the closest one, and if so target them
							float distance = Vector2.DistanceSquared(targets[i].position, npc.position);
							if (distance < closestPlayerDistance)
							{
								closestPlayerDistance = distance;
								npc.target = targets[i].whoAmI;
							}
						}
					}
					//If there's no player that has not died, then despawn.
					if (!viableTarget)
					{
						Main.NewText("You have fallen before the Lord of Cinder...", Color.OrangeRed);
						despawnTime = 240;
					}
				}
			}
			 else
            {
				//Adios
				if(despawnTime == 0)
                {
					npc.active = false;
					for (int i = 0; i < 60; i++)
					{
						int dustID = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, Color.Red, 7f);
						Main.dust[dustID].noGravity = true;
					}
				} else
                {
					despawnTime--;
				}
			}


			int num58;
			int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Red, 2f);
			Main.dust[dust].noGravity = true;

			bool flag2 = false;
			int num5 = 60;
			bool flag3 = true;
			if (npc.velocity.Y == 0f && (npc.velocity.X == 0f && npc.direction < 0))
			{
				npc.velocity.Y -= 8f;
				npc.velocity.X -= 2f;
			}
			else if (npc.velocity.Y == 0f && (npc.velocity.X == 0f && npc.direction > 0))
			{
				npc.velocity.Y -= 8f;
				npc.velocity.X += 2f;
			}
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


			#region Charge
			if (Main.netMode != 1)
			{
				if (Main.rand.Next(80) == 1)
				{
					chargeDamageFlag = true;
					Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
					float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
					npc.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
					npc.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
					npc.ai[1] = 1f;
					npc.netUpdate = true;
				}
				if (chargeDamageFlag == true)
				{
					npc.damage = 150;
					chargeDamage++;
				}
				if (chargeDamage >= 106)
				{
					chargeDamageFlag = false;
					npc.damage = 105;
					chargeDamage = 0;
				}

			}
			#endregion

			#region Projectiles
			if (Main.netMode != 1)
			{
				customAi1 += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
				if (customAi1 >= 10f)
				{
					if ((customspawn1 < 16) && Main.rand.Next(200) == 1)
					{
						int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), NPCID.Hellbat, 0);
						Main.npc[Spawned].velocity.Y = -8;
						Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
						npc.ai[0] = 20 - Main.rand.Next(80);
						customspawn1 += 1f;
						if (Main.netMode == 2)
						{
							NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
						}
					}
					if ((customspawn2 < 5) && Main.rand.Next(350) == 1)
					{
						int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Enemies.BlackKnight>(), 0);
						Main.npc[Spawned].velocity.Y = -8;
						Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
						npc.ai[0] = 20 - Main.rand.Next(80);
						customspawn2 += 1f;
						if (Main.netMode == 2)
						{
							NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
						}
					}

					if ((customspawn3 < 1) && Main.rand.Next(550) == 1)
					{
						int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<SwordOfLordGwyn>(), 0);
						Main.npc[Spawned].velocity.Y = -8;
						Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
						npc.ai[0] = 20 - Main.rand.Next(80);
						customspawn3 += 1f;
						if (Main.netMode == 2)
						{
							NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
						}
					}

					if (Main.rand.Next(100) == 1)
					{
						float num48 = 10f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, deathBallDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 150;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(200) == 1)
					{
						num58 = Projectile.NewProjectile(this.npc.position.X + 20, this.npc.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.Enemy.BurningPhantomSeeker>(), phantomSeekerDamage, 0f, Main.myPlayer);
						Main.projectile[num58].timeLeft = 460;
						Main.projectile[num58].rotation = Main.rand.Next(700) / 100f;
						Main.projectile[num58].ai[0] = this.npc.target;


						Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
						customAi1 = 1f;

						npc.netUpdate = true;
					}

					if (Main.rand.Next(900) == 1)
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
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellArmageddonBall>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, armageddonBallDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 0;
							Main.projectile[num54].aiStyle = -1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(220) == 1)
					{
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
						npc.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
						npc.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
						npc.ai[1] = 1f;
						npc.netUpdate = true;
					}

					if (Main.rand.Next(250) == 1)
					{
						float num48 = 18f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 100 + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellHoldBall>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, holdBallDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 115;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;





						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(500) == 1)
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
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, deathBallDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 100;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(150) == 1)
					{
						float num48 = 15f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, fireballBallDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 90;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(150) == 1)
					{
						float num48 = 15f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, fireballBallDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 90;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(60) == 1)
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
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlazeBall>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, blazeBallDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 0;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(200) == 1)
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
							int type = ModContent.ProjectileType<Projectiles.Enemy.BlackBreath>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, blackBreathDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 10;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(400) == 1)
					{
						float num48 = 13f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 1000 + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, purpleCrushDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 600;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(26) == 1)
					{
						float num48 = 14f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 400 + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 3000;
							Main.projectile[num54].aiStyle = 23; //was 23
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(66) == 1) //might remove
					{
						float num48 = 14f;
						Vector2 vector8 = new Vector2(npc.position.X - 1800 + (npc.width * 0.5f), npc.position.Y - 600 + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 200;
							Main.projectile[num54].aiStyle = 23; //was 23
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(66) == 1) // might remove
					{
						float num48 = 14f;
						Vector2 vector8 = new Vector2(npc.position.X + 1800 + (npc.width * 0.5f), npc.position.Y - 600 + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 200;
							Main.projectile[num54].aiStyle = 23; //was 23
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(250) == 1)
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
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIcestormBall>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, iceStormDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 0;//was 70
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;
						}
						npc.netUpdate = true;
					}



					if (Main.rand.Next(305) == 1)
					{
						float num48 = 7f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.FireBreath>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, fireBreathDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 1900;
							Main.projectile[num54].aiStyle = 23;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(800) == 1)
					{
						float num48 = 12f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGravity4Ball>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, gravityBallDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 60;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}
				}


			}
			#endregion
			#region Phase Through Walls
			if ((Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height)))
			{
				npc.noTileCollide = false;
				npc.noGravity = false;
			}
			if ((!Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height)))
			{
				npc.noTileCollide = true;
				npc.noGravity = true;
				npc.velocity.Y = 0f;
				if (npc.position.Y > Main.player[npc.target].position.Y)
				{
					npc.velocity.Y -= 3f;
				}
				if (npc.position.Y < Main.player[npc.target].position.Y)
				{
					npc.velocity.Y += 8f;
				}
			}
			#endregion


			if (!Main.bloodMoon)
			{
				

				if (npc.timeLeft > 5)
				{
					Main.NewText("You have broken the Covenant of The Abyss...");

					npc.timeLeft = 5;
					npc.damage = 9999;
					return;
				}

			}
		}

		#region Gore
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Hero of Lumelia Gore 1"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Hero of Lumelia Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Hero of Lumelia Gore 3"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Hero of Lumelia Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Hero of Lumelia Gore 3"), 1f);
			
			if (Main.expertMode)
			{
				npc.DropBossBags();
			}
			else
			{
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.GuardianSoul>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DraxEX>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Epilogue>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.EssenceOfTerraria>());
			}

			tsorcRevampWorld.InitiateTheEnd();
		}
		#endregion
	}
}