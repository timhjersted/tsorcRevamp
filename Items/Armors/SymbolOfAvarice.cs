using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]

    public class SymbolOfAvarice : ModItem
    {
        public static float SoulAmplifier = 40f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SoulAmplifier);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 28;
            Item.defense = 2;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SOADrain = true;
        }
    }
}
