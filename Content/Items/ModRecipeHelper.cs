using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Potions;

namespace tsorcRevamp.Content.Items
{
    class ModRecipeHelper
    {

        public static void AddRecipes()
        {
            Mod mod = ModContent.GetInstance<tsorcRevamp>();



            #region add s tier potion recipes
            PermaPotionRecipeS(mod, ModContent.ItemType<ArmorDrugPotion>(), ModContent.ItemType<PermanentArmorDrug>());
            PermaPotionRecipeS(mod, ModContent.ItemType<BattlefrontPotion>(), ModContent.ItemType<PermanentBattlefrontPotion>());
            PermaPotionRecipeS(mod, ModContent.ItemType<DemonDrugPotion>(), ModContent.ItemType<PermanentDemonDrug>());
            PermaPotionRecipeS(mod, ModContent.ItemType<StrengthPotion>(), ModContent.ItemType<PermanentStrengthPotion>());
            PermaPotionRecipeS(mod, ItemID.EndurancePotion, ModContent.ItemType<PermanentEndurancePotion>());
            PermaPotionRecipeS(mod, ItemID.LifeforcePotion, ModContent.ItemType<PermanentLifeforcePotion>());
            PermaPotionRecipeS(mod, ItemID.MagicPowerPotion, ModContent.ItemType<PermanentMagicPowerPotion>());
            PermaPotionRecipeS(mod, ItemID.FeatherfallPotion, ModContent.ItemType<PermanentFeatherfallPotion>());
            #endregion

            #region add a tier recipes
            PermaPotionRecipeA(mod, ItemID.Ale, ModContent.ItemType<PermanentTipsy>());
            PermaPotionRecipeA(mod, ItemID.CalmingPotion, ModContent.ItemType<PermanentCalmingPotion>());
            PermaPotionRecipeA(mod, ItemID.ArcheryPotion, ModContent.ItemType<PermanentArcheryPotion>());
            PermaPotionRecipeA(mod, ItemID.BattlePotion, ModContent.ItemType<PermanentBattlePotion>());
            PermaPotionRecipeA(mod, ItemID.RagePotion, ModContent.ItemType<PermanentRagePotion>());
            PermaPotionRecipeA(mod, ItemID.SpelunkerPotion, ModContent.ItemType<PermanentSpelunkerPotion>());
            PermaPotionRecipeA(mod, ItemID.SwiftnessPotion, ModContent.ItemType<PermanentSwiftnessPotion>());
            PermaPotionRecipeA(mod, ItemID.SummoningPotion, ModContent.ItemType<PermanentSummoningPotion>());
            PermaPotionRecipeA(mod, ModContent.ItemType<CrimsonPotion>(), ModContent.ItemType<PermanentCrimsonPotion>());
            PermaPotionRecipeA(mod, ModContent.ItemType<GreenBlossom>(), ModContent.ItemType<PermanentGreenBlossom>());
            #endregion

            #region add b tier recipes
            PermaPotionRecipeB(mod, ModContent.ItemType<BoostPotion>(), ModContent.ItemType<PermanentBoostPotion>());
            PermaPotionRecipeB(mod, ItemID.ManaRegenerationPotion, ModContent.ItemType<PermanentManaRegenerationPotion>());
            PermaPotionRecipeB(mod, ItemID.WrathPotion, ModContent.ItemType<PermanentWrathPotion>());
            PermaPotionRecipeB(mod, ItemID.CratePotion, ModContent.ItemType<PermanentCratePotion>());
            PermaPotionRecipeB(mod, ItemID.FishingPotion, ModContent.ItemType<PermanentFishingPotion>());
            PermaPotionRecipeB(mod, ItemID.SonarPotion, ModContent.ItemType<PermanentSonarPotion>());
            PermaPotionRecipeB(mod, ItemID.GillsPotion, ModContent.ItemType<PermanentGillsPotion>());
            PermaPotionRecipeB(mod, ItemID.HeartreachPotion, ModContent.ItemType<PermanentHeartreachPotion>());
            PermaPotionRecipeB(mod, ItemID.IronskinPotion, ModContent.ItemType<PermanentIronskinPotion>());
            PermaPotionRecipeB(mod, ItemID.MiningPotion, ModContent.ItemType<PermanentMiningPotion>());
            PermaPotionRecipeB(mod, ItemID.RegenerationPotion, ModContent.ItemType<PermanentRegenerationPotion>());
            PermaPotionRecipeB(mod, ModContent.ItemType<ShockwavePotion>(), ModContent.ItemType<PermanentShockwavePotion>());
            PermaPotionRecipeB(mod, ItemID.TitanPotion, ModContent.ItemType<PermanentTitanPotion>());
            PermaPotionRecipeB(mod, ItemID.WaterWalkingPotion, ModContent.ItemType<PermanentWaterWalkingPotion>());
            #endregion

            #region add c tier recipes
            PermaPotionRecipeC(mod, ItemID.AmmoReservationPotion, ModContent.ItemType<PermanentAmmoReservationPotion>());
            PermaPotionRecipeC(mod, ItemID.BuilderPotion, ModContent.ItemType<PermanentBuilderPotion>());
            PermaPotionRecipeC(mod, ItemID.ShinePotion, ModContent.ItemType<PermanentShinePotion>());
            PermaPotionRecipeC(mod, ItemID.TrapsightPotion, ModContent.ItemType<PermanentDangersensePotion>());
            PermaPotionRecipeC(mod, ItemID.FlipperPotion, ModContent.ItemType<PermanentFlipperPotion>());
            PermaPotionRecipeC(mod, ItemID.HunterPotion, ModContent.ItemType<PermanentHunterPotion>());
            PermaPotionRecipeC(mod, ItemID.InvisibilityPotion, ModContent.ItemType<PermanentInvisibilityPotion>());
            PermaPotionRecipeC(mod, ItemID.NightOwlPotion, ModContent.ItemType<PermanentNightOwlPotion>());
            PermaPotionRecipeC(mod, ItemID.ThornsPotion, ModContent.ItemType<PermanentThornsPotion>());
            PermaPotionRecipeC(mod, ItemID.InfernoPotion, ModContent.ItemType<PermanentInfernoPotion>());
            PermaPotionRecipeC(mod, ItemID.WarmthPotion, ModContent.ItemType<PermanentWarmthPotion>());
            #endregion

            #region special perma recipes
            Recipe recipe = Recipe.Create(ModContent.ItemType<PermanentGravitationPotion>(), 1)
            .AddIngredient(ModContent.ItemType<DarkSoulItem>(), 31000)
            .AddIngredient(ItemID.GravitationPotion)
            .AddIngredient(ItemID.SoulofFlight)
            .AddIngredient(ModContent.ItemType<EternalCrystal>(), 4)
            .AddTile(TileID.DemonAltar);
            recipe.Register();
            
            recipe = Recipe.Create(ModContent.ItemType<PermanentLuckPotion>(), 1)
            .AddIngredient(ModContent.ItemType<DarkSoulItem>(), 4444)
            .AddIngredient(ItemID.Waterleaf, 4)
            .AddIngredient(ItemID.LuckPotionLesser, 4)
            .AddIngredient(ItemID.LuckPotion, 4)
            .AddIngredient(ItemID.LuckPotionGreater, 4)
            .AddIngredient(ModContent.ItemType<EternalCrystal>())
            .AddTile(TileID.DemonAltar);
            recipe.Register();
            #endregion

            #region Permanent food recipes
            
            recipe = Recipe.Create(ModContent.ItemType<PermanentExquisitelyStuffed>())
                .AddIngredient(ModContent.ItemType<DarkSoulItem>(), 18000)
                .AddIngredient(ModContent.ItemType<PermanentPlentySatisfied>())
                .AddIngredient(ModContent.ItemType<EternalCrystal>(), 2)
                .AddTile(TileID.DemonAltar);
            recipe.Register();
            
            recipe = Recipe.Create(ModContent.ItemType<PermanentPlentySatisfied>())
                .AddIngredient(ModContent.ItemType<DarkSoulItem>(), 8000)
                .AddIngredient(ModContent.ItemType<PermanentWellFed>())
                .AddIngredient(ModContent.ItemType<EternalCrystal>(), 1)
                .AddTile(TileID.DemonAltar);
            recipe.Register();
            
            PermaPotionRecipeC(mod, ItemID.Teacup, ModContent.ItemType<PermanentWellFed>());

            #endregion

            #region Flask recipes
            
            PermaFlaskRecipe(mod, ModContent.ItemType<PermanentAbyssImbuement>(), 
                ModContent.ItemType<AbyssFlask>(), ModContent.ItemType<PermanentVenomImbuement>(), 7000, 1);
            PermaFlaskRecipe(mod, ModContent.ItemType<PermanentAbyssImbuement>(), 
                ModContent.ItemType<AbyssFlask>(),ModContent.ItemType<PermanentNanitesImbuement>(), 5500, 1);
            PermaFlaskRecipe(mod, ModContent.ItemType<PermanentVenomImbuement>(),
                ItemID.FlaskofVenom, ModContent.ItemType<PermanentIchorImbuement>(), 3500, 1);
            PermaFlaskRecipe(mod, ModContent.ItemType<PermanentNanitesImbuement>(),
                ItemID.FlaskofNanites, ModContent.ItemType<PermanentCursedFlamesImbuement>(), 5000, 1);
            PermaFlaskRecipe(mod, ModContent.ItemType<PermanentIchorImbuement>(),
                ItemID.FlaskofIchor, ModContent.ItemType<PermanentPoisonImbuement>(), 3000, 1);
            PermaFlaskRecipe(mod, ModContent.ItemType<PermanentCursedFlamesImbuement>(),
                ItemID.FlaskofCursedFlames, ModContent.ItemType<PermanentFireImbuement>(), 2000, 1);
            PermaFlaskRecipe0(mod, ModContent.ItemType<PermanentFireImbuement>(), ItemID.FlaskofFire, 2000, 1);
            PermaFlaskRecipe0(mod, ModContent.ItemType<PermanentPoisonImbuement>(), ItemID.FlaskofPoison, 1000, 1);
            PermaFlaskRecipe0(mod, ModContent.ItemType<PermanentGoldImbuement>(), ItemID.FlaskofGold, 1000, 1);
            PermaFlaskRecipe0(mod, ModContent.ItemType<PermanentPartyImbuement>(), ItemID.FlaskofParty, 500, 1);

                #endregion


            /*recipe = Recipe.Create(ItemID.LesserManaPotion, 10)
            .AddIngredient(ItemID.FallenStar)
            .AddIngredient(ItemID.Gel, 2)
            .AddIngredient(ItemID.Bottle, 10)
            .AddTile(TileID.Bottles);
            recipe.Register();*/
            




            recipe = Recipe.Create(ItemID.MagicMirror)
            .AddRecipeGroup(tsorcRevampSystems.UpgradedMirrors)
            .AddTile(TileID.DemonAltar)
            .AddOnCraftCallback(delegate
            { //refund the player's souls when they revert to a base mirror
                Item.NewItem(new EntitySource_Misc("¯\\_(ツ)_/¯"), Main.LocalPlayer.getRect(), ModContent.ItemType<DarkSoulItem>(), 100);
            }
            );
            recipe.Register();

            recipe = Recipe.Create(ItemID.WormholePotion)
            .AddIngredient(ItemID.BottledWater)
            .AddTile(TileID.DemonAltar);
            recipe.Register();
        }

