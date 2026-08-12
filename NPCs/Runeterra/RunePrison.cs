using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Runeterra;

public class RunePrison : ModProjectile
{
    public override void SetStaticDefaults()
    {
    }

    public override void SetDefaults()
    {
        Projectile.width = 50;
        Projectile.height = 100;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.scale = 1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 300;
        //Projectile.alpha = 160;
    }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        Projectile.velocity = owner.velocity;
        owner.AddBuff(ModContent.BuffType<RunePrisonDebuff>(), 2);
    }
}