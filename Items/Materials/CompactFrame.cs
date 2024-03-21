using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class CompactFrame : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemNoGravity[Item.type] = true;
        }
        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i + rotation) * 2f;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.OrangeRed, 0, origin, scale, SpriteEffects.None, 0);

                offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i - rotation) * 2f;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.OrangeRed, 0, origin, scale, SpriteEffects.None, 0);
            }
            return true;
        }


        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 18;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = 350000;
            Item.rare = ItemRarityID.Red;
        }
    }
}
