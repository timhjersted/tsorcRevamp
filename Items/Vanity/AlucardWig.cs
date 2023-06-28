using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Vanity
{
    [AutoloadEquip(EquipType.Head)]
    public class AlucardWig : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Part of the TSORC Revamp Team dev set belonging to NephilimDeath");
        }

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 26;
            Item.height = 20;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
        }
    }
}