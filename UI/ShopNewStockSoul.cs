using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace tsorcRevamp.UI
{
    // "New stock" marker: a white Dark Soul floats over any shopkeeper selling something this character
    // has never seen in that NPC's shop. Talking to the NPC marks its whole current stock as seen.
    // Purely client-side: the local player evaluates shop conditions and draws; nothing is synced.
    public class ShopNewStockPlayer : ModPlayer
    {
        // NPC type -> item types this character has already been shown by that NPC. Saved per character.
        private Dictionary<int, HashSet<int>> seenStock = new Dictionary<int, HashSet<int>>();

        // NPC type -> "has unseen stock". Shop conditions are re-evaluated at most once per refresh, not every draw.
        private Dictionary<int, bool> unseenCache = new Dictionary<int, bool>();
        private int cacheRefreshTimer = 0;
        private const int CacheRefreshTicks = 60;

        // Every item an NPC's shops would offer right now, across all of its NPCShops (e.g. Painter has two).
        // Travelling Merchant style shops aren't NPCShop and are skipped: their random stock isn't "new".
        private static List<int> GetAvailableStock(int npcType)
        {
            List<int> availableItems = new List<int>();

            foreach (AbstractNPCShop abstractShop in NPCShopDatabase.AllShops)
            {
                if (abstractShop.NpcType != npcType || abstractShop is not NPCShop shop)
                {
                    continue;
                }

                foreach (NPCShop.Entry entry in shop.ActiveEntries)
                {
                    // Some conditions assume an open chat (e.g. read the talked-to NPC). Treat a throw as "not offered".
                    bool offered;
                    try
                    {
                        offered = entry.ConditionsMet();
                    }
                    catch (Exception)
                    {
                        offered = false;
                    }

                    if (offered && !entry.Item.IsAir)
                    {
                        availableItems.Add(entry.Item.type);
                    }
                }
            }

            return availableItems;
        }

        public bool HasUnseenStock(int npcType)
        {
            if (unseenCache.TryGetValue(npcType, out bool cachedResult))
            {
                return cachedResult;
            }

            List<int> availableItems = GetAvailableStock(npcType);
            seenStock.TryGetValue(npcType, out HashSet<int> seenItems);

            bool hasUnseen = false;
            foreach (int itemType in availableItems)
            {
                if (seenItems == null || !seenItems.Contains(itemType))
                {
                    hasUnseen = true;
                    break;
                }
            }

            unseenCache[npcType] = hasUnseen;
            return hasUnseen;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || Main.dedServ)
            {
                return;
            }

            // Progression (boss kills, time of day, biome) changes stock without any event to hook, so just re-check every second.
            cacheRefreshTimer--;
            if (cacheRefreshTimer <= 0)
            {
                cacheRefreshTimer = CacheRefreshTicks;
                unseenCache.Clear();
            }

            // Chatting clears the marker: everything the NPC offers right now counts as seen, whether or not the shop was opened.
            if (Player.talkNPC < 0)
            {
                return;
            }

            int talkedNPCType = Main.npc[Player.talkNPC].type;
            List<int> availableItems = GetAvailableStock(talkedNPCType);
            if (availableItems.Count == 0)
            {
                return;
            }

            if (!seenStock.TryGetValue(talkedNPCType, out HashSet<int> seenItems))
            {
                seenItems = new HashSet<int>();
                seenStock[talkedNPCType] = seenItems;
            }

            seenItems.UnionWith(availableItems);
            unseenCache[talkedNPCType] = false;
        }

        // Saved by content name ("Terraria/Merchant", "tsorcRevamp/DarkSoulItem"), so mod reloads and content shuffles don't remap IDs.
        public override void SaveData(TagCompound tag)
        {
            List<TagCompound> savedShops = new List<TagCompound>();

            foreach (KeyValuePair<int, HashSet<int>> shopEntry in seenStock)
            {
                List<string> itemNames = new List<string>();
                foreach (int itemType in shopEntry.Value)
                {
                    itemNames.Add(ItemID.Search.GetName(itemType));
                }

                TagCompound shopTag = new TagCompound();
                shopTag["npc"] = NPCID.Search.GetName(shopEntry.Key);
                shopTag["items"] = itemNames;
                savedShops.Add(shopTag);
            }

            tag["seenShopStock"] = savedShops;
        }

        public override void LoadData(TagCompound tag)
        {
            seenStock.Clear();
            unseenCache.Clear();

            IList<TagCompound> savedShops = tag.GetList<TagCompound>("seenShopStock");
            foreach (TagCompound shopTag in savedShops)
            {
                // Unknown names belong to unloaded/removed content; drop them quietly.
                if (!NPCID.Search.TryGetId(shopTag.GetString("npc"), out int npcType))
                {
                    continue;
                }

                HashSet<int> seenItems = new HashSet<int>();
                foreach (string itemName in shopTag.GetList<string>("items"))
                {
                    if (ItemID.Search.TryGetId(itemName, out int itemType))
                    {
                        seenItems.Add(itemType);
                    }
                }

                seenStock[npcType] = seenItems;
            }
        }
    }

    public class ShopNewStockSoul : GlobalNPC
    {
        // Sheet is the Dark Soul item sprite recoloured white: 4 vertical frames, 22x28 each.
        private const int FrameCount = 4;
        private const int TicksPerFrame = 5;

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!npc.active || npc.IsABestiaryIconDummy)
            {
                return;
            }

            // Hide on the NPC we're talking to — the chat itself is what clears it, so no flicker while it updates.
            Player localPlayer = Main.LocalPlayer;
            if (localPlayer.talkNPC == npc.whoAmI)
            {
                return;
            }

            ShopNewStockPlayer stockPlayer = localPlayer.GetModPlayer<ShopNewStockPlayer>();
            if (!stockPlayer.HasUnseenStock(npc.type))
            {
                return;
            }

            Texture2D texture = ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(ShopNewStockSoul)), AssetRequestMode.ImmediateLoad).Value;
            int frameHeight = texture.Height / FrameCount;
            int frameIndex = (int)(Main.GameUpdateCount / TicksPerFrame % FrameCount);
            Rectangle frame = new Rectangle(0, frameIndex * frameHeight, texture.Width, frameHeight);

            // Float 22px above the head with a gentle 3px bob (~2.5s period). Full-bright so it reads at night and underground.
            float bob = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f) * 3f;
            Vector2 drawPosition = npc.Top + new Vector2(0f, npc.gfxOffY - 22f + bob) - screenPos;
            Vector2 origin = frame.Size() / 2f;

            spriteBatch.Draw(texture, drawPosition, frame, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
        }
    }
}
