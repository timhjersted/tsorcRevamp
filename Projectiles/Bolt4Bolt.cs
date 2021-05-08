using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Bolt4Bolt : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 16;
        }

        public override void SetDefaults() {
            projectile.width = 254;
            projectile.height = 472;
            projectile.penetrate = 8;
            projectile.friendly = true;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.light = 0.8f;
        }
        public override void AI() {
            if (projectile.ai[0] == 0) {
                projectile.velocity.X *= 0.001f;
                projectile.velocity.Y *= 0.001f;
                projectile.ai[0] = 1;
            }

            projectile.frameCounter++;
            projectile.frame = (int)Math.Floor((double)projectile.frameCounter / 4);

            if (projectile.frame >= 16) {
                projectile.frame = 15;
            }
            if (projectile.frameCounter > 71) { // (projFrames * 4.5) - 1
                projectile.alpha += 15;
            }

            if (projectile.alpha >= 255) {
                projectile.Kill();
            }
        }
    }
}

