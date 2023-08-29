using Terraria;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy;

class EnemySpellGreatEnergyStrike : ModProjectile
{
    public override string Texture => "tsorcRevamp/Projectiles/EnergyField";
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 12;
    }
    public override void SetDefaults()
    {
        Projectile.width = 44;
        Projectile.height = 40;
        Projectile.aiStyle = -1;
        Projectile.hostile = true;
        Projectile.penetrate = 50;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
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
