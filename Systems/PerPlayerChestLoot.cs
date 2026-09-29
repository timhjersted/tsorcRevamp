using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.UI.Chat;

namespace tsorcRevamp.Systems
{
    // Per-Player Chest Loot (Gameplay config > Multiplayer; Auto = only worlds ever hosted in multiplayer, latched
    // per world). Every world chest becomes a READ-ONLY template:
    // the first time a player opens one, its contents are copied into that player's save, and from then on they
    // only see and take from their own copy. One set of loot per chest serves any party size, and every other
    // player still finds the chest full until they loot it themselves. Copies are take-only.
    //
    // Exempt (plain shared vanilla chests): chests a player placed, and the Ashen Peak village storage chests.
    //
    // MP: the server owns the templates and the player-placed list. A client fetches a chest's template once, by
    // packet, the first time it opens that chest. Copies live only in the owning client's player file (same trust
    // model as Storage), saved in the same file as the inventory they feed, so a crash can't keep one and not the other.
    public class PerPlayerChestLootSystem : ModSystem
    {
        public const int ChestSlotCount = 40;

        // Top-left tiles of chests players placed in this world. Server/singleplayer own it; clients mirror it
        // through NetSend on join plus one SyncPlayerPlacedChest packet per new placement.
        public static HashSet<Point16> PlayerPlacedChests = new HashSet<Point16>();

        // Ashen Peak village, in legacy (2000-tall map) tile space. Same two rectangles, exclusive bounds, that
        // tsorcMusic uses for the village music.
        static readonly (int MinX, int MaxX, int MinY, int MaxY)[] AshenPeakVillageLegacyBounds =
        {
            (3999, 4393, 600, 742),
            (4053, 4255, 600, 765),
        };

        // Client-side open state. OpenChestItems is the live array inside the player's ChestCopies, so taking
        // from the UI edits the saved copy directly.
        public static bool IsOpen;
        public static Point16 OpenChestPosition;
        public static Item[] OpenChestItems;
        public static string OpenChestName = "";

        // MP client: chest whose template was requested and not yet answered. (-1, -1) = none pending.
        static Point16 pendingTemplateRequest = new Point16(-1, -1);

        internal static UserInterface ChestInterface;
        internal static PerPlayerChestUIState ChestUIState;

        // Random ID for this world, keying every player's copies. Not the vanilla world GUID: every copy of the
        // adventure map shares that one. New worlds are made by copying only the .wld template, and this lives in
        // the .twld, so each fresh copy rolls its own ID while an existing world keeps its ID for good.
        public static string WorldLootId = "";

        // Set the first time this world is loaded by a server (dedicated or Host & Play) and saved from then on.
        // Auto mode turns per-player loot on only for these worlds.
        public static bool PlayedInMultiplayer;

        public override void Load()
        {
            On_Player.TileInteractionsUse += InterceptChestRightClick;
            On_Chest.IsPlayerInChest += SkipTemplatesDuringQuickStack;
            On_Chest.AfterPlacement_Hook += TrackChestPlacedInSingleplayer;
            On_WorldGen.PlaceChest += TrackChestPlacedOnServer;

            if (!Main.dedServ)
            {
                ChestUIState = new PerPlayerChestUIState();
                ChestUIState.Activate();
                ChestInterface = new UserInterface();
                ChestInterface.SetState(ChestUIState);
            }
        }

        public override void Unload()
        {
            ChestUIState = null;
            ChestInterface = null;
            OpenChestItems = null;
            PlayerPlacedChests = new HashSet<Point16>();
        }

        public override void ClearWorld()
        {
            PlayerPlacedChests.Clear();
            WorldLootId = "";
            PlayedInMultiplayer = false;
            pendingTemplateRequest = new Point16(-1, -1);
            CloseChest(playSound: false);
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["PlayerPlacedChests"] = PlayerPlacedChests.ToList();
            tag["WorldLootId"] = WorldLootId;
            tag["PlayedInMultiplayer"] = PlayedInMultiplayer;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            PlayerPlacedChests = new HashSet<Point16>(tag.GetList<Point16>("PlayerPlacedChests"));
            WorldLootId = tag.GetString("WorldLootId");
            PlayedInMultiplayer = tag.GetBool("PlayedInMultiplayer");
        }

