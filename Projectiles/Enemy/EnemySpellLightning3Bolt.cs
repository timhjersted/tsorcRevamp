using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class EnemySpellLightning3Bolt : ModProjectile
{
    public override string Texture => "tsorcRevamp/Projectiles/Bolt3Bolt";
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 12;
    }

    public override void SetDefaults()
    {
        Projectile.width = 130;
        Projectile.height = 164;
        Projectile.penetrate = 8;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override void AI()
    {

        Projectile.frameCounter++;
        Projectile.frame = (int)Math.Floor((double)Projectile.frameCounter / 4);

        if (Projectile.frame >= 12)
        {
            Projectile.frame = 10;
        }
        if (Projectile.frameCounter > 53)
        { // (projFrames * 4.5) - 1
            Projectile.alpha += 15;
        }

        if (Projectile.alpha >= 255)
        {
            Projectile.Kill();
        }
    }
}
