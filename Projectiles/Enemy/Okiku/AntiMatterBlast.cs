using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {

    public class AntiMatterBlast : ModProjectile {

        public static Texture2D antiMatterBlastTexture;
        public override void SetDefaults() {
            projectile.width = 55;
            projectile.height = 55;
            projectile.scale = 2.3f;
            //projectile.aiStyle = 9;
            projectile.hostile = true;
            projectile.damage = 80;
            projectile.penetrate = 2;
            projectile.tileCollide = false;
            projectile.ranged = true;
           
            
            
        }
        public override bool PreKill(int timeLeft) {
            projectile.type = 79; //killpretendtype
            return true;
        }

        public override bool PreAI() {
            projectile.rotation += 0.5f;
            int dust = Dust.NewDust(new Vector2((float)projectile.position.X + 10, (float)projectile.position.Y), projectile.width, projectile.height, DustID.Fire, 0, 0, 200, Color.Red, 1f);
            Main.dust[dust].noGravity = true;

            if (projectile.velocity.X <= 10 && projectile.velocity.Y <= 10 && projectile.velocity.X >= -10 && projectile.velocity.Y >= -10) {
                projectile.velocity.X *= 1.01f;
                projectile.velocity.Y *= 1.01f;
            }
            return true;
        }


        
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {

            
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = tsorcRevamp.TransparentTextures[0];
            int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Color drawColor = projectile.GetAlpha(lightColor);
            Main.spriteBatch.Draw(texture,
                projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                sourceRectangle, drawColor, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);

            return false;
        }


        public override void OnHitPlayer(Player target, int damage, bool crit) {
            target.AddBuff(BuffID.Confused, 300, false);
            target.AddBuff(BuffID.Gravitation, 300, false);
            target.AddBuff(BuffID.Slow, 300, false);
        }

    }
}
