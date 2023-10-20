using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class DragoonHorn : ModItem
    {
        public static float MeleeDmgMult = 50f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeDmgMult);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.expert = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().DragoonHorn = true;
        }

    }
}
