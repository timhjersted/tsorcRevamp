using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Shockwave : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/Sand";
        public override void SetDefaults() {
            projectile.width = 150;
            projectile.height = 150;
            projectile.timeLeft = 300;
            projectile.hostile = false;
            projectile.tileCollide = false;
            projectile.damage = 60;
            projectile.friendly = true;
            projectile.penetrate = 3;
            projectile.alpha = 255;
            projectile.usesIDStaticNPCImmunity = true;
            projectile.idStaticNPCHitCooldown = 20; // a shockwave projectile can't damage an enemy more frequently than once every 20 ticks to prevent "shotgunning" by landing directly on an enemy
        }

        public override void AI() {
            int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y - 10), projectile.width, projectile.height, 31, 0, 0, 100, default, 1.0f);
            Main.dust[dust].noGravity = true;
            int dust2 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y - 20), projectile.width, projectile.height, 31, 0, 0, 100, default, 1.0f);
            Main.dust[dust2].noGravity = true;
            int dust3 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y - 30), projectile.width, projectile.height, 31, 0, 0, 100, default, 1.0f);
            Main.dust[dust3].noGravity = true;

            projectile.damage = (int)(projectile.damage * 0.925f); //scale down the damage as it gets further from the impact location
            if (projectile.damage <= 2) {
                projectile.Kill();
            }
        }
    }
}
