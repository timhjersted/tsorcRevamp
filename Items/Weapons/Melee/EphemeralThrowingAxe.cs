using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class EphemeralThrowingAxe : ModItem {

        public override void SetDefaults() {
            item.damage = 30;
            item.height = 34;
            item.knockBack = 7;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.melee = true;
            item.shootSpeed = 8;
            item.useAnimation = 22;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 22;
            item.value = 150000;
            item.width = 22;
            item.shoot = ModContent.ProjectileType<Projectiles.EphemeralThrowingAxe>();
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("ThrowingAxe"));
            recipe.AddIngredient(mod.GetItem("EphemeralDust"), 40);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 8000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
