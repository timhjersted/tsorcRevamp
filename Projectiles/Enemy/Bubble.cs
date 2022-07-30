using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{

    class Bubble : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.height = 20;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            //AIType = 1;
            Projectile.width = 20;
            Projectile.timeLeft = 360;
        }

        Vector2 initialVelocity;
        public override void AI()
        {
            Dust.NewDustPerfect(Projectile.Center, 29, null, 200, default, 3).noGravity = true;

            if (initialVelocity == Vector2.Zero)
            {
                initialVelocity = Projectile.velocity;
            }

            float rotation = Main.GameUpdateCount % (MathHelper.TwoPi * 10);
            rotation /= 10;
            rotation -= MathHelper.Pi;

            Vector2 distortion;
            distortion.X = 0;
            distortion.Y = 5 * (float)Math.Sin(rotation);
            Projectile.velocity = initialVelocity + distortion;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = ProjectileID.Bubble;
            return true;
        }
    }
}
