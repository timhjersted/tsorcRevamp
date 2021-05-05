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
            item.width = 14;
            item.height = 28;
            item.accessory = true;
            item.value = 150000;
            item.rare = ItemRarityID.LightRed;
        }
 
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CloudinaBottle, 1);
            recipe.AddIngredient(mod.GetItem("ImprovedShinyRedBalloon"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 500);
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