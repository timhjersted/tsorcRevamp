using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Weapons.Ranged {
    class EphemeralThrowingSpear : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Passes through solid walls.");
        }

        public override void SetDefaults() {
            item.consumable = true;
            item.damage = 29;
            item.height = 64;
            item.knockBack = 6;
            item.maxStack = 3000;
            item.noUseGraphic = true;
            item.ranged = true;
            item.scale = 0.9f;
            item.shootSpeed = 14;
            item.useAnimation = 15;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 15;
            item.value = 10;
            item.width = 10;
            item.shoot = ModContent.ProjectileType<Projectiles.EphemeralThrowingSpear>();
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("RoyalThrowingSpear"), 30);
            recipe.AddIngredient(mod.GetItem("EphemeralDust"), 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 30);
            recipe.AddRecipe();
        }
    }
}
