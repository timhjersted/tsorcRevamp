using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class FireTrail : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 15;
            projectile.height = 15;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.MaxUpdates = 2;
            projectile.penetrate = 3;
            projectile.hostile = true;
        }

        public override void AI() {
            projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) + 1.57f;

            if (projectile.alpha < 170 && projectile.alpha + 5 >= 170) {
                for (int j = 0; j < 3; j++) {
                    Dust.NewDust(projectile.position, projectile.width, projectile.height, 6, projectile.velocity.X * 0.025f, projectile.velocity.Y * 0.025f, 170, default, 1.2f);
                }
                Dust.NewDust(projectile.position, projectile.width, projectile.height, 14, 0f, 0f, 170, default, 1.1f);
            }
            projectile.alpha += 5;
            if (projectile.alpha > 210) { //this isnt necessary, but ive found that getting hit by a fireball you thought disappeared is really annoying
                projectile.damage = 0;
            }
            if (projectile.alpha >= 255) {
                projectile.Kill();
                return;
            }


        }


    }
}
