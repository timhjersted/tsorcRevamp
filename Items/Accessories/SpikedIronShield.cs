using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {

    public class SpikedIronShield : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("'Everyone will stay away from you'" +
                "\n-30% Movement Speed, Thorns Buff" +
                "\nCan be upgraded with a Cobalt Shield and 2000 Dark Souls");
        }

        public override void SetDefaults() {
            item.width = 20;
            item.height = 20;
            item.defense = 9;
            item.rare = ItemRarityID.Red;
            item.accessory = true;
            item.value = 8000;
        }

        public override void UpdateEquip(Player player) {
            player.moveSpeed -= 0.3f;
            player.thorns = 1f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("IronShield"));
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 800);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }

}
