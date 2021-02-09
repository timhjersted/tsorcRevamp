using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class DragonScale : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Used to craf tthe Stone of Seath, among other things");
        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.Orange;
        }
    }
}
