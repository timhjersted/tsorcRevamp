using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    class MassiveCrystalShards : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Ice4Icicle";
        public override void SetDefaults() {
            Projectile.width = 42;
            Projectile.height = 84;
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.penetrate = 16;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 400;
        }
    }
}
