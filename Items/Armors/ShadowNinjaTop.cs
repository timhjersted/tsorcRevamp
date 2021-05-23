using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Body)]
    class ShadowNinjaTop : ModItem {
        public override void SetDefaults() {
            item.width = 18;
            item.height = 18;
            item.value = 50000;
            item.rare = ItemRarityID.Orange;
            item.defense = 10;
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<BlackBeltGiTop>());
            recipe.AddIngredient(ItemID.SoulofFright);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
