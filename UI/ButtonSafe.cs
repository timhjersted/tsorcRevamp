using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.UI
{
    class ButtonSafe : UIElement
    {
        //Color color = new Color(50, 255, 153);

        static Texture2D texture = ModContent.GetTexture("tsorcRevamp/UI/ButtonSafe");
        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, new Vector2(Main.screenWidth + 90, Main.screenHeight - 20) / 2f, default);
        }
    }
}