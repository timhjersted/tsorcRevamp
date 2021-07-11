using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class WoodenWand : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("An unenchanted wooden wand. \nCan be upgraded many different ways.");
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
