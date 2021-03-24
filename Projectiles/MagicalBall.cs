using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;

namespace tsorcRevamp.Projectiles {
    class MagicalBall : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 50;
            projectile.height = 50;
            projectile.aiStyle = 1;
            projectile.timeLeft = 3600;
            projectile.friendly = true;
            projectile.penetrate = 5;
            projectile.tileCollide = true;
            projectile.magic = true;
            projectile.alpha = 255;
        }

        public override void AI() {
            int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y + 4), projectile.width, projectile.height, 27, 0, 0, 100, default, 3.0f);
            Main.dust[dust].noGravity = true;

            projectile.velocity.Y = 0;
        }
    }
}
