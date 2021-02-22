using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class GemBox : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Double cast all spells" +
                               "\nReduce magic damage and increase mana cost by 20%");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.value = 270000;
            item.rare = ItemRarityID.Pink;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.magicDamage -= 0.2f;
            player.manaCost += 0.2f;
            if (player.inventory[player.selectedItem].magic) {
                player.inventory[player.selectedItem].useTime = 5;
                player.inventory[player.selectedItem].useAnimation = 10;
            }
        }
    }
}
