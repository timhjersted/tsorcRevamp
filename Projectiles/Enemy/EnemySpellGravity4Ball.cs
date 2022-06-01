using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellGravity4Ball : ModProjectile {
        public override void SetDefaults() {
            Projectile.width = 24;
            Projectile.height = 38;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            if (Projectile.ai[1] == 0f) {
                Projectile.ai[1] = 1f;
                //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
            }
            Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) + 1.57f;
            if (Projectile.velocity.Y > 16f) {
                Projectile.velocity.Y = 16f;
                return;
            }
        }
        public override void Kill(int timeLeft) {
            if (!Projectile.active) {
                return;
            }
            Projectile.timeLeft = 0;
            {
                //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
                if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_FromThis(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height - 16)), new Vector2(0, 0), ModContent.ProjectileType<EnemySpellGravity4Strike>(), 1, 3f, Projectile.owner);
                Vector2 projectilePos = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
                int num41 = Dust.NewDust(projectilePos, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 2f);
                Main.dust[num41].noGravity = true;
                Main.dust[num41].velocity *= 2f;
                Dust.NewDust(projectilePos, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 1f);
            }
            Projectile.active = false;
        }
    }
}
