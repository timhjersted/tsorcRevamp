using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Bosses
{
	class AncientOolacileDemon : ModNPC
	{

		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 16;
			animationType = 28;
			npc.height = 120;
			npc.width = 50;
			npc.damage = 44;
			npc.defense = 1;
			npc.lifeMax = 3200;
			npc.scale = 1;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.value = 10000;
			npc.knockBackResist = 0.0f;
			npc.lavaImmune = true;

			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
			despawnHandler = new NPCDespawnHandler("The ancient Oolacile Demon decides to show mercy ...", Color.Gold, DustID.GoldFlame);
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Ancient Oolacile Demon");
		}
		NPCDespawnHandler despawnHandler;
		int meteorDamage = 17;
		int cultistFireDamage = 18;
		int cultistMagicDamage = 25;
		int fireBreathDamage = 14;
		int lostSoulDamage = 19;


		int greatFireballDamage = 35;
		int blackFireDamage = 45;
		int greatAttackDamage = 60;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			meteorDamage = (int)(meteorDamage / 2);
			cultistFireDamage = (int)(cultistFireDamage / 2);
			cultistMagicDamage = (int)(cultistMagicDamage / 2);
			fireBreathDamage = (int)(fireBreathDamage / 2);
			lostSoulDamage = (int)(lostSoulDamage / 2);
			greatFireballDamage = (int)(greatFireballDamage / 2);
			blackFireDamage = (int)(blackFireDamage / 2);
			greatAttackDamage = (int)(greatAttackDamage / 2);
		}
		public Player player
		{
			get => Main.player[npc.target];
		}
		//PROJECTILE HIT LOGIC
		public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
		{
			tsorcRevampAIs.RedKnightOnHit(npc, true);

			//JUSTHIT CODE
			//MELEE RANGE
			if (npc.Distance(player.Center) < 100 && npc.localAI[1] < 70f) //npc.justHit && 
			{
				npc.localAI[1] = 50f;

				//TELEPORT MELEE
				if (Main.rand.Next(12) == 1)
				{
					tsorcRevampAIs.Teleport(npc, 25, true);
				}
			}
			//RISK ZONE
			if (npc.Distance(player.Center) < 300 && npc.localAI[1] < 70f && Main.rand.Next(5) == 1)//npc.justHit && 
			{
				npc.velocity.Y = Main.rand.NextFloat(-5f, -3f); //was 6 and 3
				float v = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(-10f, -7f);
				npc.velocity.X = v;
				//if (Main.rand.Next(2) == 0)
				//{
				//npc.localAI[1] = 80f;
				//} 
				//else
				//{ npc.localAI[1] = 1f; }


				npc.netUpdate = true;
			}
			
		}

		public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
		{

			//tsorcRevampAIs.RedKnightOnHit(npc, projectile.melee);
			//TELEPORT RANGED
			if (Main.rand.Next(26) == 1)
			{
				tsorcRevampAIs.Teleport(npc, 20, true);
				npc.localAI[1] = 70f;
			}
			//RANGED
			if ( npc.Distance(player.Center) > 201 && npc.velocity.Y == 0f && Main.rand.Next(3) == 1)//npc.justHit &&
			{

				npc.velocity.Y = Main.rand.NextFloat(-9f, -3f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(11f, 8f);
				npc.netUpdate = true;

			}
		}


		//int breathTimer = 0;

		//int breathTimer gives weird cool arrow shape, float does the circle
		float breathTimer = 0;
		int spawnedDemons = 0;
		public override void AI()
		{
			
			despawnHandler.TargetAndDespawn(npc.whoAmI);
			int choice = Main.rand.Next(6);


			//CHANCE TO JUMP BEFORE ATTACK  
			if (npc.localAI[1] == 140 && npc.velocity.Y == 0f && Main.rand.Next(50) == 1 && npc.life >= 1001)
			{
				npc.velocity.Y = Main.rand.NextFloat(-9f, -6f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(2f, 1f);
				npc.netUpdate = true;
			}

			if (npc.localAI[1] == 140 && npc.velocity.Y == 0f && Main.rand.Next(33) == 1 && npc.life <= 1000)
			{
				npc.velocity.Y = Main.rand.NextFloat(-7f, -4f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(2f, 1f);
				npc.netUpdate = true;

			}

			//play creature sounds
			if (Main.rand.Next(1700) == 1)
			{
				Main.PlaySound(SoundLoader.customSoundType, (int)npc.position.X, (int)npc.position.Y, mod.GetSoundSlot(Terraria.ModLoader.SoundType.Custom, "Sounds/DarkSouls/low-dragon-growl"), 0.5f, 0.0f);
				//Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish
			}

			npc.localAI[1]++;
			bool lineOfSight = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
			tsorcRevampAIs.FighterAI(npc, 1, 0.1f, canTeleport: true, lavaJumping: true);
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 179, ProjectileID.CultistBossFireBallClone, cultistMagicDamage, 0.1f, Main.rand.Next(220) == 1, false, 2, 17);
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 179, ProjectileID.CultistBossFireBall, cultistMagicDamage, 1, Main.rand.Next(20) == 1, false, 3, 34);
			//tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 160, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 8, Main.rand.Next(2) == 1, false, 2, 34, 0);
			//tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 150, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 8, lineOfSight && Main.rand.Next(200) == 1, false, 2, 34, 0);
			//tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 130, ModContent.ProjectileType<Projectiles.Enemy.EnemyGreatAttack>(), greatAttackDamage, 8, lineOfSight && Main.rand.Next(140) == 1, false, 2, 34);

			//EARLY TELEGRAPH
			if (npc.localAI[1] >= 60)
			{
				Lighting.AddLight(npc.Center, Color.YellowGreen.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.Next(6) == 1)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.GoblinSorcerer, npc.velocity.X, npc.velocity.Y);
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.GoblinSorcerer, npc.velocity.X, npc.velocity.Y); //pink dusts
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.GoblinSorcerer, npc.velocity.X, npc.velocity.Y);
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.GoblinSorcerer, npc.velocity.X, npc.velocity.Y); //pink dusts

					
				}
			}
			//LAST SECOND TELEGRAPH
			if (npc.localAI[1] >= 110)
			{
				Lighting.AddLight(npc.Center, Color.DeepPink.ToVector3() * 5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.Next(2) == 1)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.CrystalSerpent, npc.velocity.X, npc.velocity.Y);
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.CrystalSerpent, npc.velocity.X, npc.velocity.Y); //pink dusts
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.CrystalSerpent, npc.velocity.X, npc.velocity.Y);
				}
			}

			if (breathTimer == 350 && Main.rand.Next(3) == 0)
			{
				breathTimer = 1;
			}
			// NEW BREATH ATTACK 
			breathTimer++;
			if (breathTimer > 480)
			{
				npc.localAI[1] = -50;
				if(npc.life >= 1001)
				{ breathTimer = -20; }
				if (npc.life <= 1000)
				{ breathTimer = -60; }

			}

			if (breathTimer == 470)
			{
				Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 103, 0.6f, 0f); //shadowflame hex (little beasty)
			}

				if (breathTimer < 0)
				{
				if (Main.netMode != NetmodeID.MultiplayerClient)
					{
						//npc.velocity.Y = -1.1f;
						npc.velocity.Y = Main.rand.NextFloat(-4f, -1.1f);
						npc.velocity.X = 0f;

						//play breath sound
						if (Main.rand.Next(3) == 0)
						{
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 34, 0.3f, .1f); //flame thrower
						}

						Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].OldPos(9), 9);
						breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
					
						//Projectile.NewProjectile(npc.Center.X + (5 * npc.direction), npc.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 0f, Main.myPlayer);
						Projectile.NewProjectile(npc.Center.X + (5 * npc.direction), npc.Center.Y - 40f, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 0f, Main.myPlayer);
						npc.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
						npc.localAI[1] = -50;
					}
				}

			if (breathTimer == 361)
			{
				Main.PlaySound(SoundLoader.customSoundType, (int)npc.position.X, (int)npc.position.Y, mod.GetSoundSlot(Terraria.ModLoader.SoundType.Custom, "Sounds/DarkSouls/breath1"), 0.5f, 0.0f);
			}
				if (breathTimer > 360 )
			{
				npc.localAI[1] = -50;
				UsefulFunctions.DustRing(npc.Center, (int)(48 * ((480 - breathTimer) / 120)), DustID.Fire, 48, 4);
				Lighting.AddLight(npc.Center * 2, Color.Red.ToVector3() * 5);
			}

			if (breathTimer == 0)
			{
				npc.localAI[1] = -150;
				//npc.TargetClosest(true);
				npc.velocity.X = 0f;
				//Projectile.NewProjectile(npc.Center.X, npc.Center.Y / 2, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 0f, Main.myPlayer);
			}

			//PLAYER RUNNING AWAY? SPAWN DesertDjinnCurse, 
			Player player3 = Main.player[npc.target];
			if (Main.rand.Next(110) == 1 && npc.Distance(player3.Center) > 700)
			{
				Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 8f, 1.06f, true, true);
				Projectile.NewProjectile(npc.Center, projectileVelocity, ProjectileID.DesertDjinnCurse, lostSoulDamage, 7f, Main.myPlayer);
				//Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
				Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 24, 0.6f, -0.5f); //wobble
																							  //Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
				npc.localAI[1] = 1f;

				npc.netUpdate = true;
			}
			//tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], ProjectileID.LostSoulHostile, lostSoulDamage, 3, lineOfSight, true, 4, 9);
			
			//SPAWN FIRE LURKER
			if ((spawnedDemons < 1) && npc.life <= 2500 && Main.rand.Next(3000) == 1)
			{
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<Enemies.FireLurker>(), 0);
					Main.npc[Spawned].velocity.Y = -8;
					spawnedDemons++;
					if (Main.netMode == NetmodeID.Server)
					{
						NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
					}
				}
			}



			

				
				//CHOICES
				if (npc.localAI[1] >= 160f && (choice == 0 || choice == 4) && npc.life >= 1001)
				{
					bool clearSpace = true;
					for (int i = 0; i < 15; i++)
					{
						if (UsefulFunctions.IsTileReallySolid((int)npc.Center.X / 16, ((int)npc.Center.Y / 16) - i))
						{
							clearSpace = false;
						}
					}
					//LOB ATTACK PURPLE; 
					if (npc.life >= 1001 && npc.life <= 2000 && clearSpace)
					{
						Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 5);

						speed.Y += Main.rand.NextFloat(-2f, -6f);
						//speed += Main.rand.NextVector2Circular(-10, -8);
						if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
						{
							int lob2 = Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, fireBreathDamage, 0f, Main.myPlayer);
			

						Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, -0.5f);

						}
						if (npc.localAI[1] >= 195f)
						{ npc.localAI[1] = 1f; }
					}
					//LOB ATTACK >> BOUNCING FIRE
					if (npc.life >= 2001 && clearSpace)
					
					{
					Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 5);
					speed.Y += Main.rand.NextFloat(2f, -2f);
					//speed += Main.rand.NextVector2Circular(-10, -8);
					if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
					{
						int lob = Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ProjectileID.Fireball, fireBreathDamage, 0f, Main.myPlayer);
						Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, -0.5f);
						if (npc.localAI[1] >= 180f)
						{ npc.localAI[1] = 1f; }
					}
					
				}

				}

				npc.TargetClosest(true);
				//Player player = Main.player[npc.target];


			


				//MULTI-FIRE 1 ATTACK
				if (npc.localAI[1] >= 160f && npc.life >= 1001 && choice == 1) //&& Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0)
				{

					Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].OldPos(4), 7);
					//speed.Y += Main.rand.NextFloat(2f, -2f); //just added
				if (Main.rand.Next(3) == 1 && ((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
					{
						Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 5f, Main.myPlayer); //5f was 0f in the example that works
						Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, -0.5f);

					}

					if (npc.localAI[1] >= 170f)
					{
						npc.localAI[1] = 1f;
					}
					npc.netUpdate = true;
				}
				//MULTI-BOUNCING DESPERATE FIRE ATTACK
				if (npc.localAI[1] >= 160f && npc.life <= 1000 && (choice == 1 || choice == 2))
				{
					Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 3);
					speed.Y += Main.rand.NextFloat(2f, -2f);
					if (Main.rand.Next(2) == 1 && ((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
					{
						Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ProjectileID.Fireball, cultistFireDamage, 3f, Main.myPlayer); //5f was 0f in the example that works
						Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, 0.5f); //fire
					}

					if (npc.localAI[1] >= 190f) //was 126
					{
						npc.localAI[1] = 1f;
					}
					npc.netUpdate = true;
				}
			//LIGHTNING ATTACK
			if (npc.localAI[1] == 160f && npc.life >= 101 && npc.life <= 2000 && (choice == 5 || choice == 4)) //&& Main.rand.Next(8) == 1 
			{
				//&& Main.rand.Next(10) == 1 Main.rand.Next(2) == 0 &&
				Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].OldPos(1), 1);
				//speed += Main.player[npc.target].velocity / 4;

				speed.Y += Main.rand.NextFloat(-2, -5f);//was -2, -6
					
				//speed += Main.rand.NextVector2Circular(-10, -8);
				if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
					{
						int lob = Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ProjectileID.CultistBossLightningOrb, cultistMagicDamage, 0f, Main.myPlayer);
						//ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>()
						//DesertDjinnCurse; ProjectileID.DD2DrakinShot

						Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, -0.5f);

					}
					//if (npc.localAI[1] >= 161f)
					//{ 
					npc.localAI[1] = -50f; 
					//}
				
			}

				/*JUMP DASH FOR FINAL
				if (npc.localAI[1] == 140 && npc.velocity.Y == 0f && Main.rand.Next(20) == 1 && npc.life <= 1000)
				{
					int dust2 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Blue, 1f);
					Main.dust[dust2].noGravity = true;
					npc.velocity.Y = Main.rand.NextFloat(-9f, -6f);
					npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(2f, 1f);
					npc.netUpdate = true;
				}
				*/
				//FINAL JUNGLE FLAMES DESPERATE ATTACK
				if (npc.localAI[1] >= 160f && npc.life <= 1000 && (choice == 0 || choice == 3))
				//if (Main.rand.Next(40) == 1)
				{
					Lighting.AddLight(npc.Center, Color.OrangeRed.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
					if (Main.rand.Next(2) == 1)
					{
						int dust3 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.OrangeRed, 1f);
						Main.dust[dust3].noGravity = true;
					}
					npc.velocity.Y = Main.rand.NextFloat(-3f, -1f);

					Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 5); //last # is speed
					speed += Main.rand.NextVector2Circular(-3, 3);
					speed.Y += Main.rand.NextFloat(3f, -3f); //just added
					if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
					{
						Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.JungleWyvernFire>(), fireBreathDamage, 0f, Main.myPlayer); //5f was 0f in the example that works
																																																		//Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 21, 0.2f, .1f); //3, 21 water
						Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 34, 0.1f, 0.2f);
					}

					if (npc.localAI[1] >= 185f) //was 206
					{
						npc.localAI[1] = -90f;
					}


					npc.netUpdate = true;
				}
			





		}

        public override void HitEffect(int hitDirection, double damage)
        {
			if (npc.life <= 0)
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Ancient Demon Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Ancient Demon Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Ancient Demon Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Ancient Demon Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Ancient Demon Gore 3"), 1f);
			}
		}
        public override void NPCLoot()
		{
			//if not killed before
			if (!(tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AncientOolacileDemon>())))
			{
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.StaminaVessel>(), 1);
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.BandOfGreatCosmicPower>(), 1);
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 5000);
			}

			if (Main.rand.Next(99) < 40) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 1);
			if (Main.rand.Next(99) < 50) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>(), 1);
			
			Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion, 10);
			
			}
	}
}