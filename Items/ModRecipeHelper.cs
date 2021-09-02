using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Localization;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Potions.PermanentPotions;

namespace tsorcRevamp.Items {
    class ModRecipeHelper {
        
        public static void AddModRecipes() {
            Mod mod = ModContent.GetInstance<tsorcRevamp>();
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                #region add s tier potion recipes
                PermaPotionRecipeS(mod, ModContent.ItemType<ArmorDrugPotion>(), ModContent.ItemType<PermanentArmorDrug>());
                PermaPotionRecipeS(mod, ModContent.ItemType<BattlefrontPotion>(), ModContent.ItemType<PermanentBattlefrontPotion>());
                PermaPotionRecipeS(mod, ModContent.ItemType<DemonDrugPotion>(), ModContent.ItemType<PermanentDemonDrug>());
                PermaPotionRecipeS(mod, ModContent.ItemType<StrengthPotion>(), ModContent.ItemType<PermanentStrengthPotion>());
                PermaPotionRecipeS(mod, ModContent.ItemType<SoulSiphonPotion>(), ModContent.ItemType<PermanentSoulSiphonPotion>());
                PermaPotionRecipeS(mod, ItemID.EndurancePotion, ModContent.ItemType<PermanentEndurancePotion>());
                PermaPotionRecipeS(mod, ItemID.LifeforcePotion, ModContent.ItemType<PermanentLifeforcePotion>());
                PermaPotionRecipeS(mod, ItemID.ManaRegenerationPotion, ModContent.ItemType<PermanentManaRegenerationPotion>());
                #endregion
                #region add a tier recipes
                PermaPotionRecipeA(mod, ItemID.Ale, ModContent.ItemType<PermanentAle>());
                PermaPotionRecipeA(mod, ItemID.CalmingPotion, ModContent.ItemType<PermanentCalmingPotion>());
                PermaPotionRecipeA(mod, ItemID.ArcheryPotion, ModContent.ItemType<PermanentArcheryPotion>());
                PermaPotionRecipeA(mod, ItemID.BattlePotion, ModContent.ItemType<PermanentBattlePotion>());
                PermaPotionRecipeA(mod, ModContent.ItemType<CrimsonPotion>(), ModContent.ItemType<PermanentCrimsonPotion>());
                PermaPotionRecipeA(mod, ItemID.FlaskofCursedFlames, ModContent.ItemType<PermanentFlaskOfCursedFlames>());
                PermaPotionRecipeA(mod, ItemID.FlaskofIchor, ModContent.ItemType<PermanentFlaskOfIchor>());
                PermaPotionRecipeA(mod, ItemID.FlaskofVenom, ModContent.ItemType<PermanentFlaskOfVenom>());
                PermaPotionRecipeA(mod, ItemID.MagicPowerPotion, ModContent.ItemType<PermanentMagicPowerPotion>());
                PermaPotionRecipeA(mod, ItemID.RagePotion, ModContent.ItemType<PermanentRagePotion>());
                PermaPotionRecipeA(mod, ItemID.WrathPotion, ModContent.ItemType<PermanentWrathPotion>());
                PermaPotionRecipeA(mod, ItemID.SpelunkerPotion, ModContent.ItemType<PermanentSpelunkerPotion>());
                PermaPotionRecipeA(mod, ItemID.SwiftnessPotion, ModContent.ItemType<PermanentSwiftnessPotion>());
                PermaPotionRecipeA(mod, ItemID.SummoningPotion, ModContent.ItemType<PermanentSummoningPotion>());
                #endregion
                #region add b tier recipes
                PermaPotionRecipeB(mod, ModContent.ItemType<BoostPotion>(), ModContent.ItemType<PermanentBoostPotion>());
                PermaPotionRecipeB(mod, ItemID.AmmoReservationPotion, ModContent.ItemType<PermanentAmmoReservationPotion>());
                PermaPotionRecipeB(mod, ItemID.CratePotion, ModContent.ItemType<PermanentCratePotion>());
                PermaPotionRecipeB(mod, ItemID.FishingPotion, ModContent.ItemType<PermanentFishingPotion>());
                PermaPotionRecipeB(mod, ItemID.SonarPotion, ModContent.ItemType<PermanentSonarPotion>());
                PermaPotionRecipeB(mod, ItemID.FlaskofFire, ModContent.ItemType<PermanentFlaskOfFire>());
                PermaPotionRecipeB(mod, ItemID.FlaskofGold, ModContent.ItemType<PermanentFlaskOfGold>());
                PermaPotionRecipeB(mod, ItemID.FlaskofNanites, ModContent.ItemType<PermanentFlaskOfNanites>());
                PermaPotionRecipeB(mod, ItemID.GillsPotion, ModContent.ItemType<PermanentGillsPotion>());
                PermaPotionRecipeB(mod, ItemID.HeartreachPotion, ModContent.ItemType<PermanentHeartreachPotion>());
                PermaPotionRecipeB(mod, ItemID.IronskinPotion, ModContent.ItemType<PermanentIronskinPotion>());
                PermaPotionRecipeB(mod, ItemID.MiningPotion, ModContent.ItemType<PermanentMiningPotion>());
                PermaPotionRecipeB(mod, ItemID.RegenerationPotion, ModContent.ItemType<PermanentRegenerationPotion>());
                PermaPotionRecipeB(mod, ModContent.ItemType<ShockwavePotion>(), ModContent.ItemType<PermanentShockwavePotion>());
                PermaPotionRecipeB(mod, ItemID.TitanPotion, ModContent.ItemType<PermanentTitanPotion>());
                PermaPotionRecipeB(mod, ItemID.InfernoPotion, ModContent.ItemType<PermanentInfernoPotion>());
                PermaPotionRecipeC(mod, ItemID.WaterWalkingPotion, ModContent.ItemType<PermanentWaterWalkingPotion>());

                #endregion
                #region add c tier recipes
                PermaPotionRecipeC(mod, ItemID.BuilderPotion, ModContent.ItemType<PermanentBuilderPotion>());
                PermaPotionRecipeC(mod, ItemID.ShinePotion, ModContent.ItemType<PermanentShinePotion>());
                PermaPotionRecipeC(mod, ItemID.TrapsightPotion, ModContent.ItemType<PermanentDangersensePotion>());
                PermaPotionRecipeC(mod, ItemID.FeatherfallPotion, ModContent.ItemType<PermanentFeatherfallPotion>());
                PermaPotionRecipeC(mod, ItemID.FlaskofParty, ModContent.ItemType<PermanentFlaskOfParty>());
                PermaPotionRecipeC(mod, ItemID.FlaskofPoison, ModContent.ItemType<PermanentFlaskOfPoison>());
                PermaPotionRecipeC(mod, ItemID.FlipperPotion, ModContent.ItemType<PermanentFlipperPotion>());
                PermaPotionRecipeC(mod, ItemID.HunterPotion, ModContent.ItemType<PermanentHunterPotion>());
                PermaPotionRecipeC(mod, ItemID.InvisibilityPotion, ModContent.ItemType<PermanentInvisibilityPotion>());
                PermaPotionRecipeC(mod, ItemID.NightOwlPotion, ModContent.ItemType<PermanentNightOwlPotion>());
                PermaPotionRecipeC(mod, ItemID.ThornsPotion, ModContent.ItemType<PermanentThornsPotion>());
                PermaPotionRecipeC(mod, ItemID.WarmthPotion, ModContent.ItemType<PermanentWarmthPotion>());
                #endregion
                #region special perma recipes
                ModRecipe recipe = new ModRecipe(mod);
                recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
                recipe.AddIngredient(ItemID.GravitationPotion, 20);
                recipe.AddIngredient(ItemID.SoulofFlight, 1);
                recipe.AddIngredient(ModContent.ItemType<EternalCrystal>(), 3);
                recipe.SetResult(ModContent.ItemType<PermanentGravitationPotion>());
                recipe.AddRecipe();

                recipe = new ModRecipe(mod);
                recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);
                recipe.AddIngredient(ItemID.ObsidianSkinPotion, 20);
                recipe.AddIngredient(ItemID.SoulofLight, 1);
                recipe.AddIngredient(ModContent.ItemType<EternalCrystal>(), 2);
                recipe.SetResult(ModContent.ItemType<PermanentObsidianSkinPotion>());
                recipe.AddRecipe();
                #endregion 
            }

