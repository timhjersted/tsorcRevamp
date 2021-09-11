using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TerraUI.Utilities {
    public static class DrawingUtils {
        public static void DrawRectangleBox(SpriteBatch spriteBatch, Color borderColour, Color backColour, Rectangle rect, int borderWidth) {
            Texture2D texture = UIUtils.GetTexture("1x1");

            spriteBatch.Draw(texture, new Rectangle(rect.X + borderWidth, rect.Y + borderWidth, rect.Width - (borderWidth * 2), rect.Height - (borderWidth * 2)), backColour);
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, borderWidth), new Rectangle(0, 0, 0, 0), borderColour);
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, borderWidth, rect.Height), new Rectangle(0, 0, 0, 0), borderColour);
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y + rect.Height - borderWidth, rect.Width, borderWidth), new Rectangle(0, 0, 0, 0), borderColour);
            spriteBatch.Draw(texture, new Rectangle(rect.X + rect.Width - borderWidth, rect.Y, borderWidth, rect.Height), new Rectangle(0, 0, 0, 0), borderColour);
        }

        public static void DrawTerrariaStyledBox(SpriteBatch spriteBatch, Color colour, Rectangle rect, bool solid = false) {
            string add = "";

            if(solid) {
                add = "Solid";
            }

            string corner = "Corner" + add;
            string side = "Side" + add;
            string background = "Background" + add;

            spriteBatch.Draw(UIUtils.GetTexture(corner), new Vector2(rect.X, rect.Y), colour);
            spriteBatch.Draw(UIUtils.GetTexture(corner), new Vector2(rect.X + rect.Width, rect.Y), null, colour, (float)(Math.PI / 2), default(Vector2), 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(UIUtils.GetTexture(corner), new Vector2(rect.X + rect.Width, rect.Y + rect.Height), null, colour, (float)(Math.PI), default(Vector2), 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(UIUtils.GetTexture(corner), new Vector2(rect.X, rect.Y + rect.Height), null, colour, (float)(Math.PI * 1.5), default(Vector2), 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(UIUtils.GetTexture(side), new Rectangle(rect.X + 16, rect.Y, rect.Width - 32, 16), colour);
            spriteBatch.Draw(UIUtils.GetTexture(side), new Rectangle(rect.X + rect.Width, rect.Y + 16, rect.Height - 32, 16), null, colour, (float)(Math.PI / 2), default(Vector2), SpriteEffects.None, 0f);
            spriteBatch.Draw(UIUtils.GetTexture(side), new Rectangle(rect.X + rect.Width - 16, rect.Y + rect.Height, rect.Width - 32, 16), null, colour, (float)(Math.PI), default(Vector2), SpriteEffects.None, 0f);
            spriteBatch.Draw(UIUtils.GetTexture(side), new Rectangle(rect.X, rect.Y + rect.Height - 16, rect.Height - 32, 16), null, colour, (float)(Math.PI * 1.5), default(Vector2), SpriteEffects.None, 0f);
            spriteBatch.Draw(UIUtils.GetTexture(background), new Rectangle(rect.X + 16, rect.Y + 16, rect.Width - 32, rect.Height - 32), null, colour);
        }
    }
}
