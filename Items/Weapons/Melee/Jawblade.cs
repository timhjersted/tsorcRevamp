using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class Jawblade : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A blade of bone and fangs");
        }
        public override void SetDefaults() {
            item.width = 68;
            item.height = 76;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 30;
            item.useTime = 30;
            item.damage = 46;
            item.knockBack = 7;
            item.scale = 1f;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Orange;
            item.value = PriceByRarity.Orange_3;
            item.melee = true;
            item.autoReuse = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ModContent.ItemType<Items.Weapons.Melee.BoneBlade>());
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
