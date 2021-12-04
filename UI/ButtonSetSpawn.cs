using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.UI
{
    class ButtonSetSpawn : UIElement
    {
        //Color color = new Color(50, 255, 153);

        static Texture2D texture = ModContent.GetTexture("tsorcRevamp/UI/ButtonSetSpawn");
        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, new Vector2(Main.screenWidth + 40, Main.screenHeight - 20) / 2f, default);
        }
    }
}