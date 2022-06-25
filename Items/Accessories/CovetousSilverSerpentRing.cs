using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class CovetousSilverSerpentRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("An ancient relic forged and lost many centuries ago" +
                                "\nOne of the 4 Kings of Arradius was said to wear this ring" +
                                "\nIncreases the number of souls dropped from fallen creatures by 20% but reduces defense by 15" +
                                "\nThe ring glows with a bright white light");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 22;
            Item.accessory = true;
            Item.defense = -15;
            Item.value = PriceByRarity.LightRed_4; //prohibitively expensive soul cost
            Item.rare = ItemRarityID.LightRed;
        }

        /*
         public override void AddRecipes() {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverBar, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 20000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
        */

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing = true;
            int posX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int posY = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(posX, posY, 0.9f, 0.8f, 0.7f);
        }

    }
}
