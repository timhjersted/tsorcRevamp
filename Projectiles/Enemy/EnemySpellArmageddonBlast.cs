using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellArmageddonBlast : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 240;
            Projectile.height = 240;
            Main.projFrames[Projectile.type] = 8;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.scale = 2f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.ignoreWater = true;
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
            if (Projectile.frame >= 8)
            {
                Projectile.Kill();
            }
        }
        #endregion
    }
}