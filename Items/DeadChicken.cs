using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class DeadChicken : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Can be cooked at a cooking pot to make 1 Cooked Chicken" +
                                "\nCooked chicken heals 100 HP and has 30 seconds of potion cooldown. Wow! Tasty!");
        }

        public override void SetDefaults() {
            item.height = 12;
            item.width = 24;
            item.maxStack = 30;
            item.value = 2;
        }
    }
}
