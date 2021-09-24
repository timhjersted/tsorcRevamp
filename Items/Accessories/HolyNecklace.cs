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
            item.value = PriceByRarity.LightRed_4;
            item.rare = ItemRarityID.LightRed;
        }
 
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("FrozenStarlight"), 1);
            recipe.AddIngredient(ItemID.StarCloak, 1);
            recipe.AddIngredient(ItemID.CrossNecklace, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();

            ModRecipe recipe2 = new ModRecipe(mod);
            recipe2.AddIngredient(mod.GetItem("FrozenStarlight"), 1);
            recipe2.AddIngredient(ItemID.StarVeil, 1);
            recipe2.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.SetResult(this, 1);
            recipe2.AddRecipe();
        }
 
        public override void UpdateEquip(Player player) {
			Lighting.AddLight((int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16, (int)(player.position.Y + 2f) / 16, 0.75f, 0.75f, 1.5f);
			player.starCloak = true;
			player.longInvince = true;
        }
 
    }
}