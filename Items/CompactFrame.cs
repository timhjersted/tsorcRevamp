using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class CompactFrame : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Used for making advanced weaponry, from a civilization not of this world\n" +
                               "The strange way it is affected by gravity reminds you of somewhere warm...");

            ItemID.Sets.ItemNoGravity[item.type] = true;
        }
        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = Main.itemTexture[item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy((MathHelper.PiOver2 * i) + rotation) * 2f;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.OrangeRed , 0, origin, scale, SpriteEffects.None, 0f);

                offsetPositon = Vector2.UnitY.RotatedBy((MathHelper.PiOver2 * i) - rotation) * 2f;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.OrangeRed, 0, origin, scale, SpriteEffects.None, 0f);
            }
            return true;
        }


        public override void SetDefaults() {
            item.width = 20;
            item.height = 18;
            item.maxStack = 4;
            item.value = 350000;
            item.rare = ItemRarityID.Red;
        }
    }
}
