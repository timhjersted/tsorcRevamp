using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class SpellUltimateExplosion : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Spell Ultimate Explosion");
            Main.projFrames[Projectile.type] = 9;
        }
        public override void SetDefaults()
        {

            Projectile.aiStyle = 1;
            Projectile.friendly = true;
            Projectile.width = 300;
            Projectile.height = 300;
            Projectile.light = 1f;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 2f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
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
}