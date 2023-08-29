using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class EnemySpellIcestormIcicle2 : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 20;
        Projectile.height = 24;
        Projectile.hostile = true;
        Projectile.penetrate = 16;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 400;
    }
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Enemy Spell Ice Storm");
    }
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        lightColor = Color.White;
        return base.PreDraw(ref lightColor);
    }
}
