using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Throwing {
    class SporePowder : ModItem {

        public override void SetDefaults() {
            item.width = 22;
            item.height = 26;
            item.maxStack = 500;
            item.damage = 13;
            item.rare = ItemRarityID.Green;
            item.value = 50;
            item.consumable = true;
            item.useTurn = true;
            item.useAnimation = 15;
            item.useTime = 15;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.UseSound = SoundID.Item1;
            item.noMelee = true;
            item.shootSpeed = 4;
            item.shoot = ModContent.ProjectileType<Projectiles.SporePowder>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.JungleSpores, 1);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(this, 5);
            recipe.AddRecipe();
        }
    }
}
