using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Lore
{
    class BrokenPicksaw : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 21;
            Item.height = 21;
            Item.rare = ItemRarityID.White;
            Item.value = 1000;
        }
    }
}
