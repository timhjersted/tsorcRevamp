using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Legs)]
    public class TriceratopsLegs : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Increases movement speed by 14%");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 6;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.14f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FossilPants, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.GladiatorLeggings, 1);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 3500);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}

