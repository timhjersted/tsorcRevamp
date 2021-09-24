using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class EyeOfTheGods : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Eye of the Gods");
            Tooltip.SetDefault("Lights up your cursor when equipped");

        }

        public override void SetDefaults() {

            item.width = 24;
            item.height = 24;
            item.maxStack = 1;
            item.rare = ItemRarityID.Green;
            item.accessory = true;
            item.value = PriceByRarity.Green_2;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.ShinePotion, 30);
            recipe.AddIngredient(ItemID.SpelunkerPotion, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 5000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            int cursorX = (int)((Main.mouseX + Main.screenPosition.X) / 16);
            int cursorY = (int)((Main.mouseY + Main.screenPosition.Y) / 16);
            Lighting.AddLight(cursorX, cursorY, 2.5f, 2.5f, 2.5f);
        }

    }
}
