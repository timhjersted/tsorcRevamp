using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class CrazyOrb : ModProjectile {

		public override void SetStaticDefaults() {
			Main.projFrames[projectile.type] = 4;
			DisplayName.SetDefault("Pulsating Energy");
		}

        public override void SetDefaults() {
			projectile.width = 32;
			projectile.height = 34;
			projectile.hostile = true;
			projectile.tileCollide = false;
			projectile.light = 1;
			projectile.timeLeft = 600;
        }

        public override void AI() {
			projectile.rotation += 0.5f;

			if (Main.player[(int)projectile.ai[0]].position.X < projectile.position.X) {
				if (projectile.velocity.X > -6) projectile.velocity.X -= 0.05f;
			}

			if (Main.player[(int)projectile.ai[0]].position.X > projectile.position.X) {
				if (projectile.velocity.X < 6) projectile.velocity.X += 0.05f;
			}

			if (Main.player[(int)projectile.ai[0]].position.Y < projectile.position.Y) {
				if (projectile.velocity.Y > -6) projectile.velocity.Y -= 0.05f;
			}

			if (Main.player[(int)projectile.ai[0]].position.Y > projectile.position.Y) {
				if (projectile.velocity.Y < 6) projectile.velocity.Y += 0.05f;
			}




			projectile.frameCounter++;
			if (projectile.frameCounter > 2) {
				projectile.frame++;
				projectile.frameCounter = 3;
			}
			if (projectile.frame >= 4) {
				projectile.frame = 0;
			}

		}

	}
}
