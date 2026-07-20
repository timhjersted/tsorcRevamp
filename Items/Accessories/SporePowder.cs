using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories
{
    class SporePowder : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.rare = ItemRarityID.Green;
            Item.value = 50;
            Item.accessory = true; 
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().HasSporePowder = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.JungleSpores, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4500);
            recipe.AddTile(TileID.Bottles);
            recipe.Register();
        }
    }
}
