using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
	[AutoloadBossHead]
	class Chaos : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 8;
			npc.width = 130;
			npc.height = 170;
			npc.aiStyle = 22;
			npc.damage = 150;
			npc.defense = 80;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.lifeMax = 80000;
			npc.knockBackResist = 0;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.value = 600000;
			npc.boss = true;
			npc.lavaImmune = true;

			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			bossBag = ModContent.ItemType<Items.BossBags.ChaosBag>();
			despawnHandler = new NPCDespawnHandler("Chaos tears you asunder...", Color.Yellow, DustID.GoldFlame);

		}


		int fireBreathDamage = 48;
		int iceStormDamage = 53;
		int greatFireballDamage = 49;
		int blazeBallDamage = 48;
		int purpleCrushDamage = 35;
		int meteorDamage = 55;
		int tornadoDamage = 45;
		int obscureSeekerDamage = 50;
		int crystalFireDamage = 50;
		int fireTrailsDamage = 35;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			fireBreathDamage = (int)(fireBreathDamage / 2);
			iceStormDamage = (int)(iceStormDamage / 2);
			greatFireballDamage = (int)(greatFireballDamage / 2);
			blazeBallDamage = (int)(blazeBallDamage / 2);
			purpleCrushDamage = (int)(purpleCrushDamage / 2);
			meteorDamage = (int)(meteorDamage / 2);
			tornadoDamage = (int)(tornadoDamage / 2);
			obscureSeekerDamage = (int)(obscureSeekerDamage / 2);
			crystalFireDamage = (int)(crystalFireDamage / 2);
			fireTrailsDamage = (int)(fireTrailsDamage / 2);
		}

		//float customAi1;
		int choasHealed = 0;
		//float customspawn1 = 0;
		int chargeDamage = 0;
		bool chargeDamageFlag = false;
		int holdTimer = 0;

		#region AI
		NPCDespawnHandler despawnHandler;
		public override void AI()
		{
			despawnHandler.TargetAndDespawn(npc.whoAmI);
			holdTimer--;
			if (holdTimer < 0)
			{
				holdTimer = 0;
			}
			if (Main.netMode != 1)
			{
				npc.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
				if (npc.ai[1] >= 10f)
				{
					if (((npc.position.X > Main.player[npc.target].position.X && (npc.position.X - Main.player[npc.target].position.X >= 1000))
					|| (npc.position.X < Main.player[npc.target].position.X && (Main.player[npc.target].position.X - npc.position.X >= 1000))
					|| (npc.position.Y > Main.player[npc.target].position.Y && (npc.position.Y - Main.player[npc.target].position.Y >= 1000))
					|| (npc.position.Y < Main.player[npc.target].position.Y && (Main.player[npc.target].position.Y - npc.position.Y >= 1000))))
					{
						npc.defense = 9999;
						if (holdTimer <= 0)
						{
							Main.NewText("Chaos is protected by unseen powers -- you're too far away!", 175, 75, 255);
							holdTimer = 200;
						}
					}
					else
					{
						npc.defense = 80;
					}
					if (Main.rand.Next(180) == 1)
					{
						chargeDamageFlag = true;

						//npc.netUpdate=true;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
						npc.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
						npc.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
						npc.ai[1] = 1f;
						npc.netUpdate = true;
					}
					if (chargeDamageFlag == true)
					{
						npc.damage = 120;
						chargeDamage++;
					}
					if (chargeDamage >= 81)
					{
						chargeDamageFlag = false;
						npc.damage = 80;
						chargeDamage = 0;
					}
					//npc.netUpdate=true; 


					if (Main.rand.Next(90) == 1)
					{
						float num48 = 9f;
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
							Main.projectile[num54].timeLeft = 550;
							Main.projectile[num54].aiStyle = 23;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							//customAi1 = 1f;
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
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIcestormBall>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, iceStormDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 0;
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
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, greatFireballDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 70;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;
						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(1000) == 1)
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
					if (Main.rand.Next(1200) == 1)
					{
						float num48 = 11f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
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
							Main.projectile[num54].timeLeft = 700;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;
						}
						npc.netUpdate = true;
					}

					if (Main.rand.Next(205) == 1)
					{
						Projectile.NewProjectile(
						(float)Main.player[npc.target].position.X - 100 + Main.rand.Next(300),
						(float)Main.player[npc.target].position.Y - 530.0f,
						(float)(-40 + Main.rand.Next(80)) / 10, 14.9f, ModContent.ProjectileType<Projectiles.Enemy.EnemyMeteor>(), meteorDamage, 2.0f);
					}

					if (Main.rand.Next(500) == 1)
					{
						float num48 = 15f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellTornado>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, tornadoDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 900;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;
						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(220) == 1)
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
							int type = ModContent.ProjectileType<Projectiles.Enemy.ObscureSeeker>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, obscureSeekerDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 450;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;
						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(50) == 1)
					{
						float num48 = 1f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width / 2), npc.position.Y - 300 + (npc.height / 2));
						float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
						rotation += Main.rand.Next(-50, 50) / 100;
						int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>(), crystalFireDamage, 0f, 0);



						Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
						npc.ai[1] = 1f;
					}
					npc.netUpdate = true;
				}
				if (Main.rand.Next(7) == 1)
				{
					if (Main.rand.Next(5) == 1)
					{
						float num48 = 16f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.FireTrails>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, fireTrailsDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 700;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
							npc.ai[1] = 1f;
						}
						npc.netUpdate = true;
					}
					if (npc.life <= 10000)
					{
						if (choasHealed == 0)
						{
							if (Main.rand.Next(2) == 1)
							{
								npc.life += 40000;
								if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
								float num48 = 8f;
								Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
								float speedX = 0;
								float speedY = 0;
								float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
								num51 = num48 / num51;
								speedX *= 0;
								speedY *= 0;
								int damage = 0;//(int) (14f * npc.scale);
								int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>();//44;//0x37; //14;
								int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
								Main.projectile[num54].timeLeft = 0;
								Main.projectile[num54].aiStyle = 1;
								Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 4);
								npc.ai[1] = 1f;
								npc.netUpdate = true;
								choasHealed += 1;
							}
						}
					}
					if (npc.life <= 6000)
					{
						if (choasHealed >= 1 && choasHealed <= 3)
						{
							if (Main.rand.Next(500) == 1)
							{
								npc.life += 15000;
								if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
								float num48 = 8f;
								Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
								float speedX = 0;
								float speedY = 0;
								float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
								num51 = num48 / num51;
								speedX *= 0;
								speedY *= 0;
								int damage = 0;//(int) (14f * npc.scale);
								int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>();//44;//0x37; //14;
								int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
								Main.projectile[num54].timeLeft = 0;
								Main.projectile[num54].aiStyle = 1;
								Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 4);
								npc.ai[1] = 1f;
								npc.netUpdate = true;
								choasHealed += 1;
							}
						}
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
			if (npc.position.Y > Main.player[npc.target].position.Y)
			{
				npc.velocity.Y -= .22f;
				if (npc.velocity.Y < -2)
				{
					npc.velocity.Y = -2;
				}
			}
			if (npc.position.Y < Main.player[npc.target].position.Y)
			{
				npc.velocity.Y += .22f;
				if (npc.velocity.Y > 2)
				{
					npc.velocity.Y = 2;
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

				npc.netUpdate = true;
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

				npc.netUpdate = true;
			}
			float num270 = 2.5f;
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

				npc.netUpdate = true;
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
					npc.netUpdate = true;
				}
			}
			if (npc.directionY == -1 && (double)npc.velocity.Y > -2.5)
			{
				npc.velocity.Y = npc.velocity.Y - 0.04f;
				if ((double)npc.velocity.Y > 2.5)
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
				if ((double)npc.velocity.Y < -2.5)
				{
					npc.velocity.Y = -2.5f;
				}

				npc.netUpdate = true;
			}
			else
			{
				if (npc.directionY == 1 && (double)npc.velocity.Y < 2.5)
				{
					npc.velocity.Y = npc.velocity.Y + 0.04f;
					if ((double)npc.velocity.Y < -2.5)
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
					if ((double)npc.velocity.Y > 2.5)
					{
						npc.velocity.Y = 2.5f;
					}
					npc.netUpdate = true;
				}
			}			
			Lighting.AddLight((int)npc.position.X / 16, (int)npc.position.Y / 16, 0.4f, 0f, 0f);
			return;
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
		}
		#endregion

		#region Gore
		public override void NPCLoot()
		{
			Projectile.NewProjectile((int)npc.position.X, (int)npc.position.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.ChaosDeathAnimation>(), 0, 0f, Main.myPlayer);
			if (Main.expertMode)
			{
				npc.DropBossBags();
			}
			else
			{
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Armors.PowerArmorNUHelmet>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Armors.PowerArmorNUTorso>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Armors.PowerArmorNUGreaves>());
				if (Main.rand.Next(99) < 20) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.FlareTome>());
				if (Main.rand.Next(99) < 40) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Ranged.ElfinBow>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>());
				if (Main.rand.Next(99) < 50) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.HiRyuuSpear>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 3000);
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.GuardianSoul>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulOfChaos>(), 3);
			}
		}
		#endregion
		public override bool CheckActive()
		{
			return false;
		}

		#region Magic Defense
		public int MagicDefenseValue()
		{
			return 65;
		}
		#endregion
	}
}