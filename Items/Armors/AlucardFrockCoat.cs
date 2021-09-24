using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class AlucardFrockCoat : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Part of the TSORC Revamp Team dev set belonging to NephilimDeath");
        }

        public override void SetDefaults()
        {
            item.vanity = true;
            item.width = 18;
            item.height = 18;
            item.value = 100000;
            item.rare = ItemRarityID.Yellow;
        }
    }
}