using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Ranged.Thrown
{
    class EphemeralThrowingSpear : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Passes through solid walls");
        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.damage = 29;
            Item.height = 64;
            Item.knockBack = 6;
            Item.maxStack = 2000;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.Ranged;
            Item.scale = 0.9f;
            Item.shootSpeed = 14;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.value = 10;
            Item.width = 10;
            Item.shoot = ModContent.ProjectileType<Projectiles.EphemeralThrowingSpear>();
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(30);
            recipe.AddIngredient(ModContent.ItemType<RoyalThrowingSpear>(), 30);
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 90);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
