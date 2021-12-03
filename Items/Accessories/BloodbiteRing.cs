using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class BloodbiteRing : ModItem {
        public override void SetStaticDefaults() {
            string defString = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode ? "" : "\n+6 defense";
            Tooltip.SetDefault("One of the infamous bite rings commissioned by Sir Arstor of Carim" +
                                "\nDespite the dreadful rumors surrounding its creation, this ring" +
                                "\nis an unmistakable asset, due to its ability to prevent bleeding." + defString);
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.value = PriceByRarity.Blue_1;
            item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("SilverRing"), 1);
            recipe.AddIngredient(mod.GetItem("BloodredMossClump"), 20);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.buffImmune[BuffID.Bleeding] = true;
            player.statDefense += ModContent.GetInstance<tsorcRevampConfig>().LegacyMode ? 0 : 6;
        }

    }
}
