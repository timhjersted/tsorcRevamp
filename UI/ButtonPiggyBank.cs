using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace tsorcRevamp.UI;

class ButtonPiggyBank : UIElement
{
    //Color color = new Color(50, 255, 153);

    static Texture2D texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/UI/ButtonPiggyBank");
    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(texture, new Vector2(Main.screenWidth + 90, Main.screenHeight - 20) / 2f, default);
    }
}