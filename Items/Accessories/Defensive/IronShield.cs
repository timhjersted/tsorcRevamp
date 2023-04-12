using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{

    [AutoloadEquip(EquipType.Shield)]

    public class IronShield : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Reduces damage taken by 4% " +
                                "\nbut also reduces movement speed by 10%" +
                                "\nCan be upgraded with 500 Dark Souls."); */
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.defense = 2;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.Blue_1;
        }

        public override void UpdateEquip(Player player)
        {
            player.endurance += 0.04f;
            player.moveSpeed *= 0.9f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
