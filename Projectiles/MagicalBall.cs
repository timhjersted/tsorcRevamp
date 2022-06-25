using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class MagicalBall : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.aiStyle = 1;
            Projectile.timeLeft = 3600;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 4), Projectile.width, Projectile.height, 27, 0, 0, 100, default, 3.0f);
            Main.dust[dust].noGravity = true;

            Projectile.velocity.Y = 0;
        }
    }
}
