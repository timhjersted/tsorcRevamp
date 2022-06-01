using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ShadowSickle : ModItem {

        public override void SetDefaults() {
            Item.rare = ItemRarityID.Blue;
            Item.damage = 16;
            Item.width = 32;
            Item.height = 32;
            Item.knockBack = 5;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1.1f;
            Item.useAnimation = 18;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = 13500;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.DemoniteBar, 10);
            recipe.AddIngredient(ItemID.ShadowScale, 6);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
