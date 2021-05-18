using Terraria;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellGreatEnergyStrike : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 12;
        }
        public override void SetDefaults() {
            projectile.width = 44;
            projectile.height = 40;
            projectile.aiStyle = -1;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.tileCollide = false;
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
