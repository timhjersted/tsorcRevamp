using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class DarkCloak : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Dark Cloak gives +15 Defense when health falls below 150" +
                                "\n+5 defense normally");
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
            recipe.AddIngredient(ItemID.SoulofNight, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            if (player.statLife <= 150) {
                player.statDefense += 15;
            }
            else {
                player.statDefense += 5;
            }
        }

    }
}
