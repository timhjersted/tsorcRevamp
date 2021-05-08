using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class GreatEnergyBeam : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 17;
        }

        public override void SetDefaults() {
            projectile.height = 40;
            projectile.width = 350;
            projectile.friendly = true;
            projectile.magic = true;
            projectile.light = 1;
            projectile.tileCollide = true;
            projectile.ignoreWater = true;
            projectile.timeLeft = 360;
            projectile.penetrate = 50;
        }

        public override void AI() {
            projectile.frameCounter++;
            if (projectile.frameCounter > 2) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 17) {
                projectile.Kill();
                return;
            }
        }
    }
}
