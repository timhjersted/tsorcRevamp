using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class MythrilMace : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Mythril Mace");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {


            
            //item.prefixType=484;
            item.damage = 40;
            item.rare = ItemRarityID.LightRed;
            item.width = 38;
            item.height = 38;
            item.knockBack = (float)7;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.15;
            item.useAnimation = 26;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 103500;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.MythrilBar, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
