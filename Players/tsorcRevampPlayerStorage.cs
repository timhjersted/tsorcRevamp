using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.UI;

namespace tsorcRevamp
{
    // Dark Souls-style bottomless Storage box. Per-player, accessed anywhere via the opener slot / keybind.
    // Almost everything the player picks up auto-files here (potions, coins, favorited items, and a few
    // hard-guarded types are excluded). The list is kept dense (no air holes survive a rebuild) so the
    // virtualized UI's scroll math and "New" ordering stay clean.
    //
    // This is purely local/client data (like the Potion Bag) — no net sync, no server authority.
    public partial class tsorcRevampPlayer
    {
        // Backing store. Dense after every rebuild/compaction; StorageSeq is kept index-aligned with it.
        public List<Item> StorageItems = new List<Item>();
        public List<int> StorageSeq = new List<int>();
        // Monotonic counter stamped on each deposit; drives the "New" tab (highest = most recent).
        public int storageSeqCounter = 0;
        public const int STORAGE_CAP = 2000;

        // Saved on-screen position of the Storage pop-up (top-left, absolute pixels). (-1,-1) = never moved,
        // use the default anchored position.
        public Vector2 StorageWindowPos = new Vector2(-1, -1);

        public int NextSeq() => ++storageSeqCounter;
        public void BumpSeq(int index)
        {
            if (index >= 0 && index < StorageSeq.Count)
            {
                StorageSeq[index] = NextSeq();
            }
        }

        // What may be deposited. Deliberately permissive: the long block list here dated from auto-deposit-on-
        // pickup, when Storage grabbed things out from under the player and had to protect quick-use items
        // (potions, coins, treasure bags, boss summons, stat-ups). Deposits are manual now, so every one of those
        // is the player explicitly choosing to stash something and there's nothing left to protect them from.
        //
        // Favorited is the one real guard, and it earns its place twice over: it's Terraria's universal "don't
        // move this" flag, and StorageUIState.WriteBack clears favorited on the clone it stores, so a favorited
        // item would silently lose its star on the way in.
        public bool IsStorageDepositable(Item item)
        {
            if (item == null || item.IsAir)
            {
                return false;
            }
            if (item.favorited)
            {
                return false;
            }
            return true;
        }

        // Deposits as much of `incoming` as possible. Merges into existing stacks first (re-touch bumps their
        // seq so they resurface in New), then appends new entries while under the cap. Returns true if the
        // whole item was consumed (TurnToAir'd). If the cap is hit with a remainder, returns false and leaves
        // the remainder on the item so the caller can fall back to normal inventory pickup (loot is never lost).
        public bool DepositToStorage(Item incoming)
        {
            if (incoming == null || incoming.IsAir)
            {
                return false;
            }
            bool changed = false;

            // 1) Merge into existing matching, non-full stacks.
            for (int i = 0; i < StorageItems.Count && incoming.stack > 0; i++)
            {
                Item stored = StorageItems[i];
                if (stored == null || stored.IsAir)
                {
                    continue;
                }
                if (stored.type != incoming.type || stored.prefix != incoming.prefix)
                {
                    continue;
                }
                if (stored.stack >= stored.maxStack)
                {
                    continue;
                }

                int moved = Math.Min(stored.maxStack - stored.stack, incoming.stack);
                stored.stack += moved;
                incoming.stack -= moved;
                BumpSeq(i);
                changed = true;
            }

            // 2) Append the remainder as fresh entries until we run out or hit the cap.
            while (incoming.stack > 0 && StorageItems.Count < STORAGE_CAP)
            {
                Item clone = incoming.Clone();
                clone.stack = Math.Min(incoming.stack, incoming.maxStack);
                clone.favorited = false;
                StorageItems.Add(clone);
                StorageSeq.Add(NextSeq());
                incoming.stack -= clone.stack;
                changed = true;
            }

            if (changed)
            {
                StorageUIState.MarkDirty();
            }

            if (incoming.stack <= 0)
            {
                incoming.TurnToAir();
                return true;
            }
            return false;
        }

