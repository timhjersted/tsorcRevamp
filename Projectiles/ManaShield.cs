using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class ManaShield : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wall");
        }
        public override void SetDefaults()
        {
            drawHeldProjInFrontOfHeldItemAndArms = true; // Makes projectile appear in front of arms, not just in between body and arms
            projectile.friendly = true;
            projectile.width = 22;
            projectile.height = 30;
            projectile.penetrate = -1;
            projectile.scale = 2.5f;
            projectile.tileCollide = false;
            projectile.timeLeft = 2;
            projectile.gfxOffY = -1;
            Main.projFrames[projectile.type] = 8;
        }
        public override void AI()
        {

            var player = Main.player[projectile.owner];

            if (player.dead)
            {
                projectile.Kill();
                return;
            }
            if (Main.rand.Next(4) == 0)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 57, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 180, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }

            projectile.frameCounter++;
            if (projectile.frameCounter > 2)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 8)
            {
                projectile.frame = 0;
            }
            projectile.position.X = player.position.X;
            projectile.position.Y = player.position.Y;
            Player projOwner = Main.player[projectile.owner];
            projOwner.heldProj = projectile.whoAmI; //this makes it appear in front of the player
        }
        public override bool CanDamage()
        {
            return false;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {

            //If the player has Mana Sickness, the shield doesn't function. So this disables it being drawn and adds some dust as a visual indicator.
            //  if (Main.player[projectile.owner].HasBuff(BuffID.ManaSickness))
            //  {
            //    int pdust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, DustID.Vile, Main.rand.Next(16), Main.rand.Next(16), 200, Color.LightCyan, 1.0f);
            //     Main.dust[pdust].noGravity = true;
            //     return false;
            // }

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = tsorcRevamp.TransparentTextures[3];
            int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            //origin.X = (float)(projectile.spriteDirection == 1 ? sourceRectangle.Width - 20 : 20);
            Color drawColor = projectile.GetAlpha(lightColor);
            //Main.spriteBatch.Draw(texture,
             //   projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
             //   sourceRectangle, drawColor, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);
             Main.spriteBatch.Draw(texture,
                Main.player[projectile.owner].Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                sourceRectangle, drawColor, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);

            return false;
        }
    }
}