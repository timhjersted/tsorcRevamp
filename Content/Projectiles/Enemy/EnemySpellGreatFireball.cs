using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    class EnemySpellGreatFireball : ModProjectile
    {
        private const int TicksPerFrame = 4;
        private const int DamagingFrames = 9;
        private const int TotalFrames = 11;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = TotalFrames;
        }

        public override void SetDefaults()
        {
            Projectile.width = 150;
            Projectile.height = 150;
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

        public override void AI()
        {
            Projectile.ai[0]++;
            int age = (int)Projectile.ai[0];
            if (age >= TotalFrames * TicksPerFrame)
            {
                Projectile.Kill();
                return;
            }

            Projectile.frame = age / TicksPerFrame;

            // Sync the transition to the ember tail using the same age that drives the frame.
            if (age == DamagingFrames * TicksPerFrame && Main.netMode == NetmodeID.Server)
            {
                Projectile.netUpdate = true;
            }
        }

        public override bool? CanDamage()
        {
            // Only the original nine frames can hit players. The extra frames are the visual tail.
            if (Projectile.ai[0] >= DamagingFrames * TicksPerFrame)
            {
                return false;
            }

            return null;
        }
    }
}
