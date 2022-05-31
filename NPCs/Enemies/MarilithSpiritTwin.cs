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
			NPC.npcSlots = 6;
			Main.npcFrameCount[NPC.type] = 8;
			NPC.width = 120;
			NPC.height = 160;
			NPC.timeLeft = 22500;
			NPC.aiStyle = 22;
			NPC.damage = 60;
			NPC.defense = 23;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath6;
			NPC.lifeMax = 20000;
			NPC.alpha = 100;
			NPC.knockBackResist = 0;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.value = 50000;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.MarilithSpiritTwinBanner>();

			NPC.buffImmune[BuffID.Confused] = true;
			NPC.buffImmune[BuffID.CursedInferno] = true;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			NPC.damage = (int)(NPC.damage / 2);
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
			Player P = spawnInfo.Player;

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
			NPC.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
			if (NPC.ai[1] >= 10f)
			{
				NPC.TargetClosest(true);
				if ((Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height)))
				{
					if (Main.rand.Next(70) == 1)
					{
						float num48 = 9f;
						Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
						float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 21);
						float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 21);

						if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning3Ball>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, lightningDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 60;
							Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)NPC.position.X, (int)NPC.position.Y, 0x11);
							NPC.ai[1] = 1f;
						}
						NPC.netUpdate = true;
					}
					if (Main.rand.Next(220) == 1)
					{
						float num48 = 8f;
						Vector2 vector8 = new Vector2((NPC.position.X + ((((NPC.width + 50) * 5f) * (NPC.direction * 2)) / 20f) + 130), NPC.position.Y + (NPC.height - 75));
						float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 21);
						float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 21);
						if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							//(int) (14f * npc.scale);
							int type = ModContent.ProjectileType<Projectiles.Enemy.Okiku.PhasedMatterBlast>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, antiMatterBlastDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 150;
							//Main.projectile[num54].aiStyle = 9;
							Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)NPC.position.X, (int)NPC.position.Y, 0x11);
							NPC.ai[1] = 1f;
						}
						NPC.netUpdate = true;
					}
					if (Main.rand.Next(20) == 1)
					{
						float num48 = 11f;
						Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
						float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-50, 50) / 100;
						float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-50, 50) / 100;
						if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							//(int) (14f * npc.scale);
							int type = ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, crazedPurpleCrushDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 150;
							//Main.projectile[num54].aiStyle = 19;
							Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)NPC.position.X, (int)NPC.position.Y, 0x11);
							NPC.ai[1] = 1f;
						}
						NPC.netUpdate = true;
					}
				}
			}
			if (NPC.justHit)
			{
				NPC.ai[2] = 0f;
			}
			if (NPC.ai[2] >= 0f)
			{
				int num258 = 16;
				bool flag26 = false;
				bool flag27 = false;
				if (NPC.position.X > NPC.ai[0] - (float)num258 && NPC.position.X < NPC.ai[0] + (float)num258)
				{
					flag26 = true;
				}
				else
				{
					if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
					{
						flag26 = true;
					}
				}
				num258 += 24;
				if (NPC.position.Y > NPC.ai[1] - (float)num258 && NPC.position.Y < NPC.ai[1] + (float)num258)
				{
					flag27 = true;
				}
				if (flag26 && flag27)
				{
					NPC.ai[2] += 1f;
					if (NPC.ai[2] >= 30f && num258 == 16)
					{
						flag25 = true;
					}
					if (NPC.ai[2] >= 60f)
					{
						NPC.ai[2] = -200f;
						NPC.direction *= -1;
						NPC.velocity.X = NPC.velocity.X * -1f;
						NPC.collideX = false;
					}
				}
				else
				{
					NPC.ai[0] = NPC.position.X;
					NPC.ai[1] = NPC.position.Y;
					NPC.ai[2] = 0f;
				}
				NPC.TargetClosest(true);
			}
			else
			{
				NPC.ai[2] += 1f;
				if (Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2) > NPC.position.X + (float)(NPC.width / 2))
				{
					NPC.direction = -1;
				}
				else
				{
					NPC.direction = 1;
				}
			}
			int num259 = (int)((NPC.position.X + (float)(NPC.width / 2)) / 16f) + NPC.direction * 2;
			int num260 = (int)((NPC.position.Y + (float)NPC.height) / 16f);
			bool flag28 = true;
			//bool flag29; //What is this? It doesn't seem to do anything, so i'm commenting it out for now.
			int num261 = 3;
			for (int num269 = num260; num269 < num260 + num261; num269++)
			{
				if (Main.tile[num259, num269] == null)
				{
					Main.tile[num259, num269].ClearTile();
				}
				if ((Main.tile[num259, num269].HasTile && Main.tileSolid[(int)Main.tile[num259, num269].TileType]) || Main.tile[num259, num269].liquid > 0)
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
				NPC.velocity.Y = NPC.velocity.Y + 0.1f;
				if (NPC.velocity.Y > 3f)
				{
					NPC.velocity.Y = 3f;
				}
			}
			else
			{
				if (NPC.directionY < 0 && NPC.velocity.Y > 0f)
				{
					NPC.velocity.Y = NPC.velocity.Y - 0.1f;
				}
				if (NPC.velocity.Y < -4f)
				{
					NPC.velocity.Y = -4f;
				}
			}
			if (NPC.collideX)
			{
				NPC.velocity.X = NPC.oldVelocity.X * -0.4f;
				if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 1f)
				{
					NPC.velocity.X = 1f;
				}
				if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -1f)
				{
					NPC.velocity.X = -1f;
				}
			}
			if (NPC.collideY)
			{
				NPC.velocity.Y = NPC.oldVelocity.Y * -0.25f;
				if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
				{
					NPC.velocity.Y = 1f;
				}
				if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
				{
					NPC.velocity.Y = -1f;
				}
			}
			float num270 = 2f;
			if (NPC.direction == -1 && NPC.velocity.X > -num270)
			{
				NPC.velocity.X = NPC.velocity.X - 0.1f;
				if (NPC.velocity.X > num270)
				{
					NPC.velocity.X = NPC.velocity.X - 0.1f;
				}
				else
				{
					if (NPC.velocity.X > 0f)
					{
						NPC.velocity.X = NPC.velocity.X + 0.05f;
					}
				}
				if (NPC.velocity.X < -num270)
				{
					NPC.velocity.X = -num270;
				}
			}
			else
			{
				if (NPC.direction == 1 && NPC.velocity.X < num270)
				{
					NPC.velocity.X = NPC.velocity.X + 0.1f;
					if (NPC.velocity.X < -num270)
					{
						NPC.velocity.X = NPC.velocity.X + 0.1f;
					}
					else
					{
						if (NPC.velocity.X < 0f)
						{
							NPC.velocity.X = NPC.velocity.X - 0.05f;
						}
					}
					if (NPC.velocity.X > num270)
					{
						NPC.velocity.X = num270;
					}
				}
			}
			if (NPC.directionY == -1 && (double)NPC.velocity.Y > -1.5)
			{
				NPC.velocity.Y = NPC.velocity.Y - 0.04f;
				if ((double)NPC.velocity.Y > 1.5)
				{
					NPC.velocity.Y = NPC.velocity.Y - 0.05f;
				}
				else
				{
					if (NPC.velocity.Y > 0f)
					{
						NPC.velocity.Y = NPC.velocity.Y + 0.03f;
					}
				}
				if ((double)NPC.velocity.Y < -1.5)
				{
					NPC.velocity.Y = -1.5f;
				}
			}
			else
			{
				if (NPC.directionY == 1 && (double)NPC.velocity.Y < 1.5)
				{
					NPC.velocity.Y = NPC.velocity.Y + 0.04f;
					if ((double)NPC.velocity.Y < -1.5)
					{
						NPC.velocity.Y = NPC.velocity.Y + 0.05f;
					}
					else
					{
						if (NPC.velocity.Y < 0f)
						{
							NPC.velocity.Y = NPC.velocity.Y - 0.03f;
						}
					}
					if ((double)NPC.velocity.Y > 1.5)
					{
						NPC.velocity.Y = 1.5f;
					}
				}
			}
			Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.4f, 0f, 0.25f);

			if (Main.player[NPC.target].dead)
			{
				NPC.velocity.Y += 0.20f;
				if (NPC.timeLeft > 10)
				{
					NPC.timeLeft = 0;
					return;
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
					num = Main.npcTexture[NPC.type].Height / Main.npcFrameCount[NPC.type];
				}
				if (NPC.velocity.X < 0)
				{
					NPC.spriteDirection = -1;
				}
				else
				{
					NPC.spriteDirection = 1;
				}
				NPC.rotation = NPC.velocity.X * 0.08f;
				NPC.frameCounter += 1.0;
				if (NPC.frameCounter >= 4.0)
				{
					NPC.frame.Y = NPC.frame.Y + num;
					NPC.frameCounter = 0.0;
				}
				if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
				{
					NPC.frame.Y = 0;
				}
				if (NPC.ai[3] == 0)
				{
					NPC.alpha = 0;
				}
				else
				{
					NPC.alpha = 200;
				}
			}
		#endregion

		#region Gore
		public override void OnKill()
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 1"), 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 2"), 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 3"), 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 4"), 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 5"), 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 6"), 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 7"), 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 8"), 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Fire Fiend Marilith Gore 9"), 1f);

			if(Main.rand.Next(99) < 10) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.Ice3Tome>(), 1);

			Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), 1);
		}
		#endregion
	}
}

