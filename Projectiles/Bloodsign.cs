using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Bloodsign : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bloodsign");
            Main.projFrames[projectile.type] = 25;
        }
        public override void SetDefaults()
        {
            projectile.friendly = true;
            projectile.width = 64;
            projectile.height = 98;
            projectile.penetrate = -1;
            projectile.scale = 1;
            projectile.tileCollide = false;
            projectile.timeLeft = 36000;
            projectile.alpha = 254; //start nearly invis
        }
        public override void AI()
        {
            var player = Main.player[projectile.owner];

            if (Main.player[projectile.owner].Distance(projectile.Center) < 300f && !player.dead/*projectile.timeLeft <= 35099*/) //kill when player returns.
            {
                projectile.alpha += 1;
                if (projectile.alpha > 254)
                {
                    projectile.Kill();
                }
            }

            if (projectile.timeLeft > 35500)
            {
                projectile.alpha -= 4; //increase visibility
            }
            
            //movement
            if (projectile.timeLeft >= 35940)
            {
                projectile.velocity.Y = -.9f; //float up for 1 second
            }
            if (projectile.timeLeft < 35939)
            {
                projectile.velocity.Y = 0; //stop upwards velocity
            }

            //animation
            projectile.ai[0] += 1f;
            if (++projectile.frameCounter >= 5)
            {
                projectile.frameCounter = 0;
                if (++projectile.frame >= 25)
                {
                    projectile.frame = 0;
                }
            }

        }
        public override bool CanDamage()
        {
            return false;
        }


    }
}