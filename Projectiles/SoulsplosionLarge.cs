using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class SoulsplosionLarge : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Large Soulsplosion"); // These are just part of the animation of the consumable souls
            Main.projFrames[projectile.type] = 4;
        }
        public override void SetDefaults()
        {

            projectile.aiStyle = 0;
            projectile.friendly = true;
            projectile.width = 176;
            projectile.height = 144;
            projectile.penetrate = -1;
            projectile.damage = 1000;
            //projectile.ranged = true;
            projectile.scale = 1;
            projectile.tileCollide = false;
            projectile.timeLeft = 16;
            projectile.alpha = 100;
            projectile.ignoreWater = true;
        }

        public int soulsplosionsmalltimer;
        public override void AI()
        {
            soulsplosionsmalltimer++;

            //ANIMATION

            if (++projectile.frameCounter >= 4) //ticks spent on each frame
            {
                projectile.frameCounter = 0;
                if (projectile.timeLeft >= 4)
                {
                    if (++projectile.frame == 4)
                    {
                        projectile.frame = 0;
                    }
                }
            }
        }
    }
}