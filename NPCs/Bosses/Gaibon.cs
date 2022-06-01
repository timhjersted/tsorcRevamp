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

			NPC.npcSlots = 5;
			Main.npcFrameCount[NPC.type] = 2;
			NPC.width = 70;
			NPC.height = 70;
			animationType = 62;
			NPC.aiStyle = 22;
			NPC.damage = 50;
			//It genuinely had none in the original.
			NPC.defense = 0;
			music = 12;
			NPC.defense = 10;
			NPC.boss = true;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = Mod.GetLegacySoundSlot(SoundType.NPCKilled, "Sounds/NPCKilled/Gaibon_Roar");
			NPC.lifeMax = 5000;
			NPC.scale = 1.1f;
			NPC.knockBackResist = 0.9f;
			NPC.value = 35000;
			NPC.noTileCollide = true;
			NPC.noGravity = true;
			bossBag = ModContent.ItemType<Items.BossBags.SlograBag>();
			despawnHandler = new NPCDespawnHandler(DustID.Torch);
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Gaibon");
		}

		//Since burning spheres are an NPC, not a projectile, this damage does not get doubled!
		int burningSphereDamage = 60;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.damage = (int)(NPC.damage * 1.3 / 2);
			NPC.defense = NPC.defense += 12;
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
			despawnHandler.TargetAndDespawn(NPC.whoAmI);

			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player thisPlayer = Main.player[i];
				if (thisPlayer != null && thisPlayer.active)
				{
					thisPlayer.AddBuff(ModContent.BuffType<Buffs.GrappleMalfunction>(), 300);
				}
			}

			//If super far away from the player, warp to them
			if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > 5000)
            {
				NPC.Center = new Vector2(Main.player[NPC.target].Center.X, Main.player[NPC.target].Center.Y + 500);
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
					Vector2 dustPos = NPC.Center + dir * dustRadius * 16;
					Vector2 dustVel = dir.RotatedBy(MathHelper.Pi / 2) * speed;
					Dust dustID = Dust.NewDustPerfect(dustPos, 262, dustVel, 200);
					dustID.noGravity = true;
				}

				if (breakCombo == true)
				{
					chargeDamageFlag = true;
					NPC.knockBackResist = 0f;
					Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
					float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
					NPC.velocity.X = (float)(Math.Cos(rotation) * 13) * -1; //12 was 10
					NPC.velocity.Y = (float)(Math.Sin(rotation) * 13) * -1;

					breakCombo = false;
					NPC.netUpdate = true;

				}
				if (chargeDamageFlag == true)
				{
					NPC.damage = 46;
					NPC.knockBackResist = 0f;
					chargeDamage++;
				}
				if (chargeDamage >= 50) //was 45
				{
					chargeDamageFlag = false;
					//npc.dontTakeDamage = false;
					NPC.damage = 40;
					chargeDamage = 0;

					NPC.knockBackResist = 0.3f;
				}
			}

			NPC.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				if (NPC.ai[1] >= 10f)
				{
					if (Main.rand.Next(45) == 1)
					{
						Vector2 randomSpawn = Main.rand.NextVector2CircularEdge(200, 200);
						int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)(NPC.position.X + randomSpawn.X), (int)(NPC.position.Y + randomSpawn.Y), NPCID.BurningSphere, 0);
						Main.npc[spawned].damage = burningSphereDamage;
						Terraria.Audio.SoundEngine.PlaySound(Mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/GaibonSpit2"), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2));
						if (Main.netMode == NetmodeID.Server)
						{
							NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned, 0f, 0f, 0f, 0);
						}
						//npc.netUpdate=true;
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
					NPC.ai[1] = NPC.position.Y; //added -60
					NPC.ai[2] = 0f;
				}
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
			int num260 = (int)(((NPC.position.Y - 30) + (float)NPC.height) / 16f);
			if (NPC.position.Y > Main.player[NPC.target].position.Y)
			{
				//npc.velocity.Y += .1f;
				//if (npc.velocity.Y > +2)
				//{
				//	npc.velocity.Y = -2;
				//}

				NPC.velocity.Y -= 0.05f;
				if (NPC.velocity.Y < -1)
				{
					NPC.velocity.Y = -1;
				}


			}
			if (NPC.position.Y < Main.player[NPC.target].position.Y)
			{
				NPC.velocity.Y += 0.05f;
				if (NPC.velocity.Y > 1)
				{
					NPC.velocity.Y = 1;
				}

				//npc.velocity.Y += .2f;
				//if (npc.velocity.Y > 2)
				//{
				//	npc.velocity.Y = 2;
				//}
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
			float num270 = 2.5f;
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
			return;
		}
		#endregion

		public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
		{
			comboDamage += (int)damage;
			if (comboDamage > 90)
			{
				breakCombo = true;
				NPC.netUpdate = true; //new
				Color color = new Color();
				for (int num36 = 0; num36 < 50; num36++)
				{
					int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.t_Slime, 0, 0, 100, color, 2f);
				}
				for (int num36 = 0; num36 < 20; num36++)
				{
					int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Vile, 0, 0, 100, color, 2f);
				}
				//npc.ai[1] = -200;
				comboDamage = 0;
				NPC.netUpdate = true; //new
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
		public override void OnKill()
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Gaibon Gore 1").Type, 0.9f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Gaibon Gore 2").Type, 0.9f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Gaibon Gore 3").Type, 0.9f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Gaibon Gore 4").Type, 0.9f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Gaibon Gore 2").Type, 0.9f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Gaibon Gore 3").Type, 0.9f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Gaibon Gore 4").Type, 0.9f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Blood Splat").Type, 0.9f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Gores/Blood Splat").Type, 0.9f);
			
			if (!NPC.AnyNPCs(ModContent.NPCType<Slogra>()))
			{
				if (Main.expertMode)
				{
					NPC.DropBossBags();
				}
				else
				{
					Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.PoisonbiteRing>(), 1);
					Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.BloodbiteRing>(), 1);
					Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<DarkSoul>(), (200 + Main.rand.Next(300)));
				}
			}
			else
			{
				int slograID = NPC.FindFirstNPC(ModContent.NPCType<Slogra>());
				int speed = 30;
				for (int i = 0; i < 200; i++)
				{
					Vector2 dir = Vector2.UnitX.RotatedByRandom(MathHelper.Pi);
					Vector2 dustPos = NPC.Center + dir * 3 * 16;
					float distanceFactor = Vector2.Distance(NPC.position, Main.npc[slograID].position) / speed;
					Vector2 speedRand = Vector2.UnitX.RotatedByRandom(MathHelper.Pi) * 10;
					float speedX = (((Main.npc[slograID].position.X + (Main.npc[slograID].width * 0.5f)) - NPC.position.X) / distanceFactor) + speedRand.X;
					float speedY = (((Main.npc[slograID].position.Y + (Main.npc[slograID].height * 0.5f)) - NPC.position.Y) / distanceFactor) + speedRand.Y;
					Vector2 dustSpeed = new Vector2(speedX, speedY);
					Dust dustObj = Dust.NewDustPerfect(dustPos, 173, dustSpeed, 200, default, 3);
					dustObj.noGravity = true;
				}
			}
		}
			#endregion

	}
}