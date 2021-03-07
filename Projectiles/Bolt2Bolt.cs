using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Bolt2Bolt : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 8;
        }

        public override void SetDefaults() {
            projectile.width = 70;
            projectile.height = 124;
            projectile.penetrate = 8;
            projectile.friendly = true;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.light = 0.8f;
        }
        public override void AI() {

            projectile.frameCounter++;
            projectile.frame = (int)Math.Floor((double)projectile.frameCounter / 4);

            if (projectile.frame >= 8) {
                projectile.frame = 6;
            }
            if (projectile.frameCounter > 35) { // (projFrames * 4.5) - 1
                projectile.alpha += 15;
            }

            if (projectile.alpha >= 255) {
                projectile.Kill();
            }
        }
    }
}

