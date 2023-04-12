using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{

    [AutoloadEquip(EquipType.Shield)]

    public class SpikedIronShield : ModItem
    {

        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("'Everyone will stay away from you'" +
                             "\nReduces damage taken by 4% and gives thorns buff" +
                             "\nbut also reduces movement speed by 5%" +
                             "\nCan be upgraded with an Obsidian Shield and 5000 Dark Souls"); */

        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.defense = 3;
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
        }

        public override void UpdateEquip(Player player)
        {
            player.thorns += 1f;
            player.endurance += 0.04f;
            player.moveSpeed *= 0.95f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<IronShield>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }

}
