using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class OolacileDarkMatter : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 24;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.sellPrice(0, 1, 80, 0);
            Item.rare = ItemRarityID.Purple;
        }

        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i + rotation) * 4;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Gray * 0.5f, 0, origin, scale, SpriteEffects.None, 0);

                offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i - rotation) * 4;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Gray * 0.5f, 0, origin, scale, SpriteEffects.None, 0);
            }
            return true;
        }
    }
}