        // Runs after LoadWorldData, on the server or in singleplayer only (never on MP clients, which get both
        // values through NetSend). A world with no saved ID is a fresh one, so it rolls a new ID here.
        public override void PostWorldLoad()
        {
            if (string.IsNullOrEmpty(WorldLootId))
            {
                WorldLootId = System.Guid.NewGuid().ToString();
            }

            if (Main.netMode == NetmodeID.Server)
            {
                PlayedInMultiplayer = true;
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(WorldLootId);
            writer.Write(PlayedInMultiplayer);
            writer.Write(PlayerPlacedChests.Count);

            foreach (Point16 chestPosition in PlayerPlacedChests)
            {
                writer.Write(chestPosition.X);
                writer.Write(chestPosition.Y);
            }
        }

        public override void NetReceive(BinaryReader reader)
        {
            WorldLootId = reader.ReadString();
            PlayedInMultiplayer = reader.ReadBoolean();
            PlayerPlacedChests.Clear();
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                short chestX = reader.ReadInt16();
                short chestY = reader.ReadInt16();
                PlayerPlacedChests.Add(new Point16(chestX, chestY));
            }
        }

        // Whether the chest with top-left tile (left, top) hands out per-player copies. Everything not exempt is,
        // including chests that were already emptied before the toggle was turned on (they stay empty for everyone).
        // Runs on both sides: the client decides what a click opens, the server re-checks before sending a template.
        public static bool IsInstancedChest(int left, int top)
        {
            PerPlayerChestLootMode mode = ModContent.GetInstance<tsorcRevampGameplayConfig>().PerPlayerChestLootMode;
            bool enabledInThisWorld = mode == PerPlayerChestLootMode.On || (mode == PerPlayerChestLootMode.Auto && PlayedInMultiplayer);

            if (!enabledInThisWorld)
            {
                return false;
            }

            // No ID yet means the world hasn't finished loading (or the sync hasn't arrived): nothing to key copies by.
            if (string.IsNullOrEmpty(WorldLootId))
            {
                return false;
            }

            if (PlayerPlacedChests.Contains(new Point16(left, top)))
            {
                return false;
            }

            // The village bounds are legacy map space, so map the chest back first (identity everywhere except the
            // 2400-tall expanded map). The coordinates only mean anything on the adventure map itself.
            if (tsorcRevampWorld.OnlyAdventureMap && !tsorcRevampWorld.RemixMap)
            {
                Point legacyTile = ExpandedWorldTransform.InverseMapTile(left, top);

                foreach ((int MinX, int MaxX, int MinY, int MaxY) bounds in AshenPeakVillageLegacyBounds)
                {
                    bool insideX = legacyTile.X > bounds.MinX && legacyTile.X < bounds.MaxX;
                    bool insideY = legacyTile.Y > bounds.MinY && legacyTile.Y < bounds.MaxY;

                    if (insideX && insideY)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Right-clicking an instanced chest opens this player's copy instead of the world chest. The first gate
        // mirrors vanilla's own (TileInteractionsUse returns early on the same conditions), so we only act on a fresh
        // local click that vanilla would also have treated as "open this chest".
        private static void InterceptChestRightClick(On_Player.orig_TileInteractionsUse orig, Player self, int myX, int myY)
        {
            bool isFreshLocalClick = self.whoAmI == Main.myPlayer
                && self.tileInteractAttempted
                && self.releaseUseTile
                && !WiresUI.Open
                && self.ownedProjectileCounts[ProjectileID.WireKite] <= 0;

            if (!isFreshLocalClick)
            {
                orig(self, myX, myY);
                return;
            }

            Tile clickedTile = Main.tile[myX, myY];

            if (!clickedTile.HasTile || !TileID.Sets.BasicChest[clickedTile.TileType])
            {
                orig(self, myX, myY);
                return;
            }

            // Chests are 2x2 tiles; frame % 36 / 18 is the clicked tile's column/row inside the chest.
            int left = myX - clickedTile.TileFrameX % 36 / 18;
            int top = myY - clickedTile.TileFrameY % 36 / 18;

            // Locked chests stay vanilla so unlocking with a key (shared world state) still works. The click after
            // the unlock comes back here and opens the copy.
            if (Chest.IsLocked(left, top) || !IsInstancedChest(left, top))
            {
                orig(self, myX, myY);
                return;
            }

            // Consume the click the same way vanilla's chest branch does.
            Main.mouseRightRelease = false;
            self.tileInteractionHappened = true;

            Point16 chestPosition = new Point16(left, top);
            bool clickedTheOpenChest = IsOpen && OpenChestPosition == chestPosition;

            if (clickedTheOpenChest)
            {
                CloseChest(playSound: true);
                return;
            }

            self.CloseSign();
            self.SetTalkNPC(-1);
            Main.npcChatText = "";
            self.chest = -1;

            PerPlayerChestLootPlayer chestPlayer = self.GetModPlayer<PerPlayerChestLootPlayer>();
            (string World, Point16 Chest) copyKey = (WorldLootId, chestPosition);

            if (chestPlayer.ChestCopies.TryGetValue(copyKey, out Item[] existingCopy))
            {
                OpenChest(chestPosition, existingCopy);
                return;
            }

            // First time this player opens this chest: snapshot the template. An MP client can't read it (only the
            // server has chest contents), so it asks and opens when the reply lands in ReceiveTemplate.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                pendingTemplateRequest = chestPosition;

                ModPacket packet = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                packet.Write(tsorcPacketID.RequestChestTemplate);
                packet.Write(chestPosition.X);
                packet.Write(chestPosition.Y);
                packet.Send();
                return;
            }

            int chestIndex = Chest.FindChest(left, top);

            if (chestIndex == -1)
            {
                return;
            }

            Item[] newCopy = new Item[ChestSlotCount];

            for (int i = 0; i < ChestSlotCount; i++)
            {
                Item templateItem = Main.chest[chestIndex].item[i];

                if (templateItem == null)
                {
                    newCopy[i] = new Item();
                }
                else
                {
                    newCopy[i] = templateItem.Clone();
                }
            }

            chestPlayer.ChestCopies[copyKey] = newCopy;
            OpenChest(chestPosition, newCopy);
        }

        // Quick-stack ("deposit to nearby chests") would write straight into the templates. Vanilla skips any chest
        // a player has open, and IsPlayerInChest is only used by that quick-stack scan, so reporting instanced chests
        // as "in use" makes quick-stack pass them by. Runs on the server in MP, where quick-stack resolves.
        private static bool SkipTemplatesDuringQuickStack(On_Chest.orig_IsPlayerInChest orig, int chestIndex)
        {
            if (orig(chestIndex))
            {
                return true;
            }

            Chest chest = Main.chest[chestIndex];
            return IsInstancedChest(chest.x, chest.y);
        }

        // Singleplayer placement creates the chest entry here. An MP client only forwards the placement to the
        // server from this hook, and the server records it in TrackChestPlacedOnServer instead.
        private static int TrackChestPlacedInSingleplayer(On_Chest.orig_AfterPlacement_Hook orig, int x, int y, int type, int style, int direction, int alternate)
        {
            int chestIndex = orig(x, y, type, style, direction, alternate);

            if (Main.netMode == NetmodeID.SinglePlayer && chestIndex != -1)
            {
                RecordPlayerPlacedChest(Main.chest[chestIndex]);
            }

            return chestIndex;
        }

        // The server builds a client's placed chest through PlaceChest (chest placement message). World generation
        // also calls it, and those chests are loot, so they're skipped.
        private static int TrackChestPlacedOnServer(On_WorldGen.orig_PlaceChest orig, int x, int y, ushort type, bool notNearOtherChests, int style)
        {
            int chestIndex = orig(x, y, type, notNearOtherChests, style);

            if (Main.netMode == NetmodeID.Server && !WorldGen.generatingWorld && chestIndex != -1)
            {
                RecordPlayerPlacedChest(Main.chest[chestIndex]);
            }

            return chestIndex;
        }

        private static void RecordPlayerPlacedChest(Chest chest)
        {
            Point16 chestPosition = new Point16(chest.x, chest.y);
            PlayerPlacedChests.Add(chestPosition);

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                packet.Write(tsorcPacketID.SyncPlayerPlacedChest);
                packet.Write(chestPosition.X);
                packet.Write(chestPosition.Y);
                packet.Send();
            }
        }

        // Server: a client opened an instanced chest for the first time. Reply with its template, or with
        // instanced=false if the server disagrees (e.g. the chest was just placed and the client hasn't heard yet).
        public static void ReceiveTemplateRequest(BinaryReader reader, int fromPlayer)
        {
            short left = reader.ReadInt16();
            short top = reader.ReadInt16();

            int chestIndex = -1;

            if (WorldGen.InWorld(left, top))
            {
                chestIndex = Chest.FindChest(left, top);
            }

            bool instanced = chestIndex != -1 && IsInstancedChest(left, top);

            ModPacket packet = ModContent.GetInstance<tsorcRevamp>().GetPacket();
            packet.Write(tsorcPacketID.ChestTemplate);
            packet.Write(left);
            packet.Write(top);
            packet.Write(instanced);

            if (instanced)
            {
                for (int i = 0; i < ChestSlotCount; i++)
                {
                    Item templateItem = Main.chest[chestIndex].item[i] ?? new Item();
                    ItemIO.Send(templateItem, packet, writeStack: true);
                }
            }

            packet.Send(toClient: fromPlayer);
        }

        // Client: the template for a chest we asked about. Always read the whole packet, then only act on the reply
        // to the latest request, so a late reply can't pop a chest open after the player clicked something else.
        public static void ReceiveTemplate(BinaryReader reader)
        {
            Point16 chestPosition = new Point16(reader.ReadInt16(), reader.ReadInt16());
            bool instanced = reader.ReadBoolean();

            Item[] newCopy = new Item[ChestSlotCount];

            if (instanced)
            {
                for (int i = 0; i < ChestSlotCount; i++)
                {
                    newCopy[i] = ItemIO.Receive(reader, readStack: true);
                }
            }

            bool answersLatestRequest = pendingTemplateRequest == chestPosition;

            if (!answersLatestRequest)
            {
                return;
            }

            pendingTemplateRequest = new Point16(-1, -1);

            if (!instanced)
            {
                return;
            }

            // A second click can send a second request. If a copy already exists by the time this lands, keep it:
            // replacing it would refill whatever was already taken.
            PerPlayerChestLootPlayer chestPlayer = Main.LocalPlayer.GetModPlayer<PerPlayerChestLootPlayer>();
            (string World, Point16 Chest) copyKey = (WorldLootId, chestPosition);

            if (!chestPlayer.ChestCopies.TryGetValue(copyKey, out Item[] copy))
            {
                copy = newCopy;
                chestPlayer.ChestCopies[copyKey] = copy;
            }

            OpenChest(chestPosition, copy);
        }

        private static void OpenChest(Point16 chestPosition, Item[] copy)
        {
            Tile chestTile = Main.tile[chestPosition.X, chestPosition.Y];
            int style = chestTile.TileFrameX / 36;

            // Same name the vanilla chest window shows: a custom name if this machine knows one, else the chest type.
            string chestName = TileLoader.DefaultContainerName(chestTile.TileType, chestTile.TileFrameX, chestTile.TileFrameY);

            if (chestTile.TileType == TileID.Containers && style < Lang.chestType.Length)
            {
                chestName = Lang.chestType[style].Value;
            }
            else if (chestTile.TileType == TileID.Containers2 && style < Lang.chestType2.Length)
            {
                chestName = Lang.chestType2[style].Value;
            }

            int chestIndex = Chest.FindChest(chestPosition.X, chestPosition.Y);

            if (chestIndex != -1 && !string.IsNullOrEmpty(Main.chest[chestIndex].name))
            {
                chestName = Main.chest[chestIndex].name;
            }

            if (string.IsNullOrEmpty(chestName))
            {
                chestName = Lang.chestType[0].Value;
            }

            IsOpen = true;
            OpenChestPosition = chestPosition;
            OpenChestItems = copy;
            OpenChestName = chestName;

            Main.playerInventory = true;
            SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        public static void CloseChest(bool playSound)
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            OpenChestItems = null;

            if (playSound)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
            }
        }

