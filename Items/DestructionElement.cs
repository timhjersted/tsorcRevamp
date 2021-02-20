using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class DestructionElement : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Used for making high damage guns.");
        }

        public override void SetDefaults() {
            item.width = 32;
            item.height = 32;
            item.rare = ItemRarityID.LightRed;
            item.value = 250000;
            item.maxStack = 4;
        }
    }
}
