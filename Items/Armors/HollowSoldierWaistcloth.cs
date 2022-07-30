using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class HollowSoldierWaistcloth : ModItem
    {
        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 18;
            Item.height = 18;
            //item.defense = 2;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }

        /*public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronGreaves, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }*/
    }
}

