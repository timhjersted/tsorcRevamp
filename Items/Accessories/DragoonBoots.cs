using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class DragoonBoots : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Steel boots made for Dragoons." +
                                "\nNo damage from falling." +
                                "\nFaster jump, which also results in a higher jump." +
                                "\nPress `Z` to toggle high jump");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 30;
            item.accessory = true;
            item.value = 150000;
            item.rare = ItemRarityID.Pink;
        }

        public override void UpdateEquip(Player player) {
            player.noFallDmg = true;

        }
    }

}
