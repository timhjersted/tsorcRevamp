using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class CursedFlames : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 20;
            projectile.height = 20;
            projectile.scale = 1.3f;
            projectile.alpha = 255;
            projectile.aiStyle = 8;
            projectile.timeLeft = 3600;
            projectile.friendly = true;
            projectile.light = 0.8f;
            projectile.penetrate = 3;
            projectile.tileCollide = true;
            projectile.magic = true;
        }
    }
}
