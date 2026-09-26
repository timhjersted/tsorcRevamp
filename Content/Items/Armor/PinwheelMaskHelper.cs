using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;

namespace tsorcRevamp.Content.Items.Armor
{
    public static class PinwheelMaskHelper
    {
        public static bool IsPinwheelMask(Item item)
        {
            return item != null && !item.IsAir && (
                item.type == ModContent.ItemType<MaskOfTheChild>() ||
                item.type == ModContent.ItemType<MaskOfTheFather>() ||
                item.type == ModContent.ItemType<MaskOfTheMother>()
            );
        }

        public static bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            // Allow equipping into vanity slots (vanity accessory slots start at 10 in standard armor array)
            if (!modded && slot >= 10)
            {
                return true;
            }

            if (modded)
            {
                var modSlotPlayer = player.GetModPlayer<ModAccessorySlotPlayer>();
                if (modSlotPlayer != null && slot >= modSlotPlayer.SlotCount)
                {
                    return true;
                }
            }

            // Cannot equip as an accessory if a Pinwheel mask is already equipped in the head armor slot
            if (IsPinwheelMask(player.armor[0]))
            {
                return false;
            }

            // Cannot equip if another Pinwheel mask is in any functional accessory slot
            for (int i = 3; i < 10; i++)
            {
                if (!modded && i == slot)
                {
                    continue;
                }

                if (IsPinwheelMask(player.armor[i]))
                {
                    return false;
                }
            }

            // Check modded functional accessory slots
            try
            {
                var modSlotPlayer = player.GetModPlayer<ModAccessorySlotPlayer>();
                if (modSlotPlayer != null)
                {
                    var loader = LoaderManager.Get<AccessorySlotLoader>();
                    for (int i = 0; i < modSlotPlayer.SlotCount; i++)
                    {
                        if (modded && i == slot)
                        {
                            continue;
                        }

                        var slotInstance = loader.Get(i, player);
                        if (slotInstance?.FunctionalItem != null && !slotInstance.FunctionalItem.IsAir && IsPinwheelMask(slotInstance.FunctionalItem))
                        {
                            return false;
                        }
                    }
                }
            }
            catch
            {
                // Ignore if modded slots not ready
            }

            return true;
        }

        public static bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem)
        {
            if (IsPinwheelMask(equippedItem) && IsPinwheelMask(incomingItem))
            {
                return false;
            }

            return true;
        }
    }
}
