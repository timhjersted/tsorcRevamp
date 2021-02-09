using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class BarrierRing : ModItem {
        public override void SetStaticDefaults() { 
            Tooltip.SetDefault("Casts barrier when the wear is critically wounded." + 
                                "\nBarrier increases defense by 20.");
        }

        public override void SetDefaults() {
            item.accessory = true;
            item.value = 7000;
            item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CobaltBar, 3);
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 20);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            if (player.statLife <= (player.statLifeMax * 0.25f)) {
                player.statDefense += 20;
            }
        }

    }
}
