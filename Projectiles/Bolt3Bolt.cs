using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Bolt3Bolt : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 12;
        }

        public override void SetDefaults() {
            Projectile.width = 130;
            Projectile.height = 164;
            Projectile.penetrate = 8;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.8f;
        }
        public override void AI() {
            if (Projectile.ai[0] == 0) {
                Projectile.velocity.X *= 0.001f;
                Projectile.velocity.Y *= 0.001f;
                Projectile.ai[0] = 1;
            }

            Projectile.frameCounter++;
            Projectile.frame = (int)Math.Floor((double)Projectile.frameCounter / 4);

            if (Projectile.frame >= 12) {
                Projectile.frame = 10;
            }
            if (Projectile.frameCounter > 53) { // (projFrames * 4.5) - 1
                Projectile.alpha += 15;
            }

            if (Projectile.alpha >= 255) {
                Projectile.Kill();
            }
        }
    }
}

