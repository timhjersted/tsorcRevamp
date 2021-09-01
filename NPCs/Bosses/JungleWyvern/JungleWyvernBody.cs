using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.JungleWyvern {
    class JungleWyvernBody : ModNPC {

		public int Timer = -1000;

		public override void SetDefaults() {
			npc.netAlways = true;
			npc.npcSlots = 1;
			npc.aiStyle = 6;
			npc.width = 45;
			npc.height = 45;
			npc.knockBackResist = 0f;
			npc.timeLeft = 1750;
			npc.damage = 49;
			npc.defense = 24;
			npc.HitSound = SoundID.NPCHit7;
			npc.DeathSound = SoundID.NPCDeath8;
			npc.lifeMax = 15000;
			npc.boss = true;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.value = 70000;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
		}

		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Ancient Jungle Wyvern");
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale) {
			npc.lifeMax = (int)(npc.lifeMax * 0.7f * bossLifeScale);
		}

		public override void AI() {
			if (Timer == -1000) {
				Timer = -Main.rand.Next(800);
				npc.netUpdate = true;
			}
			npc.TargetClosest();
			Timer++;
			if (!Main.npc[(int)npc.ai[1]].active) {
				npc.life = 0;
				npc.HitEffect(0, 10.0);
				NPCLoot();
				for (int num36 = 0; num36 < 50; num36++) {
					Color color = new Color();
					int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 10f);
					Main.dust[dust].noGravity = false;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 6f);
					Main.dust[dust].noGravity = false;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 6f);
					Main.dust[dust].noGravity = false;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, 0, 0, 100, Color.White, 10.0f);
					Main.dust[dust].noGravity = true;
					//npc.netUpdate = true; //new
				}

				npc.active = false;
			}
			if (npc.position.X > Main.npc[(int)npc.ai[1]].position.X) {
				npc.spriteDirection = 1;
			}
			if (npc.position.X < Main.npc[(int)npc.ai[1]].position.X) {
				npc.spriteDirection = -1;
			}
			if (Main.rand.Next(2) == 0) {
				int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y + 10), npc.width, npc.height, 62, 0, 0, 100, Color.White, 2.0f);
				Main.dust[dust].noGravity = true;
			}
			if (Timer >= 300 && Main.netMode != NetmodeID.Server) {
				npc.netUpdate = true;
				float num48 = 1f;
				Vector2 vector8 = new Vector2(npc.position.X + (float)(npc.width / 2), npc.position.Y + (float)(npc.height / 2));
				float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f), vector8.X - (Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f));
				rotation += (float)(Main.rand.Next(-50, 50) / 100);
				Projectile.NewProjectile(vector8.X, vector8.Y, (float)(Math.Cos(rotation) * (double)num48 * -1.0), (float)(Math.Sin(rotation) * (double)num48 * -1.0), ModContent.ProjectileType<Projectiles.Enemy.PoisonFlames>(), 40, 0f, Main.myPlayer); //enemy cursed flamess
				Timer = -300 - Main.rand.Next(300);
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor) {
			Vector2 origin = new Vector2(Main.npcTexture[npc.type].Width / 2, Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type] / 2);
			Color alpha = Color.White;
			SpriteEffects effects = SpriteEffects.None;
			if (npc.spriteDirection == 1) {
				effects = SpriteEffects.FlipHorizontally;
			}
			spriteBatch.Draw(Main.npcTexture[npc.type], new Vector2(npc.position.X - Main.screenPosition.X + (float)(npc.width / 2) - (float)Main.npcTexture[npc.type].Width * npc.scale / 2f + origin.X * npc.scale, npc.position.Y - Main.screenPosition.Y + (float)npc.height - (float)Main.npcTexture[npc.type].Height * npc.scale / (float)Main.npcFrameCount[npc.type] + 4f + origin.Y * npc.scale + 56f), npc.frame, alpha, npc.rotation, origin, npc.scale, effects, 0f);
			npc.alpha = 255;
			return true;
		}
	}
}
