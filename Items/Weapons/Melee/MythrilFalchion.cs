using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class MythrilFalchion : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Mythril Falchion");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {


            
            //item.prefixType=484;
            item.rare = ItemRarityID.LightRed;
            item.damage = 39;
            item.width = 34;
            item.height = 44;
            item.knockBack = (float)6;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.15;
            item.useAnimation = 24;
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
