using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Projectiles
{
    class GlintstoneSpeck : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 8;
            projectile.height = 8;
            projectile.friendly = true;
            projectile.magic = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 600;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];

            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, 0, 6, 8), Color.White, projectile.rotation, new Vector2(3, 4), projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            // change the hitbox size, centered about the original projectile center. This makes the projectile have small aoe.
            projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
            projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
            projectile.width = 20;
            projectile.height = 20;
            projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
            projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);

            projectile.timeLeft = 2;
        }

        public override void AI()
        {
            if (projectile.velocity.X > 0) //if going right
            {
                for (int d = 0; d < 4; d++)
                {
                    int num44 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y - 2), projectile.width, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
                    Main.dust[num44].noGravity = true;
                    Main.dust[num44].velocity *= 0f;
                }

                for (int d = 0; d < 4; d++)
                {
                    int num45 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y - 2), projectile.width - 4, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default(Color), .5f);
                    Main.dust[num45].noGravity = true;
                    Main.dust[num45].velocity *= 0f;
                    Main.dust[num45].fadeIn *= 1f;
                }
            }
            else //if going left
            {
                for (int d = 0; d < 4; d++)
                {
                    int num44 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y - 1), projectile.width, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
                    Main.dust[num44].noGravity = true;
                    Main.dust[num44].velocity *= 0f;
                }

                for (int d = 0; d < 4; d++)
                {
                    int num45 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y - 1), projectile.width - 4, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default(Color), .5f);
                    Main.dust[num45].noGravity = true;
                    Main.dust[num45].velocity *= 0f;
                    Main.dust[num45].fadeIn *= 1f;
                }
            }

            Lighting.AddLight(projectile.Center, .200f, .200f, .350f);
            projectile.rotation += 0.3f * (float)projectile.direction;

            projectile.ai[0]++;

            if (projectile.ai[0] == 1)
            {
                projectile.velocity.X += Main.rand.NextFloat(-3.5f, 3.5f);
                projectile.velocity.Y += Main.rand.NextFloat(-5, -1);
            }

            if (projectile.ai[0] == 15)
            {
                projectile.velocity = Vector2.Zero;
            }

            if (projectile.ai[0] > 125)
            {
                Vector2 vector = projectile.position;
                float num34 = 300f;
                bool targetAcquired = false;

                for (int num4 = 0; num4 < 200; num4++)
                {
                    NPC nPC2 = Main.npc[num4];
                    if (nPC2.CanBeChasedBy(this))
                    {
                        float num5 = Vector2.Distance(nPC2.Center, projectile.Center);
                        if (num5 < num34 && Collision.CanHitLine(projectile.position, projectile.width, projectile.height, nPC2.position, nPC2.width, nPC2.height))
                        {
                            num34 = num5;
                            vector = nPC2.Center;
                            targetAcquired = true;
                        }
                    }
                }

                float shotSpeed = 10f;
                Vector2 vector7 = vector - projectile.Center;
                vector7.Normalize();
                vector7 *= shotSpeed;
                if (targetAcquired)
                {
                    projectile.velocity = vector7;
                    projectile.timeLeft = 10;
                }
            }

        }

        public override void Kill(int timeLeft)
        {
            for (int d = 0; d < 14; d++)
            {
                int dust = Dust.NewDust(projectile.Center, 8, 8, 68, projectile.velocity.X * 1f, projectile.velocity.Y * 1f, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }

            Main.PlaySound(SoundID.NPCHit3.WithVolume(.35f), projectile.position);

        }
    }
}
