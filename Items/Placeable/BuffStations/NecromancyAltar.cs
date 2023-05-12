using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Tiles.BuffStations;

namespace tsorcRevamp.Items.Placeable.BuffStations
{
	public class NecromancyAltar : ModItem
	{
		public override void SetStaticDefaults() 
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void SetDefaults() 
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<NecromancyAltarTile>());

			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 9999;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.buyPrice(0, 20, 0, 0);
		}

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BewitchingTable);
            recipe.AddIngredient(ItemID.WarTable);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}