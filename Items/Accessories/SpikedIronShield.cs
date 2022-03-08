using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {

    [AutoloadEquip(EquipType.Shield)]

    public class SpikedIronShield : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("'Everyone will stay away from you'" +
                             "\nReduces damage taken by 7% and gives thorn buff" +
                             "\nbut also reduces non-melee damage by 25%" +
                             "\nCan be upgraded with an Obsidian Shield and 10000 Dark Souls");
                                
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 28;
            item.defense = 8;
            item.rare = ItemRarityID.Green;
            item.accessory = true;
            item.value = PriceByRarity.Green_2;
        }

        public override void UpdateEquip(Player player) {
            
            player.thorns = 1f;
            player.endurance += 0.07f;
            player.rangedDamage -= 0.25f;
            player.magicDamage -= 0.25f;
            player.minionDamage -= 0.25f;
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
