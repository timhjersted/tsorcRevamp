using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Fiends
{
	[AutoloadBossHead]
	class FireFiendMarilith : ModNPC
	{
		public override void SetDefaults()
		{
			npc.scale = 1;
			npc.npcSlots = 10;
			Main.npcFrameCount[npc.type] = 8;
			npc.width = 120;
			npc.height = 160;
			npc.damage = 60;
			npc.defense = 38;
			npc.aiStyle = 22;
			animationType = -1;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath6;
			npc.lifeMax = 90000;
			npc.timeLeft = 22500;
			npc.alpha = 100;
			npc.friendly = false;
			npc.noTileCollide = true;
			npc.noGravity = true;
			npc.knockBackResist = 0f;
			npc.lavaImmune = true;
			npc.boss = true;
			npc.value = 600000;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
			bossBag = ModContent.ItemType<Items.BossBags.MarilithBag>();
			despawnHandler = new NPCDespawnHandler("Fire Fiend Marilith decends to the underworld...", Color.OrangeRed, DustID.FireworkFountain_Red);
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Fire Fiend Marilith");
		}

		int lightningDamage = 90;
		int antiMatterBlastDamage = 65;
		int crazedPurpleCrushDamage = 80;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.damage = (int)(npc.damage * 1.3 / 2);
			npc.defense = npc.defense += 12;
		}

		#region AI
		NPCDespawnHandler despawnHandler;
		public override void AI()
		{
			despawnHandler.TargetAndDespawn(npc.whoAmI);
			bool flag25 = false;
		
			if (npc.ai[1] >= 10f && Main.netMode != NetmodeID.MultiplayerClient)
			{
				npc.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
				if (Main.rand.Next(70) == 1)
				{						
					Vector2 projVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 15);
					projVector += Main.rand.NextVector2Circular(5, 5);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Ball>(), lightningDamage, 0f, Main.myPlayer, Main.rand.Next(30, 180));
					Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
					npc.ai[1] = 1f;
				}
				if (Main.rand.Next(220) == 1)
				{						
					Vector2 startVector = new Vector2((npc.position.X + ((((npc.width + 50) * 5f) * (npc.direction * 2)) / 20f) + 130), npc.position.Y + (npc.height - 75));
					Vector2 projVector = UsefulFunctions.GenerateTargetingVector(startVector, Main.player[npc.target].Center, 8);
					projVector += Main.rand.NextVector2Circular(7, 7);
					projVector += Main.player[npc.target].velocity / 2;
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.Okiku.PhasedMatterBlast>(), antiMatterBlastDamage, 0f, Main.myPlayer);
					Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
					npc.ai[1] = 1f;						
				}
				if (Main.rand.Next(20) == 1)
				{						
					Vector2 projVector = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 11);
					projVector += Main.rand.NextVector2Circular(3, 3);
					projVector += Main.player[npc.target].velocity / 2;
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>(), crazedPurpleCrushDamage, 0f, Main.myPlayer);
					Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 17);
					npc.ai[1] = 1f;						
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
		public override bool CheckActive()
		{
			return false;
		}
		public override void BossLoot(ref string name, ref int potionType)
		{
			potionType = ItemID.GreaterHealingPotion;
		}
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


			if (Main.expertMode)
			{
				npc.DropBossBags();
			}
			else
			{
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), 1);
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.GuardianSoul>(), 1);
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.FairyInABottle>(), 1);
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.Ice3Tome>(), 1);
				if (!tsorcRevampWorld.Slain.ContainsKey(npc.type))
				{
					Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 30000);
				}
			}
		}
	}
}