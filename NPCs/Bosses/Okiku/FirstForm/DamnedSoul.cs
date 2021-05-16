using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Okiku.FirstForm
{
	public class DamnedSoul : ModNPC
	{
		private bool initiate;

		public int TimerHeal;

		public float TimerAnim;

		public override void SetDefaults()
		{
			npc.alpha = 50;
			npc.width = 50;
			npc.height = 50;
			npc.aiStyle = -1;
			npc.damage = 40;
			npc.defense = 18;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.lifeMax = 2000;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.knockBackResist = 0f;
			Main.npcFrameCount[npc.type] = 4;
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Damned Soul");
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)((float)npc.lifeMax * 0.7f * bossLifeScale);
		}

		public override void AI()
		{
			if (!initiate)
			{
				npc.ai[3] = -Main.rand.Next(200);
				initiate = true;
			}
			TimerAnim += 1f;
			if (TimerAnim > 10f)
			{
				if (Main.rand.Next(2) == 0)
				{
					npc.spriteDirection *= -1;
				}
				TimerAnim = 0f;
			}
			int dust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 62, 0f, 0f, 100, Color.White);
			Main.dust[dust].noGravity = true;
			for (int num37 = 0; num37 < 200; num37++)
			{
				if (Main.npc[num37].active && Main.npc[num37].realLife == npc.whoAmI)
				{
					Main.npc[num37].life = npc.life;
				}
			}
			if (Main.npc[(int)npc.ai[1]].life <= 1000)
			{
				return;
			}
			npc.ai[3] += 1f;
			npc.TargetClosest();
			if (npc.ai[3] >= 0f)
			{
				if (npc.life > 1000)
				{
					if (Main.netMode != NetmodeID.Server)
					{
						float num39 = 0.5f;
						Vector2 vector9 = new Vector2(npc.position.X + (float)(npc.width / 2), npc.position.Y + (float)(npc.height / 2));
						float rotation2 = (float)Math.Atan2(vector9.Y - (Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f), vector9.X - (Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f));
						rotation2 += (float)(Main.rand.Next(-50, 50) / 100);
						Projectile.NewProjectile(vector9.X, vector9.Y, (float)(Math.Cos(rotation2) * (double)num39 * -1.0), (float)(Math.Sin(rotation2) * (double)num39 * -1.0), ModContent.ProjectileType<ObscureShot>(), 30, 0f, Main.myPlayer);
					}
					npc.ai[3] = -200 - Main.rand.Next(200);
				}
				else
				{
					if (Main.netMode != NetmodeID.Server)
					{
						float num38 = 0.5f;
						Vector2 vector8 = new Vector2(npc.position.X + (float)(npc.width / 2), npc.position.Y + (float)(npc.height / 2));
						float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f), vector8.X - (Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f));
						rotation += (float)(Main.rand.Next(-50, 50) / 100);
						int num40 = Projectile.NewProjectile(vector8.X, vector8.Y, (float)(Math.Cos(rotation) * (double)num38 * -1.0), (float)(Math.Sin(rotation) * (double)num38 * -1.0), ModContent.ProjectileType<ObscureShot>(), 30, 0f, Main.myPlayer);
						Main.projectile[num40].scale = 3f;
					}
					npc.ai[3] = -50 - Main.rand.Next(50);
				}
			}
			if (npc.life > 1000)
			{
				return;
			}
			TimerHeal++;
			if (TimerHeal < 600)
			{
				return;
			}
			npc.life = npc.lifeMax;
			TimerHeal = 0;
			for (int num36 = 0; num36 < 200; num36++)
			{
				if (Main.npc[num36].active && Main.npc[num36].realLife == npc.whoAmI)
				{
					Main.npc[num36].life = 2000;
				}
			}
		}

		public override void FindFrame(int frameHeight)
		{
			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
			}
			num++;
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
	}
}
