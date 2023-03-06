using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class DragoonHorn : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Horn inhabited by the spirit of a dragon." +
                                "\n50% increased melee damage if falling."); */
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
