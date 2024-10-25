using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class MaskOfTheMother : ModItem
    {
        public static int MaxLifeIncrease = 60;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxLifeIncrease);
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.defense = 9;
            Item.width = 26;
            Item.height = 30;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.statLifeMax2 += MaxLifeIncrease;
        }
    }
}
