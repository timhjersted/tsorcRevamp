using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class BarrowWightNemesis : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Barrow Wight Nemesis");
		}
		public override void SetDefaults()
		{

			npc.npcSlots = 10;
			Main.npcFrameCount[npc.type] = 4;
			npc.width = 58;
			npc.height = 48;
			npc.aiStyle = 22;
			npc.damage = 115;
			npc.defense = 125;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath6;
			npc.lifeMax = 12970;
			npc.knockBackResist = 0;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.value = 6230;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
		}

		int breathCD = 45;
		//int previous = 0;
		bool breath = false;
		int chargeDamage = 0;
		bool chargeDamageFlag = false;

		//Spawns from the Surface into the Cavern, from 2/10th to 3.5/10th and again from 6.5/10th to 8/10th (Width) on Normal Mode. Also spawns in the Dungeon and in the sky in Hardmode.

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			bool oSky = (spawnInfo.player.position.Y <= (Main.rockLayer * 4));
			bool oSurface = ((spawnInfo.player.position.Y > (Main.rockLayer * 4)) && (spawnInfo.player.position.Y <= (Main.rockLayer * 8)));
			bool oUnderSurface = ((spawnInfo.player.position.Y > (Main.rockLayer * 8)) && (spawnInfo.player.position.Y < (Main.rockLayer * 13)));
			bool oUnderground = ((spawnInfo.player.position.Y >= (Main.rockLayer * 13)) && (spawnInfo.player.position.Y < (Main.rockLayer * 17)));
			bool oCavern = ((spawnInfo.player.position.Y >= (Main.rockLayer * 17)) && (spawnInfo.player.position.Y < (Main.rockLayer * 24)));
			bool oMagmaCavern = ((spawnInfo.player.position.Y >= (Main.rockLayer * 24)) && (spawnInfo.player.position.Y < (Main.rockLayer * 32)));
			bool oUnderworld = (spawnInfo.player.position.Y >= (Main.rockLayer * 32));
			//if (spawnInfo.player.townNPCs > 2f || spawnInfo.player.ZoneMeteor) return false;
			if (tsorcRevampWorld.SuperHardMode && oSky && !Main.dayTime && Main.rand.Next(15) == 1) return 1;
			if (tsorcRevampWorld.SuperHardMode && oSky && Main.dayTime && Main.rand.Next(25) == 1) return 1;
			if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && oSky && !Main.dayTime && Main.rand.Next(6) == 1) return 1;
			if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && oSky && Main.dayTime && Main.rand.Next(10) == 1) return 1;
			if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneDungeon && Main.rand.Next(30) == 1) return 1;
			if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && oSurface && Main.rand.Next(205) == 1) return 1;
			return 0;
		}
		#endregion


		//void DealtPlayer(Player player, double damage, NPC npc)



		//public void DamagePlayer(ref int damage, Player player);





		#region AI
		public override void AI()
		{
			npc.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
			if (npc.ai[1] >= 10f)
			{
				npc.TargetClosest(true);



				// charge forward code 
				if (Main.rand.Next(1650) == 1)
				{
					npc.netUpdate = true; //new
					chargeDamageFlag = true;
					Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
					float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
					npc.velocity.X = (float)(Math.Cos(rotation) * 11) * -1;
					npc.velocity.Y = (float)(Math.Sin(rotation) * 11) * -1;
					npc.ai[1] = 1f;
					npc.netUpdate = true;
				}
				if (chargeDamageFlag == true)
				{
					npc.damage = 115;
					chargeDamage++;
				}
				if (chargeDamage >= 115)
				{
					chargeDamageFlag = false;
					npc.damage = 115;
					chargeDamage = 0;
				}

				// fire breath attack
				if (Main.rand.Next(525) == 0)
				{
					breath = true;
					Main.PlaySound(2, -1, -1, 20);
				}
				if (breath)
				{
					float rotation = (float)Math.Atan2(npc.Center.Y - Main.player[npc.target].Center.Y, npc.Center.X - Main.player[npc.target].Center.X);
					int num54 = Projectile.NewProjectile(npc.Center.X, npc.Center.Y, (float)((Math.Cos(rotation) * 15) * -1), (float)((Math.Sin(rotation) * 15) * -1), ModContent.ProjectileType<Projectiles.Enemy.FrozenDragonsBreath>(), 0, 0f, Main.myPlayer);
					Main.projectile[num54].timeLeft = 30;
					npc.netUpdate = true;


					breathCD--;
					//}
				}
				if (breathCD <= 0)
				{
					breath = false;
					breathCD = 60;
					//Main.PlaySound(2, -1, -1, 20);
				}

				//end fire breath attack


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
			if (npc.position.Y > Main.player[npc.target].position.Y)
			{
				npc.velocity.Y -= .05f;
			}
			if (npc.position.Y < Main.player[npc.target].position.Y)
			{
				npc.velocity.Y += .05f;
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
			float num270 = .5f;
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
			Lighting.AddLight((int)npc.position.X / 16, (int)npc.position.Y / 16, 0.4f, 0f, 0.25f);
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
			if (npc.life <= 0)
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Barrow Wight Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Barrow Wight Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Barrow Wight Gore 2"), 1f);
				Dust.NewDust(npc.position, npc.width, npc.height, 45, 0.3f, 0.3f, 200, default(Color), 1f);
				Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default(Color), 2f);
				Dust.NewDust(npc.position, npc.width, npc.height, 45, 0.2f, 0.2f, 200, default(Color), 2f);
				Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default(Color), 3f);
				Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default(Color), 2f);
				Dust.NewDust(npc.position, npc.width, npc.height, 45, 0.2f, 0.2f, 200, default(Color), 4f);
				Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default(Color), 4f);
				Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default(Color), 2f);
				Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default(Color), 4f);
				if (Main.rand.Next(99) < 20) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.BarrowBlade>());
				if (Main.rand.Next(99) < 2) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.PurgingStone>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.CursedSoul>(), 3);
			}
		}
		#endregion

		#region Magic Defense
		public int MagicDefenseValue()
		{
			return 5;
		}
		#endregion

		#region Glowing Eye Effect
		public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		{

			int spriteWidth = npc.frame.Width; //use same number as ini Main.npcFrameCount[npc.type]
			int spriteHeight = Main.npcTexture[ModContent.NPCType<BarrowWightNemesis>()].Height / Main.npcFrameCount[npc.type];

			int spritePosDifX = (int)(npc.frame.Width / 2);
			int spritePosDifY = npc.frame.Height - 3; // was -2

			int frame = npc.frame.Y / spriteHeight;

			int offsetX = (int)(npc.position.X + (npc.width / 2) - Main.screenPosition.X - spritePosDifX + 0.5f);
			int offsetY = (int)(npc.position.Y + npc.height - Main.screenPosition.Y - spritePosDifY);

			SpriteEffects flop = SpriteEffects.None;
			if (npc.spriteDirection == 1)
			{
				flop = SpriteEffects.FlipHorizontally;
			}


			//Glowing Eye Effect
			for (int i = 15; i > -1; i--)
			{
				//draw 3 levels of trail
				int alphaVal = 255 - (15 * i);
				Color modifiedColour = new Color((int)(alphaVal), (int)(alphaVal), (int)(alphaVal), alphaVal);
				spriteBatch.Draw(Main.goreTexture[mod.GetGoreSlot("Gores/Barrow Wight Nemesis Glow")],
					new Rectangle((int)(offsetX - npc.velocity.X * (i * 0.5f)), (int)(offsetY - npc.velocity.Y * (i * 0.5f)), spriteWidth, spriteHeight),
					new Rectangle(0, npc.frame.Height * frame, spriteWidth, spriteHeight),
					modifiedColour,
					0,
					new Vector2(0, 0),
					flop,
					0);
			}
		}
		#endregion


		public void DamagePlayer(Player player, ref int damage) //hook works!
		{

			if (Main.rand.Next(2) == 0)
			{

				player.AddBuff(BuffID.BrokenArmor, 1800, false); //broken armor
				player.AddBuff(BuffID.Invisibility, 3600, false); //invisible
				player.AddBuff(BuffID.Cursed, 300, false); //cursed
				player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 36000, false); //-20 life after several hits
				player.AddBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>(), 36000, false); //-20 life after several hits
			}

			//	if (Main.rand.Next(8) == 0 && player.statLifeMax > 20) 

			//{

			//			Main.NewText("You have been cursed!");
			//	player.statLifeMax -= 20;
			//}
		}
	}
}