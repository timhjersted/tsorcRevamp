using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Mobility.Wings
{
    public class SupersonicWingSlot : ModAccessorySlot
    {
        public override bool IsEnabled()
        {
            if (Main.gameMenu) return false;
            return Player.GetModPlayer<tsorcRevampPlayer>().hasSupersonicHarness && SoulsModeMobility.Enabled(Player);
        }

        public override bool CanAcceptItem(Item checkItem, AccessorySlotType context)
        {
            if (context == AccessorySlotType.FunctionalSlot || context == AccessorySlotType.VanitySlot)
            {
                if (checkItem.type == ModContent.ItemType<SupersonicWings>() || checkItem.type == ModContent.ItemType<SupersonicWings2>())
                {
                    return false;
                }
                return checkItem.wingSlot > 0;
            }
            return base.CanAcceptItem(checkItem, context);
        }

        public override bool ModifyDefaultSwapSlot(Item item, int accSlotToSwapTo)
        {
            if (item.wingSlot > 0 && item.type != ModContent.ItemType<SupersonicWings>() && item.type != ModContent.ItemType<SupersonicWings2>())
            {
                return true;
            }
            return false;
        }

        public override void ApplyEquipEffects()
        {
            var modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
            if (FunctionalItem != null && !FunctionalItem.IsAir && FunctionalItem.wingSlot > 0)
            {
                modPlayer.hasSlottedWing = true;
                modPlayer.slottedWingSlot = FunctionalItem.wingSlot;
                modPlayer.slottedWingHideVisual = HideVisuals;
                Player.wingsLogic = FunctionalItem.wingSlot;
                Player.equippedWings = FunctionalItem;
            }
            if (VanityItem != null && !VanityItem.IsAir && VanityItem.wingSlot > 0)
            {
                modPlayer.slottedWingVanitySlot = VanityItem.wingSlot;
            }
            base.ApplyEquipEffects();
        }

        public override void OnMouseHover(AccessorySlotType context)
        {
            switch (context)
            {
                case AccessorySlotType.FunctionalSlot:
                    Main.hoverItemName = Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.SupersonicWingSlotHover", "Wing Harness Slot (Wings Only)");
                    break;
                case AccessorySlotType.VanitySlot:
                    Main.hoverItemName = Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.SupersonicWingSlotVanityHover", "Social Wings");
                    break;
            }
        }
    }
}
