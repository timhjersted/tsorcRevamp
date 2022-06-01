using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class SporePowder : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.scale = 0.8f;
            Projectile.penetrate = -1;
            Projectile.light = 1;
            Projectile.timeLeft = 180;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.thrown = true;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.95f;
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] == 180f)
            {
                Projectile.Kill();
            }
            if (Projectile.ai[1] == 0f)
            {
                Projectile.ai[1] = 1f;
                for (int k = 0; k < 30; k++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 44, Projectile.velocity.X, Projectile.velocity.Y, 50, default(Color), 1f);
                }
            }
        }

    }
}
