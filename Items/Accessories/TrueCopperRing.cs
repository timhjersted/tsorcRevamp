using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class TrueCopperRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Grants damage reduction");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 22;
            Item.defense = 3;
            Item.accessory = true;
            Item.value = PriceByRarity.White_0; // i didnt think this would actually get used...
            Item.rare = ItemRarityID.White;
        }
        public override void UpdateEquip(Player player)
        {
            player.endurance += 1f;
        }
    }
}
