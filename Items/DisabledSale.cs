using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items {
    class DisabledSale : ModItem {
        public override void SetStaticDefaults() {
            // Tooltip.SetDefault("This sale has been deemed illegal! As such, I cannot sell it to you.");
        }

        public override void SetDefaults() {
            Item.rare = ItemRarityID.Red;
            Item.value = 999999999;
        }
    }
}
