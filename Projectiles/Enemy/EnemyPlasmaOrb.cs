using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Achievements;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyPlasmaOrb : ModProjectile
    {

        public override void SetDefaults()
        {
            projectile.aiStyle = 0;
            projectile.width = 30;
            projectile.height = 30;
            projectile.hostile = true;
            projectile.timeLeft = 1500;
            projectile.scale = 2.2f;
            projectile.tileCollide = false;
            Main.projFrames[projectile.type] = 4;
            projectile.light = 1;
            //drawOffsetX = 50;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Omega Blast");
        }

        public override bool PreKill(int timeLeft)
        {
            projectile.type = 44; //killpretendtype
            return true;
        }

        public override void AI()
        {
            projectile.rotation += 1f;
           
            if (Main.rand.Next(2) == 0)
            {

                Lighting.AddLight((int)projectile.position.X / 16, (int)projectile.position.Y / 16, 15f, 0f, 0.1f);
                int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, DustID.Shadowflame, 0, 0, 100, Color.Green, 1.0f);
                Main.dust[dust].noGravity = true;
                int pdust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, DustID.Vile, 0, 0, 100, Color.Green, 1.0f);
                Main.dust[pdust].noGravity = true;
            }

            projectile.frameCounter++;
            if (projectile.frameCounter > 2)
            {
                projectile.frame++;
                projectile.frameCounter = 3;
            }
            if (projectile.frame >= 4)
            {
                projectile.frame = 0;
            }

            /**
            Rectangle projrec = new Rectangle((int)projectile.position.X + (int)projectile.velocity.X, (int)projectile.position.Y + (int)projectile.velocity.Y, projectile.width, projectile.height);
            Rectangle prec = new Rectangle((int)Main.player[Main.myPlayer].position.X, (int)Main.player[Main.myPlayer].position.Y, (int)Main.player[Main.myPlayer].width, (int)Main.player[Main.myPlayer].height);

            if (projrec.Intersects(prec))
            {

               
            }
            **/
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            base.OnHitPlayer(target, damage, crit);
            target.AddBuff(BuffID.Weak, 1200, false);
            target.AddBuff(BuffID.Slow, 1200, false);

        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.EnemyPlamaOrb];
            int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            //origin.X = (float)(projectile.spriteDirection == 1 ? sourceRectangle.Width - 20 : 20);
            Color drawColor = projectile.GetAlpha(lightColor);
            Main.spriteBatch.Draw(texture,
                projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                sourceRectangle, drawColor, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);

            return false;
        }
    }
}