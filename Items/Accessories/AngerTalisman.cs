using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class AngerTalisman : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("-30 defense" +
                                "\n30% increased damage");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.accessory = true;
            item.value = PriceByRarity.LightRed_4;
            item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.RangerEmblem, 1);
            recipe.AddIngredient(ItemID.SorcererEmblem, 1);
            recipe.AddIngredient(ItemID.WarriorEmblem, 1);
            recipe.AddIngredient(ItemID.GoldBar, 3);
            recipe.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.statDefense -= 30; //because 10 defense is a joke
            player.allDamage += 0.3f;

        }

    }
}
