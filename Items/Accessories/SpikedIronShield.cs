using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {

    public class SpikedIronShield : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Reduces damage taken by 6%" +
                                "\n'Everyone will stay away from you'" +
                                "\n-30% Movement Speed, Thorns Buff" +
                                "\nCan be upgraded with a Cobalt Shield and 3000 Dark Souls");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 28;
            item.defense = 9;
            item.rare = ItemRarityID.Red;
            item.accessory = true;
            item.value = 8000;
        }

        public override void UpdateEquip(Player player) {
            player.endurance += 0.06f;
            player.moveSpeed -= 0.3f;
            player.thorns = 1f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("IronShield"));
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }

}
