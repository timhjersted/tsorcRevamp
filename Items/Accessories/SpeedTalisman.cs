using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class SpeedTalisman : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("15% increased movement speed" + 
                                "\n20% increased melee speed");
        }

        public override void SetDefaults() {
            item.accessory = true;
            item.value = 15000;
            item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Aglet, 1);
            recipe.AddIngredient(ItemID.AnkletoftheWind, 1);
            recipe.AddIngredient(ItemID.FeralClaws, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 300);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.moveSpeed += 0.15f;
            player.meleeSpeed += 0.2f;
        }

    }
}
