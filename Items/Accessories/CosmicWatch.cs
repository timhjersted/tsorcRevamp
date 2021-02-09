using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class CosmicWatch : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Use this item to push time foward" +
                                "\nto the beginning of night or to the beginning of day");
        }

        public override void SetDefaults() {
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 15;
            item.useTime = 15;
            item.accessory = true;
            item.value = 10000;
            item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SilverWatch, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 50);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player) { // i dont know why this works. dont touch it.
            Main.dayTime = !Main.dayTime;
            Main.time = 0;
            if (Main.netMode == NetmodeID.SinglePlayer) {
                if (Main.dayTime) {
                    Main.NewText("You shift time forward and a new day begins...", 175, 75, 255);
                }
                else Main.NewText("You shift time forward and a new night begins...", 175, 75, 255);
            }
            return true;
        }
        public override void UpdateEquip(Player player) {
            player.accWatch = 1;

        }

    }
}
