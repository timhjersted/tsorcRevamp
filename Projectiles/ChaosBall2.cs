using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class ChaosBall2 : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 10;
            projectile.height = 10;
            projectile.alpha = 200;
            projectile.timeLeft = 300;
            projectile.penetrate = 10;
            projectile.friendly = true;
            projectile.tileCollide = false;
            projectile.magic = true;
        }
        public override void AI() {
            int num40 = Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 27, projectile.velocity.X, projectile.velocity.Y, 100, default, 3f);
            Main.dust[num40].noGravity = true;
            if (Main.rand.Next(10) == 0) {
                Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 27, projectile.velocity.X, projectile.velocity.Y, 100, default, 1.4f);
            }
            projectile.rotation += 0.3f * (float)projectile.direction;
            if (projectile.velocity.Y > 16f) {
                projectile.velocity.Y = 16f;
                return;
            }
        }
    }
}