        // Moves one copy slot into the inventory exactly like vanilla Loot All (coins, stacking, void bag, pickup
        // text). GetItem hands back whatever didn't fit, which becomes the slot's new contents in the same frame.
        public static bool TakeIntoInventory(Player player, int slot)
        {
            Item item = OpenChestItems[slot];

            if (item.IsAir)
            {
                return false;
            }

            int stackBefore = item.stack;
            item.position = player.Center;

            Item leftover = player.GetItem(player.whoAmI, item, GetItemSettings.LootAllSettingsRegularChest);
            OpenChestItems[slot] = leftover;

            bool movedAnything = leftover.IsAir || leftover.stack != stackBefore;
            return movedAnything;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (IsOpen)
            {
                ChestInterface?.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

            if (mouseTextIndex == -1)
            {
                return;
            }

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "tsorcRevamp: Per-Player Chest UI",
                delegate
                {
                    if (IsOpen)
                    {
                        ChestInterface.Draw(Main.spriteBatch, new GameTime());
                    }
                    return true;
                },
                InterfaceScaleType.UI)
            );
        }
    }

    // This player's copy of every instanced chest they have opened, keyed by WorldLootId + chest top-left tile.
    // A fully looted chest keeps its (all-air) entry: dropping it would make the chest look unlooted again.
    public class PerPlayerChestLootPlayer : ModPlayer
    {
        public Dictionary<(string World, Point16 Chest), Item[]> ChestCopies = new Dictionary<(string World, Point16 Chest), Item[]>();

        // Only occupied slots are written (slot index + item) to keep the file small. Unopened chests cost nothing.
        public override void SaveData(TagCompound tag)
        {
            List<TagCompound> entries = new List<TagCompound>();

            foreach (KeyValuePair<(string World, Point16 Chest), Item[]> pair in ChestCopies)
            {
                List<int> slots = new List<int>();
                List<Item> items = new List<Item>();

                for (int i = 0; i < pair.Value.Length; i++)
                {
                    Item item = pair.Value[i];

                    if (item != null && !item.IsAir)
                    {
                        slots.Add(i);
                        items.Add(item);
                    }
                }

                TagCompound entry = new TagCompound
                {
                    ["World"] = pair.Key.World,
                    ["Chest"] = pair.Key.Chest,
                    ["Slots"] = slots,
                    ["Items"] = items,
                };
                entries.Add(entry);
            }

            tag["ChestCopies"] = entries;
        }

        public override void LoadData(TagCompound tag)
        {
            ChestCopies.Clear();

            foreach (TagCompound entry in tag.GetList<TagCompound>("ChestCopies"))
            {
                Item[] copy = new Item[PerPlayerChestLootSystem.ChestSlotCount];

                for (int i = 0; i < copy.Length; i++)
                {
                    copy[i] = new Item();
                }

                IList<int> slots = entry.GetList<int>("Slots");
                IList<Item> items = entry.GetList<Item>("Items");
                int pairCount = System.Math.Min(slots.Count, items.Count);

                for (int k = 0; k < pairCount; k++)
                {
                    int slot = slots[k];

                    if (slot >= 0 && slot < copy.Length)
                    {
                        copy[slot] = items[k];
                    }
                }

                string world = entry.GetString("World");
                Point16 chestPosition = entry.Get<Point16>("Chest");
                ChestCopies[(world, chestPosition)] = copy;
            }
        }
    }

    // The per-player chest window. Same look as the Storage window (dark panel, gold title, InventoryBack9 slots),
    // laid out over the spot where vanilla draws an open chest: a 10x4 grid under the inventory, with the name,
    // Loot All and close on the right. Take-only: nothing can be put into a copy.
    internal class PerPlayerChestUIState : UIState
    {
        private const int Columns = 10;
        private const int Rows = 4;
        private const float SlotSize = 40f;
        private const float SlotSpacing = 42f;
        private const float GridPadding = 8f;
        private const float SideColumnWidth = 120f;

        private UIPanel panel;
        private UIText titleText;

        public override void OnInitialize()
        {
            float gridWidth = Columns * SlotSpacing;
            float gridHeight = Rows * SlotSpacing;
            float sideColumnLeft = GridPadding + gridWidth + GridPadding;

            // Vanilla's chest grid starts at x 73, y Main.instance.invBottom (258): directly under the inventory.
            panel = new UIPanel();
            panel.SetPadding(0);
            panel.Left.Set(67f, 0f);
            panel.Top.Set(250f, 0f);
            panel.Width.Set(sideColumnLeft + SideColumnWidth, 0f);
            panel.Height.Set(GridPadding + gridHeight + GridPadding, 0f);
            panel.BackgroundColor = new Color(30, 30, 40) * 0.95f;
            Append(panel);

            for (int i = 0; i < PerPlayerChestLootSystem.ChestSlotCount; i++)
            {
                int column = i % Columns;
                int row = i / Columns;

                PerPlayerChestSlot slot = new PerPlayerChestSlot(i);
                slot.Left.Set(GridPadding + column * SlotSpacing, 0f);
                slot.Top.Set(GridPadding + row * SlotSpacing, 0f);
                slot.Width.Set(SlotSize, 0f);
                slot.Height.Set(SlotSize, 0f);
                panel.Append(slot);
            }

            titleText = new UIText("", 0.8f);
            titleText.Left.Set(sideColumnLeft, 0f);
            titleText.Top.Set(10f, 0f);
            titleText.Width.Set(SideColumnWidth - 26f, 0f);
            titleText.IsWrapped = true;
            titleText.TextColor = new Color(255, 204, 0);
            panel.Append(titleText);

            UIText closeButton = new UIText("X", 0.9f);
            closeButton.Left.Set(sideColumnLeft + SideColumnWidth - 22f, 0f);
            closeButton.Top.Set(8f, 0f);
            closeButton.OnMouseOver += (evt, element) => { closeButton.TextColor = Color.Red; };
            closeButton.OnMouseOut += (evt, element) => { closeButton.TextColor = Color.White; };
            closeButton.OnLeftClick += (evt, element) => { PerPlayerChestLootSystem.CloseChest(playSound: true); };
            panel.Append(closeButton);

            UIText lootAllButton = new UIText(Lang.inter[29].Value, 0.9f);
            lootAllButton.Left.Set(sideColumnLeft, 0f);
            lootAllButton.Top.Set(GridPadding + gridHeight - 24f, 0f);
            lootAllButton.TextColor = Color.Gray;
            lootAllButton.OnMouseOver += (evt, element) => { lootAllButton.TextColor = Color.White; };
            lootAllButton.OnMouseOut += (evt, element) => { lootAllButton.TextColor = Color.Gray; };
            lootAllButton.OnLeftClick += (evt, element) =>
            {
                Player player = Main.LocalPlayer;
                bool movedAnything = false;

                for (int i = 0; i < PerPlayerChestLootSystem.ChestSlotCount; i++)
                {
                    if (PerPlayerChestLootSystem.TakeIntoInventory(player, i))
                    {
                        movedAnything = true;
                    }
                }

                if (movedAnything)
                {
                    SoundEngine.PlaySound(SoundID.Grab);
                    Recipe.FindRecipes();
                }
            };
            panel.Append(lootAllButton);
        }

        public override void Update(GameTime gameTime)
        {
            // Close on the same things that close a vanilla chest: inventory closed, walking out of tile range
            // (vanilla's chestX/Y +- tileRange box), another container opened, or the chest gone / no longer instanced.
            Player player = Main.LocalPlayer;
            Point16 chestPosition = PerPlayerChestLootSystem.OpenChestPosition;
            Tile chestTile = Main.tile[chestPosition.X, chestPosition.Y];

            int playerTileX = (int)(player.Center.X / 16f);
            int playerTileY = (int)(player.Center.Y / 16f);
            bool outOfRangeX = playerTileX < chestPosition.X - Player.tileRangeX || playerTileX > chestPosition.X + Player.tileRangeX + 1;
            bool outOfRangeY = playerTileY < chestPosition.Y - Player.tileRangeY || playerTileY > chestPosition.Y + Player.tileRangeY + 1;

            bool inventoryClosed = !Main.playerInventory;
            bool otherContainerOpened = player.chest != -1;
            bool playerGone = player.dead || !player.active;
            bool chestGone = !chestTile.HasTile || !TileID.Sets.BasicChest[chestTile.TileType];
            bool noLongerInstanced = !PerPlayerChestLootSystem.IsInstancedChest(chestPosition.X, chestPosition.Y);

            if (inventoryClosed || outOfRangeX || outOfRangeY || otherContainerOpened || playerGone || chestGone || noLongerInstanced)
            {
                PerPlayerChestLootSystem.CloseChest(playSound: true);
                return;
            }

            titleText.SetText(PerPlayerChestLootSystem.OpenChestName);

            if (panel.ContainsPoint(Main.MouseScreen))
            {
                player.mouseInterface = true;
            }

            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!PerPlayerChestLootSystem.IsOpen)
            {
                return;
            }

            base.Draw(spriteBatch);
        }

        public override bool ContainsPoint(Vector2 point)
        {
            return panel != null && panel.ContainsPoint(point);
        }

        // One chest slot, bound by index to the open copy. Take-only: left click picks the stack up (or tops up a
        // matching cursor stack), shift-click sends it to the inventory. Holding a different item does nothing.
        private class PerPlayerChestSlot : UIElement
        {
            private const float ItemScale = 0.77f;
            private readonly int slotIndex;

            public PerPlayerChestSlot(int slotIndex)
            {
                this.slotIndex = slotIndex;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                Item[] copy = PerPlayerChestLootSystem.OpenChestItems;

                if (copy == null)
                {
                    return;
                }

                Item item = copy[slotIndex];
                Rectangle slotRectangle = GetDimensions().ToRectangle();
                bool hovered = ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface;

                if (hovered)
                {
                    Player player = Main.LocalPlayer;
                    player.mouseInterface = true;

                    if (!item.IsAir)
                    {
                        ItemSlot.MouseHover(ref item, ItemSlot.Context.ChestItem);
                    }

                    bool leftClicked = Main.mouseLeft && Main.mouseLeftRelease;

                    if (leftClicked && !item.IsAir)
                    {
                        bool tookSomething = false;

                        if (ItemSlot.ShiftInUse)
                        {
                            tookSomething = PerPlayerChestLootSystem.TakeIntoInventory(player, slotIndex);
                        }
                        else if (Main.mouseItem.IsAir)
                        {
                            // Empty the slot and hand the same item to the cursor in one step, so it is never in both.
                            copy[slotIndex] = new Item();
                            Main.mouseItem = item;
                            tookSomething = true;
                        }
                        else if (ItemLoader.TryStackItems(Main.mouseItem, item, out int movedCount) && movedCount > 0)
                        {
                            // Tops up a matching cursor stack from the slot (still a take, never a deposit).
                            if (item.stack <= 0)
                            {
                                copy[slotIndex] = new Item();
                            }
                            tookSomething = true;
                        }

                        if (tookSomething)
                        {
                            SoundEngine.PlaySound(SoundID.Grab);
                            Recipe.FindRecipes();
                        }
                    }

                    item = copy[slotIndex];
                }

                Texture2D backTexture = TextureAssets.InventoryBack9.Value;
                Color backColor = Color.White * 0.85f;

                if (hovered)
                {
                    backColor = Color.White;
                }

                spriteBatch.Draw(backTexture, slotRectangle, backColor);

                if (item.IsAir)
                {
                    return;
                }

                Vector2 slotCenter = slotRectangle.Center.ToVector2();
                ItemSlot.DrawItemIcon(item, ItemSlot.Context.ChestItem, spriteBatch, slotCenter, ItemScale, 32f, Color.White);

                if (item.stack > 1)
                {
                    Vector2 stackPosition = slotRectangle.TopLeft() + new Vector2(8f, 26f) * ItemScale;
                    ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value,
                        item.stack.ToString(), stackPosition, Color.White, 0f, Vector2.Zero, new Vector2(ItemScale), -1f, ItemScale);
                }
            }
        }
    }
}
