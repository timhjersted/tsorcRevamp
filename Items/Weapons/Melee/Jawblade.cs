using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class Jawblade : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A blade of bone and fangs");
        }
        public override void SetDefaults() {
            Item.width = 68;
            Item.height = 76;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.damage = 46;
            Item.knockBack = 7;
            Item.scale = 1f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);

            recipe.AddIngredient(ModContent.ItemType<Items.Weapons.Melee.BoneBlade>());
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
