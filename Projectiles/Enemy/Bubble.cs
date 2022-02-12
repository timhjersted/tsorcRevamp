using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    
	class Bubble : ModProjectile {

        public override void SetDefaults() {
			projectile.hostile = true;
			projectile.height = 20;
			projectile.penetrate = 2;
			projectile.ranged = true;
			projectile.tileCollide = false;
			//aiType = 1;
			projectile.width = 20;
            projectile.timeLeft = 360;
        }

        Vector2 initialVelocity;
        public override void AI()
        {
            Dust.NewDustPerfect(projectile.Center, 29, null, 200, default, 3).noGravity = true;

            if (initialVelocity == Vector2.Zero)
            {
                initialVelocity = projectile.velocity;
            }

            float rotation = Main.GameUpdateCount % (MathHelper.TwoPi * 10);
            rotation /= 10;
            rotation -= MathHelper.Pi;

            Vector2 distortion;
            distortion.X = 0;
            distortion.Y = 5 * (float)Math.Sin(rotation);
            projectile.velocity = initialVelocity + distortion;
        }

        public override bool PreKill(int timeLeft)
        {
            projectile.type = ProjectileID.WaterBolt;
            return true;
        }        
    }
}
