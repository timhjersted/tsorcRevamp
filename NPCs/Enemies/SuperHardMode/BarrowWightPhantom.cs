using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class BarrowWightPhantom : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Barrow Wight Phantom");
		}
		public override void SetDefaults()
		{
			

			NPC.npcSlots = 1;
			Main.npcFrameCount[NPC.type] = 4;
			NPC.width = 58;
			NPC.height = 48;
			NPC.aiStyle = 22;
			NPC.damage = 100;
			NPC.defense = 15;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath6;
			NPC.lifeMax = 1000;
			NPC.knockBackResist = 0;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.value = 0;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.BarrowWightPhantomBanner>();
			NPC.buffImmune[BuffID.Poisoned] = true;
			NPC.buffImmune[BuffID.Confused] = true;
		}

		int dragonsBreathDamage = 33;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			NPC.damage = (int)(NPC.damage / 2);
			dragonsBreathDamage = (int)(dragonsBreathDamage * tsorcRevampWorld.SubtleSHMScale);
		}

		int breathCD = 45;
		//int previous = 0;
		bool breath = false;
		int chargeDamage = 0;
		bool chargeDamageFlag = false;

		//Spawns from Abysmal Oolacile Sorcerer



		#region AI
		public override void AI()
		{
			//bool flag25;
			NPC.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
			if (NPC.ai[1] >= 10f)
			{
				NPC.TargetClosest(true);



				// charge forward code 
				if (Main.rand.Next(2650) == 1)
				{
					chargeDamageFlag = true;
					Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
					float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
					NPC.velocity.X = (float)(Math.Cos(rotation) * 11) * -1;
					NPC.velocity.Y = (float)(Math.Sin(rotation) * 11) * -1;
					NPC.ai[1] = 1f;
					NPC.netUpdate = true;
				}
				if (chargeDamageFlag == true)
				{
					NPC.damage = 115;
					chargeDamage++;
				}
				if (chargeDamage >= 115)
				{
					chargeDamageFlag = false;
					NPC.damage = 105;
					chargeDamage = 0;
				}

				// fire breath attack
				if (Main.rand.Next(2075) == 0)
				{
					breath = true;
					Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 20);
				}
				if (breath)
				{
					//while (breathCD > 0) {
					//for (int pcy = 0; pcy < 10; pcy++) {
					Projectile.NewProjectile(NPC.position.X + (float)NPC.width / 2f, NPC.position.Y + (float)NPC.height / 2f, NPC.velocity.X * 2f + (float)Main.rand.Next(-2, 3), NPC.velocity.Y * 3f + (float)Main.rand.Next(-2, 3), ModContent.ProjectileType<Projectiles.Enemy.FrozenDragonsBreath>(), dragonsBreathDamage, 1.2f, Main.myPlayer); //96 was Config.projDefs.byName["Enemy Light Spirit"].type,  85 is damage
																																																																											 //}
					breathCD--;
					//}
				}
				if (breathCD <= 0)
				{
					breath = false;
					breathCD = 30;
					//Terraria.Audio.SoundEngine.PlaySound(2, -1, -1, 20);
				}
				//end fire breath attack


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
			if (NPC.position.Y > Main.player[NPC.target].position.Y)
			{
				NPC.velocity.Y -= .05f;
			}
			if (NPC.position.Y < Main.player[NPC.target].position.Y)
			{
				NPC.velocity.Y += .05f;
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
			float num270 = .5f;
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
			if (NPC.directionY == -1 && (double)NPC.velocity.Y > -2.5)
			{
				NPC.velocity.Y = NPC.velocity.Y - 0.04f;
				if ((double)NPC.velocity.Y > 2.5)
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
				if ((double)NPC.velocity.Y < -2.5)
				{
					NPC.velocity.Y = -2.5f;
				}
			}
			else
			{
				if (NPC.directionY == 1 && (double)NPC.velocity.Y < 2.5)
				{
					NPC.velocity.Y = NPC.velocity.Y + 0.04f;
					if ((double)NPC.velocity.Y < -2.5)
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
					if ((double)NPC.velocity.Y > 2.5)
					{
						NPC.velocity.Y = 2.5f;
					}
				}
			}
			Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.4f, 0f, 0.25f);
			return;
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
			if (NPC.life <= 0)
			{
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Barrow Wight Gore 1"), 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Barrow Wight Gore 2"), 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Barrow Wight Gore 2"), 1f);

				Dust.NewDust(NPC.position, NPC.width, NPC.height, 45, 0.3f, 0.3f, 200, default(Color), 1f);
				Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 2f);
				Dust.NewDust(NPC.position, NPC.width, NPC.height, 45, 0.2f, 0.2f, 200, default(Color), 2f);
				Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 3f);
				Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 2f);
				Dust.NewDust(NPC.position, NPC.width, NPC.height, 45, 0.2f, 0.2f, 200, default(Color), 4f);
				Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 4f);
				Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 2f);
				Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 4f);
				if (Main.rand.Next(99) < 5) Item.NewItem(NPC.getRect(), ItemID.Heart);
			}
		}
		#endregion

		#region Magic Defense
		public int MagicDefenseValue()
		{
			return 5;
		}
		#endregion



		public override void OnHitPlayer(Player player, int target, bool crit) 
		{
				player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 36000, false); //-20 life after several hits
				//player.AddBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>(), 36000, false); //-100 life after several hits	
			

			if (Main.rand.Next(4) == 0)
			{

				player.AddBuff(36, 150, false); //broken armor
				player.AddBuff(BuffID.Chilled, 600, false); //Chilled
				player.AddBuff(BuffID.Frostburn, 600, false); //Frostburn

			}

			//	if (Main.rand.Next(14) == 0 && player.statLifeMax > 20) 

			//{

			//			Main.NewText("You have been cursed!");
			//	player.statLifeMax -= 20;
			//}
		}
	}
}