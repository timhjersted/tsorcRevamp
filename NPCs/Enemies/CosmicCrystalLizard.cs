using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
	public class CosmicCrystalLizard : ModNPC
	{
		public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Cosmic Crystal Lizard");
			Main.npcFrameCount[npc.type] = 29;
		}

		public override void SetDefaults()
		{
			npc.width = 28;
			npc.height = 20;
			npc.aiStyle = -1;
			npc.damage = 0;
			npc.knockBackResist = 0.6f;
			npc.defense = 9999;
			npc.lifeMax = Main.rand.Next(13, 21);
			npc.HitSound = SoundID.NPCHit42;
			npc.DeathSound = SoundID.NPCDeath32;
			npc.value = 0;
			npc.rarity = 5;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.Venom] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
			npc.buffImmune[BuffID.Frostburn] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.ShadowFlame] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.CrescentMoonlight>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.DarkInferno>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.CrimsonBurn>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.ToxicCatDrain>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.ViruCatDrain>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.BiohazardDrain>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.ElectrocutedBuff>()] = true;
			npc.buffImmune[ModContent.BuffType<Buffs.PolarisElectrocutedBuff>()] = true;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.CosmicCrystalLizardBanner>();
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0;

			if (NPC.CountNPCS(mod.NPCType("CosmicCrystalLizard")) < 1 && (spawnInfo.player.ZoneRockLayerHeight || spawnInfo.player.ZoneDirtLayerHeight) && !spawnInfo.water && !Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].halfBrick() && !Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].rightSlope() && !Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].leftSlope() && !spawnInfo.player.ZoneJungle)
			{
				return 0.02f;
			}
			if (NPC.CountNPCS(mod.NPCType("CosmicCrystalLizard")) < 1 && (spawnInfo.player.ZoneRockLayerHeight || spawnInfo.player.ZoneDirtLayerHeight) && !spawnInfo.water && !Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].halfBrick() && !Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].rightSlope() && !Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].leftSlope() && spawnInfo.player.ZoneJungle)
			{
				return 0.02f;
			}
			if (NPC.CountNPCS(mod.NPCType("CosmicCrystalLizard")) < 1 && spawnInfo.player.ZoneOverworldHeight && !spawnInfo.water && !Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].halfBrick() && !Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].rightSlope() && !Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].leftSlope() && (Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY - 2].wall == WallID.DirtUnsafe || Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY - 2].wall == WallID.MudUnsafe || Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY - 2].wall == WallID.Planked))
			{
				return 0.02f;
			}
			return chance;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Texture2D texture = Main.npcTexture[npc.type];
			Texture2D textureglow = mod.GetTexture("NPCs/Enemies/CosmicCrystalLizard_Glow");
			SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			if (npc.spriteDirection == -1)
			{
				spriteBatch.Draw(textureglow, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 94, 88), Color.White, npc.rotation, new Vector2(70, 74), npc.scale, effects, 0f);
				spriteBatch.Draw(texture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 94, 88), lightColor, npc.rotation, new Vector2(70, 74), npc.scale, effects, 0.1f);
			}
			else
			{
				spriteBatch.Draw(textureglow, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 94, 88), Color.White, npc.rotation, new Vector2(24, 74), npc.scale, effects, 0f);
				spriteBatch.Draw(texture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 94, 88), lightColor, npc.rotation, new Vector2(24, 74), npc.scale, effects, 0.1f);
			}

			return false; //whether to not to draw the base sprite
		}


		#region AI

		private const int AI_State_Slot = 0;
		private const int AI_Timer_Slot = 1;

		private const int State_Idle = 0;
		private const int State_Jump = 2;
		private const int State_Fleeing = 3;
		private const int State_PeaceOut = 4;

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
		public int peaceouttimer = 0;
		public int idleframe = 1;
		public int immuneframe = 0;

		public override void AI()
		{

			npc.netUpdate = false;
			immuneframe++;

			if (immuneframe > 1)
			{
				npc.immortal = false;
				npc.defense = 9999;
			}

			if (AI_State == State_Idle)
			{
				if (!Main.dedServ) Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/CosmicSparkle"), npc.Center);
				
				npc.TargetClosest(true);
				AI_Timer++;

				if (npc.velocity.Y == 0)
				{
					npc.velocity = new Vector2(0f, 0f);
				}

				if (((npc.HasValidTarget && Main.player[npc.target].Distance(npc.Center) < 250f) || npc.life < npc.lifeMax) && npc.collideY)
				{
					AI_State = State_Fleeing;
					AI_Timer = 0;
				}

			}
			else if (AI_State == State_Jump)
			{
				AI_Timer++;
				peaceouttimer++;
				if (AI_Timer == 1)
				{
					npc.velocity = new Vector2(npc.direction * -2.7f, -3.6f);
				}
				else if (AI_Timer == 10)
				{
					AI_State = State_Fleeing;
					AI_Timer = 0;
				}
			}
			else if (AI_State == State_Fleeing)
			{
				peaceouttimer++;
				npc.TargetClosest(true);
				if (npc.direction == 1) //FACING LEFT - vel to move left
				{
					if (npc.velocity.X > -2.7f)
					{
						npc.velocity += new Vector2(-.1f, 0); //breaking power after turn
					}
					else if (npc.velocity.X < -4f) //max vel
					{
						npc.velocity += new Vector2(.04f, 0); //slowdown after knockback
					}
					else if ((npc.velocity.X <= -2.7f) && (npc.velocity.X > -4f))
					{
						npc.velocity += new Vector2(-.03f, 0); //running accel.
					}
				}

				if (npc.direction == -1) //FACING RIGHT + vel to move right
				{
					if (npc.velocity.X < 2.7f)
					{
						npc.velocity += new Vector2(.1f, 0); //breaking power
					}
					else if (npc.velocity.X > 4f) //max vel
					{
						npc.velocity += new Vector2(-.04f, 0); //slowdown after knockback
					}
					else if ((npc.velocity.X >= 2.7f) && (npc.velocity.X < 4f))
					{
						npc.velocity += new Vector2(.03f, 0); //running accel.
					}
				}
				if (npc.collideX && npc.collideY)
				{
					// NPC has stopped upon hitting a block
					AI_State = State_Jump;
					peaceouttimer += 1;
					AI_Timer = 0;
				}
				if (npc.velocity.X == 0 && npc.velocity.Y == 0)
				{
					AI_State = State_Jump;
				}
			}
			else if (AI_State == State_PeaceOut)
			{
				AI_Timer++;

				npc.noGravity = true;
				npc.velocity = new Vector2(0, 0);

				if (AI_Timer == 37)
				{
					Main.PlaySound(SoundID.Item82, npc.Center);
				}
				if (AI_Timer == 128)
				{
					npc.life = 0;
				}
			}
			if ((peaceouttimer > 150 && Main.rand.Next(100) == 0) || peaceouttimer > 420 && (npc.collideY))
			{
				AI_State = State_PeaceOut;
				AI_Timer = 0;
				peaceouttimer = 0;
			}

			if (AI_State == State_Idle || AI_State == State_Fleeing || AI_State == State_Jump) //Dusts
			{
				if (Main.rand.Next(10) == 0) //Yellow
				{
					Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 170, npc.velocity.X, npc.velocity.Y, 100, default(Color), .4f)];
					dust.velocity *= 0f;
					dust.noGravity = true;
					dust.velocity += npc.velocity;
					dust.fadeIn = 1f;
				}
				if (Main.rand.Next(10) == 0) //Pink
				{
					Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 272, npc.velocity.X, npc.velocity.Y, 100, default(Color), .5f)]; //223, 255, 272
					dust.velocity *= 0f;
					dust.noGravity = true;
					dust.velocity += npc.velocity;
					dust.fadeIn = 1f;
				}
				if (Main.rand.Next(10) == 0) //Blue
				{
					Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 185, npc.velocity.X, npc.velocity.Y, 100, default(Color), .4f)];
					dust.velocity *= 0f;
					dust.noGravity = true;
					dust.velocity += npc.velocity;
					dust.fadeIn = 1f;
				}
				if (Main.rand.Next(10) == 0) //Green
				{
					Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 107, npc.velocity.X, npc.velocity.Y, 100, default(Color), .4f)];
					dust.velocity *= 0f;
					dust.noGravity = true;
					dust.velocity += npc.velocity;
					dust.fadeIn = 1f;
				}
			}
		}

		#endregion

		#region Animation

		//Idle
		private const int Frame_Idle_1 = 0;
		private const int Frame_Idle_2 = 1;
		private const int Frame_Idle_3 = 2;

		//Fleeing
		private const int Frame_Fleeing_1 = 3;
		private const int Frame_Fleeing_2 = 4;
		private const int Frame_Fleeing_3 = 5;
		private const int Frame_Fleeing_4 = 6;
		private const int Frame_Fleeing_5 = 7;
		private const int Frame_Fleeing_6 = 8;
		private const int Frame_Fleeing_7 = 9;
		private const int Frame_Fleeing_8 = 10;
		private const int Frame_Fleeing_9 = 11;
		private const int Frame_Fleeing_10 = 12;

		//PeaceOut
		private const int Frame_PeaceOut_1 = 13;
		private const int Frame_PeaceOut_2 = 14;
		private const int Frame_PeaceOut_3 = 15;
		private const int Frame_PeaceOut_4 = 16;
		private const int Frame_PeaceOut_5 = 17;
		private const int Frame_PeaceOut_6 = 18;
		private const int Frame_PeaceOut_7 = 19;
		private const int Frame_PeaceOut_8 = 20;
		private const int Frame_PeaceOut_9 = 21;
		private const int Frame_PeaceOut_10 = 22;
		private const int Frame_PeaceOut_11 = 23;
		private const int Frame_PeaceOut_12 = 24;
		private const int Frame_PeaceOut_13 = 25;
		private const int Frame_PeaceOut_14 = 26;
		private const int Frame_PeaceOut_15 = 27;
		private const int Frame_PeaceOut_16 = 28;


		public override void FindFrame(int frameHeight)
		{

			// For the most part, our animation matches up with our states.
			if (AI_State == State_Idle)
			{
				// Cycle through all idle frames
				npc.spriteDirection = npc.direction;
				npc.frameCounter += Main.rand.Next(1, 4);
				if (npc.frameCounter < 250 && idleframe == 1)
				{
					npc.frame.Y = Frame_Idle_1 * frameHeight;
				}
				if ((idleframe == 1 && npc.frameCounter > 250) || (idleframe == 2 && npc.frameCounter > 20) || (idleframe == 3 && npc.frameCounter > 200) || (idleframe == 4 && npc.frameCounter > 20))
				{
					npc.frameCounter = 0;
					idleframe++;
				}
				else if (npc.frameCounter < 20 && idleframe == 2)
				{
					npc.frame.Y = Frame_Idle_2 * frameHeight;
				}
				else if (npc.frameCounter < 200 && idleframe == 3)
				{
					npc.frame.Y = Frame_Idle_3 * frameHeight;
				}
				else if (npc.frameCounter < 20 && idleframe == 4)
				{
					npc.frame.Y = Frame_Idle_2 * frameHeight;
				}
				else if (idleframe == 5)
				{
					npc.frameCounter = 0;
					idleframe = 1;
				}
			}

			else if (AI_State == State_Jump)
			{
				npc.frame.Y = Frame_Fleeing_3 * frameHeight;
			}

			else if (AI_State == State_Fleeing)
			{
				// Cycle through all running frames
				npc.spriteDirection = npc.direction;
				npc.frameCounter++;
				if (npc.frameCounter < 3)
				{
					npc.frame.Y = Frame_Fleeing_1 * frameHeight;
				}
				else if (npc.frameCounter < 6)
				{
					npc.frame.Y = Frame_Fleeing_2 * frameHeight;
				}
				else if (npc.frameCounter < 9)
				{
					npc.frame.Y = Frame_Fleeing_3 * frameHeight;
				}
				else if (npc.frameCounter < 12)
				{
					npc.frame.Y = Frame_Fleeing_4 * frameHeight;
				}
				else if (npc.frameCounter < 15)
				{
					npc.frame.Y = Frame_Fleeing_5 * frameHeight;
				}
				else if (npc.frameCounter < 18)
				{
					npc.frame.Y = Frame_Fleeing_6 * frameHeight;
				}
				else if (npc.frameCounter < 21)
				{
					npc.frame.Y = Frame_Fleeing_7 * frameHeight;
				}
				else if (npc.frameCounter < 24)
				{
					npc.frame.Y = Frame_Fleeing_8 * frameHeight;
				}
				else if (npc.frameCounter < 27)
				{
					npc.frame.Y = Frame_Fleeing_9 * frameHeight;
				}
				else if (npc.frameCounter < 30)
				{
					npc.frame.Y = Frame_Fleeing_10 * frameHeight;
				}
				else
				{
					npc.frameCounter = 0;
				}
			}

			else if (AI_State == State_PeaceOut)
			{
				//play despawn frames once
				npc.spriteDirection = npc.direction;
				npc.frameCounter++;

				if (npc.frameCounter < 15)
				{
					npc.frame.Y = Frame_Fleeing_10 * frameHeight;
					if (npc.direction == -1)
					{
						if (Main.rand.Next(10) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 170, -1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 272, -1, -1, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 185, -1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 107, -1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
					if (npc.direction == 1)
					{
						if (Main.rand.Next(10) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 170, 1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 272, 1, -1, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 185, 1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 107, 1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
				}

				else if (npc.frameCounter < 27)
				{
					npc.frame.Y = Frame_PeaceOut_1 * frameHeight;
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.2f, 0.2f, 0.2f); //when facing right
						if (Main.rand.Next(10) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 170, -1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 272, -1, -1, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 185, -1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 107, -1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.2f, 0.2f, 0.2f); //when facing left
						if (Main.rand.Next(10) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 170, 1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 272, 1, -1, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 185, 1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 107, 1, -1, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
				}

				else if (npc.frameCounter < 37)
				{
					npc.dontTakeDamage = true;
					npc.frame.Y = Frame_PeaceOut_2 * frameHeight;
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.25f, 0.25f, 0.25f);

						if (Main.rand.Next(10) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 170, -2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 272, -2, -2, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 185, -2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 107, -2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.25f, 0.25f, 0.25f);
						if (Main.rand.Next(10) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 170, 2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 272, 2, -2, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 185, 2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 107, 2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}

				}

				else if (npc.frameCounter < 45)
				{
					npc.dontTakeDamage = true;
					npc.frame.Y = Frame_PeaceOut_3 * frameHeight;
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.3f, 0.3f, 0.3f);

						if (Main.rand.Next(8) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 170, -2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(8) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 272, -2, -2, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(8) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 185, -2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(8) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 107, -2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.3f, 0.3f, 0.3f);

						if (Main.rand.Next(8) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 170, 2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(8) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 272, 2, -2, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(8) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 185, 2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(8) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 2), 28, 18, 107, 2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}

				}

				else if (npc.frameCounter < 53)
				{
					npc.dontTakeDamage = true;
					npc.frame.Y = Frame_PeaceOut_4 * frameHeight;
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.35f, 0.35f, 0.35f);

						if (Main.rand.Next(6) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 4), 28, 18, 170, -2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(6) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 4), 28, 18, 272, -2, -2, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(6) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 4), 28, 18, 185, -2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(6) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 4), 28, 18, 107, -2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.35f, 0.35f, 0.35f);

						if (Main.rand.Next(6) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 4), 28, 18, 170, 2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(6) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 4), 28, 18, 272, 2, -2, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(6) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 4), 28, 18, 185, 2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(6) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X, npc.position.Y - 4), 28, 18, 107, 2, -2, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
				}

				else if (npc.frameCounter < 61)
				{
					npc.dontTakeDamage = true;
					npc.frame.Y = Frame_PeaceOut_5 * frameHeight;
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);

						if (Main.rand.Next(10) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 170, -3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 272, -3, -3, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 185, -3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 107, -3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);

						if (Main.rand.Next(10) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 170, 3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 272, 3, -3, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 185, 3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 107, 3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
				}

				else if (npc.frameCounter < 69)
				{
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);

						if (Main.rand.Next(10) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 170, -3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 272, -3, -3, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 185, -3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 107, -3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);

						if (Main.rand.Next(10) == 0) //Yellow
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 170, 3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Pink
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 272, 3, -3, 100, default(Color), .5f)]; //223, 255, 272
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Blue
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 185, 3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
						if (Main.rand.Next(10) == 0) //Green
						{
							Dust dust = Main.dust[Dust.NewDust(new Vector2(npc.position.X - 2, npc.position.Y - 6), 28, 18, 107, 3, -3, 100, default(Color), .4f)];
							dust.noGravity = true;
							dust.fadeIn = 1f;
						}
					}
					npc.dontTakeDamage = true;
					npc.frame.Y = Frame_PeaceOut_6 * frameHeight;
				}
				else if (npc.frameCounter < 77)
				{
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.45f, 0.45f, 0.45f);
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.45f, 0.45f, 0.45f);
					}
					npc.frame.Y = Frame_PeaceOut_7 * frameHeight;
					npc.dontTakeDamage = true;
				}
				else if (npc.frameCounter < 85)
				{
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.4f, 0.4f, 0.4f);
					}
					npc.frame.Y = Frame_PeaceOut_8 * frameHeight;
					npc.dontTakeDamage = true;
				}
				else if (npc.frameCounter < 93)
				{
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.35f, 0.35f, 0.35f);
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.35f, 0.35f, 0.35f);
					}
					npc.frame.Y = Frame_PeaceOut_9 * frameHeight;
					npc.dontTakeDamage = true;
				}
				else if (npc.frameCounter < 100)
				{
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.3f, 0.3f, 0.3f);
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.3f, 0.3f, 0.3f);
					}
					npc.frame.Y = Frame_PeaceOut_10 * frameHeight;
					npc.dontTakeDamage = true;
				}
				else if (npc.frameCounter < 107)
				{
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.2f, 0.2f, 0.2f);
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.2f, 0.2f, 0.2f);
					}
					npc.frame.Y = Frame_PeaceOut_11 * frameHeight;
					npc.dontTakeDamage = true;
				}
				else if (npc.frameCounter < 114)
				{
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.15f, 0.15f, 0.15f);
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.15f, 0.15f, 0.15f);
					}
					npc.frame.Y = Frame_PeaceOut_12 * frameHeight;
					npc.dontTakeDamage = true;
				}
				else if (npc.frameCounter < 121)
				{
					if (npc.direction == -1)
					{
						Lighting.AddLight(((int)npc.position.X - 24) / 16, ((int)npc.position.Y - 36) / 16, 0.1f, 0.1f, 0.1f);
					}
					if (npc.direction == 1)
					{
						Lighting.AddLight(((int)npc.position.X + 50) / 16, ((int)npc.position.Y - 36) / 16, 0.1f, 0.1f, 0.1f);
					}
					npc.frame.Y = Frame_PeaceOut_13 * frameHeight;
					npc.dontTakeDamage = true;
				}
				else if (npc.frameCounter < 123)
				{
					npc.frame.Y = Frame_PeaceOut_14 * frameHeight;
					npc.dontTakeDamage = true;
				}
				else if (npc.frameCounter < 126)
				{
					npc.frame.Y = Frame_PeaceOut_15 * frameHeight;
					npc.dontTakeDamage = true;
				}
				else if (npc.frameCounter < 128)
				{
					npc.frame.Y = Frame_PeaceOut_16 * frameHeight;
					npc.dontTakeDamage = true;
				}
			}
		}
        #endregion


        public override void HitEffect(int hitDirection, double damage)
		{
			for (int i = 0; i < 5; i++)
			{
				int dustType = 191;
				int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
				Dust dust = Main.dust[dustIndex];

				dust.scale *= .70f + Main.rand.Next(-30, 31) * 0.01f;
				dust.velocity.Y = Main.rand.Next(-2, 0);
				dust.noGravity = false;
				dust.alpha = 0;
			}
			if (npc.life <= 0)
			{
				for (int i = 0; i < 20; i++)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, 191, 0, Main.rand.Next(-2, 0), 0, default(Color), .75f);
				}
			}
		}
		public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			if (Main.rand.Next(2) == 0 && immuneframe >= 1 && !crit)
			{
				npc.immortal = true;
				immuneframe = 0;
			}
			else 
			{
				damage = 1;
			}
			if (crit && immuneframe >= 1)
			{
				damage = 2;
				npc.defense = 0;
				immuneframe = 0;
			}
		}
		public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
		{
			if (crit && immuneframe >= 1)
			{
				damage = 2;
				npc.defense = 0;
				immuneframe = 0;
			}
		}
		public override void NPCLoot()
		{
			Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"), 400);
			Item.NewItem(npc.getRect(), mod.ItemType("EternalCrystal")); //always drops 1
			Item.NewItem(npc.getRect(), ItemID.EndurancePotion);


			if (Main.rand.NextFloat() >= 0.6f) // 60% chance
			{
				Item.NewItem(npc.getRect(), mod.ItemType("SoulSiphonPotion"));
			}


			if (npc.lifeMax == 20 || npc.lifeMax == 19) //the higher the npc.lifeMax, the higher the chance of getting a second crystal
			{
				Item.NewItem(npc.getRect(), mod.ItemType("EternalCrystal"));
			}
			else if (npc.lifeMax == 18 && Main.rand.NextFloat() >= 0.15f)
			{
				Item.NewItem(npc.getRect(), mod.ItemType("EternalCrystal"));
			}
			else if (npc.lifeMax == 17 && Main.rand.NextFloat() >= 0.3f)
			{
				Item.NewItem(npc.getRect(), mod.ItemType("EternalCrystal"));
			}
			else if (npc.lifeMax == 16 && Main.rand.NextFloat() >= 0.45f)
			{
				Item.NewItem(npc.getRect(), mod.ItemType("EternalCrystal"));
			}
			else if (npc.lifeMax == 15 && Main.rand.NextFloat() >= 0.6f)
			{
				Item.NewItem(npc.getRect(), mod.ItemType("EternalCrystal"));
			}
			else if (npc.lifeMax == 14 && Main.rand.NextFloat() >= 0.75f)
			{
				Item.NewItem(npc.getRect(), mod.ItemType("EternalCrystal"));
			}

			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.15f, (float)Main.rand.Next(-30, 31) * 0.15f), mod.GetGoreSlot("Gores/CosmicCrystalLizard_Gore1"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.15f, (float)Main.rand.Next(-30, 31) * 0.15f), mod.GetGoreSlot("Gores/CosmicCrystalLizard_Gore2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.15f, (float)Main.rand.Next(-30, 31) * 0.15f), mod.GetGoreSlot("Gores/CosmicCrystalLizard_Gore3"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.15f, (float)Main.rand.Next(-30, 31) * 0.15f), mod.GetGoreSlot("Gores/CosmicCrystalLizard_Gore4"), 1f);

		}
	}
}
