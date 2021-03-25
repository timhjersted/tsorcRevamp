using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons {
    class ManaBomb : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Creates a magic vortex at your location that deals high damage over time");
        }

        public override void SetDefaults() {
            item.width = 18;
            item.height = 18;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noUseGraphic = true;
            item.damage = 250;
            item.useAnimation = 30;
            item.useTime = 30;
            item.maxStack = 30;
            item.consumable = true;
            item.scale = 1;
            item.UseSound = SoundID.Item29;
            item.value = 150000;
            item.useTurn = true;
            item.rare = ItemRarityID.Green;
            item.shoot = ModContent.ProjectileType<Projectiles.MagicalBall>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.ManaCrystal, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 100);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
