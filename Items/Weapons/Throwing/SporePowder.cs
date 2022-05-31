using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Throwing {
    class SporePowder : ModItem {

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 26;
            Item.maxStack = 500;
            Item.damage = 13;
            Item.rare = ItemRarityID.Green;
            Item.value = 50;
            Item.consumable = true;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = true;
            Item.shootSpeed = 4;
            Item.shoot = ModContent.ProjectileType<Projectiles.SporePowder>();
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.JungleSpores, 1);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(this, 5);
            recipe.AddRecipe();
        }
    }
}
