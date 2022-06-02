using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class ImprovedCloudInABalloon : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Allows the holder to double jump" +
                                "\nIncreases jump height. ");
        }

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CloudinaBottle, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("ImprovedShinyRedBalloon").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.jumpSpeedBoost += 1f;
            player.jumpBoost = true;
            player.hasJumpOption_Cloud = true;
        }

    }
}