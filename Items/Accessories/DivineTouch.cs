using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories
{
    public class DivineTouch : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.vanity = true;
        }
        
        public override void UpdateEquip(Player player)
        {
            player.treasureMagnet = true;

            int cursorX = (int)((Main.mouseX + Main.screenPosition.X) / 16);
            int cursorY = (int)((Main.mouseY + Main.screenPosition.Y) / 16);
            Lighting.AddLight(cursorX, cursorY, 1.25f, 1.25f, 1.25f);

            int playerX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int playerY = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(playerX, playerY, 1.4f, 1.4f, 1.4f);
        }

        public override void UpdateVanity(Player player)
        {
            player.treasureMagnet = true;

            int cursorX = (int)((Main.mouseX + Main.screenPosition.X) / 16);
            int cursorY = (int)((Main.mouseY + Main.screenPosition.Y) / 16);
            Lighting.AddLight(cursorX, cursorY, 1.25f, 1.25f, 1.25f);

            int playerX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int playerY = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(playerX, playerY, 1.4f, 1.4f, 1.4f);
        }
    
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<EyeOfTheGods>(), 1);
            recipe.AddIngredient(ModContent.ItemType<StarlightMagnetite>(), 1);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 4);
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
