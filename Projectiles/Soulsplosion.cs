using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

class Soulsplosion : ModProjectile
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Soulsplosion"); // These are just part of the animation of the consumable souls
        Main.projFrames[Projectile.type] = 3;
    }
    public override void SetDefaults()
    {

        Projectile.aiStyle = 0;
        Projectile.friendly = true;
        Projectile.width = 112;
        Projectile.height = 112;
        Projectile.penetrate = -1;
        Projectile.damage = 200;
        Projectile.scale = 1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 12;
        Projectile.alpha = 100;
        Projectile.ignoreWater = true;
    }

    public int soulsplosionsmalltimer;
    public override void AI()
    {
        soulsplosionsmalltimer++;

        //ANIMATION

        if (++Projectile.frameCounter >= 4) //ticks spent on each frame
        {
            Projectile.frameCounter = 0;
            if (Projectile.timeLeft >= 4)
            {
                if (++Projectile.frame == 3)
                {
                    Projectile.frame = 0;
                }
            }
        }
    }
}
