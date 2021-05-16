using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class EnergyPulse : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 4;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            projectile.hostile = true;
            projectile.height = 32;
            projectile.scale = 1.5f;
            projectile.tileCollide = false;
            projectile.width = 32;
            projectile.timeLeft = 500;
            projectile.light = 1;
        }
        public override bool PreKill(int timeLeft) {
            projectile.type = 44; //killpretendtype
            return true;
        }
        public override void AI() {
            projectile.rotation += 0.5f;

            if (Main.player[(int)projectile.ai[0]].position.X < projectile.position.X) {
                if (projectile.velocity.X > -10) projectile.velocity.X -= 0.1f;
            }

            if (Main.player[(int)projectile.ai[0]].position.X > projectile.position.X) {
                if (projectile.velocity.X < 10) projectile.velocity.X += 0.1f;
            }

            if (Main.player[(int)projectile.ai[0]].position.Y < projectile.position.Y) {
                if (projectile.velocity.Y > -10) projectile.velocity.Y -= 0.1f;
            }

            if (Main.player[(int)projectile.ai[0]].position.Y > projectile.position.Y) {
                if (projectile.velocity.Y < 10) projectile.velocity.Y += 0.1f;
            }

            if (Main.rand.Next(4) == 0) {
                int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 5, 0, 0, 50, Color.White, 1.0f);
                Main.dust[dust].noGravity = false;
            }
            Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);

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
