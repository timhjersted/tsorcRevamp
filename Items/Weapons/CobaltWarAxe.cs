using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class CobaltWarAxe : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Cobalt War Axe");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {


            
            item.autoReuse = true;
            //item.prefixType=483;
            item.rare = ItemRarityID.LightRed;
            item.damage = 34;
            item.width = 40;
            item.height = 40;
            item.knockBack = (float)3.85;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float).9;
            item.useAnimation = 23;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 62100;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.CobaltBar, 9);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 900);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
