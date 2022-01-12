﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    public class FlameJet : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 5;
        }

        public override void SetDefaults() {
            
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.magic = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 20;
        }

        bool initialized = false;
        public override void AI() {

            if (!initialized)
            {
                if (projectile.ai[0] == 0) //Vertical mode
                {
                    projectile.width = 48;
                    projectile.height = 16 * (int)projectile.ai[1];
                }
                else //Horizontal mode
                {
                    projectile.width = 16 * (int)projectile.ai[1];
                    projectile.height = 48;
                }
            }

            int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int segmentCount = projectile.height / frameHeight;
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 lightPosition = new Vector2(projectile.Center.X, projectile.position.Y);
                lightPosition.Y += (frameHeight * i) + (frameHeight / 2);
                Lighting.AddLight(lightPosition, Color.Orange.ToVector3());
            }
            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 174, 0, Main.rand.Next(-3, 0), 0, default, 4);
                d.velocity.X = 0;
                d.noGravity = true;
            }


            projectile.frameCounter++;
            if (projectile.frameCounter > 3) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 5) {
                projectile.Kill();
                return;
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (projectile.ai[0] == 0)
            {
                if (target.Center.X < projectile.Center.X)
                {
                    target.velocity = new Vector2(-10, 0);
                }
                else
                {
                    target.velocity = new Vector2(10, 0);
                }
            }
            else
            {
                if (target.Center.Y < projectile.Center.Y)
                {
                    target.velocity = new Vector2(0, -10);
                }
                else
                {
                    target.velocity = new Vector2(0, 10);
                }
            }
            
        }

        public static Texture2D flameJetTexture;
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            /*
            if (flameJetTexture == null || flameJetTexture.IsDisposed)
            {
                flameJetTexture = mod.GetTexture("Projectiles/FlameJet");
            }
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, flameJetTexture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            int drawCount = projectile.height / frameHeight;
            for(int i = 0; i < drawCount; i++)
            {
                Vector2 startPosition = new Vector2(projectile.Center.X, projectile.position.Y);
                Vector2 drawPosition = startPosition - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
                drawPosition.Y += (frameHeight * i) + (frameHeight / 2);
                Main.spriteBatch.Draw(flameJetTexture, drawPosition, sourceRectangle, Color.White, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);
            }*/

            return false;
        }
    }
}