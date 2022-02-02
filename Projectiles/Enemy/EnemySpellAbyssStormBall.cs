using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemySpellAbyssStormBall : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Dark Wave");
		}
		public override void SetDefaults()
		{
			projectile.aiStyle = 23;
			projectile.hostile = true;
			projectile.height = 16;
			projectile.light = 1;
			projectile.magic = true;
			projectile.penetrate = 8;
			projectile.scale = 1.2f;
			projectile.tileCollide = true;
			projectile.width = 16;
			projectile.timeLeft = 0;
		}

		#region AI
		public override void AI()
		{
			if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f)
			{
				projectile.soundDelay = 10;
				Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 9);
			}
			Vector2 arg_2675_0 = new Vector2(projectile.position.X, projectile.position.Y);
			int arg_2675_1 = projectile.width;
			int arg_2675_2 = projectile.height;
			int arg_2675_3 = 15;
			float arg_2675_4 = 0f;
			float arg_2675_5 = 0f;
			int arg_2675_6 = 100;
			Color newColor = default(Color);
			int num47 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, arg_2675_3, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 2f);
			Dust expr_2684 = Main.dust[num47];
			expr_2684.velocity *= 0.3f;
			Main.dust[num47].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
			Main.dust[num47].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
			Main.dust[num47].noGravity = true;
			if (projectile.type == 34)
			{
				projectile.rotation += 0.3f * (float)projectile.direction;
			}
			else
			{
				if (projectile.velocity.X != 0f || projectile.velocity.Y != 0f)
				{
					projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) - 2.355f;
				}
			}
			if (projectile.velocity.Y > 16f)
			{
				projectile.velocity.Y = 16f;
				return;
			}
		}
		#endregion

		public override void Kill(int timeLeft)
		{
			Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 10);
			Projectile.NewProjectile(projectile.Center.X, projectile.Center.Y, 0, 0, ModContent.ProjectileType<EnemySpellAbyssStorm>(), projectile.damage, 8f, projectile.owner);

			Dust.NewDustDirect(projectile.Center, projectile.width, projectile.height, 15, 0, 0, 100, default, 2f).noGravity = true;
			Dust.NewDustDirect(projectile.Center, projectile.width, projectile.height, 15, 0, 0, 100, default, 2f).noGravity = true;
		}
	}
}