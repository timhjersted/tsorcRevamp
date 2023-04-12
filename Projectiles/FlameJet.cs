using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    public class FlameJet : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {

            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 20;
        }

        bool initialized = false;
        public override void AI()
        {

            if (!initialized)
            {
                if (Projectile.ai[0] == 0) //Vertical mode
                {
                    Projectile.width = 48;
                    Projectile.height = 16 * (int)Projectile.ai[1];
                }
                else //Horizontal mode
                {
                    Projectile.width = 16 * (int)Projectile.ai[1];
                    Projectile.height = 48;
                }
            }

            int frameHeight = ((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]).Height / Main.projFrames[Projectile.type];
            int segmentCount = Projectile.height / frameHeight;
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 lightPosition = new Vector2(Projectile.Center.X, Projectile.position.Y);
                lightPosition.Y += (frameHeight * i) + (frameHeight / 2);
                Lighting.AddLight(lightPosition, Color.Orange.ToVector3());
            }
            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 174, 0, Main.rand.Next(-3, 0), 0, default, 4);
                d.velocity.X = 0;
                d.noGravity = true;
            }


            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 5)
            {
                Projectile.Kill();
                return;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.ai[0] == 0)
            {
                if (target.Center.X < Projectile.Center.X)
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
                if (target.Center.Y < Projectile.Center.Y)
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
        public override bool PreDraw(ref Color lightColor)
        {
            /*
            if (flameJetTexture == null || flameJetTexture.IsDisposed)
            {
                flameJetTexture = (Texture2D)ModContent.Request<Texture2D>("Projectiles/FlameJet");
            }
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            int frameHeight = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[projectile.type].Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, flameJetTexture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            int drawCount = projectile.height / frameHeight;
            for(int i = 0; i < drawCount; i++)
            {
                Vector2 startPosition = new Vector2(projectile.Center.X, projectile.position.Y);
                Vector2 drawPosition = startPosition - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
                drawPosition.Y += (frameHeight * i) + (frameHeight / 2);
                Main.EntitySpriteDraw(flameJetTexture, drawPosition, sourceRectangle, Color.White, projectile.rotation, origin, projectile.scale, spriteEffects, 0);
            }*/

            return false;
        }
    }
}
