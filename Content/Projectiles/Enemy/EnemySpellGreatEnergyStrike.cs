using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    class EnemySpellGreatEnergyStrike : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnergyField));
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 12;
        }
        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 40;
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 12)
            {
                Projectile.Kill();
                return;
            }
        }
    }
}
