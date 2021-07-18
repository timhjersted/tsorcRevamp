using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses
{
	[AutoloadBossHead]
	class Gaibon : ModNPC
	{
		public override void SetDefaults()
		{

			npc.npcSlots = 30;
			Main.npcFrameCount[npc.type] = 2;
			npc.width = 100;
			npc.height = 70;
			animationType = 62;
			npc.aiStyle = 22;
			npc.damage = 50;
			//It genuinely had none in the original.
			npc.defense = 0;
			//npc.music = 12;
			npc.defense = 10;
			npc.boss = true;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = mod.GetLegacySoundSlot(SoundType.NPCKilled, "Sounds/NPCKilled/Gaibon_Roar");
			npc.lifeMax = 4000;
			npc.scale = 1.1f;
			npc.knockBackResist = 0.9f;
			npc.value = 25000;
			npc.noTileCollide = true;
			npc.noGravity = true;
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Gaibon");
		}


		//Note: This whole region was all commented out in the original code. I'm not sure why, but i'll leave it that way for now.
		//#region Spawn
		//public bool SpawnNPC(int x,int y,int PID)
		//{
		//	  Player P = Main.player[PID]; //this shortens our code up from writing this line over and over.

		//	  bool Sky = P.position.Y <= (Main.rockLayer * 4);
		//	  bool Meteor = P.zoneMeteor;
		//	  bool Jungle = P.zoneJungle;
		//	  bool Dungeon = P.zoneDungeon;
		//	  bool Corruption = P.zoneEvil;
		//	  bool Hallow = P.zoneHoly;
		//	  bool AboveEarth  = P.position.Y < Main.worldSurface;
		//	  bool InBrownLayer = P.position.Y >= Main.worldSurface && P.position.Y < Main.rockLayer;
		//	  bool InGrayLayer = P.position.Y >= Main.rockLayer && P.position.Y < (Main.maxTilesY - 200)*16;
		//	  bool InHell = P.position.Y >= (Main.maxTilesY - 200)*16;
		//	  bool Ocean = P.position.X < 3600 || P.position.X > (Main.maxTilesX-100)*16;

		//	  // these are all the regular stuff you get , now lets see......

		//	for (int num36 = 0; num36 < 200; num36++)
		//	{
		//		if (Main.npc[num36].active && Main.npc[num36].type == Config.npcDefs.byName["Gaibon"].type)
		//		{
		//			return false;
		//		}
		//	}
		//	  return false;
		//}
		//#endregion

		#region AI
		public override void AI()
		{
			bool flag25 = false;
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				npc.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
				if (npc.ai[1] >= 10f)
				{
					npc.TargetClosest(true);
					if (Main.rand.Next(60) == 1)
					{
						int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), NPCID.BurningSphere, 0);
						Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/GaibonSpit2"), (int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2));
						if (Main.netMode == NetmodeID.Server)
						{
							NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
						}
						//npc.netUpdate=true;
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
					npc.ai[1] = npc.position.Y; //added -60
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
			int num260 = (int)(((npc.position.Y - 30) + (float)npc.height) / 16f);
			if (npc.position.Y > Main.player[npc.target].position.Y)
			{
				//npc.velocity.Y += .1f;
				//if (npc.velocity.Y > +2)
				//{
				//	npc.velocity.Y = -2;
				//}

				npc.velocity.Y -= 0.05f;
				if (npc.velocity.Y < -1)
				{
					npc.velocity.Y = -1;
				}


			}
			if (npc.position.Y < Main.player[npc.target].position.Y)
			{
				npc.velocity.Y += 0.05f;
				if (npc.velocity.Y > 1)
				{
					npc.velocity.Y = 1;
				}

				//npc.velocity.Y += .2f;
				//if (npc.velocity.Y > 2)
				//{
				//	npc.velocity.Y = 2;
				//}
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
				}
			}
			return;
		}
		#endregion

		#region gore
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gaibon Gore 1"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gaibon Gore 2"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gaibon Gore 3"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gaibon Gore 4"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gaibon Gore 2"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gaibon Gore 3"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gaibon Gore 4"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Blood Splat"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Blood Splat"), 0.9f);
			Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), 500);
		}
		#endregion

	}
}