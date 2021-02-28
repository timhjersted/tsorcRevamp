using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class Oxyale : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Gear worn by Dragoons.\n" +
                                "Allows you to breath underwater.");

        }

        public override void SetDefaults() {

            item.width = 32;
            item.height = 26;
            item.accessory = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.LightRed;
            item.value = 0;
        }

        public override void UpdateEquip(Player player) {
            if (player.wet) {
                player.gills = true;
            }
        }
    }
}
