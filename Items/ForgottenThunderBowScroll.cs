using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class ForgottenThunderBowScroll : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Used to craft the Forgotten Thunder Bow, using 240,000 Dark Souls and 1 Soul of Artorias.");
        }
        public override void SetDefaults() {
            item.width = 12;
            item.height = 12;
            item.consumable = false;
            item.maxStack = 1;
            item.value = 5000000;
            item.rare = ItemRarityID.Pink;
        }
    }
}