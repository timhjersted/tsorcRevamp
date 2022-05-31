using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class EnergyPulse : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/ScrewAttack";
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 4;
            base.SetStaticDefaults();
        }
        public override void SetDefaults() {
            Projectile.hostile = true;
            Projectile.height = 32;
            Projectile.scale = 1.5f;
            Projectile.tileCollide = false;
            Projectile.width = 32;
            Projectile.timeLeft = 500;
            Projectile.light = 1;
        }
        public override bool PreKill(int timeLeft) {
            Projectile.type = 44; //killpretendtype
            return true;
        }
        public override void AI() {
            Projectile.rotation += 0.5f;

            if (Main.player[(int)Projectile.ai[0]].position.X < Projectile.position.X) {
                if (Projectile.velocity.X > -10) Projectile.velocity.X -= 0.1f;
            }

            if (Main.player[(int)Projectile.ai[0]].position.X > Projectile.position.X) {
                if (Projectile.velocity.X < 10) Projectile.velocity.X += 0.1f;
            }

            if (Main.player[(int)Projectile.ai[0]].position.Y < Projectile.position.Y) {
                if (Projectile.velocity.Y > -10) Projectile.velocity.Y -= 0.1f;
            }

            if (Main.player[(int)Projectile.ai[0]].position.Y > Projectile.position.Y) {
                if (Projectile.velocity.Y < 10) Projectile.velocity.Y += 0.1f;
            }

            if (Main.rand.Next(4) == 0) {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 5, 0, 0, 50, Color.White, 1.0f);
                Main.dust[dust].noGravity = false;
            }
            Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2) {
                Projectile.frame++;
                Projectile.frameCounter = 3;
            }
            if (Projectile.frame >= 4) {
                Projectile.frame = 0;
            }

        }
    }
}
