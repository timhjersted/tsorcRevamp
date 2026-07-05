using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using TerraUI.Objects;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.UI
{
    // A decorative inventory slot that is really a button: it never holds an item, rejects all item placement,
    // and toggles the Storage pop-up when left-clicked. It mirrors the souls slot's manual draw/update flow
    // (see tsorcRevampPlayerUI) but carries no data — no save/load, no netcode, no leak guards needed because
    // there is nothing in the slot to protect.
    public class StorageOpenerSlot : UIItemSlot
    {
        // Self-tracked press edge, same fix as StorageUIState's header drag: the inherited UIItemSlot.Update()
        // detects clicks via TerraUI's MouseUtils.JustPressed, which keys off PlayerInput.MouseInfoOld/MouseInfo
        // (refreshed at a different point in the frame than our custom interface layer runs) — that made this
        // button miss most clicks. Main.mouseLeft/!prevMouseLeft is the reliable primitive we already use
        // elsewhere in this mod.
        private bool prevMouseLeft;

        public StorageOpenerSlot(int size = 52)
            : base(Vector2.Zero, size, ItemSlot.Context.InventoryItem,
                   LangUtils.GetTextValue("UI.OpenStorage"), null, null, null, null, null, false, true)
        {
            BackOpacity = 0.8f;
            // Never allow anything to occupy or be pulled from the slot.
            DisallowManualRemoval = true;
        }

        public override void Update()
        {
            UpdateRectangle();

            bool hovered = Rectangle.Contains(Main.mouseX, Main.mouseY);
            if (hovered)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.HoverItem = Item;
                Main.hoverItemName = HoverText;

                if (Main.mouseLeft && !prevMouseLeft)
                {
                    OnLeftClick();
                }
            }

            prevMouseLeft = Main.mouseLeft;
        }

        // Left click opens/closes Storage instead of doing any item swap.
        public override void OnLeftClick()
        {
            tsorcRevampPlayer.ToggleStorage();
        }

        // No right-click item interaction.
        public override void OnRightClick() { }

        public override void Draw(SpriteBatch spriteBatch)
        {
            UpdateRectangle();
            OnDrawBackground(spriteBatch);

            // Placeholder icon: a vanilla chest. Swap for a custom "Storage" sprite later.
            // LoadItem first so the (lazily-loaded) chest texture is ready, otherwise the slot draws blank.
            Main.instance.LoadItem(ItemID.Chest);
            Texture2D tex = TextureAssets.Item[ItemID.Chest].Value;
            Rectangle frame = tex.Frame(1, 1, 0, 0);
            Vector2 origin = frame.Size() / 2f;
            spriteBatch.Draw(tex, Rectangle.Center.ToVector2(), frame, Color.White, 0f, origin,
                Scale(true), SpriteEffects.None, 0f);
        }
    }
}
