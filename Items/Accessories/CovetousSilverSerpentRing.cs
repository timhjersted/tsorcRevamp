using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class CovetousSilverSerpentRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("An ancient relic forged and lost many centuries ago" +
                                "\nOne of the 4 Kings of Arradius was said to wear this ring" + 
                                "\nIncreases the number of souls dropped from fallen creatures by 20% but reduces defense by 15" + 
                                "\nThe ring glows with a bright white light");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 22;
            item.accessory = true;
            item.defense = -15;
            item.value = PriceByRarity.LightRed_4; //prohibitively expensive soul cost
            item.rare = ItemRarityID.LightRed;
        }

        /*
         public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SilverBar, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
        */

        public override void UpdateEquip(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing = true;
            int posX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int posY = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(posX, posY, 0.9f, 0.8f, 0.7f);
        }

    }
}
