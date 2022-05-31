using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class GreatFireball : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 9;
        }

        public override void SetDefaults() {
            Projectile.width = 150;
            Projectile.height = 150;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 50;
            Projectile.timeLeft = 360;
        }

        public override void AI() {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3) {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 9) {
                Projectile.Kill();
                return;
            }
        }
    }
}
