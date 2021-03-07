using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class GemBox_Global : GlobalItem {

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BottledWater, 1);
            recipe.AddIngredient(ItemID.Deathweed, 1);
            recipe.AddIngredient(ItemID.Fireblossom, 4);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(ItemID.FlaskofFire, 1);
            recipe.AddRecipe();
        }

        public override float UseTimeMultiplier(Item item, Player player) {
            if ((player.inventory[player.selectedItem].magic) &&(Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().GemBox)) {
                return 2f;
            }
            else return 1f;
        }
    }
}
