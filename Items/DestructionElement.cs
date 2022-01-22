using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class DestructionElement : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Used for making high damage guns\n" +
                               "Guarded by entities of living cosmic metal, it's warm to the touch...");
        }

        public override void SetDefaults() {
            item.width = 32;
            item.height = 32;
            item.rare = ItemRarityID.Red;
            item.value = 350000;
            item.maxStack = 4;
        }

        float rotation = 0;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = Main.itemTexture[item.type];
            for (int i = 0; i < 4; i++)
            {
                rotation += 0.01f;
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy((MathHelper.PiOver2 * i) + rotation) * 2f;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Red, 0, origin, scale, SpriteEffects.None, 0f);

                offsetPositon = Vector2.UnitY.RotatedBy((MathHelper.PiOver2 * i) - rotation) * 2f;
                spriteBatch.Draw(texture, position + offsetPositon, null, Color.Red, 0, origin, scale, SpriteEffects.None, 0f);
            }
            return true;
        }
    }
}
