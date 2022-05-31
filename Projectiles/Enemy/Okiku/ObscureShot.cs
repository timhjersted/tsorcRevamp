using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class ObscureShot : ModProjectile {

        public override void SetDefaults() {
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.height = 9;
            Projectile.width = 9;
            Projectile.scale = 2;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 1500;
        }

        public override bool PreKill(int timeLeft) {
            Projectile.type = 44; //killpretendtype
            return true;
        }

        public override void AI() {
            Projectile.rotation++;

            if (Main.rand.Next(2) == 0) {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 62, 0, 0, 100, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;
            }

            if (Projectile.velocity.X <= 6 && Projectile.velocity.Y <= 6 && Projectile.velocity.X >= -6 && Projectile.velocity.Y >= -6) {
                Projectile.velocity.X *= 1.02f;
                Projectile.velocity.Y *= 1.02f;
            }
        }

        //This is too hard to see especially at night, so i'm making it ignore all lighting and always draw at full brightness
        static Texture2D texture;
        public override bool PreDraw(ref Color lightColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/ObscureShot");
            }
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            int frameHeight = Main.projectileTexture[Projectile.type].Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);

            return false;
        }
    }
}
