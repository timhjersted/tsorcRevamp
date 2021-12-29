using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.Projectiles.Enemy
{
	class RedKnightsSpear : ModProjectile
	{
		public override void SetDefaults()
		{
			projectile.aiStyle = 2;
			aiType = 1;
			projectile.hostile = true;
			projectile.height = 14;
			projectile.penetrate = 1;
			projectile.ranged = true;
			projectile.scale = 0.8f;
			projectile.tileCollide = true;
			projectile.width = 14;
		}

		public override void AI()
		{
			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
		}

		public override bool PreKill(int timeLeft)
		{
			projectile.type = 0;
			Main.PlaySound(0, (int)projectile.position.X, (int)projectile.position.Y, 1);
			for (int i = 0; i < 10; i++)
			{
				Dust.NewDust(projectile.position, projectile.width, projectile.height, 6, 0, 0, 0, default, 1f); //6 is a flame dust
				Dust.NewDust(projectile.position, projectile.width, projectile.height, 6, 0, 0, 0, default, 2f);
				Dust.NewDust(projectile.position, projectile.width, projectile.height, 7, 0, 0, 0, default, 1f); //7 is a wood splinter dust
			}
			return true;
		}

		#region Kill
		public void Kill()
		{
			//int num98 = -1;
			if (!projectile.active)
			{
				return;
			}
			projectile.timeLeft = 0;
			{
				Main.PlaySound(0, (int)projectile.position.X, (int)projectile.position.Y, 1);
				for (int i = 0; i < 10; i++)
				{
					Vector2 arg_92_0 = new Vector2(projectile.position.X, projectile.position.Y);
					int arg_92_1 = projectile.width;
					int arg_92_2 = projectile.height;
					int arg_92_3 = 7;
					float arg_92_4 = 0f;
					float arg_92_5 = 0f;
					int arg_92_6 = 0;
					Color newColor = default(Color);
					Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
				}
			}
			projectile.active = false;
		}
		#endregion
	}
}


