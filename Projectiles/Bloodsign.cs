using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

class Bloodsign : ModProjectile
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Bloodsign");
        Main.projFrames[Projectile.type] = 25;
    }
    public override void SetDefaults()
    {
        Projectile.friendly = true;
        Projectile.width = 64;
        Projectile.height = 98;
        Projectile.penetrate = -1;
        Projectile.scale = 1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 36000;
        Projectile.alpha = 254; //start nearly invis
    }
    public float AI_Projectile_Lifetime
    {
        get => Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public bool playerReturned = false;
    public override void AI()
    {
        AI_Projectile_Lifetime += 1f;

        var player = Main.player[Projectile.owner];

        if ((player.Distance(Projectile.Center) < 360f) && !player.dead) //kill when player returns.
        {
            playerReturned = true;
            if (player.HasBuff(ModContent.BuffType<Buffs.Hollowed>())) {
                player.ClearBuff(ModContent.BuffType<Buffs.Hollowed>());
            }
        }

        if (playerReturned)
        {
            Projectile.alpha += 1;
            if (Projectile.alpha > 254)
            {
                Projectile.Kill();
            }
        }
        if (AI_Projectile_Lifetime < 100)
        {
            Projectile.alpha -= 4; //increase visibility
        }

        //movement
        if (AI_Projectile_Lifetime <= 60)
        {
            Projectile.velocity.Y = -.9f; //float up for 1 second
        }
        else
        {
            Projectile.velocity.Y = 0f; //stop upwards velocity
        }

        //animation

        if (++Projectile.frameCounter >= 5)
        {
            Projectile.frameCounter = 0;
            if (++Projectile.frame >= 25)
            {
                Projectile.frame = 0;
            }
        }

    }
    public override bool? CanDamage()
    {
        return false;
    }


}