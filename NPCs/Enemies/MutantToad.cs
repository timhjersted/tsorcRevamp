using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class MutantToad : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 3;
			animationType = 3;
			npc.aiStyle = 3;
			npc.damage = 38;
			npc.defense = 15;
			npc.height = 40;
			npc.width = 30;
			npc.lifeMax = 78;
			npc.scale = 0.9f;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.knockBackResist = 0.2f;
			npc.value = 200;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			for (int num36 = 0; num36 < 200; num36++)
			{
				if (Main.npc[num36].active && Main.npc[num36].type == npc.type)
				{
					return 0;
				}
			}
			bool nospecialbiome = !spawnInfo.player.ZoneJungle && !(spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson) && !spawnInfo.player.ZoneHoly && !spawnInfo.player.ZoneMeteor && !spawnInfo.player.ZoneDungeon; // Not necessary at all to use but needed to make all this work.

			bool sky = nospecialbiome && ((double)spawnInfo.player.position.Y < Main.worldSurface * 0.44999998807907104);
			bool surface = nospecialbiome && !sky && (spawnInfo.player.position.Y <= Main.worldSurface);
			bool underground = nospecialbiome && !surface && (spawnInfo.player.position.Y <= Main.rockLayer);
			bool underworld = (spawnInfo.player.position.Y > Main.maxTilesY - 190);
			bool cavern = nospecialbiome && (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25);
			bool undergroundJungle = (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25) && spawnInfo.player.ZoneJungle;
			bool undergroundEvil = (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25) && (spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson);
			bool undergroundHoly = (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25) && spawnInfo.player.ZoneHoly;
			if (!Main.dayTime && undergroundJungle)
			{
				if (Main.rand.Next(15) == 0)
				{
					return 1;
				}
			}

			int closeTownNPCs = 0;
			if (!Main.bloodMoon)
			{
				Vector2 playerPosition = spawnInfo.player.position + new Vector2(spawnInfo.player.width / 2, spawnInfo.player.height / 2);
				for (int num36 = 0; num36 < 200; num36++)
				{
					Vector2 npcPosition = Main.npc[num36].position + new Vector2(Main.npc[num36].width / 2, Main.npc[num36].height / 2);
					if (Main.npc[num36].active && Main.npc[num36].townNPC && Vector2.Distance(playerPosition, npcPosition) < 1500)
					{
						closeTownNPCs++;
					}
				}
			}
			if (closeTownNPCs == 1 && Main.rand.Next(3) == 0) return 0;
			if (closeTownNPCs == 2 && Main.rand.Next(2) == 0) return 0;
			if (closeTownNPCs == 3 && Main.rand.Next(3) <= 1) return 0;
			if (closeTownNPCs >= 4) return 0;

			return 1;
		}



		public override void OnHitPlayer(Player player, int damage, bool crit) //hook works!
		{
			if (Main.rand.Next(2) == 0)
			{
				player.AddBuff(20, 3600, false); //poisoned!
			}

		}


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
			npc.TargetClosest(true);
			if (npc.velocity.X < -1.5f || npc.velocity.X > 1.5f)
			{
				if (npc.velocity.Y == 0f)
				{
					npc.velocity *= 0.8f;
				}
			}
			else
			{
				if (npc.velocity.X < 0.9f && npc.direction == 1)
				{
					npc.velocity.X = npc.velocity.X + 0.07f;
					if (npc.velocity.X > 0.9f)
					{
						npc.velocity.X = 0.9f;
					}
				}
				else
				{
					if (npc.velocity.X > -0.9f && npc.direction == -1)
					{
						npc.velocity.X = npc.velocity.X - 0.07f;
						if (npc.velocity.X < -0.9f)
						{
							npc.velocity.X = -0.9f;
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
		}

		public override void NPCLoot()
		{
			if (npc.life <= 0)
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Mutant Toad Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Mutant Toad Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Mutant Toad Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Mutant Toad Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Mutant Toad Gore 3"), 1f);
			}
		}
	}
}