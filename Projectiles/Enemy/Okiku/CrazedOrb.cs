using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class CrazedOrb : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 4;
            DisplayName.SetDefault("Pulsating Energy");
        }

        public override void SetDefaults() {
            projectile.width = 32;
            projectile.height = 34;
            projectile.hostile = true;
            projectile.scale = 1.5f;
            projectile.tileCollide = false;
            projectile.timeLeft = 1500;
            projectile.light = 1;
        }

        public override void AI() {
            projectile.rotation++;

            if (Main.rand.Next(2) == 0) {
                int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 62, 0, 0, 100, Color.White, 3.0f);
                Main.dust[dust].noGravity = true;
            }

            projectile.frameCounter++;
            if (projectile.frameCounter > 2) {
                projectile.frame++;
                projectile.frameCounter = 3;
            }
            if (projectile.frame >= 4) {
                projectile.frame = 0;
            }
        }
    }
}
