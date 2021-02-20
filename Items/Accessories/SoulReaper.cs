using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class SoulReaper : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Possesses a mysterious benefit" +
                                "\nCan be upgraded with 7000 Dark Souls");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.value = 200000;
            item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 300);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulReaper = 2;
        }

    }
}
