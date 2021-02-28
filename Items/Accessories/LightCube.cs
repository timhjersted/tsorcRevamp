using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class LightCube : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Contains the essence of fire\n" +
                                "Gives off a strong light!");

        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 28;
            item.accessory = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.LightRed;
            item.value = 60000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 200);
            recipe.AddIngredient(ItemID.ShadowOrb, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            int playerX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int playerY = (int)(player.position.Y + 2f) / 16;

            Lighting.AddLight(playerX, playerY, 0.92f, 0.8f, 0.65f);
        }
    }
}
