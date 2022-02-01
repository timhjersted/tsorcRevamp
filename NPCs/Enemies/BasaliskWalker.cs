using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class BasaliskWalker : ModNPC
	{

		float customAi1;
		int drownTimerMax = 2000;
		int drownTimer = 2000;
		int drowningRisk = 1200;
		int boredTimer = 0;
		int tBored = 1;//increasing this increases how long it take for the NP to get bored
		int boredResetT = 0;
		int bReset = 50;//increasing this will increase how long an NPC "gives up" before coming back to try again.
		int chargeDamage = 0;
		bool chargeDamageFlag = false;
		int meteorDamage = 17;
		int hypnoticDisruptorDamage = 30; //was 15
		int bioSpitDamage = 20; //was 10


		public override void SetDefaults()
		{
			npc.npcSlots = 2;
			Main.npcFrameCount[npc.type] = 12;
			animationType = 28;
			npc.knockBackResist = 0.3f;
			npc.aiStyle = 3;
			npc.damage = 50;
			npc.defense = 8;
			npc.height = 50;
			npc.width = 24;
			npc.lifeMax = 180; //was 280

			if (Main.hardMode)
			{
				npc.lifeMax = 380;
				npc.defense = 20;
				npc.value = 1000;
				npc.damage = 60;
				hypnoticDisruptorDamage = 45;
				bioSpitDamage = 35;
			}

			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.value = 400; //was 2000
			npc.lavaImmune = true;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.BasaliskWalkerBanner>();

			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[24] = true;
		}



		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			meteorDamage = (int)(meteorDamage / 2);
			hypnoticDisruptorDamage = (int)(hypnoticDisruptorDamage / 2);
			bioSpitDamage = (int)(bioSpitDamage / 2);
		}
		
	


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

			if (spawnInfo.invasion)
			{
				return 0;
			}

			if (spawnInfo.water) return 0f;

			if (!Main.hardMode && !Main.dayTime && (Corruption || Jungle) && AboveEarth && P.townNPCs <= 0f && tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronHead) && Main.rand.Next(24) == 1) return 1;

			//new chance to spawn in the corruption or crimson below ground (poison and cursed aren't activated until EoW and Skeletron respectively for balance; now we'll finally have a unique mod npc that fits well in these zones)
			if (!Main.hardMode && Corruption && !Main.dayTime && !AboveEarth && Main.rand.Next(20) == 1) return 1;

			if (!Main.hardMode && Corruption && Main.dayTime && !AboveEarth && Main.rand.Next(40) == 1) return 1;

			//jungle or meteor
			if (!Main.hardMode && Meteor && !Dungeon && !Main.dayTime && (InBrownLayer || InGrayLayer) && Main.rand.Next(10) == 1) return 1;

			if (!Main.hardMode && Meteor && !Dungeon && Main.dayTime && InGrayLayer && Main.rand.Next(20) == 1) return 1; //was 60

			if (!Main.hardMode && Jungle && Main.dayTime && !Dungeon && InGrayLayer && Main.rand.Next(100) == 1) return 1; //was 200

			if (!Main.hardMode && Jungle && !Main.dayTime && !Dungeon && InGrayLayer && Main.rand.Next(80) == 1) return 1; //was 850

			//hard mode
			if (Main.hardMode && P.townNPCs <= 0f && !Main.dayTime && (Meteor || Jungle || Corruption) && !Dungeon && (AboveEarth || InBrownLayer || InGrayLayer) && Main.rand.Next(45) == 1) return 1;

			if (Main.hardMode && P.townNPCs <= 0f && Main.dayTime && (Meteor || Jungle || Corruption) && !Dungeon && (InBrownLayer || InGrayLayer) && Main.rand.Next(55) == 1) return 1;

			return 0;
		}
		#endregion

		#region AI // code by GrtAndPwrflTrtl (http://www.terrariaonline.com/members/grtandpwrfltrtl.86018/)
		public override void AI()  //  warrior ai
		{
			#region set up NPC's attributes & behaviors
			// set parameters
			//  is_archer OR can_pass_doors OR shoot_and_walk, pick only 1.  They use the same ai[] vars (1&2)
			bool is_archer = false; // stops and shoots when target sighted; skel archer & gob archer are the archers
			bool can_pass_doors = false;  //  can open or break doors; c. bunny, crab, clown, skel archer, gob archer, & chaos elemental cannot
			bool shoot_and_walk = true;  //  can shoot while walking like clown; uses ai[2] so cannot be used with is_archer or can_pass_doors

			//  can_teleport==true code uses boredom_time and ai[3] (boredom), but not mutually exclusive
			bool can_teleport = false;  //  tp around like chaos ele
			int boredom_time = 60; // time until it stops targeting player if blocked etc, 60 for anything but chaos ele, 20 for chaos ele
			int boredom_cooldown = 10 * boredom_time; // boredom level where boredom wears off; usually 10*boredom_time

			bool hates_light = false;  //  flees in daylight like: Zombie, Skeleton, Undead Miner, Doctor Bones, The Groom, Werewolf, Clown, Bald Zombie, Possessed Armor
			bool can_pass_doors_bloodmoon_only = false;  //  can open or break doors, but only during bloodmoon: zombies & bald zombies. Will keep trying anyway.

			int sound_type = 26; // Parameter for Main.PlaySound().  14 for Zombie, Skeleton, Angry Bones, Heavy Skeleton, Skeleton Archer, Bald Zombie.  26 for Mummy, Light & Dark Mummy. 0 means no sounds
			int sound_frequency = 1000;  //  chance to play sound every frame, 1000 for zombie/skel, 500 for mummies

			float acceleration = .03f;  //  how fast it can speed up
			float top_speed = 1f;  //  max walking speed, also affects jump length
			//float braking_power = .2f;  //  %of speed that can be shed every tick when above max walking speed
			double bored_speed = .9;  //  above this speed boredom decreases(if not already bored); usually .9

			float enrage_percentage = .2f;  //  double movement speed below this life fraction. 0 for no enrage. Mummies enrage below .5
			float enrage_acceleration = .14f;  //  faster when enraged, usually 2*acceleration
			float enrage_top_speed = 2;  //  faster when enraged, usually 2*top_speed

			bool clown_sized = false; // is hitbox the same as clowns' for purposes of when to jump?
			bool jump_gaps = true; // attempt to jump gaps; everything but crabs do this

			bool hops = false; // hops when close to target like Angry Bones, Corrupt Bunny, Armored Skeleton, and Werewolf
			float hop_velocity = 1f; // forward velocity needed to initiate hopping; usually 1
			float hop_range_x = 100; // less than this is 'close to target'; usually 100
			float hop_range_y = 50; // less than this is 'close to target'; usually 50
			float hop_power = 4; // how hard/high offensive hops are; usually 4
			float hop_speed = 3; // how fast hops can accelerate vertically; usually 3 (2xSpd is 4 for Hvy Skel & Werewolf so they're noticably capped)

			// is_archer & clown bombs only
			int shot_rate = 70;  //  rate at which archers/bombers fire; 70 for skeleton archer, 180 for goblin archer, 450 for clown; atm must be an even # or won't fire at shot_rate/2
			//int fuse_time = 300;  //  fuse time on bombs, 300 for clown bombs
			//int projectile_damage = 35;  //  projectile dmg: 35 for Skeleton Archer, 11 for Goblin Archer
			int projectile_id = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellMeteor>(); // projectile id: 82(Flaming Arrow) for Skeleton Archer, 81(Wooden Arrow) for Goblin Archer, 75(Happy Bomb) for Clown
			float projectile_velocity = 11; // initial velocity? 11 for Skeleton Archers, 9 for Goblin Archers, bombs have fixed speed & direction atm

			// can_pass_doors only
			float door_break_pow = 2; // 10 dmg breaks door; 2 for goblin thief and 7 for Angry Bones; 1 for others
			bool breaks_doors = false; // meaningless unless can_pass_doors; if this is true the door breaks down instead of trying to open; Goblin Peon is only warrior to do this

			// Omnirs creature sorts
			//bool tooBig = false; // force bigger creatures to jump
			//bool lavaJumping = true; // Enemies jump on lava.
			bool canDrown = false; // They will drown if in the water for too long
			bool quickBored = false; //Enemy will respond to boredom much faster(? -- test)
			bool oBored = false; //Whether they're bored under the "quickBored" conditions

			// calculated parameters
			bool moonwalking = false;  //  not jump/fall and moving backwards to facing
			if (npc.velocity.Y == 0f && ((npc.velocity.X > 0f && npc.direction < 0) || (npc.velocity.X < 0f && npc.direction > 0)))
				moonwalking = false;
			#endregion
			//-------------------------------------------------------------------
			#region teleportation particle effects
			if (can_teleport)  //  chaos elemental type teleporter
			{
				if (npc.ai[3] == -120f)  //  boredom goes negative? I think this makes disappear/arrival effects after it just teleported
				{
					npc.velocity *= 0f; // stop moving
					npc.ai[3] = 0f; // reset boredom to 0
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 8);
					Vector2 vector = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f); // current location
					float num6 = npc.oldPos[2].X + (float)npc.width * 0.5f - vector.X; // direction to where it was 3 frames ago?
					float num7 = npc.oldPos[2].Y + (float)npc.height * 0.5f - vector.Y; // direction to where it was 3 frames ago?
					float num8 = (float)Math.Sqrt((double)(num6 * num6 + num7 * num7)); // distance to where it was 3 frames ago?
					num8 = 2f / num8; // to normalize to 2 unit long vector
					num6 *= num8; // direction to where it was 3 frames ago, vector normalized
					num7 *= num8; // direction to where it was 3 frames ago, vector normalized
					for (int j = 0; j < 20; j++) // make 20 dusts at current position
					{
						int num9 = Dust.NewDust(npc.position, npc.width, npc.height, 71, num6, num7, 200, default(Color), 2f);
						Main.dust[num9].noGravity = true; // floating
						Dust expr_19EE_cp_0 = Main.dust[num9]; // make a dust handle?
						expr_19EE_cp_0.velocity.X = expr_19EE_cp_0.velocity.X * 2f; // faster in x direction
					}
					for (int k = 0; k < 20; k++) // more dust effects at old position
					{
						int num10 = Dust.NewDust(npc.oldPos[2], npc.width, npc.height, 71, -num6, -num7, 200, default(Color), 2f);
						Main.dust[num10].noGravity = true;
						Dust expr_1A6F_cp_0 = Main.dust[num10];
						expr_1A6F_cp_0.velocity.X = expr_1A6F_cp_0.velocity.X * 2f;
					}
				} // END just teleported
			} // END can teleport
			#endregion
			//-------------------------------------------------------------------
			#region adjust boredom level
			if (!is_archer || npc.ai[2] <= 0f)  //  loop to set ai[3] (boredom)
			{
				if (npc.position.X == npc.oldPosition.X || npc.ai[3] >= (float)boredom_time || moonwalking)  //  stopped or bored or moonwalking
					npc.ai[3] += 1f; // increase boredom
				else if ((double)Math.Abs(npc.velocity.X) > bored_speed && npc.ai[3] > 0f)  //  moving fast and not bored
					npc.ai[3] -= 1f; // decrease boredom

				if (npc.justHit || npc.ai[3] > boredom_cooldown)
					npc.ai[3] = 0f; // boredom wears off if enough time passes, or if hit

				if (npc.ai[3] == (float)boredom_time)
					npc.netUpdate = true; // netupdate when state changes to bored
			}
			#endregion
			//-------------------------------------------------------------------
			#region play creature sounds, target/face player, respond to boredom
			if ((!hates_light || !Main.dayTime || (double)npc.position.Y > Main.worldSurface * 16.0) && npc.ai[3] < (float)boredom_time)
			{  // not fleeing light & not bored
				if (sound_type > 0 && Main.rand.Next(sound_frequency) <= 0)
					Main.PlaySound(sound_type, (int)npc.position.X, (int)npc.position.Y, 1); // random creature sounds
				if (!canDrown || (canDrown && !npc.wet) || (quickBored && boredTimer > tBored))
				{
					npc.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)
				}
			}
			else if (!is_archer || npc.ai[2] <= 0f) //  fleeing light or bored (& not aiming)
			{
				if (hates_light && Main.dayTime && (double)(npc.position.Y / 16f) < Main.worldSurface && npc.timeLeft > 10)
					npc.timeLeft = 10;  //  if hates light & in light, hasten despawn

				if (npc.velocity.X == 0f)
				{
					if (npc.velocity.Y == 0f)
					{ // not moving
						if (npc.ai[0] == 0f)
							npc.ai[0] = 1f; // facing change delay
						else
						{ // change movement and facing direction, reset delay
							npc.direction *= -1;
							npc.spriteDirection = npc.direction;
							npc.ai[0] = 0f;
						}
					}
				}
				else // moving in x direction,
					npc.ai[0] = 0f; // reset facing change delay

				if (npc.direction == 0) // what does it mean if direction is 0?
					npc.direction = 1; // flee right if direction not set? or is initial direction?
			} // END fleeing light or bored (& not aiming)
			#endregion
			//-------------------------------------------------------------------
			#region enrage
			bool enraged = false; // angry from damage; not stored from tick to tick
			if ((enrage_percentage > 0) && (npc.life < (float)npc.lifeMax * enrage_percentage))  //  speed up at low life
				enraged = true;
			if (enraged)
			{ // speed up movement if enraged
				acceleration = enrage_acceleration;
				top_speed = enrage_top_speed;
			}
			#endregion
			//-------------------------------------------------------------------
			#region melee movement

			//int dust = Dust.NewDust(new Vector2((float) npc.position.X, (float) npc.position.Y), npc.width, npc.height, 6, npc.velocity.X-6f, npc.velocity.Y, 150, Color.Blue, 1f);
			//			Main.dust[dust].noGravity = true;



			#endregion
			

			#region shoot and walk
			if (!oBored && shoot_and_walk && Main.netMode != 1 && !Main.player[npc.target].dead) // can generalize this section to moving+projectile code 
			{

                #region LOGIC
				//should this be added to other npcs?
                bool flag2 = false;
				int num5 = 60;
				bool flag3 = true;
				if (npc.velocity.Y == 0f && (npc.velocity.X == 0f && npc.direction < 0))
				{
					npc.velocity.Y -= 8f;
					npc.velocity.X -= 2f;
				}
				else if (npc.velocity.Y == 0f && (npc.velocity.X == 0f && npc.direction > 0))
				{
					npc.velocity.Y -= 8f;
					npc.velocity.X += 2f;
				}
				if (npc.lavaWet)
				{
					npc.velocity.Y -= 2;
				}
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
				
				//JUSTHIT CODE
				Player player2 = Main.player[npc.target];
				if (npc.justHit && npc.Distance(player2.Center) < 100)
				{
					customAi1 = 40f;
					npc.ai[3] = 0f;
				}
				if (npc.justHit && npc.Distance(player2.Center) < 150 && Main.rand.Next(2) == 1)
				{
					customAi1 = 100f;
					npc.velocity.Y = Main.rand.NextFloat(-6f, -3f);
					npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(-5f, -3f);
					npc.netUpdate = true;
				}
				if (npc.justHit && npc.Distance(player2.Center) > 150 && Main.rand.Next(2) == 1)
				{
					npc.velocity.Y = Main.rand.NextFloat(-5f, -2f);
					npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(-5f, 3f);
					npc.netUpdate = true;
				}
				if (npc.ai[3] == (float)num5)
				{
					npc.netUpdate = true;
				}
				else
				{
					if (npc.velocity.X == 0f)
					{
						if (npc.velocity.Y == 0f)
						{
							npc.ai[0] += 1f;
							if (npc.ai[0] >= 2f)
							{
								npc.direction *= -1;
								npc.spriteDirection = npc.direction;
								npc.ai[0] = 0f;
							}
						}
					}
					else
					{
						npc.ai[0] = 0f;
					}
					if (npc.direction == 0)
					{
						npc.direction = 1;
					}
				}
				if (npc.velocity.X < -1.5f || npc.velocity.X > 1.5f)
				{
					if (npc.velocity.Y == 0f)
					{
						npc.velocity *= 0.8f;
					}
				}
				else
				{
					if (npc.velocity.X < 1.5f && npc.direction == 1)
					{
						npc.velocity.X = npc.velocity.X + 0.07f;
						if (npc.velocity.X > 1.5f)
						{
							npc.velocity.X = 1.5f;
						}
					}
					else
					{
						if (npc.velocity.X > -1.5f && npc.direction == -1)
						{
							npc.velocity.X = npc.velocity.X - 0.07f;
							if (npc.velocity.X < -1.5f)
							{
								npc.velocity.X = -1.5f;
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
                #endregion

                #region Charge
                if (Main.netMode != 1)
				{
					Player player = Main.player[npc.target];
					if (Main.rand.Next(200) == 1 && npc.Distance(player.Center) > 260)
					{
						chargeDamageFlag = true;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
						npc.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
						npc.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
						npc.ai[1] = 1f;
						npc.netUpdate = true;
					}
					if (chargeDamageFlag == true)
					{
						npc.damage = 50;
						chargeDamage++;
					}
					if (chargeDamage >= 55)
					{
						chargeDamageFlag = false;
						npc.damage = 45;
						chargeDamage = 0;
					}

				}
				#endregion




				#region Projectiles
				if (Main.netMode != 1)
				{

					//MAKE SOUND WHEN JUMPING/HOVERING
					if (Main.rand.Next(12) == 0 && npc.velocity.Y <= -1f)
					{
						Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 24, 0.2f, .1f);
					}

					customAi1++; ;
					//TELEGRAPH DUSTS
					if (customAi1 >= 100)
					{
						Lighting.AddLight(npc.Center, Color.Purple.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
						if (Main.rand.Next(3) == 1)
						{
							Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, npc.velocity.X, npc.velocity.Y);
							//Dust.NewDust(npc.position, npc.width, npc.height, DustID.EmeraldBolt, npc.velocity.X, npc.velocity.Y);
						}

					}


					if (customAi1 >= 120f)
					{

						//DISRUPTOR ATTACK
						npc.TargetClosest(true);
						//if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
						if (Collision.CanHitLine(npc.Center, 2, 2, Main.player[npc.target].Center, 2, 2))
						{

							if (Main.rand.Next(800) == 1)
							{
								float num48 = 3f;
								Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
								float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
								float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
								if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
								{
									float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
									num51 = num48 / num51;
									speedX *= num51;
									speedY *= num51;
									int type = ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>();//44;//0x37; //14;
									int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, hypnoticDisruptorDamage, 0f, Main.myPlayer);
									Main.projectile[num54].timeLeft = 330;
									Main.projectile[num54].aiStyle = 1;
									Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 24, 0.6f, -0.5f); //wobble
																												  
								}
								npc.netUpdate = true;
							}

							//MAIN SPIT ATTACK
							if (customAi1 >= 140f) //was main rand 70
							{
								Vector2 speed = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center, 8);

								if (Main.rand.Next(3) == 1 && ((speed.X < 0f) && (npc.velocity.X < 0f)) || ((speed.X > 0f) && (npc.velocity.X > 0f)))
								{
									Projectile.NewProjectile(npc.Center.X, npc.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer); //5f was 0f in the example that works
									Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, 0.3f);
									customAi1 = 1f;
								}
								else
								{ 
									float num48 = 6f;
									Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
									float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
									float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
									if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
									{
										float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
										num51 = num48 / num51;
										speedX *= num51;
										speedY *= num51;
										int type = ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>();//44;//0x37; //14;
										int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, bioSpitDamage, 0f, Main.myPlayer);
										Main.projectile[num54].timeLeft = 60;
										Main.projectile[num54].aiStyle = 1;
										Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, -0.1f); //fire
										customAi1 = 1f;
									}
								}
								npc.netUpdate = true;
							}

						}

					}

				}

				#endregion

				//DESPAWN WHEN PLAYER DIES
				if (Main.player[npc.target].dead)
				{
					if (npc.timeLeft > 20)
					{
						npc.timeLeft = 20;
						return;
					}
				}



			}

			#endregion

			//-------------------------------------------------------------------
			#region check if standing on a solid tile
			// warning: this section contains a return statement
			bool standing_on_solid_tile = false;
			if (npc.velocity.Y == 0f) // no jump/fall
			{
				int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
				int x_left_edge = (int)npc.position.X / 16;
				int x_right_edge = (int)(npc.position.X + (float)npc.width) / 16;
				for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
				{
					if (Main.tile[l, y_below_feet] == null) // null tile means ??
						return;

					if (Main.tile[l, y_below_feet].active() && Main.tileSolid[(int)Main.tile[l, y_below_feet].type]) // tile exists and is solid
					{
						standing_on_solid_tile = true;
						break; // one is enough so stop checking
					}
				} // END traverse blocks under feet
			} // END no jump/fall
			#endregion
			//-------------------------------------------------------------------
			#region new Tile()s, door opening/breaking
			if (standing_on_solid_tile)  //  if standing on solid tile
			{
				int x_in_front = (int)((npc.position.X + (float)(npc.width / 2) + (float)(15 * npc.direction)) / 16f); // 15 pix in front of center of mass
				int y_above_feet = (int)((npc.position.Y + (float)npc.height - 15f) / 16f); // 15 pix above feet
				if (clown_sized)
					x_in_front = (int)((npc.position.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 16) * npc.direction)) / 16f); // 16 pix in front of edge
																																		 //  create? 5 tile high stack in front
				if (Main.tile[x_in_front, y_above_feet] == null)
					Main.tile[x_in_front, y_above_feet] = new Tile();

				if (Main.tile[x_in_front, y_above_feet - 1] == null)
					Main.tile[x_in_front, y_above_feet - 1] = new Tile();

				if (Main.tile[x_in_front, y_above_feet - 2] == null)
					Main.tile[x_in_front, y_above_feet - 2] = new Tile();

				if (Main.tile[x_in_front, y_above_feet - 3] == null)
					Main.tile[x_in_front, y_above_feet - 3] = new Tile();

				if (Main.tile[x_in_front, y_above_feet + 1] == null)
					Main.tile[x_in_front, y_above_feet + 1] = new Tile();
				//  create? 2 other tiles farther in front
				if (Main.tile[x_in_front + npc.direction, y_above_feet - 1] == null)
					Main.tile[x_in_front + npc.direction, y_above_feet - 1] = new Tile();

				if (Main.tile[x_in_front + npc.direction, y_above_feet + 1] == null)
					Main.tile[x_in_front + npc.direction, y_above_feet + 1] = new Tile();

				if (Main.tile[x_in_front, y_above_feet - 1].active() && Main.tile[x_in_front, y_above_feet - 1].type == 10 && can_pass_doors)
				{ // tile in front is active, is door and NPC can pass doors: trying to break door
					npc.ai[2] += 1f; // inc knock countdown
					npc.ai[3] = 0f; // not bored if working on breaking a door
					if (npc.ai[2] >= 60f)  //  knock once per second
					{
						if (!Main.bloodMoon && can_pass_doors_bloodmoon_only)
							npc.ai[1] = 0f;  //  damage counter zeroed unless bloodmoon, but will still knock

						npc.velocity.X = 0.5f * (float)(-(float)npc.direction); //  slight recoil from hitting it
						npc.ai[1] += door_break_pow;  //  increase door damage counter
						npc.ai[2] = 0f;  //  knock finished; start next knock
						bool door_breaking = false;  //  door break flag
						if (npc.ai[1] >= 10f)  //  at 10 damage, set door as breaking (and cap at 10)
						{
							door_breaking = true;
							npc.ai[1] = 10f;
						}
						WorldGen.KillTile(x_in_front, y_above_feet - 1, true, false, false);  //  kill door ? when door not breaking too? can fail=true; effect only would make more sense, to make knocking sound
						if (door_breaking && Main.netMode != 1)  //  server and door breaking
						{
							if (breaks_doors)  //  breaks doors rather than attempt to open
							{
								WorldGen.KillTile(x_in_front, y_above_feet - 1, false, false, false);  //  kill door
								if (Main.netMode == 2) // server
									NetMessage.SendData(17, -1, -1, null, 0, (float)x_in_front, (float)(y_above_feet - 1), 0f, 0); // ?? tile breaking and/or item drop probably
							}
							else  //  try to open without breaking
							{
								bool door_opened = WorldGen.OpenDoor(x_in_front, y_above_feet, npc.direction);  //  open the door
								if (!door_opened)  //  door not opened successfully
								{
									npc.ai[3] = (float)boredom_time;  //  bored if door is stuck
									npc.netUpdate = true;
									npc.velocity.X = 0; // cancel recoil so boredom wall reflection can trigger
								}
								if (Main.netMode == 2 && door_opened) // is server & door was just opened
									NetMessage.SendData(19, -1, -1, null, 0, (float)x_in_front, (float)y_above_feet, (float)npc.direction, 0); // ??
							}
						}  //  END server and door breaking
					} // END knock on door
				} // END trying to break door
				#endregion
				//-------------------------------------------------------------------
				#region jumping, reset door knock & damage counters
				else // standing on solid tile but not in front of a passable door
				{
					if ((npc.velocity.X < 0f && npc.spriteDirection == -1) || (npc.velocity.X > 0f && npc.spriteDirection == 1))
					{  //  moving forward
						if (Main.tile[x_in_front, y_above_feet - 2].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 2].type])
						{ // 3 blocks above ground level(head height) blocked
							if (Main.tile[x_in_front, y_above_feet - 3].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 3].type])
							{ // 4 blocks above ground level(over head) blocked
								npc.velocity.Y = -8f; // jump with power 8 (for 4 block steps)
								npc.netUpdate = true;
							}
							else
							{
								npc.velocity.Y = -7f; // jump with power 7 (for 3 block steps)
								npc.netUpdate = true;
							}
						} // for everything else, head height clear:
						else if (Main.tile[x_in_front, y_above_feet - 1].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 1].type])
						{ // 2 blocks above ground level(mid body height) blocked
							npc.velocity.Y = -6f; // jump with power 6 (for 2 block steps)
							npc.netUpdate = true;
						}
						else if (Main.tile[x_in_front, y_above_feet].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet].type])
						{ // 1 block above ground level(foot height) blocked
							npc.velocity.Y = -5f; // jump with power 5 (for 1 block steps)
							npc.netUpdate = true;
						}
						else if (npc.directionY < 0 && jump_gaps && (!Main.tile[x_in_front, y_above_feet + 1].active() || !Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet + 1].type]) && (!Main.tile[x_in_front + npc.direction, y_above_feet + 1].active() || !Main.tileSolid[(int)Main.tile[x_in_front + npc.direction, y_above_feet + 1].type]))
						{ // rising? & jumps gaps & no solid tile ahead to step on for 2 spaces in front
							npc.velocity.Y = -8f; // jump with power 8
							npc.velocity.X = npc.velocity.X * 1.5f; // jump forward hard as well; we're trying to jump a gap
							npc.netUpdate = true;
						}
						else if (can_pass_doors) // standing on solid tile but not in front of a passable door, moving forward, didnt jump.  I assume recoil from hitting door is too small to move passable door out of range and trigger this
						{
							npc.ai[1] = 0f;  //  reset door dmg counter
							npc.ai[2] = 0f;  //  reset knock counter
						}
					} // END moving forward, still: standing on solid tile but not in front of a passable door
					if (hops && npc.velocity.Y == 0f && Math.Abs(npc.position.X + (float)(npc.width / 2) - (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2))) < hop_range_x && Math.Abs(npc.position.Y + (float)(npc.height / 2) - (Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2))) < hop_range_y && ((npc.direction > 0 && npc.velocity.X >= hop_velocity) || (npc.direction < 0 && npc.velocity.X <= -hop_velocity)))
					{ // type that hops & no jump/fall & near target & moving forward fast enough: hop code
						npc.velocity.X = npc.velocity.X * 2f; // burst forward
						if (npc.velocity.X > hop_speed) // but cap at hop_speed
							npc.velocity.X = hop_speed;
						else if (npc.velocity.X < -hop_speed)
							npc.velocity.X = -hop_speed;

						npc.velocity.Y = -hop_power; // and jump of course
						npc.netUpdate = true;
					}
					if (can_teleport && npc.velocity.Y < 0f) // jumping
						npc.velocity.Y = npc.velocity.Y * 1.1f; // infinite jump? antigravity?
				}
			}
			else if (can_pass_doors)  //  not standing on a solid tile & can open/break doors
			{
				npc.ai[1] = 0f;  //  reset door damage counter
				npc.ai[2] = 0f;  //  reset knock counter
			}//*/
			#endregion
			//-------------------------------------------------------------------
			#region teleportation
			if (Main.netMode != 1 && can_teleport && npc.ai[3] >= (float)boredom_time) // is server & chaos ele & bored
			{
				int target_x_blockpos = (int)Main.player[npc.target].position.X / 16; // corner not center
				int target_y_blockpos = (int)Main.player[npc.target].position.Y / 16; // corner not center
				int x_blockpos = (int)npc.position.X / 16; // corner not center
				int y_blockpos = (int)npc.position.Y / 16; // corner not center
				int tp_radius = 35; // radius around target(upper left corner) in blocks to teleport into
				int tp_counter = 0;
				bool flag7 = false;
				if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
				{ // far away from target; 2000 pixels = 125 blocks
					tp_counter = 100;
					flag7 = true; // no teleport
				}
				while (!flag7) // loop always ran full 100 time before I added "flag7 = true" below
				{
					if (tp_counter >= 100) // run 100 times
						break; //return;
					tp_counter++;

					int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
					int tp_y_target = Main.rand.Next(target_y_blockpos - tp_radius, target_y_blockpos + tp_radius);  //  pick random tp point (centered on corner)
					for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
					{ // (tp_x_target,m) is block under its feet I think
						if ((m < target_y_blockpos - 20 || m > target_y_blockpos + 20 || tp_x_target < target_x_blockpos - 20 || tp_x_target > target_x_blockpos + 20) && (m < y_blockpos - 1 || m > y_blockpos + 1 || tp_x_target < x_blockpos - 1 || tp_x_target > x_blockpos + 1) && Main.tile[tp_x_target, m].active())
						{ // over 6 blocks distant from player & over 1 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
							bool safe_to_stand = true;
							bool dark_caster = false; // not a fighter type AI...
							if (dark_caster && Main.tile[tp_x_target, m - 1].wall == 0) // Dark Caster & ?outdoors
								safe_to_stand = false;
							else if (Main.tile[tp_x_target, m - 1].lava()) // feet submerged in lava
								safe_to_stand = false;

							if (safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].type] && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
							{ // safe enviornment & solid below feet & 3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile
								npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
								npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
								npc.netUpdate = true;
								npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
								flag7 = true; // end the loop (after testing every lower point :/)
							}
						} // END over 6 blocks distant from player...
					} // END traverse y down to edge of radius
				} // END try 100 times
			} // END is server & chaos ele & bored
			#endregion
			//-------------------------------------------------------------------
			#region drown // code by Omnir
			if (canDrown)
			{
				if (!npc.wet)
				{
					npc.TargetClosest(true);
					drownTimer = drownTimerMax;
				}
				if (npc.wet)
				{
					drownTimer--;
				}
				if (npc.wet && drownTimer > drowningRisk)
				{
					npc.TargetClosest(true);
				}
				else if (npc.wet && drownTimer <= drowningRisk)
				{
					npc.TargetClosest(false);
					if (npc.timeLeft > 10)
					{
						npc.timeLeft = 10;
					}
					npc.directionY = -1;
					if (npc.velocity.Y > 0f)
					{
						npc.direction = 1;
					}
					npc.direction = -1;
					if (npc.velocity.X > 0f)
					{
						npc.direction = 1;
					}
				}
				if (drownTimer <= 0)
				{
					npc.life--;
					if (npc.life <= 0)
					{
						Main.PlaySound(4, (int)npc.position.X, (int)npc.position.Y, 1);
						npc.NPCLoot();
						npc.netUpdate = true;
					}
				}
			}
			#endregion
			//-------------------------------------------------------------------*/
			#region New Boredom by Omnir
			if (quickBored)
			{
				if (!oBored)
				{
					if (npc.velocity.X == 0f)
					{
						boredTimer++;
						if (boredTimer > tBored)
						{
							boredResetT = 0;
							npc.TargetClosest(false);
							if (npc.timeLeft > 10)
							{
								npc.timeLeft = 10;
							}
							npc.directionY = -1;
							if (npc.velocity.Y > 0f)
							{
								npc.direction = 1;
							}
							npc.direction = -1;
							if (npc.velocity.X > 0f)
							{
								npc.direction = 1;
							}
							oBored = true;
						}
					}
				}
				if (oBored)
				{
					boredResetT++;
					if (boredResetT > bReset)
					{
						boredTimer = 0;
						npc.TargetClosest(true);
						oBored = false;
					}
				}
			}
			#endregion
		}
		#endregion

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
					npc.frameCounter += (double)(Math.Abs(npc.velocity.X) * .2f);
					//npc.frameCounter += 1.0;
					if (npc.frameCounter > 10)
					{
						npc.frame.Y = npc.frame.Y + num;
						npc.frameCounter = 0;
					}
					if (npc.frame.Y / num >= Main.npcFrameCount[npc.type])
					{
						npc.frame.Y = num * 1;
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
			player.AddBuff(17, 180, false); //hunter

			if (Main.rand.Next(2) == 0)
			{
				player.AddBuff(20, 300, false); //poisoned
			}
			if(tsorcRevampWorld.Slain.ContainsKey(NPCID.EaterofWorldsHead))
			{ 
				player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 life if counter hits 100
				player.GetModPlayer<tsorcRevampPlayer>().CurseLevel += 5;
			}
			if (Main.rand.Next(10) == 0)
			{
				player.AddBuff(36, 600, false); //broken armor
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

			if (!Main.hardMode && Main.rand.Next(100) < 30) Item.NewItem(npc.getRect(), ItemID.HealingPotion);
			if (Main.rand.Next(100) < 30) Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion);
			//if (Main.rand.Next(100) < 20) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.BossItems.TomeOfSlograAndGaibon>());
			
		}
	}
}