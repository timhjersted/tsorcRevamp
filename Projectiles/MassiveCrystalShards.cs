using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class MassiveCrystalShards : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 42;
            projectile.height = 84;
            projectile.aiStyle = 4;
            projectile.hostile = true;
            projectile.penetrate = 16;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 400;
        }
    }
}
