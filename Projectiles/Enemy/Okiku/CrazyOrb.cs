using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku;

public class CrazyOrb : ModProjectile
{

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4;
        DisplayName.SetDefault("Pulsating Energy");
    }

    public override void SetDefaults()
    {
        Projectile.width = 32;
        Projectile.height = 34;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.light = 1;
        Projectile.timeLeft = 600;
    }

    public override void AI()
    {
        Projectile.rotation += 0.5f;

        if (Main.player[(int)Projectile.ai[0]].position.X < Projectile.position.X)
        {
            if (Projectile.velocity.X > -6) Projectile.velocity.X -= 0.05f;
        }

        if (Main.player[(int)Projectile.ai[0]].position.X > Projectile.position.X)
        {
            if (Projectile.velocity.X < 6) Projectile.velocity.X += 0.05f;
        }

        if (Main.player[(int)Projectile.ai[0]].position.Y < Projectile.position.Y)
        {
            if (Projectile.velocity.Y > -6) Projectile.velocity.Y -= 0.05f;
        }

        if (Main.player[(int)Projectile.ai[0]].position.Y > Projectile.position.Y)
        {
            if (Projectile.velocity.Y < 6) Projectile.velocity.Y += 0.05f;
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
