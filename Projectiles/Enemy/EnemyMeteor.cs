using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyMeteor : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.height = 16;
            Projectile.light = 0.5f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 45;
            Projectile.tileCollide = true;
            Projectile.width = 16;
        }
        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 15;


            return true;
        }
    }
}