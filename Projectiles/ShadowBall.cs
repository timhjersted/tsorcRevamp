using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class ShadowBall : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = 0;
            projectile.height = 15;
            projectile.penetrate = 4;
            projectile.tileCollide = true;
            projectile.timeLeft = 3600;
            projectile.friendly = true;
            projectile.width = 15;
            projectile.magic = true;
        }

        public override void AI() {
            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 52, 0, 0, 100, default, 3.0f);
            Main.dust[dust].noGravity = true;
        }
    }
}
