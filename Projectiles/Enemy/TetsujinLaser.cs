using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	public class TetsujinLaser : ModProjectile
	{

		public override void SetDefaults()
		{
			projectile.aiStyle = 0;
			projectile.hostile = true;
			projectile.height = 14;
			projectile.penetrate = 1;
			projectile.scale = 1.5f;
			projectile.tileCollide = true;
			projectile.width = 2;
		}
		public override void AI()
		{
			projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) + 1.57f;
			//Color color = new Color();
			//int dust = Dust.NewDust(new Vector2((float) projectile.position.X, (float) projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 20, color, 1.0f);
			//Main.dust[dust].noGravity = true;
			float red = 1.0f;
			float green = 0.0f;
			float blue = 1.0f;

			Lighting.AddLight((int)((projectile.position.X + (float)(projectile.width / 2)) / 16f), (int)((projectile.position.Y + (float)(projectile.height / 2)) / 16f), red, green, blue);
		}

        public override bool PreKill(int timeLeft)
        {
			projectile.type = 15;
            return base.PreKill(timeLeft);
        }
    }
}