using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp {
    public static class RecipeHelper {

        public static void RecipeRemover(int ItemRecipeToRemove) {
                RecipeFinder finder = new RecipeFinder();
                finder.SetResult(ItemRecipeToRemove);

                foreach (Recipe recipe in finder.SearchRecipes()) {
                    RecipeEditor editor = new RecipeEditor(recipe);
                    editor.DeleteRecipe();
                }
            
        }

        public static void RecipeIngredientAdder(int ItemRecipeToEdit, int ItemIngredientToAdd, int ItemCount = 1) {
            RecipeFinder finder = new RecipeFinder();
            finder.SetResult(ItemRecipeToEdit);

            foreach (Recipe recipe in finder.SearchRecipes()) {
                RecipeEditor editor = new RecipeEditor(recipe);
                editor.AddIngredient(ItemIngredientToAdd, ItemCount);
            }
        }

        public static void ExactRecipeRemover2Ingredients(int Ingredient1, int Ingredient1Amount, int Ingredient2, int Ingredient2Amount, int CraftingStation, int RecipeResult) {
            RecipeFinder finder = new RecipeFinder();
            finder.AddIngredient(Ingredient1, Ingredient1Amount);
            finder.AddIngredient(Ingredient2, Ingredient2Amount);
            finder.AddTile(CraftingStation);
            finder.SetResult(RecipeResult);
            Recipe locateRecipe = finder.FindExactRecipe();

            bool recipeFound = locateRecipe != null;
            if (recipeFound) {
                RecipeEditor editor = new RecipeEditor(locateRecipe);
                editor.DeleteRecipe();
            }
        }
        public static void EditRecipes() {
            RecipeRemover(ItemID.AdamantiteDrill);
            RecipeRemover(ItemID.AdamantitePickaxe);
            RecipeRemover(ItemID.AngelWings);
            RecipeRemover(ItemID.CobaltDrill);
            RecipeRemover(ItemID.CobaltPickaxe);
            RecipeRemover(ItemID.MythrilDrill);
            RecipeRemover(ItemID.MythrilPickaxe);
            RecipeRemover(ItemID.BladeofGrass);
            RecipeRemover(ItemID.RopeCoil);
            RecipeRemover(ItemID.VineRopeCoil);
            RecipeIngredientAdder(ItemID.IvyWhip, ItemID.SoulofNight, 3);
            RecipeIngredientAdder(ItemID.GrapplingHook, ItemID.SoulofNight, 6);
            ExactRecipeRemover2Ingredients(ItemID.Hellstone, 3, ItemID.BottledWater, 1, TileID.ImbuingStation, ItemID.FlaskofFire);
        }
    }
}
