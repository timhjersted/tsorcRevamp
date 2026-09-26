using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Head, EquipType.Face)]
    public class MaskOfTheChild : ModItem
    {
        public static float MoveSpeedMult = 25f;
        public static float StaminaRegen = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeedMult, StaminaRegen);

        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;

            int faceSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Face);
            ArmorIDs.Face.Sets.PreventHairDraw[faceSlot] = true;
            ArmorIDs.Face.Sets.OverrideHelmet[faceSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.defense = 7;
            Item.accessory = true;
            Item.width = 44;
            Item.height = 40;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            ApplyMaskEffects(player);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Defense is only active when equipped in head armor slot
            player.statDefense -= Item.defense;

            // Head armor mask takes priority; don't stack if equipped simultaneously
            if (PinwheelMaskHelper.IsPinwheelMask(player.armor[0]))
            {
                return;
            }

            ApplyMaskEffects(player);
        }

        private void ApplyMaskEffects(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.equippedPinwheelMask)
            {
                return;
            }
            modPlayer.equippedPinwheelMask = true;

            player.moveSpeed += MoveSpeedMult / 100f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += StaminaRegen / 100f;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            if (!PinwheelMaskHelper.CanEquipAccessory(player, slot, modded))
            {
                return false;
            }

            return base.CanEquipAccessory(player, slot, modded);
        }

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            if (!PinwheelMaskHelper.CanAccessoryBeEquippedWith(equippedItem, incomingItem))
            {
                return false;
            }

            return base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
        }
    }
}
