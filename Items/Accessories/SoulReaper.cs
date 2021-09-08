using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class SoulReaper : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Increases Dark Soul pick-up range and increases" +
                                "\nconsumable soul drop chance by 25%" +
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
            player.GetModPlayer<tsorcRevampPlayer>().SoulReaper += 5;
            player.GetModPlayer<tsorcRevampPlayer>().ConsSoulChanceMult += 5; //25% increase

        }

    }
}
