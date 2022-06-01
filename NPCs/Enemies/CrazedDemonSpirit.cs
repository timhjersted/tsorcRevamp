using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
	class CrazedDemonSpirit : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Crazed Demon Spirit");
		}

		public override void SetDefaults()
		{
			NPC.aiStyle = 22;
			NPC.npcSlots = 6;
			Main.npcFrameCount[NPC.type] = 4;
			animationType = 60;
			NPC.width = 50;
			NPC.height = 50;
			NPC.damage = 38;
			NPC.defense = 18;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath6;
			NPC.lifeMax = 600;
			NPC.friendly = false;
			NPC.noTileCollide = true;
			NPC.lavaImmune = true;
			NPC.noGravity = true;
			NPC.knockBackResist = 0;
			NPC.alpha = 100;
			NPC.value = 1600;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.CrazedDemonSpiritBanner>();

			NPC.buffImmune[BuffID.Confused] = true;
			NPC.buffImmune[BuffID.OnFire] = true;
			NPC.buffImmune[BuffID.Poisoned] = true;
		}
		float customAi1;

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player p = spawnInfo.Player;

			//Ensuring it can't spawn if two already exist.
			int count = 0;
			for (int i = 0; i < Main.npc.Length; i++)
			{
				if (Main.npc[i].type == NPC.type)
				{
					count++;
					if (count > 1)
					{
						return 0;
					}
				}
			}

			bool nospecialbiome = !p.ZoneJungle && !p.ZoneCorrupt && !p.ZoneCrimson && !p.ZoneHallow && !p.ZoneMeteor && !p.ZoneDungeon; // Not necessary at all to use but needed to make all this work.
			bool sky = nospecialbiome && ((double)spawnInfo.SpawnTileY < Main.worldSurface * 0.44999998807907104);
			bool surface = nospecialbiome && !sky && (spawnInfo.SpawnTileY <= Main.worldSurface);
			bool underground = nospecialbiome && !surface && (spawnInfo.SpawnTileY <= Main.rockLayer);
			bool underworld = (p.ZoneUnderworldHeight);
			bool cavern = nospecialbiome && (spawnInfo.SpawnTileY >= Main.rockLayer) && (spawnInfo.SpawnTileY <= Main.rockLayer * 25);
			bool undergroundJungle = (spawnInfo.SpawnTileY >= Main.rockLayer) && (spawnInfo.SpawnTileY <= Main.rockLayer * 25) && p.ZoneJungle;
			bool undergroundEvil = (spawnInfo.SpawnTileY >= Main.rockLayer) && (spawnInfo.SpawnTileY <= Main.rockLayer * 25) && (p.ZoneCorrupt || p.ZoneCrimson);
			bool undergroundHoly = (spawnInfo.SpawnTileY >= Main.rockLayer) && (spawnInfo.SpawnTileY <= Main.rockLayer * 25) && p.ZoneHallow;

			if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheSorrow>()))
			{
				if (underworld && Main.rand.Next(500) == 0) return 1;
				else if (underworld && Main.hardMode && Main.rand.Next(10) == 0) return 1;
			}
			return 0;
		}
		#endregion

		#region AI
		public override void AI()
		{

			//I don't know why this is multiplied by its scale, don't even ask. Its scale is 1, so this doesn't even do anything.
			customAi1 += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;



			if (NPC.life > 200)
			{
				int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Torch, NPC.velocity.X, NPC.velocity.Y, 200, Color.Violet, 2f);
				Main.dust[dust].noGravity = true;
			}
			else if (NPC.life <= 200)
			{
				int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Torch, NPC.velocity.X, NPC.velocity.Y, 200, Color.Violet, 3f);
				Main.dust[dust].noGravity = true;
			}


			#region Shooting code
			if (customAi1 >= 10f)
			{

				NPC.TargetClosest(true);
				if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
				{
					if (Main.rand.Next(30) == 1)
					{
						float num48 = 10f;
						Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
						float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
						float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
						if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
						{
							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int damage = (int)(14f * NPC.scale);
							int type = ModContent.ProjectileType<Projectiles.Enemy.DemonSpirit>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 120;
							//Main.projectile[num54].aiStyle = 4;
							Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)NPC.position.X, (int)NPC.position.Y, 0x11);
							customAi1 = 1f;
						}
						NPC.netUpdate = true;
					}
					if (Main.rand.Next(10) == 1)
					{
						float num48 = 2f;
						Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
						float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-50, 50) / 100;
						float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-50, 50) / 100;
						if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
						{

							float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
							num51 = num48 / num51;
							speedX *= num51;
							speedY *= num51;
							int damage = 32;//(int) (14f * npc.scale);
							int type = ModContent.ProjectileType<Projectiles.Enemy.PurpleCrush>();//44;//0x37; //14;
							int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
							Main.projectile[num54].timeLeft = 170;
							//Main.projectile[num54].aiStyle = 19;
							Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)NPC.position.X, (int)NPC.position.Y, 0x11);
							customAi1 = 1f;
							int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.VilePowder, NPC.velocity.X, NPC.velocity.Y, 200, Color.Red, 1f);
							Main.dust[dust].noGravity = true;
						}
						NPC.netUpdate = true;
					}
				}
			}
			#endregion
			if (NPC.justHit)
			{
				NPC.ai[2] = 0f;


				Color color = new Color();
				int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Torch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
				Main.dust[dust].noGravity = true;
				dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Torch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
				Main.dust[dust].noGravity = true;
				dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Wraith, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
				Main.dust[dust].noGravity = true;
				dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
				Main.dust[dust].noGravity = true;



			}

			//Basically just modifications to the vanilla wraith code. I tried to clean it up and rename the variables to more useful things, but I could only do so much.
			#region Movement AI
			bool flag25 = false;

			//npc.ai[0] = a snapshot of its X position at some point in time
			//npc.ai[1] = a snapshot of its Y position at some point in time
			//npc.ai[2] = Counts up every frame to 60, then drops to 200
			if (NPC.ai[2] >= 0f)
			{

				int strayLimit = 16;


				bool flag26 = false;
				bool flag27 = false;

				//If it HAS NOT moved 16 units left or right since ai[0] was last set to its position
				if (NPC.position.X > (NPC.ai[0] - (float)strayLimit) && NPC.position.X < (NPC.ai[0] + (float)strayLimit))
				{
					flag26 = true;
				}
				else
				{   //If the direction it's facing and the direction it's moving are different
					if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
					{
						flag26 = true;
					}
				}
				strayLimit += 24;

				//
				//If it is within 40 tiles of where npc.ai[1] was last set to
				if (NPC.position.Y > NPC.ai[1] - (float)strayLimit && NPC.position.Y < NPC.ai[1] + (float)strayLimit)
				{
					flag27 = true;
				}

				//If both of these are true
				if (flag26 && flag27)
				{
					NPC.ai[2] += 1f;

					if (NPC.ai[2] >= 30f && strayLimit == 16)
					{
						//This will NEVER be true, since num258 gets set to 40 first.
						flag25 = true;
					}

					//Once it hits 60, turn the NPC around and set it back to -200
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

			//The centered X position of it
			int centeredXInTiles = (int)((NPC.position.X + (float)(NPC.width / 2)) / 16f) + NPC.direction * 2;
			//The centered Y position of it
			int centeredYInTiles = (int)((NPC.position.Y + (float)NPC.height) / 16f);
			bool isNotAboveSolidTile = true;
			//Literally nothing! This bool literally just exists to make this even harder to read. It gets set a few times, but never checked or used at all. For sanity's sake, i'm commenting them all out.
			//bool flag29 = false;

			//If it's above a solid tile or liquid within 3 blocks below its center, set flag 28 to false
			for (int tileIterator = centeredYInTiles; tileIterator < centeredYInTiles + 3; tileIterator++)
			{
				if (Main.tile[centeredXInTiles, tileIterator] == null)
				{
					Main.tile[centeredXInTiles, tileIterator].ClearTile();
				}
				if ((Main.tile[centeredXInTiles, tileIterator].HasTile && Main.tileSolid[(int)Main.tile[centeredXInTiles, tileIterator].TileType]) || Main.tile[centeredXInTiles, tileIterator].LiquidAmount > 0)
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
				NPC.velocity.Y = NPC.velocity.Y + 0.1f;
				if (NPC.velocity.Y > 3f)
				{
					NPC.velocity.Y = 3f;
				}
			}
			else
			{
				if (NPC.directionY < 0 && NPC.velocity.Y > 0f)
				{
					NPC.velocity.Y = NPC.velocity.Y - 0.1f;
				}
				if (NPC.velocity.Y < -4f)
				{
					NPC.velocity.Y = -4f;
				}
			}
			//CollideX will always be false, this will never run!!
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


			float speedLimit = 2f;
			if (NPC.direction == -1 && NPC.velocity.X > -speedLimit)
			{

				NPC.velocity.X = NPC.velocity.X - 0.1f;

				if (NPC.velocity.X > speedLimit)
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
				if (NPC.velocity.X < -speedLimit)
				{
					NPC.velocity.X = -speedLimit;

				}
			}
			else
			{
				if (NPC.direction == 1 && NPC.velocity.X < speedLimit)
				{
					NPC.velocity.X = NPC.velocity.X + 0.1f;
					if (NPC.velocity.X < -speedLimit)
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
					if (NPC.velocity.X > speedLimit)
					{
						NPC.velocity.X = speedLimit;
					}
				}
			}
			if (NPC.directionY == -1 && (double)NPC.velocity.Y > -1.5)
			{
				NPC.velocity.Y = NPC.velocity.Y - 0.04f;
				if ((double)NPC.velocity.Y > 1.5)
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
				if ((double)NPC.velocity.Y < -1.5)
				{
					NPC.velocity.Y = -1.5f;
				}
			}
			else
			{
				if (NPC.directionY == 1 && (double)NPC.velocity.Y < 1.5)
				{
					NPC.velocity.Y = NPC.velocity.Y + 0.04f;
					if ((double)NPC.velocity.Y < -1.5)
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
					if ((double)NPC.velocity.Y > 1.5)
					{
						NPC.velocity.Y = 1.5f;
					}
				}
			}
			#endregion

			Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.4f, 0f, 0.25f);
			return;
		}
		#endregion

		#region Gore
		public override void OnKill()
		{

			UsefulFunctions.BroadcastText("A lost spirit has been freed from its curse...", 175, 75, 255);

			Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
			if (NPC.life <= 0)
			{
				for (int num36 = 0; num36 < 50; num36++)
				{
					{
						Color color = new Color();
						int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.UnholyWater, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Wraith, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
						Main.dust[dust].noGravity = true;
						dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
						Main.dust[dust].noGravity = true;
					}
				}

				if (Main.rand.Next(99) < 3) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.WallTome>(), 1);
				if (Main.rand.Next(99) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>(), 1);
				if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.IronskinPotion, 1);
				if (Main.rand.Next(99) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ManaRegenerationPotion, 1);
				if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GreaterHealingPotion, 1);
			}
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
			if (NPC.frameCounter >= 8.0)
			{
				NPC.frame.Y = NPC.frame.Y + num;
				NPC.frameCounter = 0.0;
			}
			if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
			{
				NPC.frame.Y = 0;
			}
		}
		#endregion

	}
}