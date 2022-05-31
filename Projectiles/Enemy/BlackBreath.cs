using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class BlackBreath : ModProjectile {
        public override void SetDefaults() {
            Projectile.alpha = 150;
            Projectile.aiStyle = 23;
            Projectile.hostile = true;
            Projectile.height = 38;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.width = 18;
        }
    }
}
