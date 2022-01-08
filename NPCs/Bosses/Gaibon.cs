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

			npc.npcSlots = 5;
			Main.npcFrameCount[npc.type] = 2;
			npc.width = 70;
			npc.height = 70;
			animationType = 62;
			npc.aiStyle = 22;
			npc.damage = 50;
			//It genuinely had none in the original.
			npc.defense = 0;
			music = 12;
			npc.defense = 10;
			npc.boss = true;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = mod.GetLegacySoundSlot(SoundType.NPCKilled, "Sounds/NPCKilled/Gaibon_Roar");
			npc.lifeMax = 5000;
			npc.scale = 1.1f;
			npc.knockBackResist = 0.9f;
			npc.value = 35000;
			npc.noTileCollide = true;
			npc.noGravity = true;
			bossBag = ModContent.ItemType<Items.BossBags.SlograBag>();
			despawnHandler = new NPCDespawnHandler(DustID.Fire);
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Gaibon");
		}

		//Since burning spheres are an NPC, not a projectile, this damage does not get doubled!
		int burningSphereDamage = 60;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.damage = (int)(npc.damage * 1.3 / 2);
			npc.defense = npc.defense += 12;
			//For some reason, its contact damage doesn't get doubled due to expert mode either apparently?
			//burningSphereDamage = (int)(burningSphereDamage / 2);
		}
		

		#region AI
		NPCDespawnHandler despawnHandler;
		bool slograDead = false;
		int comboDamage = 0;
		bool breakCombo = false;
		bool chargeDamageFlag = false;
		int chargeDamage = 0;
		float dustRadius = 20;
		float dustMin = 3;
		public override void AI()
		{
			despawnHandler.TargetAndDespawn(npc.whoAmI);

			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player thisPlayer = Main.player[i];
				if (thisPlayer != null && thisPlayer.active)
				{
					thisPlayer.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 300);
				}
			}

			//If super far away from the player, warp to them
			if (Vector2.Distance(npc.Center, Main.player[npc.target].Center) > 5000)
            {
				npc.Center = new Vector2(Main.player[npc.target].Center.X, Main.player[npc.target].Center.Y + 500);
            }

			//If Slogra is dead, we don't need to keep calling AnyNPCs.
			if (!slograDead)
			{
				if (!NPC.AnyNPCs(ModContent.NPCType<Slogra>()))
				{
					slograDead = true;
				}
			}
			else
			{
				if (dustRadius > dustMin)
				{
					dustRadius -= 0.25f;
				}

				int dustPerTick = 20;
				float speed = 2;
				for (int i = 0; i < dustPerTick; i++)
				{
					Vector2 dir = Vector2.UnitX.RotatedByRandom(MathHelper.Pi);
					Vector2 dustPos = npc.Center + dir * dustRadius * 16;
					Vector2 dustVel = dir.RotatedBy(MathHelper.Pi / 2) * speed;
					Dust dustID = Dust.NewDustPerfect(dustPos, 262, dustVel, 200);
					dustID.noGravity = true;
				}

				if (breakCombo == true)
				{
					chargeDamageFlag = true;
					npc.knockBackResist = 0f;
					Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
					float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
					npc.velocity.X = (float)(Math.Cos(rotation) * 13) * -1; //12 was 10
					npc.velocity.Y = (float)(Math.Sin(rotation) * 13) * -1;

					breakCombo = false;
					npc.netUpdate = true;

				}
				if (chargeDamageFlag == true)
				{
					npc.damage = 46;
					npc.knockBackResist = 0f;
					chargeDamage++;
				}
				if (chargeDamage >= 50) //was 45
				{
					chargeDamageFlag = false;
					//npc.dontTakeDamage = false;
					npc.damage = 40;
					chargeDamage = 0;

					npc.knockBackResist = 0.3f;
				}
			}

			npc.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				if (npc.ai[1] >= 10f)
				{
					if (Main.rand.Next(45) == 1)
					{
						Vector2 randomSpawn = Main.rand.NextVector2CircularEdge(200, 200);
						int spawned = NPC.NewNPC((int)(npc.position.X + randomSpawn.X), (int)(npc.position.Y + randomSpawn.Y), NPCID.BurningSphere, 0);
						Main.npc[spawned].damage = burningSphereDamage;
						Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/GaibonSpit2"), (int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2));
						if (Main.netMode == NetmodeID.Server)
						{
							NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
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

		public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
		{
			comboDamage += (int)damage;
			if (comboDamage > 90)
			{
				breakCombo = true;
				npc.netUpdate = true; //new
				Color color = new Color();
				for (int num36 = 0; num36 < 50; num36++)
				{
					int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.t_Slime, 0, 0, 100, color, 2f);
				}
				for (int num36 = 0; num36 < 20; num36++)
				{
					int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.Vile, 0, 0, 100, color, 2f);
				}
				//npc.ai[1] = -200;
				comboDamage = 0;
				npc.netUpdate = true; //new
			}
			return true;
			//if (!npc.justHit)
			//{
			//comboDamage --;

			//	if (comboDamage < 0)
			//	{
			//	comboDamage = 0;
			//	}
			//}
		}

		public override bool CheckActive()
		{
			return false;
		}
		public override void BossLoot(ref string name, ref int potionType)
		{
			potionType = ItemID.GreaterHealingPotion;
		}

		#region gore
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Gaibon Gore 1"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Gaibon Gore 2"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Gaibon Gore 3"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Gaibon Gore 4"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Gaibon Gore 2"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Gaibon Gore 3"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Gaibon Gore 4"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Blood Splat"), 0.9f);
			
			if (!NPC.AnyNPCs(ModContent.NPCType<Slogra>()))
			{
				if (Main.expertMode)
				{
					npc.DropBossBags();
				}
				else
				{
					Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.PoisonbiteRing>(), 1);
					Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.BloodbiteRing>(), 1);
					Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), (200 + Main.rand.Next(300)));
				}
			}
			else
			{
				int slograID = NPC.FindFirstNPC(ModContent.NPCType<Slogra>());
				int speed = 30;
				for (int i = 0; i < 200; i++)
				{
					Vector2 dir = Vector2.UnitX.RotatedByRandom(MathHelper.Pi);
					Vector2 dustPos = npc.Center + dir * 3 * 16;
					float distanceFactor = Vector2.Distance(npc.position, Main.npc[slograID].position) / speed;
					Vector2 speedRand = Vector2.UnitX.RotatedByRandom(MathHelper.Pi) * 10;
					float speedX = (((Main.npc[slograID].position.X + (Main.npc[slograID].width * 0.5f)) - npc.position.X) / distanceFactor) + speedRand.X;
					float speedY = (((Main.npc[slograID].position.Y + (Main.npc[slograID].height * 0.5f)) - npc.position.Y) / distanceFactor) + speedRand.Y;
					Vector2 dustSpeed = new Vector2(speedX, speedY);
					Dust dustObj = Dust.NewDustPerfect(dustPos, 173, dustSpeed, 200, default, 3);
					dustObj.noGravity = true;
				}
			}
		}
			#endregion

	}
}