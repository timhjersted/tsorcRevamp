using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Projectiles {
    class HeavenSword : ModProjectile {

        public override void SetDefaults() { 
            projectile.width = 28;
            projectile.height = 28;
            projectile.aiStyle = 3;
            projectile.timeLeft = 3600;
            projectile.penetrate = 3;
            projectile.friendly = true;
            projectile.tileCollide = false;
            projectile.ranged = true;
        }

        public override void AI() {
            if (Main.rand.Next(20) == 0) {
                Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 57, projectile.velocity.X * 0.1f, projectile.velocity.Y * 0.1f, 200, default, 1f);
            }
        }
    }
}