        // Moves as much of `item` (currently sitting in a Storage slot) into the player's main inventory as
        // will fit — merges into existing matching stacks first, then the first empty slot (mirrors
        // DepositToStorage's logic in reverse). Mutates `item` in place, turning it to air if fully moved.
        // Returns true if anything moved (including a partial move), so the caller knows whether to play a
        // sound / treat it as a change.
        public bool WithdrawToInventory(Item item)
        {
            if (item == null || item.IsAir)
            {
                return false;
            }
            bool changed = false;

            for (int i = 0; i < 50 && item.stack > 0; i++)
            {
                Item inv = Player.inventory[i];
                if (inv == null || inv.IsAir)
                {
                    continue;
                }
                if (inv.type != item.type || inv.prefix != item.prefix)
                {
                    continue;
                }
                if (inv.stack >= inv.maxStack)
                {
                    continue;
                }

                int moved = Math.Min(inv.maxStack - inv.stack, item.stack);
                inv.stack += moved;
                item.stack -= moved;
                changed = true;
            }

            for (int i = 0; i < 50 && item.stack > 0; i++)
            {
                if (Player.inventory[i] == null || Player.inventory[i].IsAir)
                {
                    Item clone = item.Clone();
                    clone.stack = item.stack;
                    Player.inventory[i] = clone;
                    item.stack = 0;
                    changed = true;
                }
            }

            if (item.stack <= 0)
            {
                item.TurnToAir();
            }
            return changed;
        }

        // Drop air holes (created by withdrawals) and keep StorageSeq aligned. Called before each view rebuild.
        public void CompactStorage()
        {
            for (int i = StorageItems.Count - 1; i >= 0; i--)
            {
                if (StorageItems[i] == null || StorageItems[i].IsAir)
                {
                    StorageItems.RemoveAt(i);
                    if (i < StorageSeq.Count)
                    {
                        StorageSeq.RemoveAt(i);
                    }
                }
            }
            // Repair any length mismatch defensively (e.g. after a migration / manual edit).
            while (StorageSeq.Count < StorageItems.Count)
            {
                StorageSeq.Add(NextSeq());
            }
            while (StorageSeq.Count > StorageItems.Count)
            {
                StorageSeq.RemoveAt(StorageSeq.Count - 1);
            }
        }

        // Open/close the Storage pop-up. Shared by the opener slot click and the keybind.
        public static void ToggleStorage()
        {
            if (Main.LocalPlayer == null)
            {
                return;
            }

            if (!StorageUIState.Visible)
            {
                Main.LocalPlayer.chest = -1;
                Main.playerInventory = true;
                StorageUIState.OpenToNewTab();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
            }
            else
            {
                StorageUIState.Visible = false;
                StorageUIState.ReopenWithInventory = false; // explicit close via keybind — don't reopen automatically
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose);
            }
        }

        // ---- Save / load (called FIRST from SaveData / LoadData in tsorcRevampPlayerMain) ----
        // These run before the rest of the player's save/load so unrelated failures elsewhere can't skip them.
        // Because they run first, they must never throw (a throw here would skip everything else), hence the
        // try/catch — a serialization hiccup degrades to empty storage rather than losing the whole player.
        public void SaveStorage(TagCompound tag)
        {
            try
            {
                if (StorageItems == null)
                {
                    StorageItems = new List<Item>();
                }
                if (StorageSeq == null)
                {
                    StorageSeq = new List<int>();
                }
                CompactStorage();

                // Mirror the Potion Bag's proven idiom: build a fresh, dense, null-free list to serialize.
                List<Item> toSave = new List<Item>();
                foreach (Item stored in StorageItems)
                {
                    if (stored != null && !stored.IsAir)
                    {
                        toSave.Add(stored);
                    }
                }

                tag["StorageItems"] = toSave;
                tag["StorageSeq"] = StorageSeq;
                tag["StorageSeqCounter"] = storageSeqCounter;
                tag["StorageWindowPos"] = StorageWindowPos;
            }
            catch (System.Exception e)
            {
                Mod.Logger.Error("Failed to save Storage data", e);
            }
        }

        public void LoadStorage(TagCompound tag)
        {
            try
            {
                StorageItems = tag.ContainsKey("StorageItems")
                    ? tag.GetList<Item>("StorageItems").ToList()
                    : new List<Item>();
                StorageSeq = tag.ContainsKey("StorageSeq")
                    ? tag.GetList<int>("StorageSeq").ToList()
                    : new List<int>();
                storageSeqCounter = tag.GetInt("StorageSeqCounter");
                StorageWindowPos = tag.ContainsKey("StorageWindowPos")
                    ? tag.Get<Vector2>("StorageWindowPos")
                    : new Vector2(-1, -1);
            }
            catch (System.Exception e)
            {
                Mod.Logger.Error("Failed to load Storage data", e);
                StorageItems ??= new List<Item>();
                StorageSeq ??= new List<int>();
            }

            // Length guard: if an older/edited save desyncs the parallel lists, realign them.
            while (StorageSeq.Count < StorageItems.Count)
            {
                StorageSeq.Add(NextSeq());
            }
            while (StorageSeq.Count > StorageItems.Count)
            {
                StorageSeq.RemoveAt(StorageSeq.Count - 1);
            }
            StorageUIState.MarkDirty();
        }
    }
}
