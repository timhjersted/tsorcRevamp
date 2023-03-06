using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class BlueHerosHat : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Blue Hero's hat");
            /* Tooltip.SetDefault("Worn by the hero himself!" +
                "\n+40 Mana" +
                "\nCan be upgraded eventually with 5 Souls of Sight and 40,000 Dark Souls"); */
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 10;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += 40;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HerosHat, 1);
            recipe.AddIngredient(ItemID.MythrilBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
