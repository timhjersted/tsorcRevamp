using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class CobaltNinjato : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Cobalt Ninjato");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {


            
            item.autoReuse = true;
            //item.prefixType=483;
            item.rare = ItemRarityID.LightRed;
            item.damage = 34;
            item.width = 50;
            item.height = 50;
            item.knockBack = (float)2.85;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.1;
            item.useAnimation = 16;
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
