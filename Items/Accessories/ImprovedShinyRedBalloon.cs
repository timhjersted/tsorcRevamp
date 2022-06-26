using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class ImprovedShinyRedBalloon : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases jump height. ");
        }

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShinyRedBalloon, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.jumpSpeedBoost += 1f;
            player.jumpBoost = true;
        }

    }
}