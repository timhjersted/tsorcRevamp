using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class RedMagePants : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.defense = 3;
            Item.value = 9000;
            Item.rare = ItemRarityID.Blue;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Silk, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 100);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

