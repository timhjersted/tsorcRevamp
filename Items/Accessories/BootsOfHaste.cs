using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    [AutoloadEquip(EquipType.Shoes)]
    public class BootsOfHaste : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Boots of Haste");
            Tooltip.SetDefault("Adds 20% speed");

        }

        public override void SetDefaults() {
            item.width = 32;
            item.height = 26;
            item.accessory = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.Orange;
            item.value = 10000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.HermesBoots, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.moveSpeed += 0.2f;
        }
    }
}
