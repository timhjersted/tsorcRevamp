using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class CobaltPoleWarAxe : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Cobalt Pole War Axe");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {


            
            item.autoReuse = true;
            //item.prefixType=483;
            item.rare = ItemRarityID.LightRed;
            item.damage = 37;
            item.width = 54;
            item.height = 54;
            item.knockBack = (float)4.85;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.3;
            item.useAnimation = 32;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 69000;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.CobaltBar, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
