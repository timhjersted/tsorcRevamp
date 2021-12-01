using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class StarfallProjectile : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 22;
            projectile.height = 24;
            projectile.friendly = true;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.light = 0.8f;
        }

        public override void AI() {
            projectile.rotation += 0.1f;

            if (projectile.position.Y > Main.player[projectile.owner].position.Y - 50f) projectile.tileCollide = true;

            UsefulFunctions.HomeOnEnemy(projectile, 240, 15f, false, 1.25f, true);

            
            int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 92, projectile.velocity.X, projectile.velocity.Y, 128, default, 1.2f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.3f;
            
        }

        public override void Kill(int timeLeft) {
            Main.PlaySound(SoundID.Item10, projectile.Center);
            for (int i = 0; i < 4; i++) {
                int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width * 2, projectile.height * 2, 92, 0, 0, 50, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
