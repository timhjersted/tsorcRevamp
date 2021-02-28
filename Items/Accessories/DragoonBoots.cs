using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class DragoonBoots : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Steel Boots made for Dragoons.\n" +
                                "No damage from falling.\n" +
                                "Faster Jump, which also results in a higher jump.\n" +
                                "Press the Dragoon Boots key to toggle high jump (default Z)");

        }

        public override void SetDefaults() {
            item.accessory = true;
            item.width = 32;
            item.height = 26;
            item.maxStack = 1;
            item.rare = ItemRarityID.Orange;
            item.value = 150000;
        }
        public override void UpdateEquip(Player player) {
            player.noFallDmg = true;
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().DragoonBoots = true;
        }

    }
}
