using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class StardustShot : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = 0;
            projectile.height = 16;
            projectile.scale = 1.2f;
            projectile.tileCollide = false;
            projectile.width = 16;
            projectile.hostile = false;            
        }
        
        Projectiles.GenericLaser laser = null;

        public override void AI() {
            projectile.rotation++;

            if (laser == null)
            {
                laser = (GenericLaser)Projectile.NewProjectileDirect(projectile.Center, new Vector2(0, 5), ModContent.ProjectileType<GenericLaser>(), projectile.damage, .5f).modProjectile;
                laser.LaserOrigin = projectile.position;
                laser.LaserTarget = Main.player[(int)projectile.ai[0]].position;
                laser.TelegraphTime = 300;
                laser.FiringDuration = 120;
                laser.LaserLength = 8000; //What could go wrong? Turns out, plenty!
                laser.LaserColor = Color.DeepSkyBlue;
                laser.TileCollide = false;
                laser.CastLight = false;
                laser.LaserDust = 234;
                laser.MaxCharge = projectile.ai[1];
                projectile.timeLeft = (int)projectile.ai[1] + 130;
            }
            else
            {
                laser.LaserOrigin = projectile.Center;
                laser.LaserTarget = Vector2.Lerp(laser.LaserTarget, Main.player[(int)projectile.ai[0]].position, 0.02f);
            }

            if (Main.rand.Next(2) == 0) {
                int dust = Dust.NewDust(new Vector2((float)projectile.Center.X, (float)projectile.Center.Y), projectile.width, projectile.height, 234, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;
            }

            projectile.velocity.X *= .95f;
            projectile.velocity.Y *= .95f;            
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = ModContent.GetTexture("tsorcRevamp/Projectiles/Enemy/Okiku/StardustShot");
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
