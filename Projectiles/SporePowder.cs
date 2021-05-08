using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    class SporePowder : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 50;
            projectile.height = 50;
            projectile.scale = 0.8f;
            projectile.penetrate = -1;
            projectile.light = 1;
            projectile.timeLeft = 180;
            projectile.friendly = true;
            projectile.tileCollide = false;
            projectile.thrown = true;
        }

        public override void AI() {
            projectile.velocity *= 0.95f;
            projectile.ai[0] += 1f;
            if (projectile.ai[0] == 180f) {
                projectile.Kill();
            }
            if (projectile.ai[1] == 0f) {
                projectile.ai[1] = 1f;
                for (int k = 0; k < 30; k++) {
                    Dust.NewDust(projectile.position, projectile.width, projectile.height, 44, projectile.velocity.X, projectile.velocity.Y, 50, default(Color), 1f);
                }
            }
        }

    }
}
