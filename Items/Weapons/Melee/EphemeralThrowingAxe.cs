using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class EphemeralThrowingAxe : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Passes through solid walls");
        }

        public override void SetDefaults()
        {
            Item.damage = 30;
            Item.height = 34;
            Item.knockBack = 7;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.Melee;
            Item.shootSpeed = 8;
            Item.useAnimation = 22;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 22;
            Item.value = 150000;
            Item.width = 22;
            Item.shoot = ModContent.ProjectileType<Projectiles.EphemeralThrowingAxeProj>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<ThrowingAxe>());
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 15);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
