using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class ShadowSickle : ModItem {

        public override void SetDefaults() {
            item.rare = ItemRarityID.Blue;
            item.damage = 16;
            item.width = 32;
            item.height = 32;
            item.knockBack = 5;
            item.melee = true;
            item.scale = 1.1f;
            item.useAnimation = 18;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 13500;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.DemoniteBar, 10);
            recipe.AddIngredient(ItemID.ShadowScale, 6);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
