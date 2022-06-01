using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Bolt4Bolt : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 16;
        }

        public override void SetDefaults()
        {
            Projectile.width = 254;
            Projectile.height = 472;
            Projectile.penetrate = 8;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.8f;
        }
        public override void AI()
        {
            if (Projectile.ai[0] == 0)
            {
                Projectile.velocity.X *= 0.001f;
                Projectile.velocity.Y *= 0.001f;
                Projectile.ai[0] = 1;
            }

            Projectile.frameCounter++;
            Projectile.frame = (int)Math.Floor((double)Projectile.frameCounter / 4);

            if (Projectile.frame >= 16)
            {
                Projectile.frame = 15;
            }
            if (Projectile.frameCounter > 71)
            { // (projFrames * 4.5) - 1
                Projectile.alpha += 15;
            }

            if (Projectile.alpha >= 255)
            {
                Projectile.Kill();
            }
        }
    }
}

