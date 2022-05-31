using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenAxe : ModItem {

        public override void SetDefaults() {
            Item.rare = ItemRarityID.Blue;
            Item.damage = 18;
            Item.height = 30;
            Item.knockBack = 6;
            Item.melee = true;
            Item.autoReuse = true;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = 4500;
            Item.width = 30;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.StoneBlock, 5);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 1200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
