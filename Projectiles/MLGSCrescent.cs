using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class MLGSCrescent : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.penetrate = 5;
            projectile.friendly = true;
            projectile.tileCollide = false;
            projectile.melee = true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Color color = Color.White * 1f;

            if (projectile.ai[0] > 8)
            {
                spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, projectile.frame * 64, 68, 64), color, projectile.rotation, new Vector2(34, 32), projectile.scale, SpriteEffects.None, 0);
            }

            return false;
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(new Vector2(projectile.position.X - 11, projectile.position.Y - 11), projectile.width + 22, projectile.height + 22, 89, 0, 0, 70, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
        }


        public override void AI()
        {
            projectile.ai[0]++;
            projectile.rotation = projectile.velocity.ToRotation();

            projectile.velocity *= 1.2f;

            if (projectile.ai[0] > 8)
            {

                Lighting.AddLight(projectile.position, 0.0452f, 0.21f, 0.1f);

                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(projectile.position.X - 11, projectile.position.Y - 11), projectile.width + 22, projectile.height + 22, 89, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;

                }

                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(projectile.position.X - 11, projectile.position.Y - 11), projectile.width + 22, projectile.height + 22, 110, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }

                if (++projectile.frameCounter >= 3)
                {
                    projectile.frameCounter = 0;
                    if (++projectile.frame > 2)
                    {
                        projectile.Kill();
                    }
                }
            }

        }
    }
}
