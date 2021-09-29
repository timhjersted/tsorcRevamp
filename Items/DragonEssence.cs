using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class DragonEssence : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The essence of slain dragons.\nA rare and valuable material.");
        }

        public override void SetDefaults() {
            item.width = 18;
            item.height = 20;
            item.rare = ItemRarityID.Orange;
            item.value = 1000;
            item.maxStack = 250;
        }
    }
}
