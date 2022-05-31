using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class DragonEssence : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The essence of slain dragons.\nA rare and valuable material.");
        }

        public override void SetDefaults() {
            Item.width = 18;
            Item.height = 20;
            Item.rare = ItemRarityID.Orange;
            Item.value = 1000;
            Item.maxStack = 250;
        }
    }
}
