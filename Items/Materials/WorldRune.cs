using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Materials
{
    class WorldRune : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemNoGravity[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 42;
            Item.maxStack = 999;
            Item.value = 250000;
            Item.rare = ItemRarityID.Blue;
        }
    }
}
