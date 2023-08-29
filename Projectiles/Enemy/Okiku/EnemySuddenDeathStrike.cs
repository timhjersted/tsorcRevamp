using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku;

class EnemySuddenDeathStrike : ModProjectile
{

    public override void SetDefaults()
    {
        Projectile.width = 44;
        Projectile.height = 40;
        Projectile.hostile = true;
        Projectile.penetrate = 50;
        Projectile.light = 1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Main.projFrames[Projectile.type] = 12;
    }

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Sudden Death Strike");
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
