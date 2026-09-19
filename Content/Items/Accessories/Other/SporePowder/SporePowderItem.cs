using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Other.SporePowder
{
    class SporePowderItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.rare = ItemRarityID.Green;
            Item.value = 50;
            Item.accessory = true; 
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<SporePowderPlayer>().Equipped = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.JungleSpores, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 4500);
            recipe.AddTile(TileID.Bottles);
            recipe.Register();
        }
    }
}
