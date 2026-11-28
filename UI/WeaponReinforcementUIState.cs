using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Systems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.UI
{
    /// <summary>
    /// Bonfire weapon reinforcement window for iconic weapons (+0 to +10).
    /// Keeps the slotted weapon in place across consecutive upgrades and safely returns it on close or save-and-quit.
    /// </summary>
    public class WeaponReinforcementUIState : UIState
    {
        public static bool Visible;

        private const float PanelWidth = 520f;
        private const float PanelHeight = 292f;
        private const float HeaderDragHeight = 36f;

        private UIPanel panel;
        private UIText titleLabel;
        private ReinforcementWeaponSlot weaponSlot;
        private UIPanel reinforceButton;
        private UIText reinforceButtonLabel;
        private UIPanel closeButton;
        private string feedbackText = string.Empty;
        private int feedbackTimer;

        private bool dragging;
        private Vector2 dragOffset;
        private bool prevMouseLeft;

        public bool HasSlottedItem
        {
            get
            {
                return weaponSlot != null && weaponSlot.Item != null && !weaponSlot.Item.IsAir;
            }
        }

        public static void Open()
        {
            if (!WeaponReinforcement.Enabled)
            {
                return;
            }

            tsorcRevamp mod = ModContent.GetInstance<tsorcRevamp>();
            mod.WeaponReinforcementUI?.ResetDragAndClamp();

            Visible = true;
            Main.playerInventory = true;
            SoundEngine.PlaySound(SoundID.MenuOpen, Main.LocalPlayer.Center);
        }

        public static void Close(bool playSound = true, bool allowWorldDrop = true)
        {
            tsorcRevamp mod = ModContent.GetInstance<tsorcRevamp>();
            WeaponReinforcementUIState state = mod.WeaponReinforcementUI;

            if (state != null)
            {
                state.dragging = false;
                state.ReturnSlottedItem(playSound: playSound, allowWorldDrop: allowWorldDrop);
            }

            if (!Visible)
            {
                return;
            }

            Visible = false;
            if (playSound && !Main.dedServ && Main.LocalPlayer != null)
            {
                SoundEngine.PlaySound(SoundID.MenuClose, Main.LocalPlayer.Center);
            }
        }

        public static void Toggle()
        {
            if (Visible)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public override void OnInitialize()
        {
            panel = new UIPanel();
            panel.SetPadding(12f);
            float defaultLeft = ComputeDefaultPanelLeft();
            panel.Left.Set(defaultLeft, 0f);
            panel.Top.Set(120f, 0f);
            panel.Width.Set(PanelWidth, 0f);
            panel.Height.Set(PanelHeight, 0f);
            panel.BackgroundColor = new Color(36, 22, 22, 238);
            panel.BorderColor = new Color(112, 74, 52, 240);

            titleLabel = new UIText(LangUtils.GetTextValue("UI.WeaponReinforcement.Title"), 1.0f, false);
            titleLabel.Left.Set(4f, 0f);
            titleLabel.Top.Set(2f, 0f);
            titleLabel.TextColor = new Color(242, 206, 128);
            panel.Append(titleLabel);

            closeButton = new UIPanel();
            closeButton.Left.Set(PanelWidth - 52f, 0f);
            closeButton.Top.Set(-2f, 0f);
            closeButton.Width.Set(26f, 0f);
            closeButton.Height.Set(24f, 0f);
            closeButton.SetPadding(0f);
            closeButton.BackgroundColor = new Color(74, 34, 34, 230);
            closeButton.BorderColor = new Color(138, 86, 66, 240);
            closeButton.OnLeftClick += CloseClicked;

            UIText closeLabel = new UIText("X", 0.85f, false);
            closeLabel.HAlign = 0.5f;
            closeLabel.VAlign = 0.5f;
            closeButton.Append(closeLabel);
            panel.Append(closeButton);

            weaponSlot = new ReinforcementWeaponSlot(this, 0.95f);
            weaponSlot.Left.Set(10f, 0f);
            weaponSlot.Top.Set(44f, 0f);
            panel.Append(weaponSlot);

            reinforceButton = new UIPanel();
            reinforceButton.Left.Set(10f, 0f);
            reinforceButton.Top.Set(PanelHeight - 62f, 0f);
            reinforceButton.Width.Set(PanelWidth - 44f, 0f);
            reinforceButton.Height.Set(34f, 0f);
            reinforceButton.SetPadding(0f);
            reinforceButton.BackgroundColor = new Color(82, 54, 34, 235);
            reinforceButton.BorderColor = new Color(186, 134, 72, 245);
            reinforceButton.OnLeftClick += ReinforceClicked;

            reinforceButtonLabel = new UIText(LangUtils.GetTextValue("UI.WeaponReinforcement.ReinforceButton"), 0.95f, false);
            reinforceButtonLabel.HAlign = 0.5f;
            reinforceButtonLabel.VAlign = 0.5f;
            reinforceButton.Append(reinforceButtonLabel);
            panel.Append(reinforceButton);

            Append(panel);
        }

        private static float ComputeDefaultPanelLeft()
        {
            // Keep default placement right of the 10x5 inventory grid and trash slot (X <= ~575) so clicks cannot bleed into them.
            float centered = (Main.screenWidth - PanelWidth) * 0.5f;
            float clearOfInventory = Math.Max(585f, centered);
            float maxLeft = Math.Max(20f, Main.screenWidth - PanelWidth - 20f);
            return Math.Clamp(clearOfInventory, 20f, maxLeft);
        }

        private void ResetDragAndClamp()
        {
            dragging = false;
            prevMouseLeft = Main.mouseLeft;

            if (panel == null)
            {
                return;
            }

            float maxLeft = Math.Max(20f, Main.screenWidth - PanelWidth - 20f);
            float maxTop = Math.Max(20f, Main.screenHeight - PanelHeight - 20f);
            float clampedLeft = Math.Clamp(panel.Left.Pixels, 20f, maxLeft);
            float clampedTop = Math.Clamp(panel.Top.Pixels, 20f, maxTop);
            panel.Left.Set(clampedLeft, 0f);
            panel.Top.Set(clampedTop, 0f);
            panel.Recalculate();
        }

        private void UpdateHeaderDrag()
        {
            bool mouseLeftNow = Main.mouseLeft;
            bool justPressed = mouseLeftNow && !prevMouseLeft;
            prevMouseLeft = mouseLeftNow;

            if (justPressed && !PlayerInput.IgnoreMouseInterface)
            {
                CalculatedStyle outer = panel.GetOuterDimensions();
                Rectangle headerRect = new Rectangle((int)outer.X, (int)outer.Y, (int)outer.Width, (int)HeaderDragHeight);
                bool inHeader = headerRect.Contains((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y);
                bool overClose = closeButton != null && closeButton.ContainsPoint(Main.MouseScreen);

                // Drag only from the title strip so clicking the weapon slot or buttons never moves the panel.
                if (inHeader && !overClose)
                {
                    dragging = true;
                    dragOffset = new Vector2(Main.MouseScreen.X - panel.Left.Pixels, Main.MouseScreen.Y - panel.Top.Pixels);
                }
            }

            if (dragging)
            {
                if (!mouseLeftNow)
                {
                    dragging = false;
                }
                else
                {
                    float maxLeft = Math.Max(20f, Main.screenWidth - PanelWidth - 20f);
                    float maxTop = Math.Max(20f, Main.screenHeight - PanelHeight - 20f);
                    float nextLeft = Math.Clamp(Main.MouseScreen.X - dragOffset.X, 20f, maxLeft);
                    float nextTop = Math.Clamp(Main.MouseScreen.Y - dragOffset.Y, 20f, maxTop);
                    panel.Left.Set(nextLeft, 0f);
                    panel.Top.Set(nextTop, 0f);
                    panel.Recalculate();
                }
            }
        }

        public bool ReturnSlottedItem(bool playSound = true, bool allowWorldDrop = true)
        {
            if (weaponSlot == null || weaponSlot.Item == null || weaponSlot.Item.IsAir)
            {
                return false;
            }

            Player player = Main.LocalPlayer;
            if (player == null)
            {
                return false;
            }

            Item returnedItem = weaponSlot.Item.Clone();
            if (returnedItem == null || returnedItem.IsAir)
            {
                return false;
            }

            // Place directly into an open main inventory slot first so closing or save-and-quit never drops the weapon into an unloading world.
            for (int i = 0; i < 50; i++)
            {
                if (player.inventory[i] == null || player.inventory[i].IsAir)
                {
                    player.inventory[i] = returnedItem;
                    weaponSlot.Item = new Item();

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Main.myPlayer, i, returnedItem.prefix);
                    }

                    if (playSound && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Grab);
                    }

                    Recipe.FindRecipes();
                    return true;
                }
            }

            if (allowWorldDrop)
            {
                int spawnedIndex = player.QuickSpawnItem(player.GetSource_Misc("WeaponReinforcementReturn"), returnedItem, returnedItem.stack);
                if (spawnedIndex >= 0 && spawnedIndex < Main.maxItems)
                {
                    weaponSlot.Item = new Item();
                    if (playSound && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Grab);
                    }

                    return true;
                }
            }

            // Emergency fallback when main inventory is full during quit/unload: persist in player Storage so the weapon can never be lost.
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer != null)
            {
                modPlayer.DepositToStorage(returnedItem);
                if (!returnedItem.IsAir)
                {
                    modPlayer.StorageItems.Add(returnedItem);
                    modPlayer.StorageSeq.Add(modPlayer.NextSeq());
                }

                weaponSlot.Item = new Item();
                StorageUIState.MarkDirty();

                if (playSound && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Grab);
                }

                return true;
            }

            return false;
        }

        public override void OnDeactivate()
        {
            dragging = false;
            ReturnSlottedItem(playSound: false, allowWorldDrop: false);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!Visible)
            {
                return;
            }

            if (feedbackTimer > 0)
            {
                feedbackTimer--;
                if (feedbackTimer == 0)
                {
                    feedbackText = string.Empty;
                }
            }

            Player player = Main.LocalPlayer;
            bool contextValid = WeaponReinforcement.Enabled
                && player != null
                && player.active
                && !player.dead
                && BonfireUIState.Visible
                && Main.playerInventory;

            if (!contextValid)
            {
                Close();
                return;
            }

            if (panel.ContainsPoint(Main.MouseScreen))
            {
                player.mouseInterface = true;
            }

            UpdateHeaderDrag();

            titleLabel.SetText(LangUtils.GetTextValue("UI.WeaponReinforcement.Title"));
            reinforceButtonLabel.SetText(LangUtils.GetTextValue("UI.WeaponReinforcement.ReinforceButton"));

            bool canReinforce = WeaponReinforcement.CanReinforceNow(player, weaponSlot.Item, out _);
            if (canReinforce)
            {
                reinforceButton.BackgroundColor = reinforceButton.IsMouseHovering
                    ? new Color(128, 86, 42, 245)
                    : new Color(96, 62, 34, 240);
                reinforceButton.BorderColor = new Color(230, 178, 92, 255);
                reinforceButtonLabel.TextColor = new Color(255, 240, 198);
            }
            else
            {
                reinforceButton.BackgroundColor = new Color(52, 42, 42, 220);
                reinforceButton.BorderColor = new Color(90, 74, 72, 220);
                reinforceButtonLabel.TextColor = new Color(150, 140, 136);
            }
        }

        private void CloseClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            Close();
        }

        private void ReinforceClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            Player player = Main.LocalPlayer;
            bool reinforced = WeaponReinforcement.TryReinforce(player, weaponSlot.Item, out string status);
            feedbackText = status;
            feedbackTimer = 150;

            if (!reinforced)
            {
                SoundEngine.PlaySound(SoundID.MenuTick, player.Center);
            }
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            // Draw panel and its child controls first so dynamic text and item icons render cleanly on top of the background.
            base.DrawChildren(spriteBatch);

            CalculatedStyle dimensions = panel.GetInnerDimensions();
            float textX = dimensions.X + 76f;
            float lineY = dimensions.Y + 38f;
            float lineStep = 23f;
            int maxTextWidth = (int)(dimensions.Width - 88f);
            Player player = Main.LocalPlayer;

            if (panel.ContainsPoint(Main.MouseScreen))
            {
                player.mouseInterface = true;
            }

            Item slotted = weaponSlot.Item;
            byte worldCap = WeaponReinforcement.GetWorldCapLevel();

            if (slotted == null || slotted.IsAir)
            {
                DrawLine(spriteBatch, LangUtils.GetTextValue("UI.WeaponReinforcement.EmptySlotPrompt"), textX, lineY, new Color(224, 214, 200));
                DrawLine(spriteBatch, LangUtils.GetTextValue("UI.WeaponReinforcement.WorldCapFull", worldCap), textX, lineY + lineStep, new Color(232, 196, 118));
                DrawLine(spriteBatch, LangUtils.GetTextValue("UI.WeaponReinforcement.StoragePullHint"), textX, lineY + lineStep * 2f, new Color(166, 158, 150));
                return;
            }

            if (!WeaponReinforcement.TryGetEntry(slotted.type, out WeaponReinforcementEntry entry))
            {
                DrawLine(spriteBatch, LangUtils.GetTextValue("UI.WeaponReinforcement.InvalidWeapon"), textX, lineY, new Color(220, 120, 110));
                return;
            }

            byte currentLevel = WeaponReinforcement.GetDisplayedLevel(slotted);
            int currentDamage = WeaponReinforcement.GetDamageForLevel(entry, currentLevel);
            string baseName = Lang.GetItemNameValue(slotted.type);

            if (currentLevel >= entry.MaxLevel)
            {
                DrawLine(spriteBatch, LangUtils.GetTextValue("UI.WeaponReinforcement.MaxHeader", baseName, currentLevel), textX, lineY, new Color(245, 214, 124));
                DrawLine(spriteBatch, LangUtils.GetTextValue("UI.WeaponReinforcement.BaseDamageCurrent", currentDamage), textX, lineY + lineStep, new Color(155, 240, 168));
                DrawLine(spriteBatch, LangUtils.GetTextValue("UI.WeaponReinforcement.MaxReached"), textX, lineY + lineStep * 2f, new Color(180, 174, 166));
                return;
            }

            byte nextLevel = (byte)(currentLevel + 1);
            int nextDamage = WeaponReinforcement.GetDamageForLevel(entry, nextLevel);
            ReinforcementCost cost = WeaponReinforcement.GetUpgradeCost(nextLevel);
            int darkSoulType = ModContent.ItemType<DarkSoulItem>();
            long ownedMaterial = WeaponReinforcement.CountItemAcrossAllStorage(player, cost.MaterialItemType);
            long ownedSouls = WeaponReinforcement.CountItemAcrossAllStorage(player, darkSoulType);

            DrawLine(spriteBatch, LangUtils.GetTextValue("UI.WeaponReinforcement.UpgradeHeader", baseName, currentLevel, nextLevel), textX, lineY, new Color(248, 224, 154));
            DrawLine(spriteBatch, LangUtils.GetTextValue("UI.WeaponReinforcement.BaseDamageUpgrade", currentDamage, nextDamage), textX, lineY + lineStep, new Color(150, 240, 164));

            float materialRowY = lineY + lineStep * 2f;
            DrawItemIcon(spriteBatch, cost.MaterialItemType, textX + 9f, materialRowY + 9f, 18f);
            Color matColor = ownedMaterial >= cost.MaterialCount ? new Color(155, 236, 164) : new Color(238, 116, 112);
            string materialLine = LangUtils.GetTextValue("UI.WeaponReinforcement.MaterialCost", cost.MaterialName, cost.MaterialCount, ownedMaterial);
            DrawLine(spriteBatch, materialLine, textX + 24f, materialRowY, matColor);

            float soulRowY = lineY + lineStep * 3f;
            DrawItemIcon(spriteBatch, darkSoulType, textX + 9f, soulRowY + 9f, 18f);
            Color soulColor = ownedSouls >= cost.DarkSouls ? new Color(155, 236, 164) : new Color(238, 116, 112);
            string soulsLine = LangUtils.GetTextValue("UI.WeaponReinforcement.DarkSoulsCost", cost.DarkSouls.ToString("N0"), ownedSouls.ToString("N0"));
            DrawLine(spriteBatch, soulsLine, textX + 24f, soulRowY, soulColor);

            float statusRowY = lineY + lineStep * 4f + 2f;
            float nextRowY = statusRowY + lineStep;

            if (nextLevel > worldCap)
            {
                string gateLabel = WeaponReinforcement.GetGateLabelForLevel(nextLevel);
                string lockLine = LangUtils.GetTextValue("UI.WeaponReinforcement.LockedLine", gateLabel);
                nextRowY = DrawWrappedLines(spriteBatch, lockLine, textX, statusRowY, maxTextWidth, 2, new Color(240, 146, 112));
            }
            else if (entry.MilestonePerks != null && entry.MilestonePerks.TryGetValue(nextLevel, out ReinforcementPerk nextPerk))
            {
                string perkLine = LangUtils.GetTextValue("UI.WeaponReinforcement.PerkPreviewLine", nextLevel, nextPerk.GetText());
                nextRowY = DrawWrappedLines(spriteBatch, perkLine, textX, statusRowY, maxTextWidth, 2, new Color(148, 226, 255));
            }
            else
            {
                DrawLine(spriteBatch, LangUtils.GetTextValue("UI.WeaponReinforcement.WorldCapShort", worldCap), textX, statusRowY, new Color(188, 178, 166));
            }

            if (!string.IsNullOrEmpty(feedbackText))
            {
                float maxFeedbackY = dimensions.Y + dimensions.Height - 76f;
                float feedbackY = Math.Min(nextRowY + 2f, maxFeedbackY);
                DrawLine(spriteBatch, feedbackText, textX, feedbackY, new Color(248, 224, 134));
            }
        }

        private static void DrawItemIcon(SpriteBatch spriteBatch, int itemType, float centerX, float centerY, float maxExtent)
        {
            Main.instance.LoadItem(itemType);
            Texture2D texture = TextureAssets.Item[itemType].Value;
            if (texture == null)
            {
                return;
            }

            Rectangle frame;
            DrawAnimation animation = Main.itemAnimations[itemType];
            if (animation != null)
            {
                frame = animation.GetFrame(texture);
            }
            else
            {
                frame = texture.Frame();
            }

            float longestSide = Math.Max(frame.Width, frame.Height);
            float scale = longestSide > 0f ? Math.Min(1f, maxExtent / longestSide) : 1f;
            Vector2 origin = frame.Size() * 0.5f;

            spriteBatch.Draw(texture, new Vector2(centerX, centerY), frame, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        private static float DrawWrappedLines(SpriteBatch spriteBatch, string text, float posX, float startY, int maxWidth, int maxLines, Color color)
        {
            // WordwrapString measures at 1.0 font scale while DrawLine renders at 0.85, so scale the wrap limit to match.
            int scaledMaxWidth = (int)(maxWidth / 0.85f);
            string[] wrapped = Utils.WordwrapString(text, FontAssets.MouseText.Value, scaledMaxWidth, maxLines, out int lastLineIndex);
            float currentY = startY;
            float wrappedLineStep = 20f;

            for (int i = 0; i <= lastLineIndex && i < wrapped.Length; i++)
            {
                if (!string.IsNullOrEmpty(wrapped[i]))
                {
                    DrawLine(spriteBatch, wrapped[i], posX, currentY, color);
                    currentY += wrappedLineStep;
                }
            }

            return currentY;
        }

        private static void DrawLine(SpriteBatch spriteBatch, string text, float posX, float posY, Color color)
        {
            ChatManager.DrawColorCodedStringWithShadow(
                spriteBatch,
                FontAssets.MouseText.Value,
                text,
                new Vector2(posX, posY),
                color,
                0f,
                Vector2.Zero,
                new Vector2(0.85f));
        }

        /// <summary>
        /// Dedicated weapon slot that handles swap and shift-return directly instead of vanilla BankItem ItemSlot.Handle,
        /// preventing accidental Ctrl/Shift quick-trash, chest/potion-bag hooks, and stale TurnToAir item state.
        /// </summary>
        private sealed class ReinforcementWeaponSlot : UIElement
        {
            public Item Item;

            private readonly WeaponReinforcementUIState owner;
            private readonly float slotScale;

            public ReinforcementWeaponSlot(WeaponReinforcementUIState ownerState, float scale)
            {
                owner = ownerState;
                slotScale = scale;
                Item = new Item();
                Item.SetDefaults(ItemID.None);

                Width.Set(TextureAssets.InventoryBack9.Value.Width * scale, 0f);
                Height.Set(TextureAssets.InventoryBack9.Value.Height * scale, 0f);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                float oldScale = Main.inventoryScale;
                Main.inventoryScale = slotScale;
                Rectangle slotRect = GetDimensions().ToRectangle();

                if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface && !owner.dragging)
                {
                    Main.LocalPlayer.mouseInterface = true;

                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        Main.mouseLeftRelease = false;

                        if (ItemSlot.ShiftInUse && Item != null && !Item.IsAir)
                        {
                            owner.ReturnSlottedItem(playSound: true, allowWorldDrop: true);
                        }
                        else if (Main.mouseItem == null || Main.mouseItem.IsAir)
                        {
                            if (Item != null && !Item.IsAir)
                            {
                                Main.mouseItem = Item;
                                Item = new Item();
                                SoundEngine.PlaySound(SoundID.Grab);
                                Recipe.FindRecipes();
                            }
                        }
                        else if (WeaponReinforcement.CanEverReinforce(Main.mouseItem))
                        {
                            Item previousSlotted = Item;
                            Item = Main.mouseItem;
                            if (previousSlotted != null && !previousSlotted.IsAir)
                            {
                                Main.mouseItem = previousSlotted;
                            }
                            else
                            {
                                Main.mouseItem = new Item();
                            }

                            SoundEngine.PlaySound(SoundID.Grab);
                            Recipe.FindRecipes();
                        }
                    }

                    if (Item != null && !Item.IsAir)
                    {
                        ItemSlot.MouseHover(ref Item, ItemSlot.Context.PrefixItem);
                    }
                }

                if (Item == null)
                {
                    Item = new Item();
                }

                ItemSlot.Draw(spriteBatch, ref Item, ItemSlot.Context.PrefixItem, slotRect.TopLeft());
                Main.inventoryScale = oldScale;
            }
        }
    }
}
