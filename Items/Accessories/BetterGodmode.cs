using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    class BetterGodmode : ModItem {

        public override string Texture => "tsorcRevamp/Items/Potions/HolyWarElixir";

        public override void SetDefaults() {
            item.width = 1;
            item.height = 1;
            item.value = 69; //i am the master of comedy
            item.accessory = true;
            item.rare = ItemRarityID.Expert;
        }

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("For testing purposes only \n\"For when God Mode just isnt enough...\"");
        }

        public override void UpdateEquip(Player player) {
            player.immune = true;
            player.lifeRegen += 8000;
        }
    }
}
