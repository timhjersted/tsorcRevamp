using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.Rings
{
    public class SilverRing : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 22;
            Item.defense = 4;
            Item.accessory = true;
            Item.value = PriceByRarity.White_0;
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 400);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
        }

    }
}
