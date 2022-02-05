using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class SilverRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Grants 3 defense");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 22;
            item.accessory = true;
            item.value = PriceByRarity.White_0;
            item.rare = ItemRarityID.White;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SilverBar, 10);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.statDefense += 3;
        }

    }
}
