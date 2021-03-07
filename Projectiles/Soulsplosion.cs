using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Soulsplosion : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soulsplosion"); // These are just part of the animation of the consumable souls
            Main.projFrames[projectile.type] = 3;
        }
        public override void SetDefaults()
        {

            projectile.aiStyle = 0;
            projectile.friendly = true;
            projectile.width = 112;
            projectile.height = 112;
            projectile.penetrate = -1;
            projectile.damage = 200;
            projectile.scale = 1;
            projectile.tileCollide = false;
            projectile.timeLeft = 12;
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
                    if (++projectile.frame == 3)
                    {
                        projectile.frame = 0;
                    }
                }
            }
        }
    }
}
