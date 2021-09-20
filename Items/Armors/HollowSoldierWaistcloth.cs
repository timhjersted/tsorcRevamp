using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class HollowSoldierWaistcloth : ModItem
    {
        public override void SetDefaults()
        {
            item.vanity = true;
            item.width = 18;
            item.height = 18;
            //item.defense = 2;
            item.value = 10000;
            item.rare = ItemRarityID.Green;
        }

        /*public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.IronGreaves, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 100);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }*/
    }
}

