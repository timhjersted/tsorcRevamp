using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class ThornDecapitator : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Thorn Decapitator");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {


            
            item.width = 40;
            item.height = 40;
            item.useStyle =ItemUseStyleID.SwingThrow;
            item.useAnimation = 25;
            item.autoReuse = true;
            item.useTime = 25;
            item.maxStack = 1;
            item.damage = 29;
            item.knockBack = (float)5;
            item.useTurn = false;
            item.scale = (float)0.9;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Green;
            item.value = 10000;
            item.melee = true;
            //item.prefixType=483;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.BladeofGrass, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
