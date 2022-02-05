using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
	public class LivingShroomThief : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Living Shroom Thief");
			Main.npcFrameCount[npc.type] = 8;
		}

		public override void SetDefaults()
		{
			npc.width = 8;
			npc.height = 18;
			npc.aiStyle = -1; //Unique AI is -1
			npc.damage = 0;
			npc.knockBackResist = 1;
			npc.defense = 6;
			npc.lifeMax = 16;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.value = 1000;
			npc.buffImmune[BuffID.Confused] = true;
			npc.noGravity = false;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.LivingShroomThiefBanner>();
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0;

			if (Main.dayTime && NPC.CountNPCS(mod.NPCType("LivingShroomThief")) < 1 && TileID.Sets.Conversion.Grass[spawnInfo.spawnTileType] && !spawnInfo.water && (Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY - 5].wall == WallID.None || Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY - 8].wall == WallID.None || Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY - 12].wall == WallID.None) && !(spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson || spawnInfo.player.ZoneDesert || spawnInfo.player.ZoneJungle || spawnInfo.player.ZoneMeteor))
			{
				return 0.15f;
			}
			return chance;
		}

		private const int AI_State_Slot = 0;
		private const int AI_Timer_Slot = 1;

		private const int State_Asleep = 0;
		//private const int State_Notice = 1; not used
		private const int State_Jump = 2;
		private const int State_Fleeing = 3;

		public float AI_State
		{
			get => npc.ai[AI_State_Slot];
			set => npc.ai[AI_State_Slot] = value;
		}

		public float AI_Timer
		{
			get => npc.ai[AI_Timer_Slot];
			set => npc.ai[AI_Timer_Slot] = value;
		}

		public int spawntimer = 0;
		public int coindroptimer = 0;
		public int knifetimer = 0;

		// Our AI here makes our NPC sit waiting for a player to enter range then spawns minions to attack.
		public override void AI()
		{
			// The npc starts in the asleep state, waiting for a player to enter range
			if (AI_State == State_Asleep)
			{
				npc.GivenName = "???";
				// TargetClosest sets npc.target to the player.whoAmI of the closest player. the faceTarget parameter means that npc.direction will automatically be 1 or -1 if the targeted player is to the right or left. This is also automatically flipped if npc.confused
				npc.TargetClosest(true);
				AI_Timer++;
				if (npc.velocity.Y == 0)
				{
					npc.velocity = new Vector2(0f, 0f);
				}
				if (npc.HasValidTarget && Main.player[npc.target].Distance(npc.Center) < 200f)
				{
					AI_State = State_Jump;
					AI_Timer = 0;
				}
				if ((npc.life < npc.lifeMax) && (Main.rand.Next(8) == 0))
				{
					Dust.NewDust(npc.position, npc.width - 6, npc.height - 16, 107, 0, 0, 0, default(Color), .75f); //regenerating hp, which it will never actually do
				}
			}
			else if (AI_State == State_Jump)
			{
				npc.GivenName = "Fungi Felon";
				AI_Timer++;
				if (AI_Timer == 1)
				{
					npc.velocity = new Vector2(npc.direction * -2.7f, -3.6f);
				}
				if ((Main.rand.Next(12) == 0) && (AI_Timer == 2) && npc.collideX)
				{
					if (npc.direction == -1) //right-facing bump
					{
						npc.velocity += new Vector2(-1f, 0);
						//if (!Main.dedServ) Main.PlaySound(mod.GetLegacySoundSlot(SoundType.NPCHit, "Sounds/NPCHit/Squeak").WithVolume(0.5f), npc.Center);
						if (coinsdropped < 10)
						{
							coinsdropped++;
							if (Main.rand.Next(10) == 0)
							{
								Item.NewItem(npc.Bottom, ItemID.Diamond);
							}
							else
							{
								Item.NewItem(npc.Bottom, ItemID.SilverCoin, Main.rand.Next(3, 7));
							}
						}
					}
					if (npc.direction == 1) //left-facing bump
					{
						npc.velocity += new Vector2(1f, 0);
						//if (!Main.dedServ) Main.PlaySound(mod.GetLegacySoundSlot(SoundType.NPCHit, "Sounds/NPCHit/Squeak").WithVolume(0.5f), npc.Center);
						if (coinsdropped < 10)
						{
							coinsdropped++;
							if (Main.rand.Next(10) == 0)
							{
								Item.NewItem(npc.Bottom, ItemID.Diamond);
							}
							else
							{
								Item.NewItem(npc.Bottom, ItemID.SilverCoin, Main.rand.Next(3, 7));
							}
						}
					}
					npc.netUpdate = true;
				}
				else if (AI_Timer == 10)
				{
					AI_State = State_Fleeing;
					AI_Timer = 0;
				}
			}
			else if (AI_State == State_Fleeing) //everything is inverted due to npc running away from the player instead of towards. Sprite is also manually mirrored (the png, not codewise)
			{
				coindroptimer++;
				knifetimer++;
				npc.TargetClosest(true);
				if (Main.player[npc.target].Distance(npc.Center) < 599f) // To bring alpha back down when player is back in range
				{
					npc.alpha = 0;
				}
				if (npc.direction == 1) //FACING LEFT - vel to move left
				{

					if (npc.velocity.X > -2.7f)
					{
						npc.velocity += new Vector2(-.06f, 0); //breaking power after turn
					}
					if ((knifetimer > 80) && (Main.rand.Next(70) == 0) && Main.netMode != NetmodeID.MultiplayerClient) //Having this timer allows at least 80 ticks between next knife thrown
					{
						Projectile.NewProjectile(npc.Center, new Vector2(Main.rand.Next(3, 6), Main.rand.Next(-3, -1)), ModContent.ProjectileType<Projectiles.ThrowingKnifeHostile>(), 8, 4);
						knifetimer = 0;
					}
					else if (npc.velocity.X < -4f) //max vel
					{
						npc.velocity += new Vector2(.04f, 0); //slowdown after knockback
					}
					else if ((npc.velocity.X <= -2.7f) && (npc.velocity.X > -4f))
					{
						npc.velocity += new Vector2(-.03f, 0); //running accel.
					}
					npc.netUpdate = true;
				}
				if (npc.direction == -1) //FACING RIGHT + vel to move right
				{
					if (npc.velocity.X < 2.7f)
					{
						npc.velocity += new Vector2(.06f, 0); //breaking power
					}
					if ((knifetimer > 80) && (Main.rand.Next(70) == 0) && Main.netMode != NetmodeID.MultiplayerClient)
					{
						Projectile.NewProjectile(npc.Center, new Vector2(Main.rand.Next(-6, -3), Main.rand.Next(-3, -1)), ModContent.ProjectileType<Projectiles.ThrowingKnifeHostile>(), 8, 4);
						knifetimer = 0;
					}
					else if (npc.velocity.X > 4f) //max vel
					{
						npc.velocity += new Vector2(-.04f, 0); //slowdown after knockback
					}
					else if ((npc.velocity.X >= 2.7f) && (npc.velocity.X < 4f))
					{
						npc.velocity += new Vector2(.03f, 0); //running accel.
					}
					npc.netUpdate = true;
				}
				if (npc.collideX)
				{
					// NPC has stopped upon hitting a block
					AI_State = State_Jump;
					AI_Timer = 0;
				}
				if (!npc.HasValidTarget || Main.player[npc.target].Distance(npc.Center) > 600f) //Despawn
				{
					// Our targeted player seems to have left our range, so we'll drop Alpha rapidly and die, no loot or sound played.
					AI_Timer = 0;
					npc.alpha += 5; 
					if (npc.alpha > 254)
					{
						npc.life = 0;
						if (!Main.dedServ) Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/Escaped").WithVolume(0.7f), npc.Center);
						npc.netUpdate = true;
					}
				}
				if (coinsdropped < 5 && coindroptimer > 40 && Main.rand.Next(100) == 0)
				{
					coinsdropped++;
					coindroptimer = 0;
					if (Main.rand.Next(8) == 0)
					{
						Item.NewItem(npc.Bottom, ItemID.Diamond);
					}
					else
					{
						Item.NewItem(npc.Bottom, ItemID.SilverCoin, Main.rand.Next(3, 7));
					}
				}
			}
		}

		private const int Frame_Asleep = 7;
		private const int Frame_Fleeing_0 = 0;
		private const int Frame_Fleeing_1 = 1;
		private const int Frame_Fleeing_2 = 2;
		private const int Frame_Fleeing_3 = 3;
		private const int Frame_Fleeing_4 = 4;
		private const int Frame_Fleeing_5 = 5;
		private const int Frame_Fleeing_6 = 6;

		public override void FindFrame(int frameHeight)
		{
			// This makes the sprite flip horizontally in conjunction with the npc.direction.


			// For the most part, our animation matches up with our states.
			if (AI_State == State_Asleep)
			{
				// npc.frame.Y is the goto way of changing animation frames. npc.frame starts from the top left corner in pixel coordinates, so keep that in mind.
				npc.frame.Y = Frame_Asleep * frameHeight;

			}
			else if (AI_State == State_Jump)
			{
				npc.frame.Y = Frame_Fleeing_5 * frameHeight;
			}
			else if (AI_State == State_Fleeing)
			{
				// Cycle through all 8 frames
				npc.spriteDirection = npc.direction;
				npc.frameCounter++;
				if (npc.frameCounter < 4)
				{
					npc.frame.Y = Frame_Fleeing_0 * frameHeight;
				}
				else if (npc.frameCounter < 8)
				{
					npc.frame.Y = Frame_Fleeing_1 * frameHeight;
				}
				else if (npc.frameCounter < 12)
				{
					npc.frame.Y = Frame_Fleeing_2 * frameHeight;
				}
				else if (npc.frameCounter < 16)
				{
					npc.frame.Y = Frame_Fleeing_3 * frameHeight;
				}
				else if (npc.frameCounter < 20)
				{
					npc.frame.Y = Frame_Fleeing_4 * frameHeight;
				}
				else if (npc.frameCounter < 24)
				{
					npc.frame.Y = Frame_Fleeing_5 * frameHeight;
				}
				else if (npc.frameCounter < 28)
				{
					npc.frame.Y = Frame_Fleeing_6 * frameHeight;
				}
				else
				{
					npc.frameCounter = 0;
				}
			}
		}
		public override void UpdateLifeRegen(ref int damage)
		{
			if (AI_State == State_Asleep)
			{
				npc.lifeRegen = 2;
			}
		}
		public int coinsdropped = 0;
		public override void HitEffect(int hitDirection, double damage)
		{
			for (int i = 0; i < 15; i++)
			{
				int dustType = 147;
				int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
				Dust dust = Main.dust[dustIndex];

				dust.scale *= .70f + Main.rand.Next(-30, 31) * 0.01f;
				dust.velocity.Y = Main.rand.Next(-2, 0);
				dust.noGravity = false;
				dust.alpha = 120;
			}
			if (npc.life <= 0)
			{
				for (int i = 0; i < 20; i++)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, 147, 0, Main.rand.Next(-2, 0), 120, default(Color), .75f);
				}
			}
			if (coinsdropped < 12)
			{
				coinsdropped++;
				if (Main.rand.Next(8) == 0)
				{
					Item.NewItem(npc.Bottom, ItemID.Diamond);
				}
				else
				{
					Item.NewItem(npc.Bottom, ItemID.SilverCoin, Main.rand.Next(3, 7));
				}
			}
		}
		public override void NPCLoot()
		{
			Item.NewItem(npc.getRect(), ItemID.Mushroom, Main.rand.Next(1, 3));
			Item.NewItem(npc.getRect(), ItemID.ThrowingKnife, Main.rand.Next(20, 50));
		}
	}
}
