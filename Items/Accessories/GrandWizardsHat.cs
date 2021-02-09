using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class GrandWizardsHat : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Grand Wizard's Hat");
            Tooltip.SetDefault("25% increased magic damage, +100 mana");
        }

        public override void SetDefaults() {
            item.width = 34;
            item.height = 22;
            item.accessory = true;
            item.value = 100000;
            item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SorcererEmblem, 1);
            recipe.AddIngredient(ItemID.WizardHat, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.magicDamage += 0.25f;
            player.statManaMax2 += 100;
        }

    }
}
