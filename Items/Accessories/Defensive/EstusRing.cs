using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class EstusRing : ModItem
    {
        public const int HealIncrease = 30;
        public const int PercentHealIncrease = 5;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(HealIncrease, PercentHealIncrease);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampEstusPlayer>().estusRing = true;
        }

    }
}
