using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    class ShadowNinjaTop : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = 50000;
            Item.rare = ItemRarityID.Orange;
            Item.defense = 10;
        }
        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ModContent.ItemType<BlackBeltGiTop>());
            recipe.AddIngredient(ItemID.SoulofFright);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
