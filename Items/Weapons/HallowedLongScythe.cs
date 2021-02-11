using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class HallowedLongScythe : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Hallowed Long Scythe");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {


            
            item.autoReuse = true;
            //item.prefixType=368;
            item.rare = ItemRarityID.Pink;
            item.damage = 66;
            item.width = 66;
            item.height = 44;
            item.knockBack = (float)4.5;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1;
            item.useAnimation = 34;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 230000;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ModLoader.GetMod("DarkSouls"), "AdamantiteScythe", 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
