using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;
using tsorcRevamp.NPCs;


namespace tsorcRevamp.Projectiles
{
    class VirulentCatExplosion : ModProjectile
    {
        public override void SetDefaults()
        {
            // while the sprite is actually bigger than 15x15, we use 15x15 since it lets the projectile clip into tiles as it bounces. It looks better.
            projectile.width = 30;
            projectile.height = 30;
            projectile.friendly = true;
            projectile.aiStyle = 0;
            projectile.ranged = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 2;
            projectile.penetrate = -1; //this can be removed to only damage the host
            drawOffsetX = -2;
            drawOriginOffsetY = -2;
            projectile.usesLocalNPCImmunity = true; //any amount of explosions can damage a target simultaneously
            projectile.localNPCHitCooldown = -1; //but a single explosion can never damage the same enemy more than once
            projectile.alpha = 255;
        }

        public override void AI()
        {

            float tags = projectile.ai[0];

            projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
            projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
            projectile.width = ((int)tags * 20) + 30;
            projectile.height = ((int)tags * 20) + 30;
            projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
            projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
            projectile.damage += (int)tags * 5;
            projectile.knockBack = (tags * 1.2f) + 4f;


            float loops = (tags * 2) + 10;

            if (tags < 4)
            {
                for (int i = 0; i < loops; i++)
                {
                    int dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 75, 0f, 0f, 100, default(Color), 2f);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity *= 2.5f;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 107, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[dustIndex].velocity *= 3.5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 107, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 2.5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 75, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 3f;
                    Main.dust[dustIndex].noGravity = true;
                }
            }

            if (tags >= 4 && tags < 8)
            {
                for (int i = 0; i < loops; i++)
                {
                    int dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 75, 0f, 0f, 100, default(Color), 2f);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity *= 3.5f;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 107, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[dustIndex].velocity *= 4.5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 107, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 3.5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 75, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 4f;
                    Main.dust[dustIndex].noGravity = true;
                }
            }

            if (tags >= 8 && tags <= 10)
            {
                for (int i = 0; i < loops; i++)
                {
                    int dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 75, 1f, 1f, 100, default(Color), 2f);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity *= 4f;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 107, 1f, 1f, 100, default(Color), 1f);
                    Main.dust[dustIndex].velocity *= 5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 107, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 4f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 75, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 4.5f;
                    Main.dust[dustIndex].noGravity = true;
                }
            }
        }
    }
}