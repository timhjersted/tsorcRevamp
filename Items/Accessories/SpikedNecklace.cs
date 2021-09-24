using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class SpikedNecklace : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Thorns Effect.");

        }

        public override void SetDefaults() {
            item.accessory = true;
            item.width = 20;
            item.height = 22;
            item.maxStack = 1;
            item.rare = ItemRarityID.Blue;//for the soul cost
            item.value = PriceByRarity.Blue_1; 
            item.defense = 1;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.IronBar, 2);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.thorns += 1f;
        }
    }
}
