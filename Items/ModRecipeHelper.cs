using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Potions.PermanentPotions;

namespace tsorcRevamp.Items
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
            PermaPotionRecipeS(mod, ItemID.ManaRegenerationPotion, ModContent.ItemType<PermanentManaRegenerationPotion>());
            PermaPotionRecipeS(mod, ItemID.GoldenDelight, ModContent.ItemType<PermanentExquisitelyStuffed>());
            PermaPotionRecipeS(mod, ItemID.FeatherfallPotion, ModContent.ItemType<PermanentFeatherfallPotion>());
            PermaPotionRecipeS(mod, ItemID.FlaskofVenom, ModContent.ItemType<PermanentVenomImbuement>());
            PermaPotionRecipeS(mod, ItemID.FlaskofNanites, ModContent.ItemType<PermanentNanitesImbuement>());
            #endregion

            #region add a tier recipes
            PermaPotionRecipeA(mod, ItemID.Ale, ModContent.ItemType<PermanentTipsy>());
            PermaPotionRecipeA(mod, ItemID.CalmingPotion, ModContent.ItemType<PermanentCalmingPotion>());
            PermaPotionRecipeA(mod, ItemID.ArcheryPotion, ModContent.ItemType<PermanentArcheryPotion>());
            PermaPotionRecipeA(mod, ItemID.BattlePotion, ModContent.ItemType<PermanentBattlePotion>());
            PermaPotionRecipeA(mod, ItemID.MagicPowerPotion, ModContent.ItemType<PermanentMagicPowerPotion>());
            PermaPotionRecipeA(mod, ItemID.RagePotion, ModContent.ItemType<PermanentRagePotion>());
            PermaPotionRecipeA(mod, ItemID.WrathPotion, ModContent.ItemType<PermanentWrathPotion>());
            PermaPotionRecipeA(mod, ItemID.SpelunkerPotion, ModContent.ItemType<PermanentSpelunkerPotion>());
            PermaPotionRecipeA(mod, ItemID.SwiftnessPotion, ModContent.ItemType<PermanentSwiftnessPotion>());
            PermaPotionRecipeA(mod, ItemID.SummoningPotion, ModContent.ItemType<PermanentSummoningPotion>());
            PermaPotionRecipeA(mod, ModContent.ItemType<CrimsonPotion>(), ModContent.ItemType<PermanentCrimsonPotion>());
            PermaPotionRecipeA(mod, ModContent.ItemType<GreenBlossom>(), ModContent.ItemType<PermanentGreenBlossom>());
            PermaPotionRecipeA(mod, ItemID.FlaskofIchor, ModContent.ItemType<PermanentIchorImbuement>());
            PermaPotionRecipeA(mod, ItemID.FlaskofCursedFlames, ModContent.ItemType<PermanentCursedFlamesImbuement>());
            #endregion

            #region add b tier recipes
            PermaPotionRecipeB(mod, ModContent.ItemType<BoostPotion>(), ModContent.ItemType<PermanentBoostPotion>());
            PermaPotionRecipeB(mod, ItemID.AmmoReservationPotion, ModContent.ItemType<PermanentAmmoReservationPotion>());
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
            PermaPotionRecipeB(mod, ItemID.BowlofSoup, ModContent.ItemType<PermanentPlentySatisfied>());
            PermaPotionRecipeB(mod, ItemID.FlaskofFire, ModContent.ItemType<PermanentFireImbuement>());
            PermaPotionRecipeB(mod, ItemID.FlaskofParty, ModContent.ItemType<PermanentPartyImbuement>());
            PermaPotionRecipeB(mod, ItemID.FlaskofGold, ModContent.ItemType<PermanentGoldImbuement>());
            #endregion

            #region add c tier recipes
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
            PermaPotionRecipeC(mod, ItemID.Teacup, ModContent.ItemType<PermanentWellFed>());
            PermaPotionRecipeC(mod, ItemID.FlaskofPoison, ModContent.ItemType<PermanentPoisonImbuement>());
            #endregion

            #region special perma recipes
            Recipe recipe = Recipe.Create(ModContent.ItemType<PermanentGravitationPotion>(), 1)
            .AddIngredient(ModContent.ItemType<DarkSoul>(), 31000)
            .AddIngredient(ItemID.GravitationPotion)
            .AddIngredient(ItemID.SoulofFlight)
            .AddIngredient(ModContent.ItemType<EternalCrystal>(), 4)
            .AddTile(TileID.DemonAltar);
            recipe.Register();
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
                Item.NewItem(new EntitySource_Misc("¯\\_(ツ)_/¯"), Main.LocalPlayer.getRect(), ModContent.ItemType<DarkSoul>(), 100);
            }
            );
            recipe.Register();

            recipe = Recipe.Create(ItemID.WormholePotion)
            .AddIngredient(ItemID.BottledWater)
            .AddTile(TileID.DemonAltar);            
            recipe.Register();
        }

        #region permanent potion recipes
        public static void PermaPotionRecipeS(Mod mod, int IngredientPotion, int ResultPotion)
        {
            Recipe recipe = Recipe.Create(ResultPotion, 1)
            .AddIngredient(ModContent.ItemType<DarkSoul>(), 31000)
            .AddIngredient(IngredientPotion)
            .AddIngredient(ModContent.ItemType<EternalCrystal>(), 4)
            .AddTile(TileID.DemonAltar);
            recipe.Register();
        }
        public static void PermaPotionRecipeA(Mod mod, int IngredientPotion, int ResultPotion)
        {
            Recipe recipe = Recipe.Create(ResultPotion, 1)
            .AddIngredient(ModContent.ItemType<DarkSoul>(), 16000)
            .AddIngredient(IngredientPotion)
            .AddIngredient(ModContent.ItemType<EternalCrystal>(), 3)
            .AddTile(TileID.DemonAltar);
            recipe.Register();
        }
        public static void PermaPotionRecipeB(Mod mod, int IngredientPotion, int ResultPotion)
        {
            Recipe recipe = Recipe.Create(ResultPotion, 1)
            .AddIngredient(ModContent.ItemType<DarkSoul>(), 9000)
            .AddIngredient(IngredientPotion)
            .AddIngredient(ModContent.ItemType<EternalCrystal>(), 2)
            .AddTile(TileID.DemonAltar);
            recipe.Register();
        }
        public static void PermaPotionRecipeC(Mod mod, int IngredientPotion, int ResultPotion)
        {
            Recipe recipe = Recipe.Create(ResultPotion, 1)
            .AddIngredient(ModContent.ItemType<DarkSoul>(), 5000)
            .AddIngredient(IngredientPotion)
            .AddIngredient(ModContent.ItemType<EternalCrystal>())
            .AddTile(TileID.DemonAltar);
            recipe.Register();
        }
        #endregion

    }
}