        #region permanent potion recipes
        /// <summary>
        /// 31k Dark Souls, 4 Eternal Crystal
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="IngredientPotion"></param>
        /// <param name="ResultPotion"></param>
        public static void PermaPotionRecipeS(Mod mod, int IngredientPotion, int ResultPotion)
        {
            Recipe recipe = Recipe.Create(ResultPotion, 1)
            .AddIngredient(ModContent.ItemType<DarkSoulItem>(), 31000)
            .AddIngredient(IngredientPotion)
            .AddIngredient(ModContent.ItemType<EternalCrystal>(), 4)
            .AddTile(TileID.DemonAltar);
            recipe.Register();
        }
        /// <summary>
        /// 16k Dark Souls, 3 Eternal Crystal
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="IngredientPotion"></param>
        /// <param name="ResultPotion"></param>
        public static void PermaPotionRecipeA(Mod mod, int IngredientPotion, int ResultPotion)
        {
            Recipe recipe = Recipe.Create(ResultPotion, 1)
            .AddIngredient(ModContent.ItemType<DarkSoulItem>(), 16000)
            .AddIngredient(IngredientPotion)
            .AddIngredient(ModContent.ItemType<EternalCrystal>(), 3)
            .AddTile(TileID.DemonAltar);
            recipe.Register();
        }
        /// <summary>
        /// 9k Dark Souls, 2 Eternal Crystal
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="IngredientPotion"></param>
        /// <param name="ResultPotion"></param>
        public static void PermaPotionRecipeB(Mod mod, int IngredientPotion, int ResultPotion)
        {
            Recipe recipe = Recipe.Create(ResultPotion, 1)
            .AddIngredient(ModContent.ItemType<DarkSoulItem>(), 9000)
            .AddIngredient(IngredientPotion)
            .AddIngredient(ModContent.ItemType<EternalCrystal>(), 2)
            .AddTile(TileID.DemonAltar);
            recipe.Register();
        }
        /// <summary>
        /// 5k Dark Souls, 1 Eternal Crystal
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="IngredientPotion"></param>
        /// <param name="ResultPotion"></param>
        public static void PermaPotionRecipeC(Mod mod, int IngredientPotion, int ResultPotion)
        {
            Recipe recipe = Recipe.Create(ResultPotion, 1)
            .AddIngredient(ModContent.ItemType<DarkSoulItem>(), 5000)
            .AddIngredient(IngredientPotion)
            .AddIngredient(ModContent.ItemType<EternalCrystal>())
            .AddTile(TileID.DemonAltar);
            recipe.Register();
        }

        public static void PermaFlaskRecipe(Mod mod, int result, int ingredientFlask, int ingredientPermaFlask, int soulCost, int crystalCost)
        {
            Recipe recipe = Recipe.Create(result)
                .AddIngredient(ingredientFlask)
                .AddIngredient(ingredientPermaFlask)
                .AddIngredient(ModContent.ItemType<DarkSoulItem>(), soulCost)
                .AddIngredient(ModContent.ItemType<EternalCrystal>(), crystalCost)
                .AddTile(TileID.DemonAltar);
            recipe.Register();
        }
        public static void PermaFlaskRecipe0(Mod mod, int result, int ingredientFlask, int soulCost, int crystalCost)
        {
            Recipe recipe = Recipe.Create(result)
                .AddIngredient(ingredientFlask)
                .AddIngredient(ModContent.ItemType<DarkSoulItem>(), soulCost)
                .AddIngredient(ModContent.ItemType<EternalCrystal>(), crystalCost)
                .AddTile(TileID.DemonAltar);
            recipe.Register();
        }
        #endregion

    }
}
