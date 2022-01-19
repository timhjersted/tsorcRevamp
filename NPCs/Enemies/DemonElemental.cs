using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
	class DemonElemental : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Demon Elemental");
		}

		int crystalFireDamage = 55;

		public override void SetDefaults()
		{
			npc.aiStyle = 22;
			npc.npcSlots = 5;
			Main.npcFrameCount[npc.type] = 3;
			animationType = -1; //was 60
			npc.width = 30;
			npc.height = 80;
			npc.damage = 82;
			npc.defense = 18;
			npc.aiStyle = -1;//22;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.lifeMax = 400;
			npc.friendly = false;
			npc.noTileCollide = true;
			npc.lavaImmune = true;
			npc.noGravity = true;
			npc.knockBackResist = 0f;
			npc.alpha = 70; // was 100
			npc.value = 500;
			banner = npc.type;
			
			//bannerItem = ModContent.ItemType<Banners.DemonSpiritBanner>();

			if (tsorcRevampWorld.SuperHardMode)
			{
				npc.lifeMax = 3660;
				npc.defense = 67;
				npc.value = 16500;
				npc.damage = 295;
				npc.knockBackResist = 0.0f;
				crystalFireDamage = 95;
			}

			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.Poisoned] = true;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			crystalFireDamage = (int)(crystalFireDamage / 2);
		}


		float customAi1;

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player p = spawnInfo.player;

			//Ensuring it can't spawn if two already exist.
			int count = 0;
			for(int i = 0; i < Main.npc.Length; i++)
            {
				if(Main.npc[i].type == npc.type)
                {
					count++;
					if(count > 1)
                    {
						return 0;
                    }
                }
            }
			
			bool nospecialbiome = !p.ZoneJungle && !p.ZoneCorrupt && !p.ZoneCrimson && !p.ZoneHoly && !p.ZoneMeteor && !p.ZoneDungeon; // Not necessary at all to use but needed to make all this work.
			bool sky = nospecialbiome && ((double)spawnInfo.spawnTileY < Main.worldSurface * 0.44999998807907104);
			bool surface = nospecialbiome && !sky && (spawnInfo.spawnTileY <= Main.worldSurface);
			bool underground = nospecialbiome && !surface && (spawnInfo.spawnTileY <= Main.rockLayer);
			bool underworld = (p.ZoneUnderworldHeight);
			
			bool cavern = nospecialbiome && (spawnInfo.spawnTileY >= Main.rockLayer) && (spawnInfo.spawnTileY <= Main.rockLayer * 25);
			bool undergroundJungle = (spawnInfo.spawnTileY >= Main.rockLayer) && (spawnInfo.spawnTileY <= Main.rockLayer * 25) && p.ZoneJungle;
			bool undergroundEvil = (spawnInfo.spawnTileY >= Main.rockLayer) && (spawnInfo.spawnTileY <= Main.rockLayer * 25) && (p.ZoneCorrupt || p.ZoneCrimson);
			bool undergroundHoly = (spawnInfo.spawnTileY >= Main.rockLayer) && (spawnInfo.spawnTileY <= Main.rockLayer * 25) && p.ZoneHoly;
						
			if (underworld && Main.rand.Next(35) == 0) return 1;
			else if (underworld && Main.hardMode && Main.rand.Next(1000) == 0) return 1;
			return 0;
		}
		#endregion

		#region AI
		public override void AI()
		{

			//I don't know why this is multiplied by its scale, don't even ask. Its scale is 1, so this doesn't even do anything.
			customAi1 += (Main.rand.Next(2, 5) * 0.1f) * npc.scale;
			


			if (npc.life > 200)
			{
				int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.Fire, npc.velocity.X, npc.velocity.Y, 200, Color.Violet, 2f);
				Main.dust[dust].noGravity = true;
			}
			else if (npc.life <= 200)
			{
				int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.Fire, npc.velocity.X, npc.velocity.Y, 200, Color.Violet, 3f);
				Main.dust[dust].noGravity = true;
			}


            #region Shooting code
            if (customAi1 >= 10f)
			{

				npc.TargetClosest(true);
				if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
				{
					if (Main.rand.Next(80) == 1) //was 30
					{
						float num48 = 6f; //was 8
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int damage = 39; //(int)(14f * npc.scale);
							int type = ModContent.ProjectileType<Projectiles.Enemy.CrystalFire>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, crystalFireDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 120;
							//Main.projectile[num54].aiStyle = 4;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}
					if (Main.rand.Next(90) == 1)
					{
						float num48 = 6f;
						Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
						float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-50, 50) / 100;
						float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-50, 50) / 100;
						if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
						{

							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int damage = 59;//(int) (14f * npc.scale);
							int type = ModContent.ProjectileType<Projectiles.Enemy.CrystalFire>();//44;//0x37; //14; was purple crush
							int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, crystalFireDamage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 150;
							//Main.projectile[num54].aiStyle = 19;
							Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
							customAi1 = 1f;
						}
						npc.netUpdate = true;
					}
				}
			}
			#endregion 
			if (npc.justHit)
			{
				npc.ai[2] = 0f;


				Color color = new Color();
				int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.Fire, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
				Main.dust[dust].noGravity = true;
				dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.Fire, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
				Main.dust[dust].noGravity = true;
				dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.Wraith, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
				Main.dust[dust].noGravity = true;
				dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
				Main.dust[dust].noGravity = true;



			}

			//Basically just modifications to the vanilla wraith code. I tried to clean it up and rename the variables to more useful things, but I could only do so much.
            #region Movement AI
            bool flag25 = false;

			//npc.ai[0] = a snapshot of its X position at some point in time
			//npc.ai[1] = a snapshot of its Y position at some point in time
			//npc.ai[2] = Counts up every frame to 60, then drops to 200
			if (npc.ai[2] >= 0f)
			{

				int strayLimit = 16;

				
				bool flag26 = false;
				bool flag27 = false;

				//If it HAS NOT moved 16 units left or right since ai[0] was last set to its position
				if (npc.position.X > (npc.ai[0] - (float)strayLimit) && npc.position.X < (npc.ai[0] + (float)strayLimit))
				{
					flag26 = true;
				}
				else
				{	//If the direction it's facing and the direction it's moving are different
					if ((npc.velocity.X < 0f && npc.direction > 0) || (npc.velocity.X > 0f && npc.direction < 0))
					{
						flag26 = true;
					}
				}
				strayLimit += 24;

				//
				//If it is within 40 tiles of where npc.ai[1] was last set to
				if (npc.position.Y > npc.ai[1] - (float)strayLimit && npc.position.Y < npc.ai[1] + (float)strayLimit)
				{
					flag27 = true;
				}

				//If both of these are true
				if (flag26 && flag27)
				{
					npc.ai[2] += 1f;

					if (npc.ai[2] >= 30f && strayLimit == 16)
					{
						//This will NEVER be true, since num258 gets set to 40 first.
						flag25 = true;
					}

					//Once it hits 60, turn the NPC around and set it back to -200
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

			//The centered X position of it
			int centeredXInTiles = (int)((npc.position.X + (float)(npc.width / 2)) / 16f) + npc.direction * 2;
			//The centered Y position of it
			int centeredYInTiles = (int)((npc.position.Y + (float)npc.height) / 16f);
			bool isNotAboveSolidTile = true;
			//Literally nothing! This bool literally just exists to make this even harder to read. It gets set a few times, but never checked or used at all. For sanity's sake, i'm commenting them all out.
			//bool flag29 = false;

			//If it's above a solid tile or liquid within 3 blocks below its center, set flag 28 to false
			for (int tileIterator = centeredYInTiles; tileIterator < centeredYInTiles + 3; tileIterator++)
			{
				if (Main.tile[centeredXInTiles, tileIterator] == null)
				{
					Main.tile[centeredXInTiles, tileIterator] = new Tile();
				}
				if ((Main.tile[centeredXInTiles, tileIterator].active() && Main.tileSolid[(int)Main.tile[centeredXInTiles, tileIterator].type]) || Main.tile[centeredXInTiles, tileIterator].liquid > 0)
				{
					/**if (num269 <= num260 + 1)
				//	{
				//		flag29 = true;
				//	}**/
					isNotAboveSolidTile = false;
					break;
				}
			}

			//This will never run because flag25 will always be false! Why?
			if (flag25)
			{
			//	flag29 = false;
				isNotAboveSolidTile = true;
			}

			//If flag 28, then accelerate it down in the Y direction
			if (isNotAboveSolidTile)
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
			//CollideX will always be false, this will never run!!
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


			float speedLimit = 2f;
			if (npc.direction == -1 && npc.velocity.X > -speedLimit)
			{

				npc.velocity.X = npc.velocity.X - 0.1f;

				if (npc.velocity.X > speedLimit)
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
				if (npc.velocity.X < -speedLimit)
				{
					npc.velocity.X = -speedLimit;

				}
			}
			else
			{
				if (npc.direction == 1 && npc.velocity.X < speedLimit)
				{
					npc.velocity.X = npc.velocity.X + 0.1f;
					if (npc.velocity.X < -speedLimit)
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
					if (npc.velocity.X > speedLimit)
					{
						npc.velocity.X = speedLimit;
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
            #endregion

            Lighting.AddLight((int)npc.position.X / 16, (int)npc.position.Y / 16, 0.4f, 0f, 0.25f);
			return;
		}
		#endregion

		#region Gore
		public override void NPCLoot()
		{

			//Main.NewText("A demon elemental has faded from existence...", 175, 75, 255);

			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/DemonElementalGore1"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/DemonElementalGore2"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/DemonElementalGore3"), 1.1f);


			Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
			if (npc.life <= 0)
			{
				for (int num36 = 0; num36 < 50; num36++)
				{
					{
						Color color = new Color();
						int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.UnholyWater, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.Wraith, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
						Main.dust[dust].noGravity = true;
					}
				}

				if (Main.rand.Next(99) < 50) Item.NewItem(npc.getRect(), ItemID.Heart, 1);
				if (Main.rand.Next(99) < 50) Item.NewItem(npc.getRect(), ItemID.Heart, 1);
			}
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
			if (npc.frameCounter >= 8.0)
			{
				npc.frame.Y = npc.frame.Y + num;
				npc.frameCounter = 0.0;
			}
			if (npc.frame.Y >= num * Main.npcFrameCount[npc.type])
			{
				npc.frame.Y = 0;
			}
		}
		#endregion

	}
}