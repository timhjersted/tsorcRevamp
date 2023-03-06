using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Mobility
{
    [AutoloadEquip(EquipType.Shoes)]
    public class BootsOfHaste : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Multiplies your movement speed by 10%\n" +
                "Inherits Hermes Boots effect"); */

        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.Blue_1;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HermesBoots, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.accRunSpeed = 6;
            player.moveSpeed *= 1.1f;
        }
    }
}
