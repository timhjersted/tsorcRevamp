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

        public static void EditRecipes() {
            RecipeRemover(ItemID.AdamantiteDrill);
            RecipeRemover(ItemID.AdamantitePickaxe);
            RecipeRemover(ItemID.AngelWings);
            RecipeRemover(ItemID.CobaltDrill);
            RecipeRemover(ItemID.CobaltPickaxe);
            RecipeRemover(ItemID.MythrilDrill);
            RecipeRemover(ItemID.MythrilPickaxe);
            RecipeRemover(ItemID.BladeofGrass);
            RecipeIngredientAdder(ItemID.IvyWhip, ItemID.SoulofNight, 3);
        }
    }
}
