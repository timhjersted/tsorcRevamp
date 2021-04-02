using Terraria;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles {
    class SuddenDeathStrike : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 12;
        }

        public override void SetDefaults() {
            projectile.width = 44;
            projectile.height = 40;
            projectile.friendly = true;
            projectile.penetrate = 50;
            projectile.magic = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.light = 1f;
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
