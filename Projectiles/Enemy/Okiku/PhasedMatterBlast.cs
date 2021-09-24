using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {

    public class PhasedMatterBlast : ModProjectile {

        public static Texture2D antiMatterBlastTexture;
        public override void SetDefaults() {
            projectile.width = 35;
            projectile.height = 35;
            projectile.scale = 1f;
            //projectile.aiStyle = 9;
            projectile.hostile = true;
            projectile.damage = 80;
            projectile.penetrate = 2;
            projectile.tileCollide = false;
            projectile.ranged = true;    
        }

        public override bool PreKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
            {
                Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.PurpleCrystalShard, 0, 0, 200, Color.Red, 2f);
            }
            return true;
        }

        public override void AI()
        {
            float accel = 0.03f;
            projectile.rotation += 0.5f;
            if (Main.player[(int)projectile.ai[0]].position.X < projectile.position.X)
            {
                if (projectile.velocity.X > -10) projectile.velocity.X -= accel;
            }

            if (Main.player[(int)projectile.ai[0]].position.X > projectile.position.X)
            {
                if (projectile.velocity.X < 10) projectile.velocity.X += accel;
            }

            if (Main.player[(int)projectile.ai[0]].position.Y < projectile.position.Y)
            {
                if (projectile.velocity.Y > -10) projectile.velocity.Y -= accel;
            }

            if (Main.player[(int)projectile.ai[0]].position.Y > projectile.position.Y)
            {
                if (projectile.velocity.Y < 10) projectile.velocity.Y += accel;
            }

            float maxSpeed = 8;
            projectile.velocity = Vector2.Clamp(projectile.velocity, new Vector2(-maxSpeed, -maxSpeed), new Vector2(maxSpeed, maxSpeed));

            if (Main.rand.Next(12) == 0)
            {
                int dust = Dust.NewDust(new Vector2((float)projectile.position.X + 10, (float)projectile.position.Y), projectile.width, projectile.height, DustID.Fire, 0, 0, 200, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
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
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PhasedMatterBlast];
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
    }
}
