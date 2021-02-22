using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class HallowedGreatPoleAxe : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Hallowed Great Pole Axe");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {


            
            item.autoReuse = true;
            //item.prefixType=368;
            item.rare = ItemRarityID.Pink;
            item.damage = 55;
            item.width = 62;
            item.height = 62;
            item.knockBack = (float)6;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1;
            item.useAnimation = 38;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 230000;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(mod.GetItem("AdamantitePoleWarAxe"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
