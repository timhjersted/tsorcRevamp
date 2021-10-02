using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles
{
    class EnchantedThrowingSpear : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = 3;
        }
        public override void SetDefaults()
        {
            projectile.width = 45;
            projectile.height = 45;
            projectile.timeLeft = 120; 
            projectile.light = 0.5f;
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.ownerHitCheck = false;
            projectile.melee = false;
            projectile.tileCollide = false;
            projectile.hide = false;
            projectile.scale = 1f;
            projectile.magic = true;

        }

        public override void AI()
        {
            projectile.ai[0] += 1f;
            if (++projectile.frameCounter >= 3)
            {
                projectile.frameCounter = 0;
                if (++projectile.frame >= 3)
                {
                    projectile.frame = 0;
                }
            }
            if (projectile.ai[0] >= 15f) { //this is the bit that makes it arc down
                projectile.ai[0] = 15f;
                projectile.velocity.Y += 0.1f;
            }
            if (projectile.velocity.Y > 16f) { //this bit caps down velocity, and thus also caps down rotation if fired at a positive angle
                projectile.velocity.Y = 16f;
            }
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(90f); //simplified rotation code (no trig!)
        }
        public override void Kill(int timeLeft) {
            Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 1);
            for (int i = 0; i < 10; i++) {
                Vector2 projPosition = new Vector2(projectile.position.X, projectile.position.Y);
                Dust.NewDust(projPosition, projectile.width, projectile.height, 7, 0f, 0f, 0, default, 1f);
            }
        }

    }

}
