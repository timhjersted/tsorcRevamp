using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class MysteriousIdol : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("May attract the archaeologist to stay in your town if held in your inventory.");
        }

        public override void SetDefaults() {
            item.width = 18;
            item.height = 20;
            item.rare = ItemRarityID.Blue;
            item.value = 1000;
            item.maxStack = 1;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.GoldBar, 3);
            recipe.AddIngredient(ItemID.Ruby, 5);
            recipe.AddIngredient(ItemID.Diamond, 5);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

    }
}
