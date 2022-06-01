using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Legs)]
    class AncientDwarvenGreaves : ModItem {

        public override void SetDefaults() {
            Item.height = Item.width = 18;
            Item.defense = 4;
            Item.value = 24000;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverGreaves);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 500);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}
