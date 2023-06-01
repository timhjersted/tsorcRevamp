using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles;

namespace tsorcRevamp.Items.Placeable
{
    public class BonfirePlaceable : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<BonfireCheckpoint>());
            Item.rare = ItemRarityID.Master;
            Item.value = Item.sellPrice(50);
        }

        public override void AddRecipes()
        {
            var recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Campfire);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);
            recipe.Register();
        }
    }
}