using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenAxe : ModItem {

        public override void SetDefaults() {
            item.rare = ItemRarityID.Blue;
            item.damage = 16;
            item.height = 30;
            item.knockBack = 6;
            item.melee = true;
            item.useAnimation = 25;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 4500;
            item.width = 30;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.StoneBlock, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 600);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
