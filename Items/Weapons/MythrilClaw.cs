using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class MythrilClaw : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Mythril Claw");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {


            
            item.autoReuse = true;
            //item.prefixType=484;
            item.rare = ItemRarityID.LightRed;
            item.damage = 37;
            item.width = 26;
            item.height = 20;
            item.knockBack = (float)4;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.15;
            item.useAnimation = 13;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 72450;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.MythrilBar, 7);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
