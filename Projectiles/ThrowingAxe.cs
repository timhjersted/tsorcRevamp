using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class ThrowingAxe : ModProjectile {

        public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/ThrowingAxe";
        public override void SetDefaults() {
            projectile.aiStyle = 2;
            projectile.friendly = true;
            projectile.ranged = true;
            projectile.tileCollide = true;
            projectile.height = 22;
            projectile.penetrate = 1;
            projectile.width = 22;
        }
    }
}
