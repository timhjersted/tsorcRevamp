using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class UndeadTalisman : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Reduces damage from undead by 15");

        }

        public override void SetDefaults() {

            item.width = 22;
            item.height = 32;
            item.maxStack = 1;
            item.rare = ItemRarityID.Blue;
            item.accessory = true;
            item.value = 1500;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.GoldBar, 1);
            recipe.AddIngredient(ItemID.Bone, 25);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().UndeadTalisman = true;
        }

    }
}
