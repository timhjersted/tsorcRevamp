using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    class MassiveCrystalShards : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Ice4Icicle";
        public override void SetDefaults() {
            projectile.width = 42;
            projectile.height = 84;
            projectile.aiStyle = 0;
            projectile.hostile = true;
            projectile.penetrate = 16;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 400;
        }
    }
}
