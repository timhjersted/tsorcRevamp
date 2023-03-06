using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class WorldRune : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("An otherworldy artifact of great power" +
                "\nIt seems to react to dark souls..."); */

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
