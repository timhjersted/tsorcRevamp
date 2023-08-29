using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

public class EnemyCrystalKnightBolt : ModProjectile
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Crystal Bolt");
    }
    public override void SetDefaults()
    {
        Projectile.aiStyle = 1;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.height = 16;
        Projectile.light = 1;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 8;
        Projectile.scale = 1.3f;
        Projectile.tileCollide = true;
        AIType = 4;
        Projectile.width = 16;
        Projectile.timeLeft = 300;
        Projectile.ignoreWater = true;
    }



    public override void AI()
    {
        Dust thisDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 15, 0, 0, 250, default, 2f);
        thisDust.noGravity = true;
        thisDust.velocity = Vector2.Zero;
    }

    public override void OnHitPlayer(Player target, int damage, bool crit)
    {
        if (Main.expertMode)
        {
            Main.player[Main.myPlayer].AddBuff(BuffID.Frozen, 10, false); //slowed
            Main.player[Main.myPlayer].AddBuff(32, 300, false); //normal slow
        }
        else
        {
            Main.player[Main.myPlayer].AddBuff(BuffID.Frozen, 15, false); //slowed
            Main.player[Main.myPlayer].AddBuff(32, 600, false); //normal slow
        }
    }

    public override bool PreKill(int timeLeft)
    {
        Projectile.type = 30;
        return true;
    }

}