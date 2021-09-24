using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class RunningEnchantment : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Running Enchantment");
            Tooltip.SetDefault("Adds an enchantment to your feet\n" +
                                "25% Increased movement speed");

        }

        public override void SetDefaults() {

            item.width = 28;
            item.height = 28;
            item.accessory = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.Blue;
            item.value = PriceByRarity.Blue_1 + 2500 /*because being the same cost as boots of haste would be weird*/;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.HermesBoots, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.moveSpeed += 0.25f;
        }
    }
}
