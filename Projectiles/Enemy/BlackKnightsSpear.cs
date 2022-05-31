using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.Projectiles.Enemy
{
	class BlackKnightsSpear : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.aiStyle = 2;
			aiType = 1;
			Projectile.hostile = true;
			Projectile.height = 14;
			Projectile.penetrate = 1;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.scale = 0.8f;
			Projectile.tileCollide = true;
			Projectile.width = 14;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
		}

		public override bool PreKill(int timeLeft)
		{
			Projectile.type = 0;
			Terraria.Audio.SoundEngine.PlaySound(0, (int)Projectile.position.X, (int)Projectile.position.Y, 1);
			for (int i = 0; i < 10; i++)
			{
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 7, 0, 0, 0, default, 1f);
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 7, 0, 0, 0, default, 0.5f);
			}
			return true;
		}

		#region Kill
		public void Kill()
		{
			//int num98 = -1;
			if (!Projectile.active)
			{
				return;
			}
			Projectile.timeLeft = 0;
			{
				Terraria.Audio.SoundEngine.PlaySound(0, (int)Projectile.position.X, (int)Projectile.position.Y, 1);
				for (int i = 0; i < 10; i++)
				{
					Vector2 arg_92_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
					int arg_92_1 = Projectile.width;
					int arg_92_2 = Projectile.height;
					int arg_92_3 = 7;
					float arg_92_4 = 0f;
					float arg_92_5 = 0f;
					int arg_92_6 = 0;
					Color newColor = default(Color);
					Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
				}
			}
			Projectile.active = false;
		}
		#endregion
	}
}


