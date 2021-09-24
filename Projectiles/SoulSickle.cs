using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class SoulSickle : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul Sickle");
            Main.projFrames[projectile.type] = 3;
        }
        public override void SetDefaults()
        {
            projectile.aiStyle = 0;
            projectile.friendly = true;
            projectile.width = 68;
            projectile.height = 68;
            projectile.penetrate = -1;
            projectile.damage = 40;
            projectile.scale = 1;
            projectile.tileCollide = false;
            projectile.timeLeft = 30;
            projectile.alpha = 120;
            projectile.ignoreWater = true;
        }

        public override void AI()
        {

            Lighting.AddLight(projectile.Center, 0.3f, 0.462f, 0.4f);

            int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 89, 0, 0, 0, default, .5f);
            Main.dust[dust].velocity *= 0.25f;
            Main.dust[dust].noGravity = true;
            Main.dust[dust].fadeIn = 1f;


            //ANIMATION

            if (++projectile.frameCounter >= 5) //ticks spent on each frame
            {
                projectile.frameCounter = 0;
                if (projectile.timeLeft >= 5)
                {
                    if (++projectile.frame == 3)
                    {
                        projectile.frame = 0;
                    }
                }
            }

            if (projectile.timeLeft < 20)
            {
                projectile.alpha += 6;
            }
        }
    }
}