            ModRecipe recipe1 = new ModRecipe(mod);
            recipe1.AddIngredient(ItemID.FallenStar);
            recipe1.AddIngredient(ItemID.Gel, 2);
            recipe1.AddIngredient(ItemID.Bottle, 10);
            recipe1.AddTile(TileID.Bottles);
            recipe1.SetResult(ItemID.LesserManaPotion, 10);
            recipe1.AddRecipe();

            ReverseMirror recipe2 = new ReverseMirror(mod);
            recipe2.AddRecipeGroup("tsorcRevamp:UpgradedMirrors", 1);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.SetResult(ItemID.MagicMirror);
            recipe2.AddRecipe();
        }



        #region permanent potion recipes
        public static void PermaPotionRecipeS(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddIngredient(IngredientPotion);
            recipe.AddIngredient(ModContent.ItemType<EternalCrystal>(), 5);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        public static void PermaPotionRecipeA(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddIngredient(IngredientPotion);
            recipe.AddIngredient(ModContent.ItemType<EternalCrystal>(), 3);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        public static void PermaPotionRecipeB(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);
            recipe.AddIngredient(IngredientPotion);
            recipe.AddIngredient(ModContent.ItemType<EternalCrystal>(), 2);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        public static void PermaPotionRecipeC(Mod mod, int IngredientPotion, int ResultPotion) {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe.AddIngredient(IngredientPotion);
            recipe.AddIngredient(ModContent.ItemType<EternalCrystal>());
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(ResultPotion, 1);
            recipe.AddRecipe();
        }
        #endregion

        public static void AddRecipeGroups() {
            RecipeGroup group = new RecipeGroup(() => "Upgraded Mirrors", new int[] {
                ModContent.ItemType<GreatMagicMirror>(),
                ModContent.ItemType<VillageMirror>()
            });
            RecipeGroup.RegisterGroup("tsorcRevamp:UpgradedMirrors", group);
        }

    }

    public class ReverseMirror : ModRecipe { //custom recipe to refund the player their souls if they revert their mirror
        public ReverseMirror(Mod mod) : base(mod) { } //constructor declares which mod we're in

        public override void OnCraft(Item item) {
            Item.NewItem(Main.LocalPlayer.getRect(), ModContent.ItemType<DarkSoul>(), 100);
            //recipes cant have 2 results, so just spawn 100 souls on the player when they use a ReverseMirror recipe
        }

    }
}
