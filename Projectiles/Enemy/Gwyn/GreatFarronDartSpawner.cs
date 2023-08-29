using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Gwyn;

class GreatFarronDartSpawner : ModProjectile
{

    public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.friendly = false;
        Projectile.alpha = 255;
        Projectile.timeLeft = 225; //arbitrary, so long as it's greater than Interval
        Projectile.tileCollide = false;
        Projectile.timeLeft = 500;
    }

    internal float AI_Timer
    {
        get => Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    internal float AI_Interval
    {
        get => Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public override void AI()
    {
        AI_Timer++;
        if (AI_Timer == 1)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //zero damage means it's a telegraphing tracer not an actual bullet
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position, Projectile.velocity * 0.75f, ModContent.ProjectileType<GreatFarronDart>(), 0, 0);
            }
            Projectile.velocity *= 0.01f;
        }

        if (AI_Timer == Math.Floor(AI_Interval / 2))
        {

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position, Projectile.velocity * 100, ModContent.ProjectileType<GreatFarronDart>(), Projectile.damage, 1);
            }

        }


        if (AI_Timer > (AI_Interval))
        {
            Projectile.Kill();
        }
    }
}
