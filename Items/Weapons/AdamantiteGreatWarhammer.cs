using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {

    public class AdamantiteGreatWarhammer : ModItem {

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.damage = 47;
            item.width = 46;
            item.height = 46;
            item.knockBack = 9;
            item.melee = true;
            item.scale = 1.4f;
            item.useAnimation = 36;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 149500;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.AdamantiteBar, 13);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
