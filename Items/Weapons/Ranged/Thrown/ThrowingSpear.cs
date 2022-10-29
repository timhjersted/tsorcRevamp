using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Ranged.Thrown
{
    class ThrowingSpear : ModItem
    {
        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.damage = 13;
            Item.height = 62;
            Item.knockBack = 3;
            Item.maxStack = 2000;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.Ranged;
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

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(100);
            recipe.AddRecipeGroup(RecipeGroupID.Wood, 10);
            recipe.AddIngredient(ItemID.StoneBlock, 10);
            recipe.Register();
        }
    }
}
