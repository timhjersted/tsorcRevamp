using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class ObscureSaw : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 4;
            DisplayName.SetDefault("Wave Attack");
        }
        public override void SetDefaults() {
            projectile.width = 34;
            projectile.height = 34;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 160;
            projectile.light = 1;
        }
        public override bool PreKill(int timeLeft) {
            projectile.type = 41; //killpretendtype
            return true;
        }
        public override void AI() {
            projectile.rotation++;

            if (projectile.velocity.X <= 6 && projectile.velocity.Y <= 6 && projectile.velocity.X >= -6 && projectile.velocity.Y >= -6) {
                projectile.velocity.X *= 1.02f;
                projectile.velocity.Y *= 1.02f;
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
