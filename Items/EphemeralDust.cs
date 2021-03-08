using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class EphemeralDust : ModItem {

        public override void SetDefaults() {
            item.width = 16;
            item.height = 14;
            item.rare = ItemRarityID.Blue;
            item.value = 1000;
            item.maxStack = 250;
        }
    }
}
