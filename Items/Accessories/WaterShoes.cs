using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class WaterShoes : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The wearer can walk on water ");
        }

        public override void SetDefaults() {
            item.width = 26;
            item.height = 14;
            item.accessory = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.LightPurple;
            item.value = PriceByRarity.LightPurple_6;
        }

        public override void UpdateEquip(Player player) {
            player.waterWalk = true;
        }
    }
}
