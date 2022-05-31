using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class ThrowingSpear : ModItem {
        public override void SetDefaults() {
            Item.consumable = true;
            Item.damage = 13;
            Item.height = 62;
            Item.knockBack = 3;
            Item.maxStack = 2000;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.ranged = true;
            Item.scale = 0.8f;
            Item.shootSpeed = 8;
            Item.useAnimation = 18;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 18;
            Item.value = 8;
            Item.width = 10;
            Item.shoot = ModContent.ProjectileType<Projectiles.ThrowingSpear>();
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddRecipeGroup(RecipeGroupID.Wood, 10);
            recipe.AddIngredient(ItemID.StoneBlock, 10);
            recipe.SetResult(this, 30);
            recipe.AddRecipe();
        }
    }
}
