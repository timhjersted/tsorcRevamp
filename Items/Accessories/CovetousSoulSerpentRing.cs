using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class CovetousSoulSerpentRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("An ancient relic forged and lost many centuries ago" +
                                "\nIncreases the number of Dark Souls dropped by fallen creatures by 50%. Defense reduced by 40." +
                                "\nAll souls are drawn to the wearer from a large distance" +
                                "\nThe ring glows with a bright white light");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 22;
            item.accessory = true;
            item.defense = -40;
            item.value = PriceByRarity.Pink_5;
            item.rare = ItemRarityID.Pink;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("CovetousSilverSerpentRing"), 1);
            recipe.AddIngredient(mod.GetItem("SoulReaper2"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing = true;
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulReaper += 13;
            player.GetModPlayer<tsorcRevampPlayer>().ConsSoulChanceMult += 10; //50% increase
            int posX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int posY = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(posX, posY, 0.9f, 0.8f, 0.7f);
        }

    }
}
