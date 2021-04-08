using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Bolt1Bolt : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults() {
            projectile.width = 60;
            projectile.height = 110;
            projectile.penetrate = 4;
            projectile.friendly = true;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.light = 0.8f;

        }
        public override void AI() {
            //keep a portion of the projectile's velocity when spawned, so we canmake sure it has the right knockback
            if (projectile.ai[0] == 0) {
                projectile.velocity.X *= 0.01f;
                projectile.velocity.Y *= 0.01f;
                projectile.ai[0] = 1;
            }
            projectile.frameCounter++;
            projectile.frame = (int)Math.Floor((double)projectile.frameCounter / 4);

            if (projectile.frame >= 4) {
                projectile.frame = 2;
            }
            if (projectile.frameCounter > 17) { // (projFrames * 4.5) - 1
                projectile.alpha += 15;
            }

            if (projectile.alpha >= 255) {
                projectile.Kill();
            }

        }
    }
}
