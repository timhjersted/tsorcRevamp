using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    public class ImprovedShinyRedBalloon : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Increases jump height. ");
        }
 
        public override void SetDefaults() {
            item.width = 14;
            item.height = 28;
            item.accessory = true;
            item.value = 150000;
            item.rare = ItemRarityID.Blue;
        }
 
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.ShinyRedBalloon, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
 
        public override void UpdateEquip(Player player) {
			player.jumpHeight += 5;
			player.jumpSpeed += 1f;
        }
 
    }
}