using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class GreatFireballBall : ModProjectile {

        public override void SetDefaults() {
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.tileCollide = true;
			Projectile.height = 16;
			Projectile.width = 16;
			Projectile.penetrate = 1;
        }

        public override void AI() {
			if (Projectile.soundDelay == 0 && Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f) {
				Projectile.soundDelay = 10;
				Main.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 9);
			}
			int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f);
			Main.dust[thisDust].noGravity = true;

			Projectile.rotation += 0.25f;		
		}

		public override void Kill(int timeLeft) {

			Main.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 10);
			if (Projectile.owner == Main.myPlayer)
			{
				Projectile.NewProjectile(Projectile.Center.X, Projectile.Center.Y, 0, 0, ModContent.ProjectileType<GreatFireball>(), Projectile.damage, 6f, Projectile.owner);
			}

			for (int i = 0; i < 5; i++)
			{
				Vector2 vel = Main.rand.NextVector2Circular(12, 12);
				int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, vel.X, vel.Y, 100, default, 2f);
				Main.dust[thisDust].noGravity = true;
			}
		}
	}
}
