using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    
	class Bubble : ModProjectile {

        public override void SetDefaults() {
			projectile.aiStyle = 1;
			projectile.hostile = true;
			projectile.height = 20;
			projectile.penetrate = 2;
			projectile.ranged = true;
			projectile.tileCollide = false;
			//aiType = 1;
			projectile.width = 20;
        }

        Vector2 initialVelocity;
        public override void AI()
        {
            Dust.NewDustPerfect(projectile.position, 29, null, 200, default, 3).noGravity = true;

            if (initialVelocity == Vector2.Zero)
            {
                initialVelocity = projectile.velocity;
            }

            float rotation = Main.GameUpdateCount % (MathHelper.TwoPi * 10);
            rotation /= 10;

            projectile.velocity.X = 5 * (float)Math.Sin(rotation);
            projectile.velocity.Y = 5 * (float)Math.Cos(rotation);
            projectile.velocity += initialVelocity;
        }

        public override bool PreKill(int timeLeft)
        {
            projectile.type = ProjectileID.WaterBolt;
            return true;
        }        
    }
}
