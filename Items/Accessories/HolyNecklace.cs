using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    public class HolyNecklace : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Emits blue light, longer invincibility and starcloak effect.");
        }
 
        public override void SetDefaults() {
            item.width = 32;
            item.height = 32;
            item.accessory = true;
            item.value = 200000;
            item.rare = ItemRarityID.Pink;
        }
 
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            //recipe.AddIngredient(mod.GetItem("FrozenStarlight"), 1);
            recipe.AddIngredient(ItemID.StarCloak, 1);
            recipe.AddIngredient(ItemID.CrossNecklace, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
 
        public override void UpdateEquip(Player player) {
			Lighting.AddLight((int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16, (int)(player.position.Y + 2f) / 16, 0.25f, 0.25f, 1.0f);
			player.starCloak = true;
			player.longInvince = true;
        }
 
    }
}