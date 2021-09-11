using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class CompactFrame : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Used for making advanced weaponry, from a civilization not of this world\n" +
                               "The strange way it is affected by gravity reminds you of somewhere...");

            ItemID.Sets.ItemNoGravity[item.type] = true;
        }
        public override void SetDefaults() {
            item.width = 20;
            item.height = 18;
            item.maxStack = 4;
            item.value = 350000;
            item.rare = ItemRarityID.Red;
        }
    }
}
