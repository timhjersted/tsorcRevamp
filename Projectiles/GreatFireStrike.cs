using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

public class GreatFireStrike : ModProjectile
{

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 5;
    }

    public override void SetDefaults()
    {
        Projectile.width = 26;
        Projectile.height = 40;
        Projectile.friendly = true;
        Projectile.penetrate = 50;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 360;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 45;
    }

    public override void AI()
    {
        Projectile.frameCounter++;
        if (Projectile.frameCounter > 3)
        {
            Projectile.frame++;
            Projectile.frameCounter = 0;
        }
        if (Projectile.frame >= 5)
        {
            Projectile.Kill();
            return;
        }
    }
}
