using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class TreasureMap : ModItem {

        //this thing has no drop location or recipe. what?
        public override bool Autoload(ref string name) => false;
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Shows location, and may reveal hidden rewards");

        }

        public override void SetDefaults() {
            item.accessory = true;
            item.width = 20;
            item.height = 20;
            item.rare = ItemRarityID.Blue;
            item.value = 9000000;
        }

        public override void UpdateEquip(Player player) {
            player.accDepthMeter = 1;
            player.accCompass = 1;
            player.findTreasure = true;
        }
    }
}
