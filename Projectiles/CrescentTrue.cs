using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class CrescentTrue : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = 28;
        }

        public override void SetDefaults()
        {
            projectile.width = 26;
            projectile.height = 26;
            projectile.penetrate = 5;
            projectile.friendly = true;
            projectile.tileCollide = true;
            projectile.melee = true;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];

            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, projectile.frame * 64, 68, 64), Color.White, projectile.rotation, new Vector2(34, 32), projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(new Vector2(projectile.position.X - 11, projectile.position.Y - 11), projectile.width + 22, projectile.height + 22, 234, 0, 0, 70, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(new Vector2(projectile.position.X - 11, projectile.position.Y - 11), projectile.width + 22, projectile.height + 22, 234, projectile.velocity.X, projectile.velocity.Y, 70, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
            return true;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Buffs.CrescentMoonlight>(), 180);
        }

        public bool spawned = false;
        public int ChosenStartFrame = 0;

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation();
            Lighting.AddLight(projectile.position, 0.0452f, 0.21f, 0.1f);

            for (int d = 0; d < 2; d++)
            {
                int dust = Dust.NewDust(new Vector2(projectile.position.X - 11, projectile.position.Y - 11), projectile.width + 22, projectile.height + 22, 234, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;

            }

            for (int d = 0; d < 2; d++)
            {
                int dust = Dust.NewDust(new Vector2(projectile.position.X - 11, projectile.position.Y - 11), projectile.width + 22, projectile.height + 22, 21, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }

            if (!spawned)
            {
                int[] StartFrameChoices = new int[] { 0, 7, 14, 21 };
                int StartFrame = Main.rand.Next(StartFrameChoices);


                spawned = true;
                ChosenStartFrame = StartFrame;
                projectile.frame = ChosenStartFrame;

            }

            if (spawned && ChosenStartFrame == 0)
            {
                if (++projectile.frameCounter >= 3)
                {
                    projectile.frameCounter = 0;
                    if (++projectile.frame > 7)
                    {
                        projectile.Kill();
                    }
                }
            }

            if (spawned && ChosenStartFrame == 7)
            {
                if (++projectile.frameCounter >= 3)
                {
                    projectile.frameCounter = 0;
                    if (++projectile.frame > 13)
                    {
                        projectile.Kill();
                    }
                }
            }

            if (spawned && ChosenStartFrame == 14)
            {
                if (++projectile.frameCounter >= 3)
                {
                    projectile.frameCounter = 0;
                    if (++projectile.frame > 20)
                    {
                        projectile.Kill();
                    }
                }
            }

            if (spawned && ChosenStartFrame == 21)
            {
                if (++projectile.frameCounter >= 3)
                {
                    projectile.frameCounter = 0;
                    if (++projectile.frame > 27)
                    {
                        projectile.Kill();
                    }
                }
            }
        }
    }
}

