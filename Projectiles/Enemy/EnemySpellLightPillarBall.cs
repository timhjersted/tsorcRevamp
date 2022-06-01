using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellLightPillarBall : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
        public override void SetDefaults() {
            Projectile.aiStyle = 23;
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.penetrate = 8;
            Projectile.tileCollide = true;
            Projectile.width = 16;
            Projectile.timeLeft = 0;
        }

        public override void AI() {
            if (Projectile.aiStyle == 1) {
                if (Projectile.ai[1] == 0f) {
                    Projectile.ai[1] = 1f;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 9);
                }
                Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) + 1.57f;
                if (Projectile.velocity.Y > 16f) {
                    Projectile.velocity.Y = 16f;
                    return;
                }
            }
        }

        public override void Kill(int timeLeft) {
            if (Main.rand.NextBool(1)) {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 10);
                if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_FromThis(), new Vector2(Projectile.position.X + (float)(Projectile.width * -9), Projectile.position.Y + (float)(Projectile.height - 0.5f)), new Vector2(0, 0), ModContent.ProjectileType<EnemySpellLightPillar>(), Projectile.damage, 8f, Projectile.owner);
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
