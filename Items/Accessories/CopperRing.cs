using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class CopperRing : ModItem {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 22;
            item.defense = 3;
            item.accessory = true;
            item.value = PriceByRarity.White_0; // i didnt think this would actually get used...
            item.rare = ItemRarityID.White;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CopperBar, 2);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 300);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

 

    }
}
