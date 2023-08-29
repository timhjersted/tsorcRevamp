using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy;

class EnemySpellGreatFireball : ModProjectile
{

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Enemy Spell Great Fireball");

    }
    public override void SetDefaults()
    {
        Projectile.width = 150;
        Projectile.height = 150;
        Main.projFrames[Projectile.type] = 9;
        Projectile.aiStyle = -1;
        Projectile.hostile = true;
        Projectile.scale = 2;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.light = 1;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 360;
        Projectile.penetrate = 50;
        DrawOriginOffsetX = -75;
        DrawOriginOffsetY = 70;
    }
    #region AI
    public override void AI()
    {

        Projectile.frameCounter++;
        if (Projectile.frameCounter > 3)
        {
            Projectile.frame++;
            Projectile.frameCounter = 0;
        }
        if (Projectile.frame >= 9)
        {
            Projectile.Kill();
            return;
        }
    }
    #endregion
}