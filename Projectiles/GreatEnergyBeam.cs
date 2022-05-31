using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class GreatEnergyBeam : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 17;
        }

        public override void SetDefaults() {
            Projectile.height = 40;
            Projectile.width = 350;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 360;
            Projectile.penetrate = 50;
        }

        public override void AI() {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2) {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 17) {
                Projectile.Kill();
                return;
            }
        }
    }
}
