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

        // Vanilla chest-window geometry (ChestUI): slot (column, row) sits at 73 + column*56*scale,
        // invBottom + row*56*scale, at inventoryScale 0.755.
        private const float ChestInventoryScale = 0.755f;
        private const int ChestColumns = 10;

        // Vanilla puts the name at invBottom and Loot All at invBottom + 40. Those spots hold the bestiary/emote
        // buttons (y 278-308) unless a VANILLA chest is open, so ours sit below them, keeping the same 40px gap.
        private const int NameOffsetY = 58;
        private const int LootAllOffsetY = 98;

        // Vanilla moves the trash slot beside the chest grid only when its own chest is open. Main.trashSlotOffset
        // (read by vanilla, never written) puts it in the same spot for ours: (5, 168) is vanilla's chest-open shift.
        private static readonly Point16 ChestOpenTrashOffset = new Point16(5, 168);

        // Loot All hover animation, as in vanilla's ChestUI.ButtonScale / ButtonHovered.
        static float lootAllScale = 0.75f;
        static bool lootAllHovered;

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
        }

        public override void Unload()
        {
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
            Main.trashSlotOffset = ChestOpenTrashOffset;

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
            Main.trashSlotOffset = Point16.Zero;

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

        // Close on the same things that close a vanilla chest: inventory closed, walking out of tile range (vanilla's
        // chestX/Y +- tileRange box), another container opened, or the chest gone / no longer instanced.
        public override void UpdateUI(GameTime gameTime)
        {
            if (!IsOpen)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            Tile chestTile = Main.tile[OpenChestPosition.X, OpenChestPosition.Y];

            int playerTileX = (int)(player.Center.X / 16f);
            int playerTileY = (int)(player.Center.Y / 16f);
            bool outOfRangeX = playerTileX < OpenChestPosition.X - Player.tileRangeX || playerTileX > OpenChestPosition.X + Player.tileRangeX + 1;
            bool outOfRangeY = playerTileY < OpenChestPosition.Y - Player.tileRangeY || playerTileY > OpenChestPosition.Y + Player.tileRangeY + 1;

            bool inventoryClosed = !Main.playerInventory;
            bool otherContainerOpened = player.chest != -1;
            bool playerGone = player.dead || !player.active;
            bool chestGone = !chestTile.HasTile || !TileID.Sets.BasicChest[chestTile.TileType];
            bool noLongerInstanced = !IsInstancedChest(OpenChestPosition.X, OpenChestPosition.Y);

            if (inventoryClosed || outOfRangeX || outOfRangeY || otherContainerOpened || playerGone || chestGone || noLongerInstanced)
            {
                CloseChest(playSound: true);
            }
        }

        // Drawn with the inventory, just before mouse text, so hover tooltips land on top like vanilla's.
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
                    if (IsOpen && Main.playerInventory)
                    {
                        DrawChestWindow(Main.spriteBatch);
                    }
                    return true;
                },
                InterfaceScaleType.UI)
            );
        }

        // A copy of vanilla's chest window (ChestUI.Draw), drawn and clicked in one pass the way vanilla does it.
        // Same slot positions and art; only the take-only click rules and a single Loot All button differ.
        // Hidden while the big recipe list is open, since that covers this area (vanilla hides its chest too).
        private static void DrawChestWindow(SpriteBatch spriteBatch)
        {
            if (Main.recBigList)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            int invBottom = Main.instance.invBottom;
            float previousInventoryScale = Main.inventoryScale;
            Main.inventoryScale = ChestInventoryScale;

            // Vanilla's hover box over the whole grid, so clicks between slots don't reach the world either.
            float gridHoverWidth = 560f * ChestInventoryScale;
            float gridHoverHeight = 224f * ChestInventoryScale;
            bool hoveringGrid = Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0f, 0f, 73f, invBottom, gridHoverWidth, gridHoverHeight);

            if (hoveringGrid && !PlayerInput.IgnoreMouseInterface)
            {
                player.mouseInterface = true;
            }

            float slotSize = TextureAssets.InventoryBack.Width() * ChestInventoryScale;

            for (int slot = 0; slot < ChestSlotCount; slot++)
            {
                int column = slot % ChestColumns;
                int row = slot / ChestColumns;
                int slotX = (int)(73f + column * 56 * ChestInventoryScale);
                int slotY = (int)(invBottom + row * 56 * ChestInventoryScale);

                bool hoveringSlot = Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0f, 0f, slotX, slotY, slotSize, slotSize)
                    && !PlayerInput.IgnoreMouseInterface;

                if (hoveringSlot)
                {
                    player.mouseInterface = true;
                    ItemSlot.OverrideHover(OpenChestItems, ItemSlot.Context.ChestItem, slot);

                    // Take-only: left click picks the stack up (or tops up a matching cursor stack), shift-click
                    // sends it to the inventory. Holding a different item does nothing, so nothing can go in.
                    Item item = OpenChestItems[slot];
                    bool leftClicked = Main.mouseLeft && Main.mouseLeftRelease;

                    if (leftClicked && !item.IsAir)
                    {
                        bool tookSomething = false;

                        if (ItemSlot.ShiftInUse)
                        {
                            tookSomething = TakeIntoInventory(player, slot);
                        }
                        else if (Main.mouseItem.IsAir)
                        {
                            // Empty the slot and hand the same item to the cursor in one step, so it is never in both.
                            OpenChestItems[slot] = new Item();
                            Main.mouseItem = item;
                            tookSomething = true;
                        }
                        else if (ItemLoader.TryStackItems(Main.mouseItem, item, out int movedCount) && movedCount > 0)
                        {
                            if (item.stack <= 0)
                            {
                                OpenChestItems[slot] = new Item();
                            }
                            tookSomething = true;
                        }

                        if (tookSomething)
                        {
                            SoundEngine.PlaySound(SoundID.Grab);
                            Recipe.FindRecipes();
                        }
                    }

                    ItemSlot.MouseHover(OpenChestItems, ItemSlot.Context.ChestItem, slot);
                }

                ItemSlot.Draw(spriteBatch, OpenChestItems, ItemSlot.Context.ChestItem, slot, new Vector2(slotX, slotY));
            }

            // Name, in vanilla's pulsing white (brightness follows Main.mouseTextColor).
            Color nameColor = Color.White * (1f - (255f - Main.mouseTextColor) / 255f * 0.5f);
            nameColor.A = byte.MaxValue;
            Vector2 namePosition = new Vector2(504f, invBottom + NameOffsetY);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, OpenChestName, namePosition,
                nameColor, 0f, Vector2.Zero, Vector2.One, -1f, 1.5f);

            // Loot All, drawn like vanilla's ChestUI.DrawButton: text centred on (x, y), 0.75 scale growing to 1 while
            // hovered. Once hovered the hit box widens (-10 / +16 px) so the growing text can't flicker out of it.
            string lootAllText = Lang.inter[29].Value;
            Vector2 lootAllSize = FontAssets.MouseText.Value.MeasureString(lootAllText);
            int lootAllX = 506 + (int)(lootAllSize.X * lootAllScale / 2f);
            int lootAllY = invBottom + LootAllOffsetY;

            float hoverBoxLeft = lootAllX - lootAllSize.X / 2f;
            float hoverBoxWidth = lootAllSize.X;

            if (lootAllHovered)
            {
                hoverBoxLeft -= 10f;
                hoverBoxWidth += 16f;
            }

            bool hoveringLootAll = Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0f, 0f, hoverBoxLeft, lootAllY - 12, hoverBoxWidth, 24f);

            Color lootAllColor = Color.White * 0.97f * (1f - (255f - Main.mouseTextColor) / 255f * 0.5f);
            lootAllColor.A = byte.MaxValue;

            if (hoveringLootAll)
            {
                lootAllColor = Main.OurFavoriteColor;
            }

            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, lootAllText, new Vector2(lootAllX, lootAllY),
                lootAllColor, 0f, lootAllSize / 2f, new Vector2(lootAllScale), -1f, 1.5f);

            if (hoveringLootAll)
            {
                if (!lootAllHovered)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
                lootAllHovered = true;
                lootAllScale = System.Math.Min(lootAllScale + 0.05f, 1f);
            }
            else
            {
                lootAllHovered = false;
                lootAllScale = System.Math.Max(lootAllScale - 0.05f, 0.75f);
            }

            if (hoveringLootAll && !PlayerInput.IgnoreMouseInterface)
            {
                player.mouseInterface = true;

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    bool movedAnything = false;

                    for (int slot = 0; slot < ChestSlotCount; slot++)
                    {
                        if (TakeIntoInventory(player, slot))
                        {
                            movedAnything = true;
                        }
                    }

                    if (movedAnything)
                    {
                        SoundEngine.PlaySound(SoundID.Grab);
                    }
                    Recipe.FindRecipes();
                }
            }

            Main.inventoryScale = previousInventoryScale;
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
}
