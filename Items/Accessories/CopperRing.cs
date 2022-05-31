using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class CopperRing : ModItem {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 22;
            Item.defense = 3;
            Item.accessory = true;
            Item.value = PriceByRarity.White_0; // i didnt think this would actually get used...
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.CopperBar, 2);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 300);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

 

    }
}
