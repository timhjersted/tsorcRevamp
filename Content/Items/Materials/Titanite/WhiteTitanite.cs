using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace tsorcRevamp.Content.Items.Materials.Titanite;

public class WhiteTitanite : Titanite
{
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
        for (int i = 0; i < 4; i++)
        {
            Rotation += 0.01f;
            Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i + Rotation) * 5;
            spriteBatch.Draw(texture, position + offsetPositon, null, Color.Purple * 0.5f, 0, origin, scale, SpriteEffects.None, 0);

            offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i - Rotation) * 5;
            spriteBatch.Draw(texture, position + offsetPositon, null, Color.Purple * 0.5f, 0, origin, scale, SpriteEffects.None, 0);
        }
        return true;
    }
}