using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    [AutoloadEquip(EquipType.Shield)]
    public class MythrilBulwark : ModItem
    {
        public static float Vulnerability = 25f;
        public static int VulnerabilityDuration = 10;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Vulnerability, VulnerabilityDuration);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.defense = 3;
            Item.expert = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.Expert;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().MythrilBulwark = true;
        }
    }
}

