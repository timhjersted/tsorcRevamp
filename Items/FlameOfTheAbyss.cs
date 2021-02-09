using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    public class FlameOfTheAbyss : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Flame of the Abyss");
            Tooltip.SetDefault("Dropped from a fallen soul that has traveled through the abyss.");
        }

        public override void SetDefaults() {
            item.width = 14;
            item.height = 16;
            item.rare = ItemRarityID.Orange;
            item.value = 50000;
            item.maxStack = 250;
        }
    }
}
