using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Weapons.Ranged {
    class EphemeralThrowingSpear : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Passes through solid walls");
        }

        public override void SetDefaults() {
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
        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("RoyalThrowingSpear"), 30);
            recipe.AddIngredient(Mod.GetItem("EphemeralDust"), 5);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 90);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 30);
            recipe.AddRecipe();
        }
    }
}
