using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {

    public class AdamantiteZweihander : ModItem {

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.damage = 48;
            item.width = 64;
            item.height = 64;
            item.knockBack = 7;
            item.melee = true;
            item.useAnimation = 36;
            item.scale = 1.4f;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 161000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.AdamantiteSword, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
