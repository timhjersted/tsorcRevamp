using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{

	public class GreatSoulArrow : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.Homing[projectile.type] = true;
			Main.projFrames[projectile.type] = 6;
		}

		public override void SetDefaults()
		{
			projectile.width = 10;
			projectile.height = 10;
			projectile.scale = 1.1f;
			projectile.alpha = 165;
			projectile.friendly = true;
			projectile.tileCollide = true;
			projectile.ignoreWater = true;
			projectile.magic = true;
			projectile.timeLeft = 300;
		}
		int gsoularrowanimtimer = 0;
		public override void AI()
		{
			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Lighting.AddLight(projectile.Center, .250f, .250f, .650f);

			//ANIMATION

			gsoularrowanimtimer++;

			if (gsoularrowanimtimer > 24)
			{
				gsoularrowanimtimer = 0;
			}

			if (++projectile.frameCounter >= 4) //ticks spent on each frame
			{
				projectile.frameCounter = 0;

				if (++projectile.frame == 6)
				{
					projectile.frame = 0;
				}
			}

			int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 68, projectile.velocity.X * -.05f, projectile.velocity.Y * -.05f, 30, default(Color), 1f);
			Main.dust[dust].noGravity = true;


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
					projectile.velocity = (15 * projectile.velocity + move) / 11f; //homing power, higher is less homing
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
			if (magnitude > 5.5f)
			{
				vector *= 5.5f / magnitude; //speed once homing towards an enemy
			}
		}
		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Texture2D texture = Main.projectileTexture[projectile.type];

			spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, projectile.frame * 28, 14, 28), Color.White, projectile.rotation, new Vector2(8, 6), projectile.scale, SpriteEffects.None, 0);

			return false;
		}

		public override void Kill(int timeLeft)
		{
			for (int d = 0; d < 20; d++)
			{
				int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 68, projectile.velocity.X * 1f, projectile.velocity.Y * 1f, 30, default(Color), 1f);
				Main.dust[dust].noGravity = true;
			}
			Main.PlaySound(SoundID.NPCHit3.WithVolume(.6f), projectile.position);
		}

		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			if (projectile.owner == Main.myPlayer)
			{
				if (Main.rand.Next(2) == 0)
				{
					target.AddBuff(mod.BuffType("Soulstruck"), 180);
				}
			}
		}
	}
}