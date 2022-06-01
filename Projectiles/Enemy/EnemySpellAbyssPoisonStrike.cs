using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellAbyssPoisonStrike : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 40;
            Main.projFrames[Projectile.type] = 5;
            Projectile.aiStyle = 4;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 50;
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
            if (Projectile.frame >= 5)
            {
                Projectile.Kill();
                return;
            }
        }
        #endregion
    }
}