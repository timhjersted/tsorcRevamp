using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{

    public class NebulaShot : ModProjectile
    {

        public static Texture2D antiMatterBlastTexture;
        public override void SetDefaults()
        {
            Projectile.width = 35;
            Projectile.height = 35;
            Projectile.hostile = true;
            Projectile.penetrate = 2;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
        }

        public override bool PreKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleCrystalShard, 0, 0, 200, Color.Red, 2f);
            }
            return true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.5f;
            Projectile.velocity = Projectile.velocity.RotatedBy(0.01f * Projectile.ai[0] * Projectile.timeLeft / 600f);


            if (Main.rand.NextBool(12))
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X + 10, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.Torch, 0, 0, 200, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PhasedMatterBlast];
            int frameHeight = ((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]).Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            return false;
        }
    }
}
