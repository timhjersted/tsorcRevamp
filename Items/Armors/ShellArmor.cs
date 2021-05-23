using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Body)]
    class ShellArmor : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Armor made from the shell of a legendary creature. \n20% chance not to consume ammo");
        }

        public override void SetDefaults() {
            item.defense = 10;
            item.rare = ItemRarityID.LightRed;
            item.width = 18;
            item.height = 18;
            item.value = 30000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.NecroBreastplate);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.ammoCost80 = true;
        }
    }
}
