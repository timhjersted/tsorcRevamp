using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenGreatAxe : ModItem {

        public override void SetDefaults() {
            item.rare = ItemRarityID.Green;
            item.damage = 28;
            item.height = 36;
            item.knockBack = 7;
            item.autoReuse = true;
            item.melee = true;
            item.scale = 1.1f;
            item.useAnimation = 25;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = PriceByRarity.Green_2;
            item.width = 36;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("ForgottenAxe"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
