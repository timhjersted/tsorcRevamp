using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    public class GreatFireStrike : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 5;
        }

        public override void SetDefaults() {
            projectile.width = 26;
            projectile.height = 40;
            projectile.friendly = true;
            projectile.penetrate = 50;
            projectile.magic = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 360;
        }

        public override void AI() {
            projectile.frameCounter++;
            if (projectile.frameCounter > 3) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 5) {
                projectile.Kill();
                return;
            }
        }
    }
}
