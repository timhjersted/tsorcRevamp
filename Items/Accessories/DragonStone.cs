using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class DragonStone : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Tooltip1" +
                                "\nTooltip2" +
                                "\nTooltip3" +
                                "\nTooltip4" +
                                "\nTooltip5");
        }

        public override void SetDefaults() {
            item.width = 26;
            item.height = 26;
            item.accessory = true;
            item.value = 375000;
            item.rare = ItemRarityID.Pink;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Bone, 69);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1337);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.meleeDamage -= 2f;
            player.meleeCrit = -50;
            if (player.statLife <= 80) {
                player.statDefense += 85;
            }
            else {
                player.statDefense += 30;
            }
        }

    }
}
