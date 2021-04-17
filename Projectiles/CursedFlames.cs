using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class CursedFlames : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 20;
            projectile.height = 20;
            projectile.scale = 1.3f;
            projectile.alpha = 255;
            projectile.timeLeft = 45;
            projectile.friendly = true;
            projectile.light = 0.8f;
            projectile.penetrate = 3;
            projectile.tileCollide = true;
            projectile.magic = true;
        }

        public override void AI() {
            for (int i = 0; i < 2; i++) {
                int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y),
                                         projectile.width,
                                         projectile.height,
                                         75,
                                         projectile.velocity.X * 0.2f,
                                         projectile.velocity.Y * 0.2f,
                                         100,
                                         default,
                                         3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity.X *= 0.3f;
                Main.dust[dust].velocity.Y *= 0.3f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {

            for (int i = 0; i < 6; i++) {
                Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y),
                                             projectile.width,
                                             projectile.height,
                                             75,
                                             -projectile.velocity.X * 0.3f,
                                             -projectile.velocity.Y * 0.3f,
                                             100,
                                             default,
                                             0.8f);
            }
            projectile.Kill();
            return true;
        }
    }
}
