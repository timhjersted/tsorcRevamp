using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Bolt3Bolt : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 12;
        }

        public override void SetDefaults() {
            projectile.width = 130;
            projectile.height = 164;
            projectile.penetrate = 8;
            projectile.friendly = true;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.light = 0.8f;
        }
        public override void AI() {
            if (projectile.ai[0] == 0) {
                projectile.velocity.X *= 0.01f;
                projectile.velocity.Y *= 0.01f;
                projectile.ai[0] = 1;
            }

            projectile.frameCounter++;
            projectile.frame = (int)Math.Floor((double)projectile.frameCounter / 4);

            if (projectile.frame >= 12) {
                projectile.frame = 10;
            }
            if (projectile.frameCounter > 53) { // (projFrames * 4.5) - 1
                projectile.alpha += 15;
            }

            if (projectile.alpha >= 255) {
                projectile.Kill();
            }
        }
    }
}

