using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

class SoulSickle : ModProjectile
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Soul Sickle");
        Main.projFrames[Projectile.type] = 3;
    }
    public override void SetDefaults()
    {
        Projectile.aiStyle = 0;
        Projectile.friendly = true;
        Projectile.width = 68;
        Projectile.height = 68;
        Projectile.penetrate = -1;
        Projectile.damage = 40;
        Projectile.scale = 1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 30;
        Projectile.alpha = 120;
        Projectile.ignoreWater = true;
    }

    public override void AI()
    {

        Lighting.AddLight(Projectile.Center, 0.3f, 0.462f, 0.4f);

        int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 89, 0, 0, 0, default, .5f);
        Main.dust[dust].velocity *= 0.25f;
        Main.dust[dust].noGravity = true;
        Main.dust[dust].fadeIn = 1f;


        //ANIMATION

        if (++Projectile.frameCounter >= 5) //ticks spent on each frame
        {
            Projectile.frameCounter = 0;
            if (Projectile.timeLeft >= 5)
            {
                if (++Projectile.frame == 3)
                {
                    Projectile.frame = 0;
                }
            }
        }

        if (Projectile.timeLeft < 20)
        {
            Projectile.alpha += 6;
        }
    }
}
