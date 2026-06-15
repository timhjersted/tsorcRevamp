using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    public class NPCAfterimage : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.timeLeft = 12;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int npcType = (int)Projectile.ai[0];
            if (npcType <= 0 || npcType >= TextureAssets.Npc.Length)
            {
                return false;
            }

            Texture2D texture = TextureAssets.Npc[npcType].Value;
            int frameCount = Main.npcFrameCount[npcType];
            int frameHeight = frameCount > 0 ? texture.Height / frameCount : texture.Height;
            int encodedFrame = (int)Projectile.ai[1];
            int frameY = System.Math.Abs(encodedFrame) - 1;
            if (frameY < 0 || frameY + frameHeight > texture.Height)
            {
                frameY = 0;
            }

            Rectangle sourceRectangle = new Rectangle(0, frameY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            float opacity = Projectile.timeLeft / 12f;
            SpriteEffects effects = encodedFrame < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Color color = Color.White * (0.45f * opacity);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, color, 0f, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}
