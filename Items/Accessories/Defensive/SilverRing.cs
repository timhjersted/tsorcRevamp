using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class SilverRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Grants 4 defense");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 22;
            Item.accessory = true;
            Item.value = PriceByRarity.White_0;
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverBar, 2);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 400);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.statDefense += 4;
        }

    }
}
