using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories
{
    public class StarlightMagnetite : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.stack = 1;
            Item.accessory = true;
            Item.height = 32;
            Item.width = 32;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.Green_2;
            Item.vanity = true;
        }
        public override void UpdateEquip(Player player)
        {
            player.treasureMagnet = true;

            int playerX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int playerY = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(playerX, playerY, 0.88f, 0.88f, 1.45f);
        }
        public override void UpdateVanity(Player player)
        {
            player.treasureMagnet = true;

            int playerX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int playerY = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(playerX, playerY, 0.88f, 0.88f, 1.45f);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.TreasureMagnet, 1);
            recipe.AddIngredient(ModContent.ItemType<FrozenStarlight>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 800);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
