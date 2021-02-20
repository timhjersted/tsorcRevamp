using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class BoneBlade : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Bone Blade");
            Tooltip.SetDefault("'A blade of sharpened bone.'");

	}

        public override void SetDefaults()
        {


            
            item.width = 61;
            item.height = 74;
            item.useStyle =ItemUseStyleID.SwingThrow;
            item.autoReuse = true;
            item.useAnimation = 25;
            item.useTime = 25;
            item.maxStack = 1;
            item.damage = 30;
            item.knockBack = (float)7;
            item.scale = (float).9;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Orange;
            item.value = 27000;
            item.melee = true;
            item.material = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.Bone, 35);
            recipe.AddIngredient(ModLoader.GetMod("DarkSouls"), "DarkSoul", 1000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
