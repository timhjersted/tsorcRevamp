using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class BasiliskShifter : ModNPC
	{
		//HARD MODE VARIANT 
		public override void SetDefaults()
		{
			npc.npcSlots = 2;
			Main.npcFrameCount[npc.type] = 12;
			animationType = 28;
			
			
			npc.aiStyle = 3;
			npc.damage = 60;
			npc.defense = 60;
			npc.height = 54;
			npc.width = 54;
			npc.lifeMax = 570; //was 870
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.value = 2000;
			npc.lavaImmune = true;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.BasiliskShifterBanner>();

			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[24] = true;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			npc.defense = (int)(npc.defense * (2 / 3));
			cursedBreathDamage = (int)(cursedBreathDamage / 2);
			darkExplosionDamage = (int)(darkExplosionDamage / 2);
			hypnoticDisruptorDamage = (int)(hypnoticDisruptorDamage / 2);
			bioSpitDamage = (int)(bioSpitDamage / 2);
		}


		int breathCD = 120;
		//int previous = 0;
		bool breath = false;

		float shotTimer;
		int chargeDamage = 0;
		bool chargeDamageFlag = false;
		int cursedBreathDamage = 25;
		int darkExplosionDamage = 35;
		int hypnoticDisruptorDamage = 35;
		int bioSpitDamage = 35;


		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player P = spawnInfo.player; //These are mostly redundant with the new zone definitions, but it still works.
			bool Meteor = P.ZoneMeteor;
			bool Jungle = P.ZoneJungle;
			bool Dungeon = P.ZoneDungeon;
			bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
			bool Hallow = P.ZoneHoly;
			bool AboveEarth = P.ZoneOverworldHeight;
			bool InBrownLayer = P.ZoneDirtLayerHeight;
			bool InGrayLayer = P.ZoneRockLayerHeight;
			bool InHell = P.ZoneUnderworldHeight;
			bool FrozenOcean = spawnInfo.spawnTileX > (Main.maxTilesX - 800);
			bool Ocean = spawnInfo.spawnTileX < 800 || FrozenOcean;
			// P.townNPCs > 0f // is no town NPCs nearby

			if (spawnInfo.water) return 0f;

			//SPAWNS IN HM JUNGLE AT NIGHT ABOVE GROUND AFTER THE RAGE IS DEFEATED
			if (Main.hardMode && Jungle && !Corruption && !Main.dayTime && AboveEarth && !Ocean && P.townNPCs <= 0f && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheRage>()) && Main.rand.Next(30) == 1) return 1;

			//SPAWNS IN HM METEOR UNDERGROUND AT NIGHT
			if (Main.hardMode && Meteor && !Main.dayTime && (InBrownLayer || InGrayLayer) && !spawnInfo.water && Main.rand.Next(10) == 1) return 1;

			if (Main.hardMode && Meteor && Main.dayTime && (InBrownLayer || InGrayLayer) && !spawnInfo.water && Main.rand.Next(20) == 1) return 1;

			//SPAWNS AGAIN IN CORRUPTION AND NOW CRIMSON
			if (Main.hardMode && Corruption && !Main.dayTime && !Ocean && (InBrownLayer || InGrayLayer) && !spawnInfo.water && Main.rand.Next(20) == 1) return 1;

			if (Main.hardMode && Corruption && Main.dayTime && !Ocean && (InBrownLayer || InGrayLayer) && !spawnInfo.water && Main.rand.Next(30) == 1) return 1;

			//SPAWNS IN DUNGEON AT NIGHT RARELY
			if (Main.hardMode && Dungeon && !Main.dayTime && (InBrownLayer || InGrayLayer) && Main.rand.Next(40) == 1) return 1;

			//SPAWNS IN HM HALLOW 
			if (Main.hardMode && (InBrownLayer || InGrayLayer) && Hallow && !Ocean && !spawnInfo.water && Main.rand.Next(30) == 1) return 1;

			//SPAWNS RARELY IN HM JUNGLE UNDERGROUND
			if (Main.hardMode && Jungle && InGrayLayer && !Ocean && !spawnInfo.water && Main.rand.Next(70) == 1) return 1;
			
			//BLOODMOON HIGH SPAWN IN METEOR OR JUNGLE
			if (Main.hardMode && !tsorcRevampWorld.SuperHardMode && (Meteor || Jungle) && !Dungeon && (AboveEarth || InBrownLayer || InGrayLayer) && !spawnInfo.water && Main.bloodMoon && Main.rand.Next(5) == 1) return 1;

			return 0;
		}
		#endregion

		public override void AI()
		{
			Player player = Main.player[npc.target];
			tsorcRevampAIs.FighterAI(npc, 1, 0.03f, canTeleport: true, soundType: 26, soundFrequency: 1000, enragePercent: 0.1f, enrageTopSpeed: 2);
			
			if (shotTimer >= 85)
			{
				Lighting.AddLight(npc.Center, Color.GreenYellow.ToVector3() * 1f);
				if (Main.rand.Next(3) == 1)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.EmeraldBolt, npc.velocity.X, npc.velocity.Y);
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.EmeraldBolt, npc.velocity.X, npc.velocity.Y);
				}


				if (shotTimer >= 100f)
				{
					npc.TargetClosest(true);
					//DISRUPTOR ATTACK
					Player player3 = Main.player[npc.target];
					if (Main.rand.Next(200) == 1 && npc.Distance(player3.Center) > 190)
					{
						Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 4f, 1.06f, true, true);
						Projectile.NewProjectile(npc.Center, projectileVelocity, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 5f, Main.myPlayer);
						//Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
						Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 24, 0.6f, -0.5f); //wobble
																									  //Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
						shotTimer = 1f;

						npc.netUpdate = true;
					}

					//CHANCE TO JUMP BEFORE ATTACK
					//FOR MAIN
					if (shotTimer == 105 && Main.rand.Next(3) == 0 && npc.life >= 221)
					{
						//npc.velocity.Y = -6f;
						npc.velocity.Y = Main.rand.NextFloat(-10f, -4f);
					}
					//FOR FINAL
					if (shotTimer >= 185 && Main.rand.Next(3) == 0 && npc.life <= 220)
					{
						npc.velocity.Y = Main.rand.NextFloat(-10f, 3f);
					}

					//BREATH ATTACK
					if (shotTimer >= 110f && Main.rand.Next(20) == 0 && npc.Distance(player.Center) > 260 && npc.life >= 221)
					{
						npc.velocity.Y = Main.rand.NextFloat(-10f, -3f);
						npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(6f, 3f);
						//if (Main.rand.Next(2) == 1)
						//{
						Lighting.AddLight(npc.Center, Color.BlueViolet.ToVector3() * 2f);
						//int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Blue, 1f);
						//Main.dust[dust].noGravity = true;
						//}

						if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
						{
							breath = true;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 34, 0.2f, 0.2f); //flamethrower
																										 //Main.PlaySound(2, -1, -1, 20);
						}

						if (breath)
						{
							Lighting.AddLight(npc.Center, Color.BlueViolet.ToVector3() * 2f);
							float rotation = (float)Math.Atan2(npc.Center.Y - Main.player[npc.target].Center.Y, npc.Center.X - Main.player[npc.target].Center.X);
							Projectile.NewProjectile(npc.Center.X, npc.Center.Y, (float)((Math.Cos(rotation) * 7) * -1), (float)((Math.Sin(rotation) * 15) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedBreathDamage, 0f, Main.myPlayer); //7 was 15
							if (Main.rand.Next(30) == 0)
							{
								Projectile.NewProjectile(npc.Center.X + Main.rand.Next(-500, 500), npc.Center.Y + Main.rand.Next(-500, 500), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DarkExplosion>(), darkExplosionDamage, 0f, Main.myPlayer);
							}

							breathCD--;
						}
						if (breathCD <= 0)
						{
							breath = false;
							breathCD = 120;
							shotTimer = 1f;
						}
					}

					int choice = Main.rand.Next(2);
					//PURPLE MAGIC LOB ATTACK; && Main.rand.Next(2) == 1
					if (shotTimer >= 110f && npc.life >= 221 && choice == 0)
					{


						bool clearSpace = true;
						for (int i = 0; i < 15; i++)
						{
							if (UsefulFunctions.IsTileReallySolid((int)npc.Center.X / 16, ((int)npc.Center.Y / 16) - i))
							{
								clearSpace = false;
							}
						}

						if (clearSpace)
						{
							Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 5);

							speed.Y += Main.rand.NextFloat(-2f, -6f);
							//speed += Main.rand.NextVector2Circular(-10, -8);
							if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
							{
								int lob = Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, bioSpitDamage, 0f, Main.myPlayer);
								//ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>()
								//DD2DrakinShot; DesertDjinnCurse; ProjectileID.DD2DrakinShot
								//if (projectile_velocity <= 0f)
								//{ Main.projectile[lob].tileCollide = false; }
								//else if (projectile_velocity >= 1f)
								//{ Main.projectile[lob].tileCollide = true; }

								//Main.projectile[lob].hostile = true;
								//Main.projectile[num555].timeLeft = 300; //40
								Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, -0.5f);

							}

							if (shotTimer >= 154f)
							{ 
								shotTimer = 1f;
							}
						}
					}

					//NORMAL SPIT ATTACK
					if (shotTimer >= 115f && npc.life >= 221 && choice >= 1)
					{
						if (Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
						{
							Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 9);

							if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
							{
								int num555 = Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer);
								Main.projectile[num555].timeLeft = 300; //40
								Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, -0.5f);
								shotTimer = 1f;
							}
						}
					}

					//FINAL DESPERATE ATTACK
					if (shotTimer >= 175f && Main.rand.Next(2) == 1 && npc.life <= 220)
					{
						int dust2 = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.Blue, 1f);
						Main.dust[dust2].noGravity = true;

						Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 10);

						if (((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
						{
							Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer);
							//Main.PlaySound(4, (int)npc.position.X, (int)npc.position.Y, 9);
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, -0.1f);
							//customAi1 = 1f;
						}

						if (shotTimer >= 206f)
						{
							shotTimer = 1f;
						}
					}
				}
			}

			//Knockback conditional
			if (npc.life >= 221)
			{
				npc.knockBackResist = 0.04f;
			}
			else
			{
				npc.knockBackResist = 0.0f;
			}

			//MAKE SOUND WHEN JUMPING/HOVERING
			if (Main.rand.Next(12) == 0 && npc.velocity.Y <= -1f)
			{
				Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 24, 0.2f, .1f);
			}

			//TELEGRAPH DUSTS
			if (shotTimer >= 100)
			{
				Lighting.AddLight(npc.Center, Color.Purple.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.Next(3) == 1)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, npc.velocity.X, npc.velocity.Y);
					//Dust.NewDust(npc.position, npc.width, npc.height, DustID.EmeraldBolt, npc.velocity.X, npc.velocity.Y);
				}
			}

			//reset attack timer when hit in melee range
			if (npc.justHit && npc.Distance(player.Center) < 100)
			{
				shotTimer = 10f;
			}

			//jump back when hit at close range; && npc.life >= 221
			if (npc.justHit && npc.Distance(player.Center) < 150 && Main.rand.Next(2) == 1)
			{

				npc.velocity.Y = Main.rand.NextFloat(-6f, -4f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(-7f, -4f);
				shotTimer = 50f;
				npc.netUpdate = true;
			}

			//jump forward when hit at range; && npc.life >= 221
			if (npc.justHit && npc.Distance(player.Center) > 150 && Main.rand.Next(2) == 1)
			{
				npc.velocity.Y = Main.rand.NextFloat(-10f, -3f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(7f, 3f);
				npc.netUpdate = true;

			}

			//Shift toward the player randomly
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				if (Main.rand.Next(80) == 1 && npc.Distance(player.Center) > 200)
				{
					Lighting.AddLight(npc.Center, Color.Red.ToVector3() * 3f); //Pick a color, any color. The 0.5f tones down its intensity by 50%

					chargeDamageFlag = true;
					Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
					float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
					npc.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
					npc.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
					npc.netUpdate = true;

				}
				if (chargeDamageFlag == true)
				{
					Lighting.AddLight(npc.Center, Color.OrangeRed.ToVector3() * 5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%

					npc.damage = 70;
					chargeDamage++;
				}
				if (chargeDamage >= 70)
				{
					chargeDamageFlag = false;
					npc.damage = 60;
					chargeDamage = 0;
				}
			}
		}

		#region Find Frame
		public override void FindFrame(int currentFrame)
		{
			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
			}			
			if (npc.velocity.Y == 0f)
			{
				if (npc.direction == 1)
				{
					npc.spriteDirection = 1;
				}
				if (npc.direction == -1)
				{
					npc.spriteDirection = -1;
				}
				if (npc.velocity.X == 0f)
				{
					npc.frame.Y = 0;
					npc.frameCounter = 0.0;
				}
				else
				{
					npc.frameCounter += (double)(Math.Abs(npc.velocity.X) * 2f);
					npc.frameCounter += 1.0;
					if (npc.frameCounter > 6.0)
					{
						npc.frame.Y = npc.frame.Y + num;
						npc.frameCounter = 0.0;
					}
					if (npc.frame.Y / num >= Main.npcFrameCount[npc.type])
					{
						npc.frame.Y = num * 2;
					}
				}
			}
			else
			{
				npc.frameCounter = 0.0;
				npc.frame.Y = num;
				npc.frame.Y = 0;
			}
		}

		#endregion

		#region Debuffs
		public override void OnHitPlayer(Player player, int damage, bool crit)
		{
			if (Main.rand.Next(2) == 0)
			{
				player.AddBuff(37, 10800, false); //horrified
				player.AddBuff(20, 600, false); //poisoned

			}
			if (Main.rand.Next(8) == 0)
			{
				player.AddBuff(36, 600, false); //broken armor
				player.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800, false);
				player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 life if counter hits 100
			}
		}
		#endregion

		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 1"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 1"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
			for (int i = 0; i < 10; i++)
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Blood Splat"), 1.1f);
			}

			if (Main.rand.Next(100) < 20) Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion);
		}
	}
}