using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class FrozenStarlight : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Accessory that emits blue light.");

        }

        public override void SetDefaults() {
            Item.stack = 1;
            Item.accessory = true;
            Item.height = 32;
            Item.width = 32;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.Blue_1;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.ShadowOrb, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 100);

            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }

        public override void UpdateEquip(Player player) {
            int playerX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int playerY = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(playerX, playerY, 0.75f, 0.75f, 1.5f);
        }
    }
}
