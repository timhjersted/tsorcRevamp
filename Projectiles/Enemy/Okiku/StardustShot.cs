using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
    public class StardustShot : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.height = 16;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = false;
            Projectile.width = 16;
            Projectile.hostile = false;
        }

        Projectiles.GenericLaser laser = null;

        public override void AI()
        {
            Projectile.rotation++;

            if (laser == null)
            {
                laser = (GenericLaser)Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(0, 5), ModContent.ProjectileType<GenericLaser>(), Projectile.damage, .5f).ModProjectile;
                laser.LaserOrigin = Projectile.position;
                laser.LaserTarget = Main.player[(int)Projectile.ai[0]].position;
                laser.TelegraphTime = 300;
                laser.FiringDuration = 120;
                laser.LaserLength = 8000; //What could go wrong? Turns out, plenty!
                laser.LaserColor = Color.DeepSkyBlue;
                laser.TileCollide = false;
                laser.CastLight = false;
                laser.LaserDust = 234;
                laser.MaxCharge = Projectile.ai[1];
                Projectile.timeLeft = (int)Projectile.ai[1] + 130;
            }
            else
            {
                laser.LaserOrigin = Projectile.Center;
                laser.LaserTarget = Vector2.Lerp(laser.LaserTarget, Main.player[(int)Projectile.ai[0]].position, 0.02f);
            }

            if (Main.rand.Next(2) == 0)
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.Center.X, (float)Projectile.Center.Y), Projectile.width, Projectile.height, 234, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;
            }

            Projectile.velocity.X *= .95f;
            Projectile.velocity.Y *= .95f;
        }
        static Texture2D texture;
        public override bool PreDraw(ref Color lightColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/StardustShot");
            }
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            int frameHeight = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            //origin.X = (float)(projectile.spriteDirection == 1 ? sourceRectangle.Width - 20 : 20);
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);

            return false;
        }
    }
}
