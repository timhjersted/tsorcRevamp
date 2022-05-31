using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class FireTrail : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/FireBall";
        public override void SetDefaults() {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.MaxUpdates = 2;
            Projectile.penetrate = 3;
            Projectile.hostile = true;
        }

        public override void AI() {
            Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) + 1.57f;

            if (Projectile.alpha < 170 && Projectile.alpha + 5 >= 170) {
                for (int j = 0; j < 3; j++) {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, Projectile.velocity.X * 0.025f, Projectile.velocity.Y * 0.025f, 170, default, 1.2f);
                }
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 14, 0f, 0f, 170, default, 1.1f);
            }
            Projectile.alpha += 5;
            if (Projectile.alpha > 210) { //this isnt necessary, but ive found that getting hit by a fireball you thought disappeared is really annoying
                Projectile.damage = 0;
            }
            if (Projectile.alpha >= 255) {
                Projectile.Kill();
                return;
            }


        }


    }
}
