using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Lore
{
    class AbyssScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.consumable = false;
            Item.value = 50000;
            Item.rare = ItemRarityID.Purple;
        }
    }
}