using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class SpikedBuckler : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Thorns Effect and No Knockback.");

        }

        public override void SetDefaults() {

            Item.accessory = true;
            Item.height = 22;
            Item.width = 20;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Green;
            Item.scale = (float)1;
            Item.value = PriceByRarity.Green_2;
            Item.defense = 3;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);

            recipe.AddIngredient(Mod.GetItem("SpikedNecklace"), 1);
            recipe.AddIngredient(ItemID.CobaltShield, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 300);
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
