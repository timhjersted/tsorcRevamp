using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class EphemeralThrowingSpear : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 2;
        }
        public override void SetDefaults() {
            projectile.width = 19;
            projectile.height = 19;
            projectile.timeLeft = 120;
            projectile.friendly = true;
            projectile.height = 14;
            projectile.penetrate = 2;
            projectile.melee = true;
            projectile.scale = 0.9f;
            projectile.tileCollide = false;
            projectile.width = 14;
        }
        public override void AI() {
            projectile.ai[0] += 1f;
            if (++projectile.frameCounter >= 10) {
                projectile.frameCounter = 0;
                if (++projectile.frame >= 2) {
                    projectile.frame = 0;
                }
            }
            if (projectile.ai[0] >= 15f) { 
                projectile.ai[0] = 15f;
                projectile.velocity.Y += 0.1f;
            }
            if (projectile.velocity.Y > 16f) {
                projectile.velocity.Y = 16f;
            }
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(90f);

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
