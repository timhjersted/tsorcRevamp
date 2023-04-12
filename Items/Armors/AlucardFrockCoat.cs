using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class AlucardFrockCoat : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Part of the TSORC Revamp Team dev set belonging to NephilimDeath");
        }

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
        }
    }
}