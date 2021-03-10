using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class MiracleSprouter : ModProjectile {

        public override void SetStaticDefaults() {
            projectile.height = 15;
            projectile.width = 15;
            projectile.scale = 1.2f;
            projectile.tileCollide = true;
            projectile.ignoreWater = false;
            projectile.hostile = true;
        }

        public override void AI() {
            projectile.rotation = (float)Math.Atan2((double)projectile.velocity.X, (double)projectile.velocity.Y);
            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 75, 0, 0, 100, default, 2.0f);
            Main.dust[dust].noGravity = true;
        }

        public override void Kill(int timeLeft) {
            Projectile.NewProjectile(projectile.position.X, projectile.position.Y, 0, -3f, ModContent.ProjectileType<MiracleVines>(), 40, 0f, Main.myPlayer);
            projectile.active = false;
        }
    }
}
