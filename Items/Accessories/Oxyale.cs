using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class Oxyale : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Gear worn by Dragoons\n" +
                                "Allows you to breathe underwater and negates water physics");

        }

        public override void SetDefaults() {

            item.width = 32;
            item.height = 26;
            item.accessory = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.Lime; //this thing is *technically* shm tier but wow is it useless at that point, lmao
            item.value = PriceByRarity.Lime_7;
        }

        public override void UpdateEquip(Player player) {
            if (player.wet) {
                player.gills = true;
                player.ignoreWater = true;
            }
        }
    }
}
