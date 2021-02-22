using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class GoldenHairpin : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Halves the mana needed for spells");

        }

        public override void SetDefaults() {

            item.width = 32;
            item.height = 26;
            item.accessory = true;
            item.rare = ItemRarityID.LightRed;
            item.value = 340000;
        }

        public override void UpdateEquip(Player player) {
            player.manaCost -= 0.50f;
        }
    }
}
