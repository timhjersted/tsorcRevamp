using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons {
    class RoyalThrowingSpear : ModItem {

        public override void SetDefaults() {
            item.consumable = true;
            item.damage = 30;
            item.height = 66;
            item.knockBack = 6;
            item.maxStack = 2000;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.ranged = true;
            item.scale = 0.9f;
            item.shootSpeed = 9;
            item.useAnimation = 15;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 15;
            item.value = 100;
            item.width = 10;
            item.shoot = ModContent.ProjectileType<Projectiles.RoyalThrowingSpear>();
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Wood, 20);
            recipe.AddIngredient(ItemID.StoneBlock, 20);
            recipe.AddIngredient(ItemID.SilverCoin, 50);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 100);
            recipe.AddRecipe();
        }
    }
}
