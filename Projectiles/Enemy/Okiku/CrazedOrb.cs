using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class CrazedOrb : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 4;
            DisplayName.SetDefault("Pulsating Energy");
        }

        public override void SetDefaults() {
            projectile.width = 32;
            projectile.height = 34;
            projectile.hostile = true;
            projectile.scale = 1.5f;
            projectile.tileCollide = false;
            projectile.timeLeft = 1500;
            projectile.light = 1;
        }

        public override void AI() {
            projectile.rotation++;

            if (Main.rand.Next(2) == 0) {
                int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 62, 0, 0, 100, Color.White, 3.0f);
                Main.dust[dust].noGravity = true;
            }

            projectile.frameCounter++;
            if (projectile.frameCounter > 2) {
                projectile.frame++;
                projectile.frameCounter = 3;
            }
            if (projectile.frame >= 4) {
                projectile.frame = 0;
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.CrazedOrb];
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
