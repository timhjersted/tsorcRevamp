using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class GreatFireballBall : ModProjectile {

        public override void SetDefaults() {
			projectile.friendly = true;
			projectile.magic = true;
			projectile.tileCollide = true;
			projectile.height = 16;
			projectile.width = 16;
			projectile.penetrate = 1;
        }

        public override void AI() {
			if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f) {
				projectile.soundDelay = 10;
				Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
			}
			int thisDust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 15, 0, 0, 100, default, 2f);
			Main.dust[thisDust].noGravity = true;

			projectile.rotation += 0.25f;		
		}

		public override void Kill(int timeLeft) {

			Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
			if (projectile.owner == Main.myPlayer)
			{
				Projectile.NewProjectile(projectile.Center.X, projectile.Center.Y, 0, 0, ModContent.ProjectileType<GreatFireball>(), projectile.damage, 6f, projectile.owner);
			}

			for (int i = 0; i < 5; i++)
			{
				Vector2 vel = Main.rand.NextVector2Circular(12, 12);
				int thisDust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 15, vel.X, vel.Y, 100, default, 2f);
				Main.dust[thisDust].noGravity = true;
			}
		}
	}
}
