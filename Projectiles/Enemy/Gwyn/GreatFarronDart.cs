using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Gwyn;

class GreatFarronDart : ModProjectile
{

    public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/Petal";
    public override void SetDefaults()
    {
        Projectile.height = 16;
        Projectile.width = 16;
        Projectile.light = 0.8f;
        Projectile.penetrate = 99999;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 120;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.damage = 40;
    }

    internal float AI_Owner
    {
        get => Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public override void AI()
    {

        if (Projectile.damage == 0)
        {
            Projectile.alpha = 255;
            Dust h = Dust.NewDustPerfect(Projectile.Center, DustID.Clentaminator_Purple);
            h.noGravity = true;
            h.velocity = Vector2.Zero;
            Projectile.extraUpdates = 60;
            Projectile.timeLeft--;
        }

    }

    public override bool PreKill(int timeLeft)
    {
        if (Projectile.damage != 0)
        {
            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.Clentaminator_Purple);
            }
        }
        return true;
    }
}
