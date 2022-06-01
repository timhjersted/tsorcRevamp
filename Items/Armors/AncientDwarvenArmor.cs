using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Body)]
    class AncientDwarvenArmor : ModItem {

        public override void SetDefaults() {
            Item.height = Item.width = 18;
            Item.defense = 4;
            Item.value = 12000;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverChainmail);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}
