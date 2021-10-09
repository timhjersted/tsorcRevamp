using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ThrowingAxe : ModItem {

        public override void SetDefaults() {
            item.consumable = true;
            item.damage = 14;
            item.height = 34;
            item.knockBack = 6;
            item.maxStack = 2000;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.melee = true;
            item.shootSpeed = 7;
            item.useAnimation = 22;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 22;
            item.value = 5;
            item.width = 22;
            item.shoot = ModContent.ProjectileType<Projectiles.ThrowingAxe>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Wood, 10);
            recipe.AddIngredient(ItemID.StoneBlock, 10);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 40);
            recipe.AddRecipe();
        }
    }
}
