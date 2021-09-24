using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class EssenceOfMana : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Essence of Mana");
            Tooltip.SetDefault("Increases mana by 100.");

        }

        public override void SetDefaults() {
            item.width = 10;
            item.accessory = true;
            item.height = 12;
            item.maxStack = 1;
            item.value = PriceByRarity.Blue_1;
            item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.ManaCrystal, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 5000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
        public override void UpdateEquip(Player player) {
            player.statManaMax2 += 100;
        }

    }
}
