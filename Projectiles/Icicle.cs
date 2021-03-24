using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Icicle : ModProjectile {
        public override void SetDefaults() {
            projectile.aiStyle = 9;
            projectile.friendly = true;
            projectile.penetrate = 3;
            projectile.height = 38;
            projectile.width = 38;
            projectile.magic = true;
            projectile.tileCollide = false;
        }
    }
}
