using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku;

public class SolarFireball : ModProjectile
{

    public override void SetStaticDefaults()
    {
        //DisplayName.SetDefault("Wave Attack");
    }
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DD2BetsyFireball;
    public override void SetDefaults()
    {
        Projectile.aiStyle = 686;
        Projectile.CloneDefaults(ProjectileID.DD2BetsyFireball);
        Projectile.tileCollide = false;
    }
}
