using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class GoldenHairpin : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Halves the mana needed for spells");

        }

        public override void SetDefaults()
        {

            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
        }

        public override void UpdateEquip(Player player)
        {
            player.manaCost -= 0.50f;
        }
    }
}
