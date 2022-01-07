using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class ObscureShot : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = 0;
            projectile.hostile = true;
            projectile.height = 9;
            projectile.width = 9;
            projectile.scale = 2;
            projectile.tileCollide = false;
            projectile.timeLeft = 1500;
        }

        public override bool PreKill(int timeLeft) {
            projectile.type = 44; //killpretendtype
            return true;
        }

        public override void AI() {
            projectile.rotation++;

            if (Main.rand.Next(2) == 0) {
                int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 62, 0, 0, 100, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;
            }

            if (projectile.velocity.X <= 6 && projectile.velocity.Y <= 6 && projectile.velocity.X >= -6 && projectile.velocity.Y >= -6) {
                projectile.velocity.X *= 1.02f;
                projectile.velocity.Y *= 1.02f;
            }
        }

        //This is too hard to see especially at night, so i'm making it ignore all lighting and always draw at full brightness
        static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = ModContent.GetTexture("tsorcRevamp/Projectiles/Enemy/Okiku/ObscureShot");
            }
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.spriteBatch.Draw(texture,
                projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                sourceRectangle, Color.White, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);

            return false;
        }
    }
}
