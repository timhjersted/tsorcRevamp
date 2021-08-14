using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class OolacileDemon : ModNPC
	{
		public override void SetStaticDefaults()
		{			
			DisplayName.SetDefault("Ephemeral Oolacile Demon");
		}
		public override void SetDefaults()
		{
			npc.npcSlots = 10;
			Main.npcFrameCount[npc.type] = 2;
			animationType = 62;
			npc.width = 70;
			npc.height = 70;
			npc.aiStyle = 22;
			npc.damage = 125;
			npc.HitSound = SoundID.NPCHit1;
			npc.defense = 170;
			npc.noTileCollide = true;
			npc.behindTiles = true;
			npc.lavaImmune = true;
			npc.DeathSound = mod.GetLegacySoundSlot(SoundType.NPCKilled, "Sounds/NPCKilled/Gaibon_Roar");
			npc.lifeMax = 30000;
			npc.scale = 1.1f;
			npc.knockBackResist = 0.2f;
			npc.noGravity = true;
			npc.noTileCollide = false;
			npc.value = 18750;
		}

		int cursedBreathDamage = 38;
		int bioSpitDamage = 45;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			cursedBreathDamage = (int)(cursedBreathDamage / 2);
			bioSpitDamage = (int)(bioSpitDamage / 2);
		}

		float customAi1;
		int breathCD = 60;
		bool breath = false;

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player P = spawnInfo.player; //this shortens our code up from writing this line over and over.

			bool Sky = P.position.Y <= (Main.rockLayer * 4);
			bool Meteor = P.ZoneMeteor;
			bool Jungle = P.ZoneJungle;
			bool Dungeon = P.ZoneDungeon;
			bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
			bool Hallow = P.ZoneHoly;
			bool AboveEarth = P.position.Y < Main.worldSurface;
			bool InBrownLayer = P.position.Y >= Main.worldSurface && P.position.Y < Main.rockLayer;
			bool InGrayLayer = P.position.Y >= Main.rockLayer && P.position.Y < (Main.maxTilesY - 200) * 16;
			bool InHell = P.position.Y >= (Main.maxTilesY - 200) * 16;
			bool Ocean = P.position.X < 3600 || P.position.X > (Main.maxTilesX - 100) * 16;

			// these are all the regular stuff you get , now lets see......

			
			if (NPC.AnyNPCs(ModContent.NPCType<OolacileDemon>()))
			{
				return 0;
			}
			

			if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && InHell && Main.rand.Next(13) == 1) return 1;

			if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && InHell && Main.rand.Next(8) == 1) return 1;

			if (tsorcRevampWorld.SuperHardMode && Main.dayTime && InHell && Main.rand.Next(20) == 1) return 1;

			return 0;
		}
		#endregion

		#region AI
		public override void AI()
		{
			////  can_teleport==true code uses boredom_time and ai[3] (boredom), but not mutually exclusive
			//	bool can_teleport=true;  //  tp around like chaos ele
			//	int boredom_time=60; // time until it stops targeting player if blocked etc, 60 for anything but chaos ele, 20 for chaos ele
			//	int boredom_cooldown=10*boredom_time; // boredom level where boredom wears off; usually 10*boredom_time
			//	double bored_speed=.2;  //  above this speed boredom decreases(if not already bored); usually .9

			//#region adjust boredom level
			//	if (npc.ai[2] <= 0f)  //  loop to set ai[3] (boredom)
			//	{
			//		if (npc.position.X == npc.oldPosition.X || npc.ai[3] >= (float)boredom_time || moonwalking)  //  stopped or bored or moonwalking
			//			npc.ai[3] += 1f; // increase boredom
			//		else if ((double)Math.Abs(npc.velocity.X) > bored_speed && npc.ai[3] > 0f)  //  moving fast and not bored
			//			npc.ai[3] -= 1f; // decrease boredom

			//		if (npc.justHit || npc.ai[3] > boredom_cooldown)
			//			npc.ai[3] = 0f; // boredom wears off if enough time passes, or if hit

			//		if (npc.ai[3] == (float)boredom_time)
			//			npc.netUpdate = true; // netupdate when state changes to bored
			//	}
			//#endregion

			//#region teleportation particle effects
			//	if (can_teleport)  //  chaos elemental type teleporter
			//	{
			//		if (npc.ai[3] == -120f)  //  boredom goes negative? I think this makes disappear/arrival effects after it just teleported
			//		{
			//			npc.velocity *= 0f; // stop moving
			//			npc.ai[3] = 0f; // reset boredom to 0
			//			Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 8);
			//			Vector2 vector = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f); // current location
			//			float num6 = npc.oldPos[2].X + (float)npc.width * 0.5f - vector.X; // direction to where it was 3 frames ago?
			//			float num7 = npc.oldPos[2].Y + (float)npc.height * 0.5f - vector.Y; // direction to where it was 3 frames ago?
			//			float num8 = (float)Math.Sqrt((double)(num6 * num6 + num7 * num7)); // distance to where it was 3 frames ago?
			//			num8 = 2f / num8; // to normalize to 2 unit long vector
			//			num6 *= num8; // direction to where it was 3 frames ago, vector normalized
			//			num7 *= num8; // direction to where it was 3 frames ago, vector normalized
			//			for (int j = 0; j < 20; j++) // make 20 dusts at current position
			//			{
			//				int num9 = Dust.NewDust(npc.position, npc.width, npc.height, 71, num6, num7, 200, default(Color), 2f);
			//				Main.dust[num9].noGravity = true; // floating
			//				Dust expr_19EE_cp_0 = Main.dust[num9]; // make a dust handle?
			//				expr_19EE_cp_0.velocity.X = expr_19EE_cp_0.velocity.X * 2f; // faster in x direction
			//			}
			//			for (int k = 0; k < 20; k++) // more dust effects at old position
			//			{
			//				int num10 = Dust.NewDust(npc.oldPos[2], npc.width, npc.height, 71, -num6, -num7, 200, default(Color), 2f);
			//				Main.dust[num10].noGravity = true;
			//				Dust expr_1A6F_cp_0 = Main.dust[num10];
			//				expr_1A6F_cp_0.velocity.X = expr_1A6F_cp_0.velocity.X * 2f;
			//			}
			//		} // END just teleported
			//	} // END can teleport
			//#endregion

			//#region check if standing on a solid tile
			//// warning: this section contains a return statement
			//	bool standing_on_solid_tile = false;
			//	if (npc.velocity.Y == 0f) // no jump/fall
			//	{
			//		int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
			//		int x_left_edge = (int)npc.position.X / 16;
			//		int x_right_edge = (int)(npc.position.X + (float)npc.width) / 16;
			//		for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
			//		{
			//			if (Main.tile[l, y_below_feet] == null) // null tile means ??
			//				return;

			//			if (Main.tile[l, y_below_feet].active() && Main.tileSolid[(int)Main.tile[l, y_below_feet].type]) // tile exists and is solid
			//			{
			//				standing_on_solid_tile = true;
			//				break; // one is enough so stop checking
			//			}
			//		} // END traverse blocks under feet
			//	} // END no jump/fall
			//#endregion


			//#region teleportation
			//	if (Main.netMode != 1 && can_teleport && npc.ai[3] >= (float)boredom_time) // is server & chaos ele & bored
			//	{
			//		int target_x_blockpos = (int)Main.player[npc.target].position.X / 16; // corner not center
			//		int target_y_blockpos = (int)Main.player[npc.target].position.Y / 16; // corner not center
			//		int x_blockpos = (int)npc.position.X / 16; // corner not center
			//		int y_blockpos = (int)npc.position.Y / 16; // corner not center
			//		int tp_radius = 15; // radius around target(upper left corner) in blocks to teleport into
			//		int tp_counter = 0;
			//		bool flag7 = false;
			//		if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f)
			//		{ // far away from target; 2000 pixels = 125 blocks
			//			tp_counter = 100;
			//			flag7 = true; // no teleport
			//		}
			//		while (!flag7) // loop always ran full 100 time before I added "flag7 = true;" below
			//		{
			//			if (tp_counter >= 100) // run 100 times
			//				break; //return;
			//			tp_counter++;

			//			int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
			//			int tp_y_target = Main.rand.Next(target_y_blockpos - tp_radius, target_y_blockpos + tp_radius);  //  pick random tp point (centered on corner)
			//			for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
			//			{ // (tp_x_target,m) is block under its feet I think
			//				if ((m < target_y_blockpos - 6 || m > target_y_blockpos + 6 || tp_x_target < target_x_blockpos - 6 || tp_x_target > target_x_blockpos + 4) && (m < y_blockpos - 1 || m > y_blockpos + 1 || tp_x_target < x_blockpos - 1 || tp_x_target > x_blockpos + 1) && Main.tile[tp_x_target, m].active())
			//				{ // over 6 blocks distant from player & over 1 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
			//					bool safe_to_stand = true;
			//					bool dark_caster = false; // not a fighter type AI...
			//					if (dark_caster && Main.tile[tp_x_target, m - 1].wall == 0) // Dark Caster & ?outdoors
			//						safe_to_stand = false;
			//					else if (Main.tile[tp_x_target, m - 1].lava()) // feet submerged in lava
			//							safe_to_stand = false;

			//					if (safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].type] && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
			//					{ // safe enviornment & solid below feet & 3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile
			//						npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
			//						npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
			//						npc.netUpdate = true;
			//						npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
			//						flag7 = true; // end the loop (after testing every lower point :/)
			//					}
			//				} // END over 6 blocks distant from player...
			//			} // END traverse y down to edge of radius
			//		} // END try 100 times
			//	} // END is server & chaos ele & bored
			//	#endregion






			//if(Main.netMode != 1)
			//{
			npc.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
			if (npc.ai[1] >= 10f)
			{

				npc.TargetClosest(true);
				if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
				{


					//Player nT = Main.player[npc.target];
					if (Main.rand.Next(1200) == 0)
					{
						breath = true;
						//Main.PlaySound(2, -1, -1, 20);
					}
					if (breath)
					{

						float rotation = (float)Math.Atan2(npc.Center.Y - Main.player[npc.target].Center.Y, npc.Center.X - Main.player[npc.target].Center.X);
						int num54 = Projectile.NewProjectile(npc.Center.X, npc.Center.Y - 5, (float)((Math.Cos(rotation) * 25) * -1), (float)((Math.Sin(rotation) * 25) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedBreathDamage, 0f, Main.myPlayer);
						Main.projectile[num54].timeLeft = 30;
						npc.netUpdate = true;





						breathCD--;
						//}
					}
					if (breathCD <= 0)
					{
						breath = false;
						breathCD = 60;
						Main.PlaySound(2, -1, -1, 20);
					}



					//if (Main.rand.Next(35) == 0) 
					//	{
					//		int num65 = Projectile.NewProjectile(npc.Center.X+Main.rand.Next(-500,500), npc.Center.Y+Main.rand.Next(-500,500), 0, 0, "Dark Explosion", 70, 0f, Main.myPlayer);
					//	}

					if (Main.rand.Next(40) == 1)
					{
						float num48 = 7f;
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
							Main.projectile[num54].timeLeft = 150;
							Main.projectile[num54].aiStyle = 1;
							Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20);

							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}


				}


			}
			//}
			//if (npc.justHit)
			//{
			//   npc.ai[2] = 0f;
			//}
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
						npc.velocity.X = npc.velocity.X * -0.2f; //was -1f
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


			}
			if (npc.collideX)
			{
				npc.velocity.X = npc.oldVelocity.X * -0.1f;
				if (npc.direction == -1 && npc.velocity.X > 0f && npc.velocity.X < 1f)
				{
					npc.velocity.X = 0.3f; //was 1f
				}
				if (npc.direction == 1 && npc.velocity.X < 0f && npc.velocity.X > -1f)
				{
					npc.velocity.X = -0.3f; //was -1f
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
						npc.velocity.X = npc.velocity.X + 0.02f;
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
							npc.velocity.X = npc.velocity.X - 0.02f;
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
		#region Debuffs
		public void DamagePlayer(Player player, ref int damage) //hook works!
		{

			if (Main.rand.Next(2) == 0)
			{
				player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 36000, false); //-20 HP curse
			}

			if (Main.rand.Next(4) == 0)
			{

				player.AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 3600, false); //armor reduced on hit
				player.AddBuff(ModContent.BuffType<Buffs.Chilled>(), 300, false); //chilled

			}

			//if (Main.rand.Next(10) == 0 && player.statLifeMax > 20) 

			//{

			//			Main.NewText("You have been cursed!");
			//	player.statLifeMax -= 20;
			//}
		}
		#endregion
		//-------------------------------------------------------------------
		#region Glowing Eye Effect
		//public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		//{
		//broken for now
		//			int spriteWidth=npc.frame.Width; //use same number as ini Main.npcFrameCount[npc.type]
		//			int spriteHeight = Main.npcTexture[Config.npcDefs.byName[npc.name].type].Height / Main.npcFrameCount[npc.type];

		//			int spritePosDifX = (int)(npc.frame.Width / 2);
		//			int spritePosDifY = npc.frame.Height; // was npc.frame.Height - 4;

		//			int frame = npc.frame.Y / spriteHeight;

		//			int offsetX = (int)(npc.position.X + (npc.width / 2) - Main.screenPosition.X - spritePosDifX + 0.5f);
		//			int offsetY = (int)(npc.position.Y + npc.height - Main.screenPosition.Y - spritePosDifY);

		//			SpriteEffects flop = SpriteEffects.None;
		//			if(npc.spriteDirection == 1){
		//				flop = SpriteEffects.FlipHorizontally;
		//			}


		//				//Glowing Eye Effect
		//				for(int i=0;i>-1;i--)
		//				{
		//						//draw 3 levels of trail
		//						int alphaVal=255-(0*i);
		//						Color modifiedColour = new Color((int)(alphaVal),(int)(alphaVal),(int)(alphaVal),alphaVal);
		//						spriteBatch.Draw(Main.goreTexture[Config.goreID["Oolacile Demon Glow"]],
		//						new Rectangle((int)(offsetX), (int)(offsetY), spriteWidth, spriteHeight),
		//						new Rectangle(0,npc.frame.Height * frame, spriteWidth, spriteHeight),
		//						modifiedColour,
		//						0,  //Just add this here I think was 0
		//						new Vector2(0,0),
		//						flop,
		//						0);  


		//				}	
		//}
		#endregion
		//-------------------------------------------------------------------
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

			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.RedTitanite>(), 1 + Main.rand.Next(1));
		}
		#endregion
	}
}