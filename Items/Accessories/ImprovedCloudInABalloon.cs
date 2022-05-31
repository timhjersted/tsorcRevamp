using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    public class ImprovedCloudInABalloon : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Allows the holder to double jump" + 
								"\nIncreases jump height. ");
        }
 
        public override void SetDefaults() {
            Item.width = 14;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }
 
        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.CloudinaBottle, 1);
            recipe.AddIngredient(Mod.GetItem("ImprovedShinyRedBalloon"), 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
 
        public override void UpdateEquip(Player player) {
            player.jumpSpeedBoost += 1f;
            player.jumpBoost = true;
            player.doubleJumpCloud = true;
        }
 
    }
}