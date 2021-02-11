using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {

    public class AdamantiteWarAxe : ModItem {

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.damage = 44;
            item.width = 42;
            item.height = 42;
            item.knockBack = 6;
            item.melee = true;
            item.useAnimation = 34;
            item.scale = 1.2f;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 126500;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.AdamantiteBar, 11);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
