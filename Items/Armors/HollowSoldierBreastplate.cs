using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class HollowSoldierBreastplate : ModItem
    {
        /*public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Set bonus: +6 defense, +10% Move Speed, +5% ranged dmg");
        }*/

        public override void SetDefaults()
        {
            Item.vanity = true;
            Item.width = 18;
            Item.height = 18;
            Item.value = 30000;
            Item.rare = ItemRarityID.Green;
        }


        /*public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronChainmail, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 200);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }*/
    }
}