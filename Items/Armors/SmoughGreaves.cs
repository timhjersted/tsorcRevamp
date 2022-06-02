using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class SmoughGreaves : ModItem
    {

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 18;
            Item.height = 18;
            //item.defense = 2;
            Item.value = 10000;
            Item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            /*Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronGreaves, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 100);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();*/
        }
    }
}

