using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

class Explosion : ModProjectile
{

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 12;
    }

    public override void SetDefaults()
    {
        Projectile.width = 26;
        Projectile.height = 26;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.penetrate = 50;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.light = 1;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        DrawOffsetX = -10;
        DrawOriginOffsetY = -10;
    }

    public override void AI()
    {
        Projectile.frameCounter++;
        if (Projectile.frameCounter > 3)
        {
            Projectile.frame++;
            Projectile.frameCounter = 0;
        }
        if (Projectile.frame >= 12)
        {
            Projectile.Kill();
            return;
        }
    }
}
