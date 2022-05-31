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
            Item.width = 14;
            Item.height = 16;
            Item.rare = ItemRarityID.Orange;
            Item.value = 50000;
            Item.maxStack = 250;
        }
    }
}
