using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class BandOfCosmicPower : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Band of Cosmic Power");
            Tooltip.SetDefault("Increases life regeneration by 2 and increases max mana by 40");
        }

        public override void SetDefaults() {
            item.lifeRegen = 2;
            item.accessory = true;
            item.value = 5000;
            item.rare = ItemRarityID.Green;
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
            player.statManaMax2 += 40;
        }

    }
}
