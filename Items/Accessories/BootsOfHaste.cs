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
            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.Blue_1;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.HermesBoots, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.moveSpeed += 0.2f;
        }
    }
}
