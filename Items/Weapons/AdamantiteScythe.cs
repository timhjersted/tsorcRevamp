using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {

    public class AdamantiteScythe : ModItem {

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.damage = 46;
            item.width = 64;
            item.height = 46;
            item.knockBack = 6;
            item.melee = true;
            item.useAnimation = 34;
            item.scale = 1.1f;
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
