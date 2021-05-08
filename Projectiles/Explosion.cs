using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Explosion : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 12;
        }

        public override void SetDefaults() {
            projectile.width = 26;
            projectile.height = 26;
            projectile.aiStyle = -1;
            projectile.friendly = true;
            projectile.penetrate = 50;
            projectile.magic = true;
            projectile.light = 1;
            projectile.tileCollide = true;
            projectile.ignoreWater = true;
        }

        public override void AI() {
            projectile.frameCounter++;
            if (projectile.frameCounter > 3) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 12) {
                projectile.Kill();
                return;
            }
        }
    }
}
