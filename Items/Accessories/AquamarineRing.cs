using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class AquamarineRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("5% increased magic damage" +
                                "\n+40 mana");
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
            recipe.AddIngredient(ItemID.SilverBar, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.magicDamage += 0.05f;
            player.statManaMax2 += 40;
        }

    }
}
