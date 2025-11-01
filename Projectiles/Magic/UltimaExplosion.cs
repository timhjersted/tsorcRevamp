using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    class UltimaExplosion : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Large Soulsplosion"); // These are just part of the animation of the consumable souls
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {

            Projectile.aiStyle = 0;
            Projectile.friendly = true;
            Projectile.width = 176;
            Projectile.height = 144;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 16;
            Projectile.ignoreWater = true;
            Projectile.scale = 1.29f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public int soulsplosionsmalltimer;
        public override void AI()
        {
            soulsplosionsmalltimer++;

            //ANIMATION

            if (++Projectile.frameCounter >= 4) //ticks spent on each frame
            {
                Projectile.frameCounter = 0;
                if (Projectile.timeLeft >= 4)
                {
                    if (++Projectile.frame == 4)
                    {
                        Projectile.frame = 0;
                    }
                }
            }
        }
    }
}