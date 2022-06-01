using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class AncientWarhammer : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ancient Warhammer");
            Tooltip.SetDefault("An old choice for advancing druids");

        }

        public override void SetDefaults()
        {

            Item.rare = ItemRarityID.Green;
            Item.DamageType = DamageClass.Melee;
            Item.damage = 32;
            Item.height = 38;
            Item.width = 38;
            Item.knockBack = 9f;
            Item.maxStack = 1;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.useAnimation = 40;
            Item.useTime = 29;
            Item.hammer = 65;
            Item.scale = 1.25f;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Green_2;


        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.TheBreaker, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
