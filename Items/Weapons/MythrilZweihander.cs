using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class MythrilZweihander : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Mythril Zweihander");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {


            
            //item.prefixType=484;
            item.rare = ItemRarityID.LightRed;
            item.damage = 43;
            item.width = 56;
            item.height = 56;
            item.knockBack = (float)7;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1;
            item.useAnimation = 35;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 144900;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.MythrilSword, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
