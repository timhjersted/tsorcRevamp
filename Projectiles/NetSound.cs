using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class NetSound : ModProjectile {

        public override void SetDefaults() {
            projectile.hide = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }
        public override void AI() {
            Main.PlaySound((int)projectile.velocity.X,
                (int)Main.player[projectile.owner].position.X,
                (int)Main.player[projectile.owner].position.Y,
                (int)projectile.velocity.Y);
            projectile.Kill();
        }
    }
}
