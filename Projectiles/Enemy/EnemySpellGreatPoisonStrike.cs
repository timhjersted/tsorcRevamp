using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellGreatPoisonStrike : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Enemy Spell Great Poison Strike");
            Main.projFrames[Projectile.type] = 5;
        }
        public override void SetDefaults()
        {

            Projectile.aiStyle = 4;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.light = 1f;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 1f;
            Projectile.tileCollide = true;
            DrawOriginOffsetY = -6;
            DrawOriginOffsetX = -6;
            DrawOffsetX = -6;
        }

        #region AI
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

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