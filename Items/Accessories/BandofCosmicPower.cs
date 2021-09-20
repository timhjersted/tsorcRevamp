using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    [AutoloadEquip(EquipType.HandsOn)]
    public class BandOfCosmicPower : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Band of Cosmic Power");
            Tooltip.SetDefault("Increases life regeneration by 2 and increases max mana by 40" +
                                "\nCan be upgraded with 10,000 Dark Souls");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 28;
            item.lifeRegen = 2;
            item.accessory = true;
            item.value = 5000;
            item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BandofRegeneration, 1);
            recipe.AddIngredient(ItemID.BandofStarpower, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();

            ModRecipe recipe2 = new ModRecipe(mod);
            recipe2.AddIngredient(ItemID.ManaRegenerationBand, 1);
            recipe2.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.SetResult(this, 1);
            recipe2.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.statManaMax2 += 40;
        }

    }
}
