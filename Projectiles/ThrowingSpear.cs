using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class ThrowingSpear : ModProjectile {
        public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/ThrowingSpear";
        public override void SetDefaults() {
            projectile.friendly = true;
            projectile.height = 14;
            projectile.width = 14;
            projectile.scale = 0.8f;
            projectile.penetrate = 1;
            projectile.ranged = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 720;
        }

        public override void AI() {
            projectile.ai[0] += 1f;
            if (projectile.ai[0] >= 15f) {
                projectile.ai[0] = 15f;
                projectile.velocity.Y += 0.1f;
            }
            if (projectile.velocity.Y > 16f) {
                projectile.velocity.Y = 16f;
            }
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(90f); //simplified rotation code (no trig!)
        }
        public override void Kill(int timeLeft) {
            Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 1);
            for (int i = 0; i < 10; i++) {
                Vector2 projPosition = new Vector2(projectile.position.X, projectile.position.Y);
                Dust.NewDust(projPosition, projectile.width, projectile.height, 7, 0f, 0f, 0, default, 1f);
            }
        }
    }
}
