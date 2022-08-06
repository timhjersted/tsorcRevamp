using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class HealingWater : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/MusicalNote";
        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.height = 18;
            Projectile.width = 18;
            Projectile.timeLeft = 30;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            for (int i = 0; i < 4; i++)
            {
                var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 29, Projectile.velocity.X * 0.4f, Projectile.velocity.Y * 0.4f, 0, default, 2f);
                dust.noGravity = true;
            }
        }
    }
}
