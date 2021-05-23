using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Legs)]
    class ShellGreaves : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Armor made from the shell of a legendary creature. \n+12% movement speed, +15% ranged damage");
        }

        public override void SetDefaults() {
            item.defense = 5;
            item.rare = ItemRarityID.LightRed;
            item.width = 18;
            item.height = 18;
            item.value = 120000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.NecroGreaves);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.moveSpeed += 0.12f;
            player.rangedDamage += 0.15f;
        }
    }
}
