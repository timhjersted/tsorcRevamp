using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Magic
{
    public class EssenceOfMana : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Essence of Mana");
            Tooltip.SetDefault("Increases max mana by 50%");

        }

        public override void SetDefaults()
        {
            Item.width = 10;
            Item.accessory = true;
            Item.height = 12;
            Item.maxStack = 1;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ManaCrystal, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 = (int)(player.statManaMax2 * 1.5f);
        }

    }
}
