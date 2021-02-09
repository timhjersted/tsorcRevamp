using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class CopperRing : ModItem {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            item.defense = 2;
            item.accessory = true;
            item.value = 100;
            item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CopperBar, 2);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

 

    }
}
