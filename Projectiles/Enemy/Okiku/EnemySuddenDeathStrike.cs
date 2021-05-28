using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    class EnemySuddenDeathStrike : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 44;
            projectile.height = 40;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.light = 1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            Main.projFrames[projectile.type] = 12;
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Sudden Death Strike");
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
