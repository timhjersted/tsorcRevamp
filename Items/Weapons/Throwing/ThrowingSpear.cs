using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Throwing
{
    class ThrowingSpear : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.damage = 13;
            Item.height = 62;
            Item.knockBack = 3;
            Item.maxStack = Item.CommonMaxStack;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.Throwing;
            Item.scale = 0.8f;
            Item.shootSpeed = 8;
            Item.useAnimation = 18;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 18;
            Item.value = 8;
            Item.width = 10;
            Item.shoot = ModContent.ProjectileType<Projectiles.Throwing.ThrowingSpear>();
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
