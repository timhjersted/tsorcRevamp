using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class WoodenWand : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Can be upgraded in 3 different ways with 150 or 2000 or (100 Crystal Shard + 6000) Dark Souls");
        }

        public override void SetDefaults() {
            item.width = 34;
            item.height = 34;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 25;
            item.useTime = 25;
            item.damage = 8;
            item.melee = true;
            item.value = 1000;
        }
    }
}
