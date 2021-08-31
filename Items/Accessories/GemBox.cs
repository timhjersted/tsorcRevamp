using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class GemBox : ModItem {
        public override void SetStaticDefaults() { //TODO "Double cast all spells"? maybe some day
            Tooltip.SetDefault("All spells have doubled speed and 25% reduced mana cost" +
                               "\nReduces magic damage by a flat 35%");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.value = 270000;
            item.rare = ItemRarityID.Pink;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.magicDamage *= .65f;
            player.manaCost -= 0.25f;
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().GemBox = true;
        }
    }
}
