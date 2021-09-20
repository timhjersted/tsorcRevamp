using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class SnarbolaxGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Part of the TSORC Revamp Team dev set belonging to ChromaEquinox");
        }

        public override void SetDefaults()
        {
            item.vanity = true;
            item.width = 30;
            item.height = 18;
            item.value = 50000;
            item.rare = ItemRarityID.Purple;
        }
    }
}