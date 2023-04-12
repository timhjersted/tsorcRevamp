using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class CrystalFire : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Crystal Fire");

        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 8;
            Projectile.hostile = true;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = true;
            Projectile.damage = 21;
            Projectile.timeLeft = 350;
            Projectile.light = .8f;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Magic;

            Main.projFrames[Projectile.type] = 4;
        }
    }
}