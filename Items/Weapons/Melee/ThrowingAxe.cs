using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ThrowingAxe : ModItem {

        public override void SetDefaults() {
            Item.consumable = true;
            Item.damage = 14;
            Item.height = 34;
            Item.knockBack = 6;
            Item.maxStack = 2000;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.Melee;
            Item.shootSpeed = 7;
            Item.useAnimation = 22;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 22;
            Item.value = 5;
            Item.width = 22;
            Item.shoot = ModContent.ProjectileType<Projectiles.ThrowingAxe>();
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Wood, 10);
            recipe.AddIngredient(ItemID.StoneBlock, 10);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 40);
            recipe.Register();
        }
    }
}
