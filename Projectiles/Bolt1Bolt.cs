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
            projectile.frameCounter++;
            if (projectile.frameCounter > 2) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 4) {
                projectile.frame = 2;
                return;
            }
            projectile.alpha += 12;
            if (projectile.alpha >= 255) {
                projectile.Kill();
                return;
            }
        }
    }
}
