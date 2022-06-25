using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class AlucardWig : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Part of the TSORC Revamp Team dev set belonging to NephilimDeath");
        }

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 26;
            Item.height = 20;
            Item.value = 100000;
            Item.rare = ItemRarityID.Yellow;
        }
    }
}