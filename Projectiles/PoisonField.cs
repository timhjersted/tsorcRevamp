using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class PoisonField : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 5;
        }

        public override void SetDefaults() {
            projectile.width = 26;
            projectile.height = 40;
            projectile.aiStyle = 4;
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
            if (projectile.frameCounter > 3) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 5) {
                projectile.frame = 0;
                return;
            }
        }
    }
}
