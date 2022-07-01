using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp
{    
    public static class RecipeHelper
    {

        public static void RecipeRemover(int ItemRecipeToRemove)
        {
            //removes ANY recipe that results in ItemRecipeToRemove
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe recipe = Main.recipe[i];

                if (recipe.HasResult(ItemRecipeToRemove))
                {
                    recipe.DisableRecipe();
                }
            }

        }

        public static void RecipeIngredientAdder(int ItemRecipeToEdit, int ItemIngredientToAdd, int ItemCount = 1)
        {
            //any recipe that results in ItemRecipeToEdit will have ItemIngredientToAdd added to it, with ItemCount amount (default 1)

            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe recipe = Main.recipe[i];

                if (recipe.HasResult(ItemRecipeToEdit))
                {
                    recipe.AddIngredient(ItemIngredientToAdd, ItemCount);
                }
            }
        }

        public static void ExactRecipeRemover2Ingredients(int Ingredient1, int Ingredient2, int CraftingStation, int RecipeResult)
        {
            //this method is for when there's an item whose recipe needs to be removed, but we can't use RecipeRemover
            //that usually means we're giving it a custom recipe somewhere else, since RecipeRemover runs on any recipe that results in that item
            //using exact recipes is thus required. not sure if we need to do this again, but if we do, now theres a method

            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe recipe = Main.recipe[i];

                if (recipe.HasIngredient(Ingredient1)
                    && recipe.HasIngredient(Ingredient2)
                    && recipe.HasTile(CraftingStation)
                    && recipe.HasResult(RecipeResult))
                {
                    recipe.DisableRecipe();
                }
            }
        }
        public static void EditRecipes()
        {
            RecipeRemover(ItemID.AdamantiteDrill);
            RecipeRemover(ItemID.AdamantitePickaxe);
            RecipeRemover(ItemID.AngelWings);
            RecipeRemover(ItemID.CobaltDrill);
            RecipeRemover(ItemID.CobaltPickaxe);
            RecipeRemover(ItemID.MythrilDrill);
            RecipeRemover(ItemID.MythrilPickaxe);
            RecipeRemover(ItemID.MoltenPickaxe);
            RecipeRemover(ItemID.Sandgun);

            RecipeRemover(ItemID.OrichalcumDrill);
            RecipeRemover(ItemID.OrichalcumPickaxe);
            RecipeRemover(ItemID.PalladiumPickaxe);
            RecipeRemover(ItemID.PalladiumDrill);
            RecipeRemover(ItemID.TitaniumDrill);
            RecipeRemover(ItemID.TitaniumPickaxe);
            RecipeRemover(ItemID.Drax);
            RecipeRemover(ItemID.PickaxeAxe);

            RecipeRemover(ItemID.BladeofGrass);
            RecipeRemover(ItemID.RopeCoil);
            RecipeRemover(ItemID.VineRopeCoil);
            RecipeRemover(ItemID.WebRope);
            RecipeRemover(ItemID.WebRopeCoil);
            RecipeRemover(ItemID.SilkRopeCoil);

            RecipeRemover(ItemID.DemonWings);
            RecipeRemover(ItemID.FairyWings);
            RecipeRemover(ItemID.HarpyWings);
            RecipeRemover(ItemID.ButterflyWings);
            RecipeRemover(ItemID.BoneWings);
            RecipeRemover(ItemID.FlameWings);
            RecipeRemover(ItemID.FrozenWings);
            RecipeRemover(ItemID.BatWings);
            RecipeRemover(ItemID.BeeWings);
            RecipeRemover(ItemID.TatteredFairyWings);
            RecipeRemover(ItemID.SpookyWings);
            RecipeRemover(ItemID.GhostWings);
            RecipeRemover(ItemID.BeetleWings);
            RecipeRemover(ItemID.WingsSolar);
            RecipeRemover(ItemID.WingsNebula);
            RecipeRemover(ItemID.WingsStardust);
            RecipeRemover(ItemID.WingsVortex);

            RecipeRemover(ItemID.LargeAmber);
            RecipeRemover(ItemID.LargeAmethyst);
            RecipeRemover(ItemID.LargeDiamond);
            RecipeRemover(ItemID.LargeEmerald);
            RecipeRemover(ItemID.LargeRuby);
            RecipeRemover(ItemID.LargeSapphire);
            RecipeRemover(ItemID.LargeTopaz);

            RecipeRemover(ItemID.MechanicalEye);
            RecipeRemover(ItemID.MechanicalSkull);
            RecipeRemover(ItemID.MechanicalWorm);

            RecipeIngredientAdder(ItemID.IvyWhip, ItemID.SoulofNight, 6);
            RecipeIngredientAdder(ItemID.GrapplingHook, ItemID.SoulofNight, 6);
            RecipeIngredientAdder(ItemID.AmethystHook, ItemID.SoulofNight, 6);
            RecipeIngredientAdder(ItemID.TopazHook, ItemID.SoulofNight, 6);
            RecipeIngredientAdder(ItemID.SapphireHook, ItemID.SoulofNight, 6);
            RecipeIngredientAdder(ItemID.EmeraldHook, ItemID.SoulofNight, 6);
            RecipeIngredientAdder(ItemID.RubyHook, ItemID.SoulofNight, 6);
            RecipeIngredientAdder(ItemID.DiamondHook, ItemID.SoulofNight, 6);

            RecipeIngredientAdder(ItemID.AmethystRobe, ModContent.ItemType<Items.DarkSoul>(), 550);
            RecipeIngredientAdder(ItemID.TopazRobe, ModContent.ItemType<Items.DarkSoul>(), 600);
            RecipeIngredientAdder(ItemID.SapphireRobe, ModContent.ItemType<Items.DarkSoul>(), 650);
            RecipeIngredientAdder(ItemID.EmeraldRobe, ModContent.ItemType<Items.DarkSoul>(), 700);
            RecipeIngredientAdder(ItemID.RubyRobe, ModContent.ItemType<Items.DarkSoul>(), 750);
            RecipeIngredientAdder(ItemID.DiamondRobe, ModContent.ItemType<Items.DarkSoul>(), 800);

            ExactRecipeRemover2Ingredients(ItemID.Hellstone, ItemID.BottledWater, TileID.ImbuingStation, ItemID.FlaskofFire);
        }
    }
}
