using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class GreatFireball : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 9;
        }

        public override void SetDefaults() {
            projectile.width = 150;
            projectile.height = 150;
            projectile.friendly = true;
            projectile.scale = 2;
            projectile.magic = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = 50;
            projectile.timeLeft = 360;
        }

        public override void AI() {
            projectile.frameCounter++;
            if (projectile.frameCounter > 3) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 9) {
                projectile.Kill();
                return;
            }
        }
    }
}
