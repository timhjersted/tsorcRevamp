using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class KrakenFlesh : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 42;
            Item.maxStack = 9999;
            Item.value = 250000;
            Item.rare = ItemRarityID.Red;
        }
    }
}
