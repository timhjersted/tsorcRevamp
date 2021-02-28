using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class FrozenStarlight : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Accessory that emits blue light.");

        }

        public override void SetDefaults() {
            item.stack = 1;
            item.accessory = true;
            item.height = 32;
            item.width = 32;
            item.maxStack = 1;
            item.rare = ItemRarityID.LightRed;
            item.value = 50000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.ShadowOrb, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 100);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            int i2 = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int j2 = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(i2, j2, 0.25f, 0.25f, 1.5f);
        }
    }
}
