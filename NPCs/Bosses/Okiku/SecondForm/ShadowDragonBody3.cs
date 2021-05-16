using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Okiku.SecondForm
{
	public class ShadowDragonBody3 : ModNPC
	{
		public int Timer = -1000;

		public override void SetDefaults()
		{
			base.npc.width = 32;
			base.npc.height = 32;
			base.npc.aiStyle = 6;
			base.npc.damage = 30;
			base.npc.defense = 20;
			base.npc.boss = true;
			base.npc.noGravity = true;
			base.npc.noTileCollide = true;
			base.npc.lifeMax = 4000;
			base.npc.HitSound = SoundID.NPCHit7;
			base.npc.DeathSound = SoundID.NPCDeath8;
			base.npc.knockBackResist = 0f;
			Main.npcFrameCount[base.npc.type] = 1;
			base.npc.netAlways = true;
			base.npc.dontCountMe = true;
		}

		public override void SetStaticDefaults()
		{
			base.DisplayName.SetDefault("Shadow Dragon");
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			base.npc.lifeMax = (int)((float)base.npc.lifeMax * 0.7f * bossLifeScale);
		}

		public override void AI()
		{
			if (this.Timer == -1000)
			{
				this.Timer = -Main.rand.Next(800);
			}
			base.npc.TargetClosest();
			this.Timer++;
			if (!Main.npc[(int)base.npc.ai[1]].active)
			{
				base.npc.life = 0;
				base.npc.HitEffect();
				base.npc.active = false;
			}
			if (base.npc.position.X > Main.npc[(int)base.npc.ai[1]].position.X)
			{
				base.npc.spriteDirection = 1;
			}
			if (base.npc.position.X < Main.npc[(int)base.npc.ai[1]].position.X)
			{
				base.npc.spriteDirection = -1;
			}
			if (Main.rand.Next(3) == 0)
			{
				int dust = Dust.NewDust(new Vector2(base.npc.position.X, base.npc.position.Y), base.npc.width, base.npc.height, 62, 0f, 0f, 100, Color.White, 2f);
				Main.dust[dust].noGravity = true;
			}
			if (this.Timer >= 0 && Main.netMode != 2)
			{
				float num48 = 1f;
				Vector2 vector8 = new Vector2(base.npc.position.X + (float)(base.npc.width / 2), base.npc.position.Y + (float)(base.npc.height / 2));
				float rotation = (float)Math.Atan2(vector8.Y - (Main.player[base.npc.target].position.Y + (float)Main.player[base.npc.target].height * 0.5f), vector8.X - (Main.player[base.npc.target].position.X + (float)Main.player[base.npc.target].width * 0.5f));
				rotation += (float)(Main.rand.Next(-50, 50) / 100);
				Projectile.NewProjectile(vector8.X, vector8.Y, (float)(Math.Cos(rotation) * (double)num48 * -1.0), (float)(Math.Sin(rotation) * (double)num48 * -1.0), base.mod.GetProjectile("ObscureSaw").projectile.type, 45, 0f, Main.myPlayer);
				this.Timer = -300 - Main.rand.Next(300);
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			Vector2 origin = new Vector2(Main.npcTexture[base.npc.type].Width / 2, Main.npcTexture[base.npc.type].Height / Main.npcFrameCount[base.npc.type] / 2);
			Color alpha = Color.White;
			SpriteEffects effects = SpriteEffects.None;
			if (base.npc.spriteDirection == 1)
			{
				effects = SpriteEffects.FlipHorizontally;
			}
			spriteBatch.Draw(Main.npcTexture[base.npc.type], new Vector2(base.npc.position.X - Main.screenPosition.X + (float)(base.npc.width / 2) - (float)Main.npcTexture[base.npc.type].Width * base.npc.scale / 2f + origin.X * base.npc.scale, base.npc.position.Y - Main.screenPosition.Y + (float)base.npc.height - (float)Main.npcTexture[base.npc.type].Height * base.npc.scale / (float)Main.npcFrameCount[base.npc.type] + 4f + origin.Y * base.npc.scale + 56f), base.npc.frame, alpha, base.npc.rotation, origin, base.npc.scale, effects, 0f);
			base.npc.alpha = 255;
			return true;
		}
	}
}
