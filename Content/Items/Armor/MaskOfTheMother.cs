using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Head, EquipType.Face)]
    public class MaskOfTheMother : ModItem
    {
        public static int MaxLifeIncrease = 70;
        public static int LifeRegen = 2;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxLifeIncrease, LifeRegen);

        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;

            int faceSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Face);
            ArmorIDs.Face.Sets.PreventHairDraw[faceSlot] = true;
            ArmorIDs.Face.Sets.OverrideHelmet[faceSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.defense = 9;
            Item.accessory = true;
            Item.width = 26;
            Item.height = 30;
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

            player.statLifeMax2 += MaxLifeIncrease;
            player.lifeRegen += LifeRegen;
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
