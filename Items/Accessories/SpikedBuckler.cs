using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class SpikedBuckler : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Thorns Effect and No Knockback.");

        }

        public override void SetDefaults() {

            item.accessory = true;
            item.height = 22;
            item.width = 20;
            item.maxStack = 1;
            item.rare = ItemRarityID.Green;
            item.scale = (float)1;
            item.value = PriceByRarity.Green_2;
            item.defense = 3;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(mod.GetItem("SpikedNecklace"), 1);
            recipe.AddIngredient(ItemID.CobaltShield, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 300);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.thorns += 1f;
            player.noKnockback = true;
        }
    }
}
