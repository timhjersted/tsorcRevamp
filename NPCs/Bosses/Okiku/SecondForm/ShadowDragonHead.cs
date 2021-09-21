using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Okiku.SecondForm {
	public class ShadowDragonHead : ModNPC
	{
		//private bool initiate;

		public int TimerHeal;

		public float TimerAnim;

		public override void SetDefaults()
		{
			npc.width = 32;
			npc.height = 32;
			npc.aiStyle = -1;
			npc.damage = 90;
			npc.defense = 19;
			npc.boss = true;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.lifeMax = 9000;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.knockBackResist = 0f;
			Main.npcFrameCount[npc.type] = 1;
			despawnHandler = new NPCDespawnHandler(DustID.PurpleCrystalShard);
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Shadow Dragon");
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)((float)npc.lifeMax * 0.7f * bossLifeScale);
		}

		NPCDespawnHandler despawnHandler;
		public override void AI()
		{
			despawnHandler.TargetAndDespawn(npc.whoAmI);

			if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[0] == 0f)
			{
				npc.ai[2] = npc.whoAmI;
				npc.realLife = npc.whoAmI;
				int num119 = npc.whoAmI;
				for (int num120 = 0; num120 < 24; num120++)
				{
					int num121 = mod.GetNPC("ShadowDragonBody").npc.type;
					switch (num120)
					{
					case 4:
					case 9:
					case 14:
					case 19:
						num121 = mod.GetNPC("ShadowDragonLegs").npc.type;
						break;
					case 21:
						num121 = mod.GetNPC("ShadowDragonBody2").npc.type;
						break;
					case 22:
						num121 = mod.GetNPC("ShadowDragonBody3").npc.type;
						break;
					case 23:
						num121 = mod.GetNPC("ShadowDragonTail").npc.type;
						break;
					}
					int num122 = NPC.NewNPC((int)(npc.position.X + (float)(npc.width / 2)), (int)(npc.position.Y + (float)npc.height), num121, npc.whoAmI);
					Main.npc[num122].ai[2] = npc.whoAmI;
					Main.npc[num122].realLife = npc.whoAmI;
					Main.npc[num122].ai[1] = num119;
					Main.npc[num119].ai[0] = num122;
					NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num122);
					num119 = num122;
				}
			}
			_ = (int)(npc.position.X / 16f) - 1;
			int num108 = (int)((npc.position.X + (float)npc.width) / 16f) + 2;
			int num109 = (int)(npc.position.Y / 16f) - 1;
			int num110 = (int)((npc.position.Y + (float)npc.height) / 16f) + 2;
			_ = 0;
			if (num108 > Main.maxTilesX)
			{
				num108 = Main.maxTilesX;
			}
			if (num109 < 0)
			{
				num109 = 0;
			}
			if (num110 > Main.maxTilesY)
			{
				num110 = Main.maxTilesY;
			}
			if (npc.velocity.X < 0f)
			{
				npc.spriteDirection = 1;
			}
			if (npc.velocity.X > 0f)
			{
				npc.spriteDirection = -1;
			}
			float num111 = 16f;
			float num112 = 0.4f;
			Vector2 vector14 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
			float num113 = (float)Main.rand.Next(-500, 500) + Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2);
			float num114 = (float)Main.rand.Next(-500, 500) + Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2);
			num113 = (int)(num113 / 16f) * 16;
			num114 = (int)(num114 / 16f) * 16;
			vector14.X = (int)(vector14.X / 16f) * 16;
			vector14.Y = (int)(vector14.Y / 16f) * 16;
			num113 -= vector14.X;
			num114 -= vector14.Y;
			float num115 = (float)Math.Sqrt(num113 * num113 + num114 * num114);
			float num116 = Math.Abs(num113);
			float num117 = Math.Abs(num114);
			float num118 = num111 / num115;
			num113 *= num118;
			num114 *= num118;
			bool flag14 = false;
			if (((npc.velocity.X > 0f && num113 < 0f) || (npc.velocity.X < 0f && num113 > 0f) || (npc.velocity.Y > 0f && num114 < 0f) || (npc.velocity.Y < 0f && num114 > 0f)) && Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) > num112 / 2f && num115 < 300f)
			{
				flag14 = true;
				if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < num111)
				{
					npc.velocity *= 1.1f;
				}
			}
			if (npc.position.Y > Main.player[npc.target].position.Y || (double)(Main.player[npc.target].position.Y / 16f) > Main.worldSurface || Main.player[npc.target].dead)
			{
				flag14 = true;
				if (Math.Abs(npc.velocity.X) < num111 / 2f)
				{
					if (npc.velocity.X == 0f)
					{
						npc.velocity.X = npc.velocity.X - (float)npc.direction;
					}
					npc.velocity.X = npc.velocity.X * 1.1f;
				}
				else if (npc.velocity.Y > 0f - num111)
				{
					npc.velocity.Y = npc.velocity.Y - num112;
				}
			}
			if (!flag14)
			{
				if ((npc.velocity.X > 0f && num113 > 0f) || (npc.velocity.X < 0f && num113 < 0f) || (npc.velocity.Y > 0f && num114 > 0f) || (npc.velocity.Y < 0f && num114 < 0f))
				{
					if (npc.velocity.X < num113)
					{
						npc.velocity.X = npc.velocity.X + num112;
					}
					else if (npc.velocity.X > num113)
					{
						npc.velocity.X = npc.velocity.X - num112;
					}
					if (npc.velocity.Y < num114)
					{
						npc.velocity.Y = npc.velocity.Y + num112;
					}
					else if (npc.velocity.Y > num114)
					{
						npc.velocity.Y = npc.velocity.Y - num112;
					}
					if ((double)Math.Abs(num114) < (double)num111 * 0.2 && ((npc.velocity.X > 0f && num113 < 0f) || (npc.velocity.X < 0f && num113 > 0f)))
					{
						if (npc.velocity.Y > 0f)
						{
							npc.velocity.Y = npc.velocity.Y + num112 * 2f;
						}
						else
						{
							npc.velocity.Y = npc.velocity.Y - num112 * 2f;
						}
					}
					if ((double)Math.Abs(num113) < (double)num111 * 0.2 && ((npc.velocity.Y > 0f && num114 < 0f) || (npc.velocity.Y < 0f && num114 > 0f)))
					{
						if (npc.velocity.X > 0f)
						{
							npc.velocity.X = npc.velocity.X + num112 * 2f;
						}
						else
						{
							npc.velocity.X = npc.velocity.X - num112 * 2f;
						}
					}
				}
				else if (num116 > num117)
				{
					if (npc.velocity.X < num113)
					{
						npc.velocity.X = npc.velocity.X + num112 * 1.1f;
					}
					else if (npc.velocity.X > num113)
					{
						npc.velocity.X = npc.velocity.X - num112 * 1.1f;
					}
					if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num111 * 0.5)
					{
						if (npc.velocity.Y > 0f)
						{
							npc.velocity.Y = npc.velocity.Y + num112;
						}
						else
						{
							npc.velocity.Y = npc.velocity.Y - num112;
						}
					}
				}
				else
				{
					if (npc.velocity.Y < num114)
					{
						npc.velocity.Y = npc.velocity.Y + num112 * 1.1f;
					}
					else if (npc.velocity.Y > num114)
					{
						npc.velocity.Y = npc.velocity.Y - num112 * 1.1f;
					}
					if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num111 * 0.5)
					{
						if (npc.velocity.X > 0f)
						{
							npc.velocity.X = npc.velocity.X + num112;
						}
						else
						{
							npc.velocity.X = npc.velocity.X - num112;
						}
					}
				}
			}
			npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
			if (Main.rand.Next(3) == 0)
			{
				int dust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 62, 0f, 0f, 100, Color.White, 2f);
				Main.dust[dust].noGravity = true;
			}
			if (npc.life <= 0)
			{
				npc.active = false;
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			Vector2 origin = new Vector2(Main.npcTexture[npc.type].Width / 2, Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type] / 2);
			Color alpha = Color.White;
			SpriteEffects effects = SpriteEffects.None;
			if (npc.spriteDirection == 1)
			{
				effects = SpriteEffects.FlipHorizontally;
			}
			spriteBatch.Draw(Main.npcTexture[npc.type], new Vector2(npc.position.X - Main.screenPosition.X + (float)(npc.width / 2) - (float)Main.npcTexture[npc.type].Width * npc.scale / 2f + origin.X * npc.scale, npc.position.Y - Main.screenPosition.Y + (float)npc.height - (float)Main.npcTexture[npc.type].Height * npc.scale / (float)Main.npcFrameCount[npc.type] + 4f + origin.Y * npc.scale + 56f), npc.frame, alpha, npc.rotation, origin, npc.scale, effects, 0f);
			npc.alpha = 255;
			return true;
		}

        public override void NPCLoot() {
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 500);
        }
    }
}
