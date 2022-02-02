using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellGravity4Ball : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 24;
            projectile.height = 38;
            projectile.hostile = true;
            projectile.penetrate = 1;
            projectile.tileCollide = true;
            projectile.ignoreWater = true;
        }

        public override void AI() {
            if (projectile.ai[1] == 0f) {
                projectile.ai[1] = 1f;
                //Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
            }
            projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) + 1.57f;
            if (projectile.velocity.Y > 16f) {
                projectile.velocity.Y = 16f;
                return;
            }
        }
        public override void Kill(int timeLeft) {
            if (!projectile.active) {
                return;
            }
            projectile.timeLeft = 0;
            {
                //Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
                if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width / 2), projectile.position.Y + (float)(projectile.height - 16)), new Vector2(0, 0), ModContent.ProjectileType<EnemySpellGravity4Strike>(), 1, 3f, projectile.owner);
                Vector2 projectilePos = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
                int num41 = Dust.NewDust(projectilePos, projectile.width, projectile.height, 15, 0f, 0f, 100, default, 2f);
                Main.dust[num41].noGravity = true;
                Main.dust[num41].velocity *= 2f;
                Dust.NewDust(projectilePos, projectile.width, projectile.height, 15, 0f, 0f, 100, default, 1f);
            }
            projectile.active = false;
        }
    }
}
