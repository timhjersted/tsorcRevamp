using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class MiracleVinesTrail : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/MiracleVines";
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
            projectile.rotation = (float)Math.Atan2((double)this.projectile.velocity.Y, (double)this.projectile.velocity.X) + 1.57f;

            if (projectile.alpha < 170 && projectile.alpha + 5 >= 170) {
                Dust.NewDust(projectile.position, projectile.width, projectile.height, 17, projectile.velocity.X * 0.025f, projectile.velocity.Y * 0.025f, 170, default, 1.2f);
            }
            projectile.alpha += 5;
            if (projectile.alpha > 210) {
                projectile.damage = 0;
            }
            if (projectile.alpha >= 255) {
                projectile.Kill();
                return;
            }


        }


    }
}
