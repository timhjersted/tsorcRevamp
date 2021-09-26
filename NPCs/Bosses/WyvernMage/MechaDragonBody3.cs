using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.WyvernMage
{
    class MechaDragonBody3 : ModNPC {
		public int Timer = -1000;

		public override void SetDefaults() {
			npc.netAlways = true;
			npc.boss = true;
			npc.npcSlots = 1;
			npc.aiStyle = 6;
			npc.width = 45;
			npc.height = 45;
			npc.knockBackResist = 0f;
			npc.timeLeft = 750;
			npc.damage = 45;
			npc.defense = 20;
			npc.HitSound = SoundID.NPCHit4;
			npc.DeathSound = SoundID.NPCDeath10;
			npc.lifeMax = 35000;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.behindTiles = true;
			npc.value = 25000;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
		}

		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Wyvern Mage Disciple");
		}
		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			return false;
		}
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale) {
			npc.lifeMax = (int)(npc.lifeMax * 0.7f * bossLifeScale);
		}
		public override void AI() {

			//Generic Worm Part Code:
			tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<MechaDragonHead>(), MechaDragonHead.bodyTypes, ModContent.NPCType<MechaDragonTail>(), 23, -1f, 12f, 0.13f, true, false);

			//Code unique to this body part:
			Timer++;

			if (!Main.npc[(int)npc.ai[1]].active)
			{
				npc.life = 0;
				for (int num36 = 0; num36 < 50; num36++)
				{
					Color color = new Color();
					int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, 0, 0, 100, color, 8.0f);
					Main.dust[dust].noGravity = true;
					Main.dust[dust].noGravity = true;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
					Main.dust[dust].noGravity = true;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
					Main.dust[dust].noGravity = true;
					dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
					Main.dust[dust].noGravity = true;
				}
				npc.HitEffect(0, 10.0);
				npc.active = false;
			}

			if (Timer >= 0)
			{
				if (Main.netMode != 2)
				{
					float num48 = 7f;
					Vector2 vector8 = new Vector2(npc.position.X + (npc.width / 2), npc.position.Y + (npc.height / 2));
					float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
					rotation += Main.rand.Next(-50, 50) / 100;
					Projectile.NewProjectile(vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), ModContent.ProjectileType<Projectiles.Enemy.CrystalFire>(), 15, 0f, 0);
					Timer = -1200 - Main.rand.Next(1400);
				}
				//npc.netUpdate=true; //new
			}
		}

		public override bool CheckActive()
		{
			return false;
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
