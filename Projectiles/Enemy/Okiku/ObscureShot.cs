using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class ObscureShot : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = 0;
            projectile.hostile = true;
            projectile.height = 16;
            projectile.scale = 2;
            projectile.tileCollide = false;
            projectile.width = 16;
            projectile.timeLeft = 1500;
        }

        public override bool PreKill(int timeLeft) {
            projectile.type = 44; //killpretendtype
            return true;
        }

        public override void AI() {
            projectile.rotation++;

            if (Main.rand.Next(2) == 0) {
                int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 62, 0, 0, 100, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;
            }

            if (projectile.velocity.X <= 6 && projectile.velocity.Y <= 6 && projectile.velocity.X >= -6 && projectile.velocity.Y >= -6) {
                projectile.velocity.X *= 1.02f;
                projectile.velocity.Y *= 1.02f;
            }
        }
    }
}
