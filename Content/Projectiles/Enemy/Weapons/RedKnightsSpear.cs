using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    class RedKnightsSpear : BlackKnightSpear
    {
        internal const float SpriteSize = 64f;

        // Match the old 61px shaft between tip and butt to the new 63px diagonal.
        internal const float ShortSpearScale = 61f / (63f * 1.41421356f);
        internal static string SpritePath => UsefulFunctions.RefactorableFilepath(typeof(RedKnightsSpear));

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.scale *= ShortSpearScale;
        }

        public override void AI()
        {
            // The sprite points up-left, so rotate its native diagonal onto the flight direction.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.PiOver4;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);
            DrawSpear(Projectile, lightColor, Projectile.rotation);
            return false;
        }

        internal static void DrawSpear(Projectile projectile, Color lightColor, float rotation)
        {
            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            drawPosition.Y += projectile.gfxOffY;
            Vector2 origin = texture.Size() * 0.5f;
            Color drawColor = projectile.GetAlpha(lightColor);

            // Rotation aims the diagonal in either direction. Flipping would turn the point off-axis.
            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, rotation,
                origin, projectile.scale, SpriteEffects.None, 0f);
        }
    }
}


