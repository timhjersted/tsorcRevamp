using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class HeavenPiercer : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.height = 22;
            Projectile.width = 22;
            Projectile.scale = 1.1f;
            Projectile.aiStyle = 20;
            Projectile.timeLeft = 3600;
            Projectile.hide = true;
            Projectile.ownerHitCheck = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 6;
        }

        public override void AI()
        {
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, -Projectile.velocity.X / 4, -Projectile.velocity.Y / 4);
            base.AI();
        }

        public override void PostDraw(Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.HeavenPiercerGlowmask];

            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;

            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
            base.PostDraw(lightColor);
        }
    }
}
