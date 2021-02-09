using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class FlameOfTheAbyss : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Flame of the Abyss");
            Tooltip.SetDefault("Dropped from a fallen soul that has traveled through the abyss.");
        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.Orange;
            item.value = 50000;
            item.maxStack = 250;
        }
    }
}
