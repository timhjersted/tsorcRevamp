using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenGreatAxe : ModItem {

        public override void SetDefaults() {
            Item.rare = ItemRarityID.Green;
            Item.damage = 28;
            Item.height = 36;
            Item.knockBack = 7;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1.1f;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = PriceByRarity.Green_2;
            Item.width = 36;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("ForgottenAxe"), 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
