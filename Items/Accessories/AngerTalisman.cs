using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class AngerTalisman : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Minus 10 defense" +
                                "\n30% increased damage");
        }

        public override void SetDefaults() {
            item.accessory = true;
            item.value = 300000;
            item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.RangerEmblem, 1);
            recipe.AddIngredient(ItemID.SorcererEmblem, 1);
            recipe.AddIngredient(ItemID.WarriorEmblem, 1);
            recipe.AddIngredient(ItemID.GoldBar, 3);
            recipe.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.statDefense -= 10;
            player.allDamage += 0.3f;

        }

    }
}
