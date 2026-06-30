using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TerraUI.Objects;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

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

        // Filter for the Right-Click (2nd) slot: weapons (without their own alt-fire), throwables,
        // consumables, tools, and shields. Rejects passive-only accessories and alt-fire weapons.
        internal static bool RightClickSlotCondition(Item item)
        {
            if (item == null || item.IsAir)
            {
                return true; // allow emptying the slot
            }
            // Shield of Cthulhu is excluded from the 2nd slot: its signature is a dash, which vanilla only grants
            // from a real accessory slot (no eocDash ram window when it's merely held in the 2nd slot), so it would
            // show no shield and do no dash here. It belongs in an accessory slot. All other shields are fine.
            if (item.type == ItemID.EoCShield)
            {
                return false;
            }
            // Registered shields are allowed — but only one shield total at a time (active mode): reject if a
            // registered shield is already worn in an accessory slot.
            if (tsorcRevamp.ActiveShieldRegistry != null && tsorcRevamp.ActiveShieldRegistry.ContainsKey(item.type))
            {
                Player p = Main.LocalPlayer;
                if (tsorcRevampActiveShieldPlayer.ActiveFor(p))
                {
                    for (int i = 3; i < 8 + p.extraAccessorySlots; i++)
                    {
                        Item acc = p.armor[i];
                        if (acc != null && !acc.IsAir && tsorcRevamp.ActiveShieldRegistry.ContainsKey(acc.type))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            // Weapons that have their own right-click (alt) function belong in the hand, not here.
            if (ItemLoader.AltFunctionUse(item, Main.LocalPlayer))
            {
                return false;
            }
            // Allow usable items: weapons, throwables/consumables, tools.
            if (item.damage > 0 || item.consumable || item.pick > 0 || item.axe > 0 || item.hammer > 0)
            {
                return true;
            }
            if (item.useStyle != ItemUseStyleID.None && !item.accessory)
            {
                return true;
            }
            // Reject passive-only accessories and everything else.
            return false;
        }

        internal void DrawRightClickSlotBackground(UIObject sender, SpriteBatch spriteBatch)
        {
            ((UIItemSlot)sender).OnDrawBackground(spriteBatch);
        }
        internal void DrawSoulSlotBackground(UIObject sender, SpriteBatch spriteBatch)
        {
            UIItemSlot slot = (UIItemSlot)sender;

            if (ShouldDrawSoulSlot())
            {
                slot.OnDrawBackground(spriteBatch);

                if (slot.Item.stack == 0)
                {
                    Texture2D tex = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/UI/SoulSlotBackground");
                    // Center the placeholder icon on the slot: draw the texture's center point at the slot's
                    // center, scaled by the inventory scale. (Origin is in TEXTURE pixels, not scaled — the engine
                    // applies the scale. The old math scaled the origin and compensated with Rectangle.Size()/2,
                    // which silently broke when Rectangle became the scaled slot size.)
                    Vector2 origin = tex.Size() / 2f;
                    Vector2 slotCenter = slot.Rectangle.TopLeft() + (slot.Rectangle.Size() / 2f);

                    spriteBatch.Draw(
                        tex,
                        slotCenter,
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
        private void PositionInventorySlots(out bool drawRightClick)
        {
            int slotIndexX = 12;
            int slotIndexY = 0;
            int slotPosX = (int)(20f + (float)(slotIndexX * 56) * Main.inventoryScale);
            int slotPosY = (int)(20f + (float)(slotIndexY * 56) * Main.inventoryScale) + 18;

            SoulSlot.Position = new Vector2(slotPosX, slotPosY);
            drawRightClick = SoulsMode;

            // Each stacked slot now has its label ABOVE it, so the pitch = slot height + a gap for the label.
            int slotH = (int)(52 * Main.inventoryScale);
            int pitch = slotH + 16;
            int storagePosY;
            if (drawRightClick)
            {
                int rcPosY = slotPosY + pitch;
                RightClickSlot.Position = new Vector2(slotPosX, rcPosY);
                storagePosY = rcPosY + pitch;
            }
            else
            {
                // No 2nd slot in this mode — slide up directly under the souls slot.
                storagePosY = slotPosY + pitch;
            }

            StorageOpenerSlot.Position = new Vector2(slotPosX, storagePosY);
        }

        public void UpdateCustomInventorySlots()
        {
            if (!ShouldDrawSoulSlot())
            {
                return;
            }

            float origScale = Main.inventoryScale;
            Main.inventoryScale = 0.85f;

            PositionInventorySlots(out bool drawRightClick);

            SoulSlot.Update();
            if (drawRightClick)
            {
                RightClickSlot.Update();
            }
            StorageOpenerSlot.Update();

            Main.inventoryScale = origScale;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!ShouldDrawSoulSlot())
            {
                return;
            }

            float origScale = Main.inventoryScale;
            Main.inventoryScale = 0.85f;

            PositionInventorySlots(out bool drawRightClick);

            int slotPosX = (int)SoulSlot.Position.X;
            int slotPosY = (int)SoulSlot.Position.Y;
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, LangUtils.GetTextValue("UI.Souls"), new Vector2(slotPosX + 6f, slotPosY - 15), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, default, 0.75f, SpriteEffects.None, 0);

            SoulSlot.Draw(spriteBatch);

            // The Right-Click (2nd) slot sits directly below the souls slot, SoulsMode only.
            if (drawRightClick)
            {
                int rcPosY = (int)RightClickSlot.Position.Y;
                RightClickSlot.Draw(spriteBatch);
                // "2nd" label sits above the slot (like the "Souls" / "Storage" labels).
                DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, LangUtils.GetTextValue("UI.SecondSlot"), new Vector2(slotPosX + 6f, rcPosY - 15), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, default, 0.75f, SpriteEffects.None, 0);
            }

            // The Storage opener slot is always available; its "Storage" label sits above it (like "Souls").
            int storagePosY = (int)StorageOpenerSlot.Position.Y;
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, LangUtils.GetTextValue("UI.Storage"), new Vector2(slotPosX + 6f, storagePosY - 15), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, default, 0.75f, SpriteEffects.None, 0);
            StorageOpenerSlot.Draw(spriteBatch);

            Main.inventoryScale = origScale;

        }
    }
}
