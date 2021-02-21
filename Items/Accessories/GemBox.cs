using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class GemBox : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Double cast all spells, but increase mana cost and decrease magic damage by 20%");
        }

        public override void SetDefaults() {
            item.width = 32;
            item.height = 26;
            item.accessory = true;
            item.value = 800000;
            item.rare = ItemRarityID.Pink;
        }

        public override void UpdateEquip(Player player) {
            player.manaCost += 0.2f;
            player.magicDamage -= 0.2f;
        }
    }
}
