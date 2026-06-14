using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class AbyssalFabric : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 32;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = 250000;
            Item.rare = ItemRarityID.Purple;
        }
    }
}
