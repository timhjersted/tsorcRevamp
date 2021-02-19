using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class FlaskOfFire : GlobalItem {

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            if (item.type == ItemID.FlaskofFire) {
                tooltips.Insert(3, new TooltipLine(mod, "", "Adds 10% melee damage"));
            }
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BottledWater, 1);
            recipe.AddIngredient(ItemID.Deathweed, 1);
            recipe.AddIngredient(ItemID.Diamond, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(ItemID.FlaskofFire, 1);
            recipe.AddRecipe();
        }
    }
}
