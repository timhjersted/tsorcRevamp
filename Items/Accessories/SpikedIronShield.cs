using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {

    public class SpikedIronShield : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Everyone will stay away from you'" +
                                "\n-30% Movement Speed, Thorns Buff" +
                                "\nCan be upgraded with a Cobalt Shield and 10000 Dark Souls");
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
            player.moveSpeed -= 0.3f;
            player.thorns = 1f;
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                player.endurance += 0.06f;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
                int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
                if (ttindex != -1) {// if we find one
                    //insert the extra tooltip line
                    tooltips.Insert(ttindex + 1, new TooltipLine(mod, "RevampShieldDR", "Reduces damage taken by 6%"));
                }
            }
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("IronShield"));
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }

}
