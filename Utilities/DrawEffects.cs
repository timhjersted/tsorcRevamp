using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace tsorcRevamp.Utilities
{
    public static class DrawEffects
    {
        public static void DrawGreatMagicMirrorRing(SpriteBatch spriteBatch, Vector2 center, float radius)
        {
            float pulse = 0.75f + 0.2f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 3f);
            float slowRotation = Main.GlobalTimeWrappedHourly * 0.18f;

            DrawCircleDots(spriteBatch, center, radius, new Color(35, 255, 145, 45) * pulse, 360, 7f, slowRotation);
            DrawCircleDots(spriteBatch, center, radius, new Color(180, 255, 210, 170) * pulse, 720, 2f, -slowRotation);
            DrawCircleDots(spriteBatch, center, radius - 12f, new Color(80, 210, 170, 80) * pulse, 360, 2f, slowRotation * 1.5f);
            DrawCircleDots(spriteBatch, center, radius + 12f, new Color(80, 210, 170, 60) * pulse, 360, 2f, -slowRotation * 1.5f);

            DrawCircleDots(spriteBatch, center, 32f + 4f * pulse, new Color(180, 255, 220, 130) * pulse, 64, 3f, -slowRotation * 2f);
            DrawCircleDots(spriteBatch, center, 18f, new Color(230, 255, 240, 160) * pulse, 40, 2f, slowRotation * 2f);
            DrawCardinalTicks(spriteBatch, center, 42f, new Color(180, 255, 220, 120) * pulse, slowRotation);
        }

        public static void DrawRadiantBurstCircle(SpriteBatch spriteBatch, Vector2 center, float radius)
        {
            float pulse = 0.65f + 0.25f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 3f);
            Color ringColor = new Color(70, 255, 130, 120) * pulse;
            Color markerColor = new Color(170, 255, 190, 170) * pulse;
            float rotationOffset = Main.GlobalTimeWrappedHourly * 0.12f;

            DrawCircleLine(spriteBatch, center, radius, ringColor, 2f, 128, rotationOffset, true);
            DrawCircleLine(spriteBatch, center, 28f + 4f * pulse, markerColor, 2f, 48, -rotationOffset * 2f, false);
        }

        private static void DrawCircleDots(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments, float size, float rotationOffset)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 origin = new Vector2(0.5f);

            for (int i = 0; i < segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments + rotationOffset;
                Vector2 position = center + angle.ToRotationVector2() * radius - Main.screenPosition;
                spriteBatch.Draw(pixel, position, null, color, 0f, origin, size, SpriteEffects.None, 0f);
            }
        }

        private static void DrawCardinalTicks(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, float rotationOffset)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.PiOver2 * i + rotationOffset;
                Vector2 inner = center + angle.ToRotationVector2() * (radius - 10f);
                Vector2 outer = center + angle.ToRotationVector2() * (radius + 10f);
                DrawWorldLine(spriteBatch, inner, outer, color, 2f);
            }
        }

        private static void DrawCircleLine(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, float thickness, int segments, float rotationOffset, bool dashed)
        {
            for (int i = 0; i < segments; i++)
            {
                if (dashed && i % 3 == 2)
                {
                    continue;
                }

                float startAngle = MathHelper.TwoPi * i / segments + rotationOffset;
                float endAngle = MathHelper.TwoPi * (i + 1) / segments + rotationOffset;
                Vector2 start = center + startAngle.ToRotationVector2() * radius;
                Vector2 end = center + endAngle.ToRotationVector2() * radius;
                DrawWorldLine(spriteBatch, start, end, color, thickness);
            }
        }

        private static void DrawWorldLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 segment = end - start;
            spriteBatch.Draw(pixel, start - Main.screenPosition, null, color, segment.ToRotation(), new Vector2(0f, 0.5f), new Vector2(segment.Length(), thickness), SpriteEffects.None, 0f);
        }
    }
}
