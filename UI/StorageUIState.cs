using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace tsorcRevamp.UI
{
    // The Storage pop-up: search bar, tab row, and a virtualized 6x5 (30-slot) grid with a scrollbar and
    // scroll-wheel paging. Tabs are non-exclusive filters over the single backing list (an item shows in every
    // tab it matches); the filtered "view" is cached and only rebuilt when the tab, search text, or storage
    // contents change. The header can be dragged to move the window; its position persists per-player.
    class StorageUIState : UIState
    {
        public static StorageUIState Instance;

        public const int COLUMNS = 6;
        public const int ROWS = 5;
        public const int VISIBLE_SLOTS = COLUMNS * ROWS; // 30
        public const int NEW_TAB_LIMIT = 60;             // "New" tracks the last 60 deposits

        private const float SlotSpacing = 48f;
        private const float HeaderHeight = 28f;

        public static bool Visible = false;
        // When the inventory closes while storage was open, this stays true so storage reopens on next inventory open.
        // Cleared when the player explicitly closes storage (X button, keybind, or toggle) so that intentional closes
        // are not undone by simply reopening the inventory.
        public static bool ReopenWithInventory = false;
        public static int ActiveTab = (int)Tab.New;
        public static int ScrollOffset = 0; // measured in rows
        public static string SearchText = "";
        private static bool dirty = true;

        public enum Tab { New, All, Weapons, Armor, Accessories, Materials, Ammo, Misc }
        private static readonly Tab[] TabOrder =
        {
            Tab.New, Tab.All, Tab.Weapons, Tab.Armor, Tab.Accessories, Tab.Materials, Tab.Ammo, Tab.Misc
        };

        public UIPanel panel;
        private UIEnemySearchBar searchBar;
        private StorageItemSlot depositSlot;
        private UIScrollbar scrollbar;
        private UIText closeButton;
        private readonly StorageItemSlot[] slots = new StorageItemSlot[VISIBLE_SLOTS];
        private readonly List<UIText> tabButtons = new List<UIText>();
        private UIText pageText;

        // Cached filtered view + parallel mapping back to StorageItems indices. Held on the singleton so the
        // slots can reach them via Instance.
        public List<Item> CurrentView = new List<Item>();
        public List<int> ViewSource = new List<int>();

        // Header drag state.
        private bool dragging;
        private Vector2 dragOffset;
        private bool prevMouseLeft; // self-tracked so we detect the press edge without relying on Main.mouseLeftRelease
        public static bool DraggingWindow = false;

        public static void MarkDirty() => dirty = true;

        public static void OpenToNewTab()
        {
            Visible = true;
            ActiveTab = (int)Tab.New;
            ScrollOffset = 0;
            MarkDirty();
            Instance?.ApplySavedPosition();
        }

        public override void OnInitialize()
        {
            Instance = this;

            panel = new UIPanel();
            panel.SetPadding(0);
            panel.Left.Set(-660f, 1f); // default: anchored near the right edge (user-shifted 300px left of original)
            panel.Top.Set(120f, 0f);
            panel.Width.Set(336f, 0f);
            panel.Height.Set(390f, 0f);
            panel.BackgroundColor = new Color(30, 30, 40) * 0.95f; // match the Quick Add / event menu look
            panel.OnUpdate += (UIElement el) =>
            {
                if (el.ContainsPoint(Main.MouseScreen))
                {
                    Main.LocalPlayer.mouseInterface = true;
                }
            };
            Append(panel);

            UIText title = new UIText("STORAGE", 0.9f);
            title.Left.Set(12, 0);
            title.Top.Set(8, 0);
            title.TextColor = new Color(255, 204, 0);
            panel.Append(title);

            closeButton = new UIText("X", 0.9f);
            closeButton.Left.Set(312, 0);
            closeButton.Top.Set(8, 0);
            closeButton.TextColor = Color.White;
            closeButton.OnMouseOver += (evt, el) => { closeButton.TextColor = Color.Red; };
            closeButton.OnMouseOut += (evt, el) => { closeButton.TextColor = Color.White; };
            closeButton.OnLeftClick += (evt, el) =>
            {
                Visible = false;
                ReopenWithInventory = false; // explicit close — don't reopen with next inventory open
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose);
            };
            panel.Append(closeButton);

            // Search bar (reuses the Enemy Debug Tome search field).
            searchBar = new UIEnemySearchBar();
            searchBar.Left.Set(12, 0);
            searchBar.Top.Set(32, 0);
            searchBar.Width.Set(260, 0); // narrowed to make room for the deposit slot on the same row
            searchBar.Height.Set(28, 0);
            searchBar.BackgroundColor = new Color(40, 40, 50) * 0.95f;
            searchBar.OnTextChanged += () =>
            {
                SearchText = searchBar.Text;
                ScrollOffset = 0;
                if (scrollbar != null) scrollbar.ViewPosition = 0;
                MarkDirty();
            };
            panel.Append(searchBar);

            // Always-empty deposit slot on the search row: drag an item onto it to store it without scrolling
            // to the first free cell at the end of the grid. Index < 0 is what keeps it unbound from the view
            // (see StorageItemSlot), so the 6x5 grid below stays a full 30 cells with no index shifting.
            depositSlot = new StorageItemSlot(-1, ItemSlot.Context.InventoryItem, 0.85f);
            depositSlot.Left.Set(280, 0);
            depositSlot.Top.Set(30, 0);
            depositSlot.Width.Set(44, 0);
            depositSlot.Height.Set(44, 0);
            panel.Append(depositSlot);

            // Tab row — flowed left-to-right, wrapping within the panel width.
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            const float tabScale = 0.8f;
            float tx = 12f, ty = 80f; // below the deposit slot (y 30-74), not just below the search bar
            const float rowHeight = 22f;
            const float gap = 10f;
            const float maxX = 324f;
            for (int i = 0; i < TabOrder.Length; i++)
            {
                string label = TabOrder[i].ToString();
                float w = font.MeasureString(label).X * tabScale;
                if (tx + w > maxX)
                {
                    tx = 12f;
                    ty += rowHeight;
                }

                int tabIndex = i;
                UIText tab = new UIText(label, tabScale);
                tab.Left.Set(tx, 0);
                tab.Top.Set(ty, 0);
                tab.OnMouseOver += (evt, el) => { if (tabIndex != ActiveTab) ((UIText)el).TextColor = Color.White; };
                tab.OnMouseOut += (evt, el) => { RefreshTabVisuals(); };
                tab.OnLeftClick += (evt, el) =>
                {
                    ActiveTab = tabIndex;
                    ScrollOffset = 0;
                    if (scrollbar != null) scrollbar.ViewPosition = 0;
                    MarkDirty();
                    RefreshTabVisuals();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                };
                tabButtons.Add(tab);
                panel.Append(tab);

                tx += w + gap;
            }

            // Grid: 30 fixed slots below the (possibly two-row) tab strip.
            float gridTop = ty + rowHeight + 8f;
            for (int i = 0; i < VISIBLE_SLOTS; i++)
            {
                int col = i % COLUMNS;
                int row = i / COLUMNS;
                StorageItemSlot slot = new StorageItemSlot(i, ItemSlot.Context.InventoryItem, 0.85f);
                slot.Left.Set(14 + col * SlotSpacing, 0);
                slot.Top.Set(gridTop + row * SlotSpacing, 0);
                slot.Width.Set(44, 0);
                slot.Height.Set(44, 0);
                slots[i] = slot;
                panel.Append(slot);
            }

            // Scrollbar to the right of the 6th column.
            float gridRight = 14 + COLUMNS * SlotSpacing; // right edge of the grid region
            scrollbar = new UIScrollbar();
            scrollbar.Left.Set(gridRight - 2f, 0);
            scrollbar.Top.Set(gridTop, 0);
            scrollbar.Width.Set(20, 0);
            scrollbar.Height.Set(ROWS * SlotSpacing - 4f, 0);
            scrollbar.SetView(ROWS, ROWS);
            panel.Append(scrollbar);

            float bottom = gridTop + ROWS * SlotSpacing;
            pageText = new UIText("", 0.7f);
            pageText.Left.Set(14, 0);
            pageText.Top.Set(bottom + 2f, 0);
            pageText.TextColor = Color.Gray;
            panel.Append(pageText);

            panel.Height.Set(bottom + 24f, 0);
            RefreshTabVisuals();
        }

        // Apply the per-player saved window position (or the default anchored position if never moved).
        public void ApplySavedPosition()
        {
            if (panel == null) return;
            tsorcRevampPlayer mp = Main.LocalPlayer?.GetModPlayer<tsorcRevampPlayer>();
            if (mp != null && mp.StorageWindowPos.X >= 0 && mp.StorageWindowPos.Y >= 0)
            {
                Vector2 p = ClampToScreen(mp.StorageWindowPos);
                panel.Left.Set(p.X, 0f);
                panel.Top.Set(p.Y, 0f);
            }
            else
            {
                panel.Left.Set(-660f, 1f);
                panel.Top.Set(120f, 0f);
            }
            panel.Recalculate();
        }

        private Vector2 ClampToScreen(Vector2 pos)
        {
            float w = panel.Width.Pixels;
            float h = panel.Height.Pixels;
            pos.X = MathHelper.Clamp(pos.X, 0, Math.Max(0, Main.screenWidth - w));
            pos.Y = MathHelper.Clamp(pos.Y, 0, Math.Max(0, Main.screenHeight - h));
            return pos;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Auto-close with the inventory. Remember the open state so storage can reopen next time
            // the inventory opens — but only if the inventory closing is what caused the close. Explicit
            // closes (X button, keybind) clear ReopenWithInventory so they aren't undone.
            if (!Main.playerInventory)
            {
                if (Visible) ReopenWithInventory = true;
                Visible = false;
            }
            if (!Visible)
            {
                dragging = false;
                prevMouseLeft = Main.mouseLeft;
                // Release the search field so it doesn't leave text input / blockInput stuck on (see UIEnemySearchBar).
                if (searchBar != null && searchBar.Focused)
                {
                    searchBar.Focused = false;
                    Main.blockInput = false;
                }
                return;
            }

            if (dirty) RebuildView();

            HandleHeaderDrag();

            // Keep the scrollbar in sync with the current row count, then derive the row offset from it.
            int totalRows = (CurrentView.Count + COLUMNS - 1) / COLUMNS;
            scrollbar.SetView(ROWS, Math.Max(totalRows, ROWS));

            if (panel != null && panel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;

                int scrollDelta = PlayerInput.ScrollWheelDeltaForUI;
                if (scrollDelta != 0)
                {
                    scrollbar.ViewPosition -= Math.Sign(scrollDelta);
                }
            }

            ScrollOffset = (int)Math.Round(scrollbar.ViewPosition);
            ClampScroll();

            prevMouseLeft = Main.mouseLeft;
        }

        private void HandleHeaderDrag()
        {
            CalculatedStyle dims = panel.GetDimensions();
            Rectangle header = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)HeaderHeight);
            bool overHeader = header.Contains(Main.MouseScreen.ToPoint());
            bool overClose = closeButton != null && closeButton.GetDimensions().ToRectangle().Contains(Main.MouseScreen.ToPoint());

            // Detect the press edge ourselves (Main.mouseLeft now, not last frame) so the whole header is grabbable
            // — relying on Main.mouseLeftRelease failed because the UI system consumes it on clicks over children.
            bool justPressed = Main.mouseLeft && !prevMouseLeft;
            if (!dragging && overHeader && !overClose && justPressed)
            {
                dragging = true;
                dragOffset = Main.MouseScreen - new Vector2(dims.X, dims.Y);
                Main.mouseLeftRelease = false; // consume the click so nothing beneath the header reacts
            }

            DraggingWindow = dragging;

            if (dragging)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (!Main.mouseLeft)
                {
                    dragging = false;
                    DraggingWindow = false;
                }
                else
                {
                    Vector2 newPos = ClampToScreen(Main.MouseScreen - dragOffset);
                    panel.Left.Set(newPos.X, 0f);
                    panel.Top.Set(newPos.Y, 0f);
                    panel.Recalculate();

                    tsorcRevampPlayer mp = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
                    mp.StorageWindowPos = newPos;
                }
            }
        }

        private void ClampScroll()
        {
            int totalRows = (CurrentView.Count + COLUMNS - 1) / COLUMNS;
            int maxRowOffset = Math.Max(0, totalRows - ROWS);
            if (ScrollOffset > maxRowOffset) ScrollOffset = maxRowOffset;
            if (ScrollOffset < 0) ScrollOffset = 0;

            if (pageText != null)
            {
                pageText.SetText(CurrentView.Count > VISIBLE_SLOTS
                    ? $"Row {ScrollOffset + 1}-{Math.Min(ScrollOffset + ROWS, totalRows)} / {totalRows}  ({CurrentView.Count} items)"
                    : $"{CurrentView.Count} items");
            }
        }

        public void RebuildView()
        {
            CurrentView.Clear();
            ViewSource.Clear();

            tsorcRevampPlayer mp = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
            mp.CompactStorage();

            string search = (SearchText ?? "").ToLowerInvariant();
            Tab tab = (Tab)ActiveTab;

            List<int> indices = new List<int>();
            for (int i = 0; i < mp.StorageItems.Count; i++)
            {
                Item it = mp.StorageItems[i];
                if (it == null || it.IsAir) continue;
                if (!TabMatches(tab, it)) continue;
                if (search.Length > 0 && !it.Name.ToLowerInvariant().Contains(search)) continue;
                indices.Add(i);
            }

            if (tab == Tab.New)
            {
                // Most-recently-deposited first, capped at 60.
                indices.Sort((a, b) => mp.StorageSeq[b].CompareTo(mp.StorageSeq[a]));
                if (indices.Count > NEW_TAB_LIMIT) indices = indices.GetRange(0, NEW_TAB_LIMIT);
            }

            foreach (int idx in indices)
            {
                CurrentView.Add(mp.StorageItems[idx]);
                ViewSource.Add(idx);
            }

            ClampScroll();
            dirty = false;
        }

        // Reconcile a slot click back into the player's StorageItems. `source` is the StorageItems index the
        // slot was bound to, or < 0 for a manual deposit into an empty slot.
        public void WriteBack(int source, Item newItem)
        {
            tsorcRevampPlayer mp = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();

            if (source >= 0 && source < mp.StorageItems.Count)
            {
                // Withdraw that empties the entry -> leave an air hole; CompactStorage drops it on rebuild.
                mp.StorageItems[source] = (newItem == null || newItem.IsAir) ? new Item() : newItem;
            }
            else if (newItem != null && !newItem.IsAir)
            {
                // Manual deposit: clone (the slot reuses its scratch item) and stamp a fresh sequence.
                Item clone = newItem.Clone();
                clone.favorited = false;
                mp.StorageItems.Add(clone);
                mp.StorageSeq.Add(mp.NextSeq());
            }

            MarkDirty();
        }

        private void RefreshTabVisuals()
        {
            for (int i = 0; i < tabButtons.Count; i++)
            {
                tabButtons[i].TextColor = (i == ActiveTab) ? new Color(255, 204, 0) : Color.Gray;
            }
        }

        public static bool TabMatches(Tab tab, Item it)
        {
            switch (tab)
            {
                case Tab.New:
                case Tab.All:
                    return true;
                case Tab.Weapons:
                    return IsWeapon(it);
                case Tab.Armor:
                    return it.headSlot >= 0 || it.bodySlot >= 0 || it.legSlot >= 0 || (it.defense > 0 && !it.accessory);
                case Tab.Accessories:
                    return it.accessory;
                case Tab.Materials:
                    return it.material;
                case Tab.Ammo:
                    return it.ammo != AmmoID.None;
                case Tab.Misc:
                    return !(IsWeapon(it)
                        || it.headSlot >= 0 || it.bodySlot >= 0 || it.legSlot >= 0
                        || it.accessory || it.material || it.ammo != AmmoID.None);
                default:
                    return false;
            }
        }

        private static bool IsWeapon(Item it)
        {
            return it.damage > 0 && it.useStyle != ItemUseStyleID.None && !it.consumable && it.ammo == AmmoID.None;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible) return;
            base.Draw(spriteBatch);
        }

        public override bool ContainsPoint(Vector2 point)
        {
            return panel != null && panel.ContainsPoint(point);
        }
    }
}
