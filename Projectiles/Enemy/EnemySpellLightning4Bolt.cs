using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellLightning4Bolt : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Bolt4Bolt";
        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 16;
        }

        public override void SetDefaults() {
            projectile.width = 254;
            projectile.height = 472;
            projectile.penetrate = 8;
            projectile.hostile = true;
            projectile.tileCollide = false;
        }
        public override void AI() {

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

