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

        // Hard guards: never auto-deposit these. They stay in the inventory (or their own systems).
        public bool IsStorageDepositable(Item item)
        {
            if (item == null || item.IsAir) return false;
            if (item.favorited) return false;
            if (item.questItem) return false;
            // Potions are quick-use and belong to the player / Potion Bag.
            if (PotionBagUIState.IsValidPotion(item)) return false;
            if (item.type == ModContent.ItemType<PotionBag>()) return false;
            // Coins are quick-use/spend — keep them in the inventory.
            if (item.type == ItemID.CopperCoin || item.type == ItemID.SilverCoin
                || item.type == ItemID.GoldCoin || item.type == ItemID.PlatinumCoin) return false;
            // Grab-on-touch resources (hearts/stars/nebula/stamina droplets), permanent stat-ups, goodie bags,
            // and soul currency — see tsorcRevamp.StorageExcludedTypes (populated in PopulateArrays). These are
            // consumed/spent on contact or wanted in-hand, and must never be filed away.
            if (tsorcRevamp.StorageExcludedTypes != null && tsorcRevamp.StorageExcludedTypes.Contains(item.type)) return false;
            // "Open me" containers — boss/treasure bags and fishing crates — stay in inventory for quick opening
            // (covers modded bags too, via the vanilla sets).
            if (item.type < ItemID.Sets.BossBag.Length && ItemID.Sets.BossBag[item.type]) return false;
            if (item.type < ItemID.Sets.IsFishingCrate.Length && ItemID.Sets.IsFishingCrate[item.type]) return false;
            // Boss / event summoning items stay handy for fights (this vanilla set marks all of them, incl. modded).
            if (item.type < ItemID.Sets.SortingPriorityBossSpawns.Length && ItemID.Sets.SortingPriorityBossSpawns[item.type] >= 0) return false;
            return true;
        }

        // Deposits as much of `incoming` as possible. Merges into existing stacks first (re-touch bumps their
        // seq so they resurface in New), then appends new entries while under the cap. Returns true if the
        // whole item was consumed (TurnToAir'd). If the cap is hit with a remainder, returns false and leaves
        // the remainder on the item so the caller can fall back to normal inventory pickup (loot is never lost).
        public bool DepositToStorage(Item incoming)
        {
            if (incoming == null || incoming.IsAir) return false;
            bool changed = false;

            // 1) Merge into existing matching, non-full stacks.
            for (int i = 0; i < StorageItems.Count && incoming.stack > 0; i++)
            {
                Item s = StorageItems[i];
                if (s == null || s.IsAir) continue;
                if (s.type != incoming.type || s.prefix != incoming.prefix) continue;
                if (s.stack >= s.maxStack) continue;

                int moved = Math.Min(s.maxStack - s.stack, incoming.stack);
                s.stack += moved;
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

            if (changed) StorageUIState.MarkDirty();

            if (incoming.stack <= 0)
            {
                incoming.TurnToAir();
                return true;
            }
            return false;
        }

        // Drop air holes (created by withdrawals) and keep StorageSeq aligned. Called before each view rebuild.
        public void CompactStorage()
        {
            for (int i = StorageItems.Count - 1; i >= 0; i--)
            {
                if (StorageItems[i] == null || StorageItems[i].IsAir)
                {
                    StorageItems.RemoveAt(i);
                    if (i < StorageSeq.Count) StorageSeq.RemoveAt(i);
                }
            }
            // Repair any length mismatch defensively (e.g. after a migration / manual edit).
            while (StorageSeq.Count < StorageItems.Count) StorageSeq.Add(NextSeq());
            while (StorageSeq.Count > StorageItems.Count) StorageSeq.RemoveAt(StorageSeq.Count - 1);
        }

        // Open/close the Storage pop-up. Shared by the opener slot click and the keybind.
        public static void ToggleStorage()
        {
            if (Main.LocalPlayer == null) return;

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
                if (StorageItems == null) StorageItems = new List<Item>();
                if (StorageSeq == null) StorageSeq = new List<int>();
                CompactStorage();

                // Mirror the Potion Bag's proven idiom: build a fresh, dense, null-free list to serialize.
                List<Item> toSave = new List<Item>();
                foreach (Item it in StorageItems)
                {
                    if (it != null && !it.IsAir) toSave.Add(it);
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
            while (StorageSeq.Count < StorageItems.Count) StorageSeq.Add(NextSeq());
            while (StorageSeq.Count > StorageItems.Count) StorageSeq.RemoveAt(StorageSeq.Count - 1);
            StorageUIState.MarkDirty();
        }
    }
}
