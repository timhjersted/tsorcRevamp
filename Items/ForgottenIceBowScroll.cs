using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items {
    class ForgottenIceBowScroll : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Used with 1 Soul of Artorias and 200000 Dark Soul at a Demon Altar");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Pink;
            item.width = 12;
            item.height = 12;
            item.value = 5000000;
        }

    }
}
