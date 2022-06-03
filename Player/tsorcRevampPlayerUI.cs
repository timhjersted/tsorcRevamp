using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using TerraUI.Objects;
using tsorcRevamp.Items;

namespace tsorcRevamp
{
    //UI
    public partial class tsorcRevampPlayer
    {
        internal static bool SoulSlotCondition(Item item)
        {
            if (item.type != ModContent.ItemType<DarkSoul>())
            {
                return false;
            }
            return true;
        }
        internal void DrawSoulSlotBackground(UIObject sender, SpriteBatch spriteBatch)
        {
            UIItemSlot slot = (UIItemSlot)sender;

            if (ShouldDrawSoulSlot())
            {
                slot.OnDrawBackground(spriteBatch);

                if (slot.Item.stack == 0)
                {
                    Texture2D tex = (Texture2D)ModContent.Request<Texture2D>("UI/SoulSlotBackground");
                    Vector2 origin = tex.Size() / 2f * Main.inventoryScale;
                    Vector2 position = slot.Rectangle.TopLeft();

                    spriteBatch.Draw(
                        tex,
                        position + (slot.Rectangle.Size() / 2f) - (origin / 2f),
                        null,
                        Color.White * 0.35f,
                        0f,
                        origin,
                        Main.inventoryScale,
                        SpriteEffects.None,
                        0f); // layer depth 0 = front
                }
            }
        }
        internal static bool ShouldDrawSoulSlot()
        {
            return (Main.playerInventory);
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!ShouldDrawSoulSlot())
            {
                return;
            }

            float origScale = Main.inventoryScale;
            Main.inventoryScale = 0.85f;

            int slotIndexX = 11;
            int slotIndexY = 0;
            int slotPosX = (int)(20f + (float)(slotIndexX * 56) * Main.inventoryScale);
            int slotPosY = (int)(20f + (float)(slotIndexY * 56) * Main.inventoryScale) + 18;

            SoulSlot.Position = new Vector2(slotPosX, slotPosY);
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, "Souls", new Vector2(slotPosX + 6f, slotPosY - 15), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, default, 0.75f, SpriteEffects.None, 0);

            SoulSlot.Draw(spriteBatch);

            Main.inventoryScale = origScale;

            SoulSlot.Update();

        }
    }
}
