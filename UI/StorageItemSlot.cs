using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace tsorcRevamp.UI
{
    // One of exactly 30 live slots in the Storage grid. It owns no item of its own — each frame it re-binds
    // to a window into the cached filtered view (view[ScrollOffset*COLUMNS + index]) and reconciles any click
    // back into the player's StorageItems list. Storage size has zero effect on the number of UI elements.
    internal class StorageItemSlot : UIElement
    {
        private readonly int _context;
        private readonly float _scale;
        public int index;

        // Reused scratch item for empty slots so we don't allocate one every frame.
        private Item airItem = new Item();

        public StorageItemSlot(int index, int context = ItemSlot.Context.InventoryItem, float scale = 0.85f)
        {
            this.index = index;
            _context = context;
            _scale = scale;
            Width.Set(TextureAssets.InventoryBack9.Value.Width * scale, 0f);
            Height.Set(TextureAssets.InventoryBack9.Value.Height * scale, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            StorageUIState ui = StorageUIState.Instance;
            if (ui == null) return;

            int viewIdx = StorageUIState.ScrollOffset * StorageUIState.COLUMNS + index;
            bool hasItem = viewIdx >= 0 && viewIdx < ui.CurrentView.Count;
            int source = hasItem ? ui.ViewSource[viewIdx] : -1;

            // Bind: existing entries use the live reference from the view; empty slots use the scratch air item.
            Item working;
            if (hasItem)
            {
                working = ui.CurrentView[viewIdx];
            }
            else
            {
                airItem.TurnToAir();
                working = airItem;
            }

            float oldScale = Main.inventoryScale;
            Main.inventoryScale = _scale;
            Rectangle rectangle = GetDimensions().ToRectangle();

            if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface && !StorageUIState.DraggingWindow)
            {
                Main.LocalPlayer.mouseInterface = true;

                // Shift-click a storage slot -> withdraw straight to inventory (mirrors shift-clicking an
                // inventory item to deposit it). Handled entirely ourselves, BEFORE ItemSlot.Handle: the
                // single-item overload it uses hands our own ShiftClickSlot hook a shared scratch array
                // (ItemSlot.singleSlotArray) instead of the real inventory, so vanilla's shift-click plumbing
                // can't tell this slot apart from a real one — see tsorcRevampPlayerMain.ShiftClickSlot.
                if (hasItem && Main.mouseLeft && Main.mouseLeftRelease && ItemSlot.ShiftInUse)
                {
                    tsorcRevampPlayer mp = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
                    if (mp.WithdrawToInventory(working))
                    {
                        ui.WriteBack(source, working);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                    }
                }
                else
                {
                    int beforeType = working.type;
                    int beforeStack = working.stack;

                    // Vanilla handles left/right click, stack splitting, etc. for free.
                    ItemSlot.Handle(ref working, _context);

                    // Reconcile only when an interaction actually changed the slot (avoids per-hover rebuilds).
                    if (working.type != beforeType || working.stack != beforeStack)
                    {
                        ui.WriteBack(source, working);

                        // For a manual deposit into an empty slot, WriteBack cloned the item into storage — clear
                        // the scratch item so it isn't counted again next frame.
                        if (source < 0)
                        {
                            working.TurnToAir();
                        }
                    }
                }
            }

            // Draw the slot manually so we fully control overlays — vanilla ItemSlot.Draw was stamping a stray
            // "1" on the slots; here the stack count is only ever drawn when it's actually > 1.
            DrawSlot(spriteBatch, working, rectangle);

            Main.inventoryScale = oldScale;
        }

        private void DrawSlot(SpriteBatch spriteBatch, Item working, Rectangle rectangle)
        {
            Texture2D backTex = TextureAssets.InventoryBack9.Value;
            Color backColor = Color.White * 0.85f;
            if (ContainsPoint(Main.MouseScreen))
            {
                backColor = Color.White; // subtle hover highlight
            }
            spriteBatch.Draw(backTex, rectangle, backColor);

            if (working == null || working.IsAir) return;

            // Force the (lazily-loaded) item texture to load before we read it — otherwise items not yet seen
            // this session (e.g. everything in storage right after a reload) render as blank sprites.
            Main.instance.LoadItem(working.type);
            Texture2D itemTex = TextureAssets.Item[working.type].Value;
            Rectangle frame = (Main.itemAnimations[working.type] != null)
                ? Main.itemAnimations[working.type].GetFrame(itemTex)
                : itemTex.Frame(1, 1, 0, 0);

            // Fit-scale oversized sprites down so they don't overflow the slot (mirrors vanilla ItemSlot).
            float drawScale = 1f;
            if (frame.Width > 32 || frame.Height > 32)
            {
                drawScale = (frame.Width > frame.Height) ? 32f / frame.Width : 32f / frame.Height;
            }
            drawScale *= _scale;

            Vector2 origin = frame.Size() / 2f;
            Vector2 center = rectangle.Center.ToVector2();
            Color itemColor = working.GetAlpha(Color.White);

            spriteBatch.Draw(itemTex, center, frame, itemColor, 0f, origin, drawScale, SpriteEffects.None, 0f);

            if (working.stack > 1)
            {
                Vector2 stackPos = rectangle.TopLeft() + new Vector2(8f, 26f) * _scale;
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value,
                    working.stack.ToString(), stackPos, Color.White, 0f, Vector2.Zero, new Vector2(_scale), -1f, _scale);
            }
        }
    }
}
