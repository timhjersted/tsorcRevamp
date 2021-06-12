using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class BlackBreath : ModProjectile {
        public override void SetDefaults() {
            projectile.alpha = 150;
            projectile.aiStyle = 23;
            projectile.hostile = true;
            projectile.height = 38;
            projectile.penetrate = 2;
            projectile.ranged = true;
            projectile.tileCollide = true;
            projectile.width = 18;
        }
    }
}
