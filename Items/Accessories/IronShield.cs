using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class IronShield : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Greater defense at the cost of mobility" +
                "\nMovement Speed -40%. Unequip to regain maximum jumping abilities." +
                "\nVery useful when low on life and survival is essential." +
                "\nCan be upgraded with 2000 Dark Souls (increased movement speed, thorns buff).");
        }

        public override void SetDefaults() {
            item.width = 20;
            item.height = 20;
            item.accessory = true;
            item.defense = 5;
            item.rare = ItemRarityID.Red;
            item.value = 5000;
        }

        public override void UpdateEquip(Player player) {
            player.moveSpeed -= 0.4f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.IronBar, 4);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 800);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
