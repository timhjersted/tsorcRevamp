using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class MysteriousIdol : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("May attract the archaeologist to stay in your town if held in your inventory.");
        }

        public override void SetDefaults() {
            Item.width = 18;
            Item.height = 20;
            Item.rare = ItemRarityID.Blue;
            Item.value = 1000;
            Item.maxStack = 1;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.GoldBar, 3);
            recipe.AddIngredient(ItemID.Ruby, 5);
            recipe.AddIngredient(ItemID.Diamond, 5);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

    }
}
