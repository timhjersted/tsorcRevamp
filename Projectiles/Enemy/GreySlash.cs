using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class GreySlash : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            projectile.width = 58;
            projectile.height = 46;
            projectile.damage = 10;
            projectile.penetrate = 3;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.alpha = 80;

        }

        float angle;
        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation();
            projectile.spriteDirection = projectile.direction;
            if (projectile.spriteDirection == -1)
            {
                projectile.rotation -= MathHelper.ToRadians(180f);
            }

            if (Main.rand.Next(3) == 0)
            {
                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 89, 0, 0, 50, default(Color), .8f)];
                dust2.velocity *= 0;
                dust2.noGravity = true;
            }

            if (projectile.ai[0] >= 10)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(projectile.position.X - 10, projectile.position.Y - 10), projectile.width + 20, projectile.height + 20, 89, 0, 0, 50, default(Color), 1f)];
                    dust2.velocity *= 0;
                    dust2.noGravity = true;
                }
            }

            //Proj when moving slow
            NPC owner = Main.npc[(int)projectile.ai[1]];
            if (projectile.ai[0] < 10)
            {
                if (projectile.ai[0] < 1)
                {
                    ++projectile.ai[0];
                    Vector2 difference = projectile.Center - owner.Center;
                    angle = difference.ToRotation();
                }

                if (projectile.ai[0] >= 1)
                {
                    //Create a new Vector2 with length offsetDistance, and then rotate it toward the correct direction
                    Vector2 offset = new Vector2(34, 0).RotatedBy(angle);
                    //Add that to the npc's position
                    projectile.Center = owner.Center + offset;
                }
            }


            //Proj when moving fast
            if (projectile.ai[0] >= 10 && projectile.ai[0] < 20)
            {
                if (projectile.ai[0] < 11)
                {
                    ++projectile.ai[0];
                    Vector2 difference = projectile.Center - owner.Center;
                    angle = difference.ToRotation();
                }

                if (projectile.ai[0] >= 1)
                {
                    //Create a new Vector2 with length offsetDistance, and then rotate it toward the correct direction
                    Vector2 offset = new Vector2(34, 0).RotatedBy(angle);
                    //Add that to the npc's position
                    projectile.Center = owner.Center + offset;
                }
            }


            if (++projectile.frameCounter >= 2)
            {
                projectile.frameCounter = 0;
                if (++projectile.frame > 6)
                {
                    projectile.Kill();
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (projectile.ai[0] >= 10 && projectile.ai[0] < 20)
            {
                target.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 900);
            }
            else
            {
                target.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 600);
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;

            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            Color alphalowered;
            Texture2D texture = Main.projectileTexture[projectile.type];
            Texture2D textureGlow = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GreySlashGlowmask];
            int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY), sourceRectangle, lightColor, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);

            if (projectile.ai[0] >= 10)
            {
                alphalowered = Color.White * 0.9f;
                spriteBatch.Draw(textureGlow, projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY), sourceRectangle, alphalowered, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);
            }

            return false;
        }
    }
}

