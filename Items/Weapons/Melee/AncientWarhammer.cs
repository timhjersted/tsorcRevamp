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

            item.rare = ItemRarityID.Green;
            item.melee = true;
            item.damage = 32;
            item.height = 38;
            item.width = 38;
            item.knockBack = 9f;
            item.maxStack = 1;
            item.autoReuse = true;
            item.useTurn = true;
            item.useAnimation = 40;
            item.useTime = 29;
            item.hammer = 65;
            item.scale = 1.25f;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = PriceByRarity.Green_2;


        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.TheBreaker, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
