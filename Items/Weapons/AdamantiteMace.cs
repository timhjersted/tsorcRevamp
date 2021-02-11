using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {

    public class AdamantiteMace : ModItem {

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.damage = 45;
            item.width = 38;
            item.height = 38;
            item.knockBack = 8;
            item.melee = true;
            item.useAnimation = 27;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 138000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.AdamantiteBar, 12);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
