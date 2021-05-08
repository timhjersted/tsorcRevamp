using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses {
    class Artorias : ModNPC {

        public override void SetStaticDefaults() {
			Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.PossessedArmor]; 
        }
        public override void SetDefaults() {
            npc.knockBackResist = 0;
            npc.damage = 200;
            npc.defense = 65535;
            npc.height = 40;
            npc.width = 30;
            npc.lifeMax = 76000;
            npc.scale = 1.2f;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath6;
            npc.value = 700000;
            npc.boss = true;
            npc.lavaImmune = true;
			animationType = NPCID.PossessedArmor; //this almost feels like cheating lol
        }
        public override void OnHitPlayer(Player target, int damage, bool crit) {
            if (Main.rand.Next(4) == 0) {
                target.AddBuff(BuffID.BrokenArmor, 180, false);
                target.AddBuff(BuffID.Poisoned, 3600, false);
                target.AddBuff(BuffID.Cursed, 300, false);
                target.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false);
            }
        }
		float customAi1;

		float customspawn2;
		public override void AI() {

			int num58;
			int num59;


			int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 32, npc.velocity.X - 3f, npc.velocity.Y, 150, Color.Yellow, 1f);
			Main.dust[dust].noGravity = true;

			bool flag2 = false;
			int num5 = 60;
			bool flag3 = true;
			if (npc.velocity.Y == 0f && (npc.velocity.X == 0f && npc.direction < 0)) {
				npc.velocity.Y -= 8f;
				npc.velocity.X -= 2f;
			}
			else if (npc.velocity.Y == 0f && (npc.velocity.X == 0f && npc.direction > 0)) {
				npc.velocity.Y -= 8f;
				npc.velocity.X += 2f;
			}
			if (npc.velocity.Y == 0f && ((npc.velocity.X > 0f && npc.direction < 0) || (npc.velocity.X < 0f && npc.direction > 0))) {
				flag2 = true;
			}
			if (npc.position.X == npc.oldPosition.X || npc.ai[3] >= (float)num5 || flag2) {
				npc.ai[3] += 1f;
			}
			else {
				if ((double)Math.Abs(npc.velocity.X) > 0.9 && npc.ai[3] > 0f) {
					npc.ai[3] -= 1f;
				}
			}
			if (npc.ai[3] > (float)(num5 * 10)) {
				npc.ai[3] = 0f;
			}
			if (npc.justHit) {
				npc.ai[3] = 0f;
			}
			if (npc.ai[3] == (float)num5) {
				npc.netUpdate = true;
			}
			if ((!Main.dayTime || (double)npc.position.Y > Main.worldSurface * 16.0) && npc.ai[3] < (float)num5) {

				npc.TargetClosest(true);
			}
			else {
				if (npc.velocity.X == 0f) {
					if (npc.velocity.Y == 0f) {
						npc.ai[0] += 1f;
						if (npc.ai[0] >= 2f) {
							npc.direction *= -1;
							npc.spriteDirection = npc.direction;
							npc.ai[0] = 0f;
						}
					}
				}
				else {
					npc.ai[0] = 0f;
				}
				if (npc.direction == 0) {
					npc.direction = 1;
				}
			}
			if (npc.velocity.X < -1.5f || npc.velocity.X > 1.5f) {
				if (npc.velocity.Y == 0f) {
					npc.velocity *= 0.8f;
				}
			}
			else {
				if (npc.velocity.X < 1.5f && npc.direction == 1) {
					npc.velocity.X = npc.velocity.X + 0.07f;
					if (npc.velocity.X > 1.5f) {
						npc.velocity.X = 1.5f;
					}
				}
				else {
					if (npc.velocity.X > -1.5f && npc.direction == -1) {
						npc.velocity.X = npc.velocity.X - 0.07f;
						if (npc.velocity.X < -1.5f) {
							npc.velocity.X = -1.5f;
						}
					}
				}
			}
			bool flag4 = false;
			if (npc.velocity.Y == 0f) {
				int num29 = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
				int num30 = (int)npc.position.X / 16;
				int num31 = (int)(npc.position.X + (float)npc.width) / 16;
				for (int l = num30; l <= num31; l++) {
					if (Main.tile[l, num29] == null) {
						return;
					}
					if (Main.tile[l, num29].active() && Main.tileSolid[(int)Main.tile[l, num29].type]) {
						flag4 = true;
						break;
					}
				}
			}
			if (flag4) {
				int num32 = (int)((npc.position.X + (float)(npc.width / 2) + (float)(15 * npc.direction)) / 16f);
				int num33 = (int)((npc.position.Y + (float)npc.height - 15f) / 16f);
				if (Main.tile[num32, num33] == null) {
					Main.tile[num32, num33] = new Tile();
				}
				if (Main.tile[num32, num33 - 1] == null) {
					Main.tile[num32, num33 - 1] = new Tile();
				}
				if (Main.tile[num32, num33 - 2] == null) {
					Main.tile[num32, num33 - 2] = new Tile();
				}
				if (Main.tile[num32, num33 - 3] == null) {
					Main.tile[num32, num33 - 3] = new Tile();
				}
				if (Main.tile[num32, num33 + 1] == null) {
					Main.tile[num32, num33 + 1] = new Tile();
				}
				if (Main.tile[num32 + npc.direction, num33 - 1] == null) {
					Main.tile[num32 + npc.direction, num33 - 1] = new Tile();
				}
				if (Main.tile[num32 + npc.direction, num33 + 1] == null) {
					Main.tile[num32 + npc.direction, num33 + 1] = new Tile();
				}
				if (Main.tile[num32, num33 - 1].active() && Main.tile[num32, num33 - 1].type == 10 && flag3) {
					npc.ai[2] += 1f;
					npc.ai[3] = 0f;
					if (npc.ai[2] >= 60f) {
						npc.velocity.X = 0.5f * (float)(-(float)npc.direction);
						npc.ai[1] += 1f;
						npc.ai[2] = 0f;
						bool flag5 = false;
						if (npc.ai[1] >= 10f) {
							flag5 = true;
							npc.ai[1] = 10f;
						}
						WorldGen.KillTile(num32, num33 - 1, true, false, false);
						if ((Main.netMode != NetmodeID.MultiplayerClient || !flag5) && flag5 && Main.netMode != NetmodeID.MultiplayerClient) {
							if (npc.type == NPCID.GoblinPeon) {
								WorldGen.KillTile(num32, num33 - 1, false, false, false);
								if (Main.netMode == NetmodeID.Server) {
									NetMessage.SendData(MessageID.TileChange, -1, -1, null, 0, (float)num32, (float)(num33 - 1), 0f, 0);
								}
							}
							else {
								bool flag6 = WorldGen.OpenDoor(num32, num33, npc.direction);
								if (!flag6) {
									npc.ai[3] = (float)num5;
									npc.netUpdate = true;
								}
								if (Main.netMode == NetmodeID.Server && flag6) {
									NetMessage.SendData(MessageID.ChangeDoor, -1, -1, null, 0, (float)num32, (float)num33, (float)npc.direction, 0);
								}
							}
						}
					}
				}
				else {
					if ((npc.velocity.X < 0f && npc.spriteDirection == -1) || (npc.velocity.X > 0f && npc.spriteDirection == 1)) {
						if (Main.tile[num32, num33 - 2].active() && Main.tileSolid[(int)Main.tile[num32, num33 - 2].type]) {
							if (Main.tile[num32, num33 - 3].active() && Main.tileSolid[(int)Main.tile[num32, num33 - 3].type]) {
								npc.velocity.Y = -8f;
								npc.netUpdate = true;
							}
							else {
								npc.velocity.Y = -7f;
								npc.netUpdate = true;
							}
						}
						else {
							if (Main.tile[num32, num33 - 1].active() && Main.tileSolid[(int)Main.tile[num32, num33 - 1].type]) {
								npc.velocity.Y = -6f;
								npc.netUpdate = true;
							}
							else {
								if (Main.tile[num32, num33].active() && Main.tileSolid[(int)Main.tile[num32, num33].type]) {
									npc.velocity.Y = -5f;
									npc.netUpdate = true;
								}
								else {
									if (npc.directionY < 0 && (!Main.tile[num32, num33 + 1].active() || !Main.tileSolid[(int)Main.tile[num32, num33 + 1].type]) && (!Main.tile[num32 + npc.direction, num33 + 1].active() || !Main.tileSolid[(int)Main.tile[num32 + npc.direction, num33 + 1].type])) {
										npc.velocity.Y = -8f;
										npc.velocity.X = npc.velocity.X * 1.5f;
										npc.netUpdate = true;
									}
									else {
										if (flag3) {
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
			else {
				if (flag3) {
					npc.ai[1] = 0f;
					npc.ai[2] = 0f;
				}
			}
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				customAi1 += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
				if (customAi1 >= 10f) {
					npc.TargetClosest(true);

					if ((customspawn2 < 1) && Main.rand.Next(950) == 1) {
						int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<Enemies.ViciousSpit>(), 0); //Witchking
						Main.npc[Spawned].velocity.Y = -8;
						Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
						npc.ai[0] = 20 - Main.rand.Next(80);
						customspawn2 += 1f;
						if (Main.netMode == NetmodeID.Server) {
							NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
						}
					}
					if (Main.rand.Next(220) == 1) {
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
						npc.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
						npc.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
						npc.ai[1] = 1f;
						npc.netUpdate = true;
					}
					if (Main.rand.Next(200) == 1) {
						float num48 = 10f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 100 + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int damage = 40;
							int type = ModContent.ProjectileType<Projectiles.EnemySpellHoldBall>();
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 105;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;





						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(100) == 1) {
						float num48 = 13f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int damage = 60;
							int type = ModContent.ProjectileType<Projectiles.EnemySpellGreatEnergyBall>();
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 100;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(40) == 1) {
						float num48 = 8f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= 0;
							speedY *= 0;
							int damage = 0;
							int type = ModContent.ProjectileType<Projectiles.EnemySpellLightPillarBall>();
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 300;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(200) == 1) {
						float num48 = 8f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int damage = 70;
							int type = ModContent.ProjectileType<Projectiles.BlackBreath>();
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 10;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(150) == 1) {
						float num48 = 9f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int damage = 50;
							int type = ModContent.ProjectileType<Projectiles.EnemySpellLightning3Ball>();
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 300;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(120) == 1) {
						float num48 = 8f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 650 + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int damage = 50;
							int type = ModContent.ProjectileType<Projectiles.EnemySpellIce3Ball>();
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 40;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(200) == 1) {
						num58 = Projectile.NewProjectile(npc.position.X + 20, npc.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.PhantomSeeker>(), 80, 0f, Main.myPlayer);
						Main.projectile[num58].timeLeft = 400;
						Main.projectile[num58].rotation = Main.rand.Next(700) / 100f;
						Main.projectile[num58].ai[0] = npc.target;


						Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
						customAi1 = 1f;

						npc.netUpdate = true;
					}
					if (Main.rand.Next(650) == 1) {
						float num48 = 8f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 400 + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int damage = 80;
							int type = ModContent.ProjectileType<Projectiles.EnemySpellLightning4Ball>();
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 300;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}



					if (Main.rand.Next(350) == 1) {
						float num48 = 8f;
						Vector2 vector9 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 520 + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int damage = 40;
							int type = ModContent.ProjectileType<Projectiles.MassiveCrystalShardsSpell>();
							int num54 = Projectile.NewProjectile(vector9.X, vector9.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 100;
							Main.projectile[num54].aiStyle = 4;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 25);
							npc.ai[3] = 0; ;
						}
						npc.netUpdate = true;
					}


					if (Main.rand.Next(250) == 1) {
						num59 = Projectile.NewProjectile(npc.position.X + 20, npc.position.Y + 50, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), ModContent.ProjectileType<Projectiles.PhantomSeeker>(), 80, 0f, Main.myPlayer);
						Main.projectile[num59].timeLeft = 500;
						Main.projectile[num59].rotation = Main.rand.Next(700) / 100f;
						Main.projectile[num59].ai[0] = npc.target;


						Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
						customAi1 = 1f;

						npc.netUpdate = true;

					}
				}





				if (Main.rand.Next(350) == 1) {
					float num48 = 8f;
					Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
					float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
					float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
					if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
						float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
						num51 = num48 / num51;
						speedX *= num51;
						speedY *= num51;
						int damage = 60;
						int type = ModContent.ProjectileType<Projectiles.EnemySpellIcestormBall>();
						int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
						Main.projectile[num54].timeLeft = 1;
						Main.projectile[num54].aiStyle = 1;
						Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
						npc.ai[1] = 1f;
					}
					npc.netUpdate = true;
				}



				if (Main.rand.Next(205) == 1) {
					float num48 = 9f;
					Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
					float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
					float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
					if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
						float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
						num51 = num48 / num51;
						speedX *= num51;
						speedY *= num51;
						int damage = 60;
						int type = ModContent.ProjectileType<Projectiles.EnemySpellGreatEnergyBall>();
						int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
						Main.projectile[num54].timeLeft = 300;
						Main.projectile[num54].aiStyle = 1;
						Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
						customAi1 = 1f;
					}
					npc.netUpdate = true;
				}
				if (Main.rand.Next(1000) == 1) {
					float num48 = 11f;
					Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
					float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
					float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
					if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
						float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
						num51 = num48 / num51;
						speedX *= num51;
						speedY *= num51;
						int damage = 0;
						int type = ModContent.ProjectileType<Projectiles.EnemySpellGravity4Ball>();
						int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
						Main.projectile[num54].timeLeft = 60;
						Main.projectile[num54].aiStyle = 1;
						Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
						customAi1 = 1f;
					}
					npc.netUpdate = true;
				}
			}
			if ((Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))) {
				npc.noTileCollide = false;
				npc.noGravity = false;
			}
			if ((!Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))) {
				npc.noTileCollide = true;
				npc.noGravity = true;
				npc.velocity.Y = 0f;
				if (npc.position.Y > Main.player[npc.target].position.Y) {
					npc.velocity.Y -= 3f;
				}
				if (npc.position.Y < Main.player[npc.target].position.Y) {
					npc.velocity.Y += 8f;
				}
			}
			if (Main.player[npc.target].dead) {
				if (npc.timeLeft > 10) {
					npc.timeLeft = 10;
					return;
				}
			}
		}

	}
}
