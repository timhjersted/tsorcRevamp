using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.UI
{
    class ButtonPiggyBank : UIElement
    {
        //Color color = new Color(50, 255, 153);

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(ModContent.GetTexture("tsorcRevamp/UI/ButtonPiggyBank"), new Vector2(Main.screenWidth + 90, Main.screenHeight - 20) / 2f, default);
        }
    }
}