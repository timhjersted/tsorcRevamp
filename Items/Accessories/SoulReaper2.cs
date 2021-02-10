using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class SoulReaper2 : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Soul Reaper II");
            Tooltip.SetDefault("Draws souls to you from more than double the previous distance");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.value = 270000;
            item.rare = ItemRarityID.Pink;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 7000);
            recipe.AddIngredient(mod.GetItem("SoulReaper"), 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulReaper = 8;
        }

    }
}
