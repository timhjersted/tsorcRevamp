using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class FireField : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Projectiles/GreatFireStrike";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 40;
            Projectile.aiStyle = -1; ;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 50;
            Projectile.timeLeft = 360;
        }

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
                Projectile.frame = 0;
                return;
            }
        }
    }
}
