using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class IceSpirit4 : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 38;
            projectile.height = 46;
            projectile.scale = 0.5f;
            projectile.timeLeft = 140;
            projectile.hostile = false;
            projectile.tileCollide = false;
            projectile.damage = 60;
            projectile.friendly = true;
            projectile.penetrate = 2;
            projectile.magic = true;
        }

        public override void AI() {
            int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 27, 0, 0, 100, default, 1.0f);
            Main.dust[dust].noGravity = true;

           
            if (projectile.timeLeft == 110) {
                projectile.scale = 0.6f;
                projectile.damage = (int)(projectile.damage * 1.05f);
            }
            if (projectile.timeLeft == 90) {
                projectile.scale = 0.8f;
                projectile.damage = (int)(projectile.damage * 1.05f);
            }
            if (projectile.timeLeft == 70) {
                projectile.scale = 1f;
                projectile.damage = (int)(projectile.damage * 1.05f);
            }
            if (projectile.timeLeft == 50) {
                projectile.scale = 1.2f;
                projectile.damage = (int)(projectile.damage * 1.05f);
            }
            if (projectile.timeLeft == 40) {
                projectile.scale = 1.4f;
                projectile.damage = (int)(projectile.damage * 1.05f);
            }

            projectile.ai[0] += 1f;


            projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) + 1.57f;
            if (projectile.velocity.Y > 16f) {
                projectile.velocity.Y = 16f;
                return;
            }
        }
    }
}
