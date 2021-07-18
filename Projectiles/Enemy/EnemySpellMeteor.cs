using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemySpellMeteor : ModProjectile
	{

		public override void SetDefaults()
		{
			projectile.aiStyle = 9;
			projectile.hostile = true;
			projectile.height = 40;
			projectile.light = 0.5f;
			projectile.magic = true;
			projectile.penetrate = 1;
			projectile.timeLeft = 50;
			projectile.tileCollide = false;
			projectile.width = 40;
		}
		public override void Kill(int timeLeft)
		{
			projectile.type = 29;
		}

		#region AI
		public override void AI()
		{
			if (projectile.aiStyle == 1)
			{
				if (projectile.ai[1] == 0f)
				{
					projectile.ai[1] = 1f;
					Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
				}
				projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) + 1.57f;
				if (projectile.velocity.Y > 16f)
				{
					projectile.velocity.Y = 16f;
					return;
				}
			}
		}
		#endregion

		#region Kill
		public override bool PreKill(int timeLeft)
		{
			if (!projectile.active)
			{
				return true;
			}
			projectile.timeLeft = 0;
			{
				for (int num40 = 0; num40 < 10; num40++)
				{
					Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
					Vector2 arg_1394_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
					int arg_1394_1 = projectile.width;
					int arg_1394_2 = projectile.height;
					int arg_1394_3 = 15;
					float arg_1394_4 = 0f;
					float arg_1394_5 = 0f;
					int arg_1394_6 = 100;
					Color newColor = default(Color);
					int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 2f);
					Main.dust[num41].noGravity = true;
					Dust expr_13B1 = Main.dust[num41];
					expr_13B1.velocity *= 2f;
					Vector2 arg_1422_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
					int arg_1422_1 = projectile.width;
					int arg_1422_2 = projectile.height;
					int arg_1422_3 = 15;
					float arg_1422_4 = 0f;
					float arg_1422_5 = 0f;
					int arg_1422_6 = 100;
					newColor = default(Color);
					num41 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, newColor, 1f);
				}
			}
			projectile.active = false;
			return true;
		}
		#endregion

	}
}