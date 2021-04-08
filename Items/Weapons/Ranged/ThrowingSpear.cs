using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class ThrowingSpear : ModItem {
        public override void SetDefaults() {
            item.consumable = true;
            item.damage = 13;
            item.height = 62;
            item.knockBack = 3;
            item.maxStack = 1000;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.ranged = true;
            item.scale = 0.8f;
            item.shootSpeed = 8;
            item.useAnimation = 15;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 15;
            item.value = 8;
            item.width = 10;
            item.shoot = ModContent.ProjectileType<Projectiles.ThrowingSpear>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddRecipeGroup(RecipeGroupID.Wood, 10);
            recipe.AddIngredient(ItemID.StoneBlock, 10);
            recipe.SetResult(this, 30);
            recipe.AddRecipe();
        }
    }
}
