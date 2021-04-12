using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class GemBox_Global : GlobalItem {

        public override float UseTimeMultiplier(Item item, Player player) {
            if ((player.inventory[player.selectedItem].magic) &&(Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().GemBox)) {
                return 2f;
            }
            else return 1f;
        }
    }
}
