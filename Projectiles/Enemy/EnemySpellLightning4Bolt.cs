using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellLightning4Bolt : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Bolt4Bolt";
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 16;
        }

        public override void SetDefaults() {
            Projectile.penetrate = 8;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.width = 130;
            Projectile.height = 402;
            drawOffsetX = -55;
            drawOriginOffsetY = -30;
        }
        public override void AI() {

            Projectile.frameCounter++;
            Projectile.frame = (int)Math.Floor((double)Projectile.frameCounter / 4);

            if (Projectile.frame >= 16) {
                Projectile.frame = 15;
            }
            if (Projectile.frameCounter > 71) { // (projFrames * 4.5) - 1
                Projectile.alpha += 15;
            }

            if (Projectile.alpha >= 255) {
                Projectile.Kill();
            }
        }
    }
}

