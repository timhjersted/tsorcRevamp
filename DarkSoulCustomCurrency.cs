using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Utilities;

namespace tsorcRevamp
{
    // Shops price in Dark Souls. Vanilla only counts and pays from the inventory and the four banks, but Dark Souls
    // live in tsorcRevampPlayer.SoulSlot, so both CountCurrency and TryPurchasing add that slot on the local player.
    // Purchases run only on the buying client; the SoulSlot change then syncs through SendClientChanges -> SyncSoulSlot.
    public class DarkSoulCustomCurrency : CustomCurrencySingleCoin
    {
        public DarkSoulCustomCurrency(int coinItemID, long currencyCap) : base(coinItemID, currencyCap)
        {
        }

        // Souls in the local player's soul slot, 0 when it's empty or this is the server.
        private static int GetSoulSlotSouls()
        {
            if (Main.dedServ)
            {
                return 0;
            }

            Item soulSlotItem = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulSlot.Item;
            if (soulSlotItem.IsAir || soulSlotItem.type != ModContent.ItemType<DarkSoulItem>())
            {
                return 0;
            }

            return soulSlotItem.stack;
        }

        // CanAfford counts the inventory once and each bank once; the slot is added only to the inventory count.
        public override long CountCurrency(out bool overFlowing, Item[] inv, params int[] ignoreSlots)
        {
            long count = base.CountCurrency(out overFlowing, inv, ignoreSlots);

            if (Main.dedServ || inv != Main.LocalPlayer.inventory)
            {
                return count;
            }

            count += GetSoulSlotSouls();
            if (count >= CurrencyCap)
            {
                overFlowing = true;
                return CurrencyCap;
            }

            return count;
        }

        // Take from the soul slot first, then let vanilla take any remainder from inventory/bank stacks.
        // If vanilla can't cover the remainder it restores its own slots, so the soul slot is put back here too.
        public override bool TryPurchasing(long price, List<Item[]> inv, List<Point> slotCoins, List<Point> slotsEmpty, List<Point> slotEmptyBank, List<Point> slotEmptyBank2, List<Point> slotEmptyBank3, List<Point> slotEmptyBank4)
        {
            if (Main.dedServ)
            {
                return base.TryPurchasing(price, inv, slotCoins, slotsEmpty, slotEmptyBank, slotEmptyBank2, slotEmptyBank3, slotEmptyBank4);
            }

            Item soulSlotItem = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulSlot.Item;
            int slotSouls = GetSoulSlotSouls();

            long takenFromSlot = price;
            if (slotSouls < takenFromSlot)
            {
                takenFromSlot = slotSouls;
            }

            long remainder = price - takenFromSlot;
            if (remainder > 0)
            {
                bool paidRemainder = base.TryPurchasing(remainder, inv, slotCoins, slotsEmpty, slotEmptyBank, slotEmptyBank2, slotEmptyBank3, slotEmptyBank4);
                if (!paidRemainder)
                {
                    return false;
                }
            }

            if (takenFromSlot > 0)
            {
                soulSlotItem.stack -= (int)takenFromSlot;
                if (soulSlotItem.stack <= 0)
                {
                    soulSlotItem.TurnToAir();
                }
            }

            // Dark Soul recipes otherwise stay "craftable" with the pre-purchase total until the next refresh.
            Recipe.FindRecipes();
            return true;
        }

        // "Buy price 12,500 Dark Souls [icon] (you have 48,210)". The owned figure is uncapped, unlike CountCurrency.
        public override void GetPriceText(string[] lines, ref int currentLine, long price)
        {
            long ownedSouls = GetSoulSlotSouls();
            if (!Main.dedServ)
            {
                ownedSouls += Main.LocalPlayer.CountItem(ModContent.ItemType<DarkSoulItem>());
            }

            string priceText = LangUtils.GetTextValue("UI.ShopDarkSoulPrice", Language.GetTextValue("LegacyTooltip.50"), price.ToString("N0"), LangUtils.GetTextValue("UI.DarkSouls"), ModContent.ItemType<DarkSoulItem>());
            string ownedText = LangUtils.GetTextValue("UI.ShopDarkSoulOwned", ownedSouls.ToString("N0"));
            lines[currentLine++] = priceText + " " + ownedText;
        }
    }
}
