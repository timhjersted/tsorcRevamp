using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {

    [AutoloadEquip(EquipType.Shield)]

    public class IronShield : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Greater defense for melee warriors" +
                                "\nReduces damage taken by 6% " +
                                "\nbut also reduces non-melee damage by 20%." +
                                "\nCan be upgraded with 2000 Dark Souls.");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 28;
            item.accessory = true;
            item.defense = 5;
            item.rare = ItemRarityID.Blue;
            item.value = PriceByRarity.Blue_1;
        }

        public override void UpdateEquip(Player player) {
            player.endurance += 0.06f;
            player.rangedDamage -= 0.2f;
            player.magicDamage -= 0.2f;
            player.minionDamage -= 0.2f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.IronBar, 4);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 800);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
