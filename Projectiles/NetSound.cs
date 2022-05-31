using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class NetSound : ModProjectile {

        public override void SetDefaults() {
            Projectile.hide = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        public override void AI() {
            Terraria.Audio.SoundEngine.PlaySound((int)Projectile.velocity.X,
                (int)Main.player[Projectile.owner].position.X,
                (int)Main.player[Projectile.owner].position.Y,
                (int)Projectile.velocity.Y);
            Projectile.Kill();
        }
    }
}
