using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class SilverRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Grants 6 defense");
        }

        public override void SetDefaults() {
            item.accessory = true;
            item.value = 10000;
            item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SilverBar, 10);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.statDefense += 6;
        }

    }
}
