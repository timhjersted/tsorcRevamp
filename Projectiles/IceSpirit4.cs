using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class IceSpirit4 : ModProjectile {

        public override void SetDefaults() {
            Projectile.width = 38;
            Projectile.height = 46;
            Projectile.scale = 0.5f;
            Projectile.timeLeft = 140;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.damage = 60;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI() {
            int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 27, 0, 0, 100, default, 1.0f);
            Main.dust[dust].noGravity = true;

           
            if (Projectile.timeLeft == 110) {
                Projectile.scale = 0.6f;
                Projectile.damage = (int)(Projectile.damage * 1.1f);
            }
            if (Projectile.timeLeft == 90) {
                Projectile.scale = 0.8f;
                Projectile.damage = (int)(Projectile.damage * 1.1f);
            }
            if (Projectile.timeLeft == 70) {
                Projectile.scale = 1f;
                Projectile.damage = (int)(Projectile.damage * 1.1f);
            }
            if (Projectile.timeLeft == 50) {
                Projectile.scale = 1.2f;
                Projectile.damage = (int)(Projectile.damage * 1.1f);
            }
            if (Projectile.timeLeft == 40) {
                Projectile.scale = 1.4f;
                Projectile.damage = (int)(Projectile.damage * 1.1f);
            }

            Projectile.ai[0] += 1f;


            Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) + 1.57f;
            if (Projectile.velocity.Y > 16f) {
                Projectile.velocity.Y = 16f;
                return;
            }
        }
    }
}
