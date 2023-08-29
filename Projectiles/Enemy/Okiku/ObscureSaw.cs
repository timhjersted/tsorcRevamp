using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku;

public class ObscureSaw : ModProjectile
{
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4;
        DisplayName.SetDefault("Wave Attack");
    }
    public override void SetDefaults()
    {
        Projectile.width = 34;
        Projectile.height = 34;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 160;
        Projectile.light = 1;
    }
    public override bool PreKill(int timeLeft)
    {
        Projectile.type = 41; //killpretendtype
        return true;
    }
    public override void AI()
    {
        Projectile.rotation++;

        if (Projectile.velocity.X <= 6 && Projectile.velocity.Y <= 6 && Projectile.velocity.X >= -6 && Projectile.velocity.Y >= -6)
        {
            Projectile.velocity.X *= 1.02f;
            Projectile.velocity.Y *= 1.02f;
        }

        Projectile.frameCounter++;
        if (Projectile.frameCounter > 2)
        {
            Projectile.frame++;
            Projectile.frameCounter = 3;
        }
        if (Projectile.frame >= 4)
        {
            Projectile.frame = 0;
        }
    }
}
