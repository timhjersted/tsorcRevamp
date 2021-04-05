using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {

    public class AdamantiteFalchion : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("All special Adamantite weapons can be upgraded.");

        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.damage = 44;
            item.width = 36;
            item.height = 48;
            item.knockBack = 6;
            item.melee = true;
            item.scale = 1.1f;
            item.useAnimation = 25;
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
