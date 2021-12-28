using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class ForgottenThunderBowScroll : ModItem {
        public override string Texture => "tsorcRevamp/Items/ForgottenIceBowScroll";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Used with 1 Soul of Artorias and 200000 Dark Soul at a Demon Altar\nSold by a powerful sorcerer");
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