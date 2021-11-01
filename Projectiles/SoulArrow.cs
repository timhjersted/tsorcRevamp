using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{

	public class SoulArrow : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.Homing[projectile.type] = true;
			Main.projFrames[projectile.type] = 6;
		}

		public override void SetDefaults()
		{
			projectile.width = 8;
			projectile.height = 8;
			projectile.alpha = 180;
			projectile.friendly = true;
			projectile.tileCollide = true;
			projectile.ignoreWater = true;
			projectile.magic = true;
			projectile.penetrate = -1;
			projectile.timeLeft = 300;
		}
		int soularrowanimtimer = 0;
		public override void AI()
		{
			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Lighting.AddLight(projectile.Center, .300f, .300f, .450f);

			//ANIMATION

			soularrowanimtimer++;

			if (soularrowanimtimer > 24)
			{
				soularrowanimtimer = 0;
			}

			if (++projectile.frameCounter >= 4) //ticks spent on each frame
			{
				projectile.frameCounter = 0;

				if (++projectile.frame == 6)
				{
					projectile.frame = 0;
				}
			}

			//int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 68, 0f, 0f, 30, default(Color), 1f);
			//Main.dust[dust].noGravity = true;

			if (projectile.velocity.X > 0) //if going right
			{
				for (int d = 0; d < 6; d++)
				{
					int num44 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y - 2), projectile.width, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
					Main.dust[num44].noGravity = true;
					Main.dust[num44].velocity *= 0f;
				}

				for (int d = 0; d < 6; d++)
				{
					int num45 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y - 2), projectile.width - 4, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default(Color), .5f);
					Main.dust[num45].noGravity = true;
					Main.dust[num45].velocity *= 0f;
					Main.dust[num45].fadeIn *= 1f;
				}
			}
			else //if going left
            {
				for (int d = 0; d < 6; d++)
				{
					int num44 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
					Main.dust[num44].noGravity = true;
					Main.dust[num44].velocity *= 0f;
				}

				for (int d = 0; d < 6; d++)
				{
					int num45 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width - 4, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default(Color), .5f);
					Main.dust[num45].noGravity = true;
					Main.dust[num45].velocity *= 0f;
					Main.dust[num45].fadeIn *= 1f;
				}
			}

			if (projectile.alpha > 70)
			{
				projectile.alpha -= 3; //so that it doesnt start homing too early
				if (projectile.alpha < 70)
				{
					projectile.alpha = 70;
				}
			}

			if (projectile.alpha <= 70)
			{
				if (projectile.localAI[0] == 0f)
				{
					AdjustMagnitude(ref projectile.velocity);
					projectile.localAI[0] = 1f;
				}
				Vector2 move = Vector2.Zero;
				float distance = 120f; // distance from enemy to begin homing
				bool target = false;
				for (int k = 0; k < 200; k++)
				{
					if (Main.npc[k].active && !Main.npc[k].dontTakeDamage && !Main.npc[k].friendly && Main.npc[k].lifeMax > 5)
					{
						Vector2 newMove = Main.npc[k].Center - projectile.Center;
						float distanceTo = (float)Math.Sqrt(newMove.X * newMove.X + newMove.Y * newMove.Y);
						if (distanceTo < distance)
						{
							move = newMove;
							distance = distanceTo;
							target = true;
						}
					}
				}
				if (target)
				{
					AdjustMagnitude(ref move);
					projectile.velocity = (20 * projectile.velocity + move) / 11f; //homing power, higher is less homing
					AdjustMagnitude(ref projectile.velocity);
				}
				if (projectile.alpha <= 70)
				{
					//int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, ModContent.DustType<SpectreDust>());
					//Main.dust[dust].velocity /= 2f;
				}
			}
		}

		private void AdjustMagnitude(ref Vector2 vector)
		{
			float magnitude = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
			if (magnitude > 5f)
			{
				vector *= 5f / magnitude; //speed once homing towards an enemy
			}
		}
		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Texture2D texture = Main.projectileTexture[projectile.type];

			spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, projectile.frame * 26, 14, 26), Color.White, projectile.rotation, new Vector2(8, 6), projectile.scale, SpriteEffects.None, 0);

			return false;
		}
		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			// change the hitbox size, centered about the original projectile center. This makes the projectile have small aoe.
			projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
			projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
			projectile.width = 40;
			projectile.height = 40;
			projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
			projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);

			projectile.timeLeft = 2;
		}
		public override void Kill(int timeLeft)
		{
			for (int d = 0; d < 20; d++)
			{
				int dust = Dust.NewDust(projectile.Center, 8, 8, 68, projectile.velocity.X * 1f, projectile.velocity.Y * 1f, 30, default(Color), 1.5f);
				Main.dust[dust].noGravity = true;
			}
			Main.PlaySound(SoundID.NPCHit3.WithVolume(.45f), projectile.position);
		}
	}
}