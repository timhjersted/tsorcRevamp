using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Other.SporePowder;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Other.VenomPowder
{
    class VenomPowderItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.rare = ItemRarityID.Lime;
            Item.value = 50;
            Item.accessory = true; 
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<VenomPowderPlayer>().Equipped = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SporePowderItem>(), 1);
            recipe.AddIngredient(ItemID.VialofVenom, 99);
            recipe.AddIngredient(ItemID.ChlorophyteBar, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 30000);
            recipe.AddTile(TileID.Bottles);
            recipe.Register();
        }
    }
}
