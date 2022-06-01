using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class BoneBlade : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Bone Blade");
            Tooltip.SetDefault("'A blade of sharpened bone.'");

	}

        public override void SetDefaults()
        {


            
            Item.width = 61;
            Item.height = 74;
            Item.useStyle =ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.maxStack = 1;
            Item.damage = 30;
            Item.knockBack = (float)7;
            Item.scale = (float).9;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.Green_2;
            Item.DamageType = DamageClass.Melee;
            Item.material = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);

            recipe.AddIngredient(ItemID.Bone, 35);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 4000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
