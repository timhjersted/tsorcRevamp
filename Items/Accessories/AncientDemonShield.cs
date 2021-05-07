using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class AncientDemonShield : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Reduces damage taken by 6%" +
                                "\nPowerful, but slows movement by 25%" +
                                "\nGreat Shield that grants immunity to knockback and gives thorns effect");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 38;
            item.defense = 10;
            item.accessory = true;
            item.value = 10000;
            item.rare = ItemRarityID.Red;
        }

        public override void UpdateEquip(Player player) {
            player.noKnockback = true;
            player.endurance += 0.06f;
            player.moveSpeed -= 0.25f;
            player.thorns = 1f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CobaltShield);
            recipe.AddIngredient(mod.GetItem("SpikedIronShield"));
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
