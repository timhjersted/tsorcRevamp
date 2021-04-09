using Terraria;
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
        public float AI_Projectile_Lifetime {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }

        public bool playerReturned = false;
        public override void AI()
        {
            AI_Projectile_Lifetime += 1f;

            var player = Main.player[projectile.owner];

            if ((Main.player[projectile.owner].Distance(projectile.Center) < 300f) && !player.dead) //kill when player returns.
            {
                playerReturned = true;
            }

            if (playerReturned) {
                projectile.alpha += 1;
                if (projectile.alpha > 254) {
                    projectile.Kill();
                }
            }
            if (AI_Projectile_Lifetime < 100)
            {
                projectile.alpha -= 4; //increase visibility
            }
            
            //movement
            if (AI_Projectile_Lifetime <= 60)
            {
                projectile.velocity.Y = -.9f; //float up for 1 second
            }
            else {
                projectile.velocity.Y = 0f; //stop upwards velocity
            }

            //animation
            
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