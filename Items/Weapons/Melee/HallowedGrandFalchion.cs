using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class HallowedGrandFalchion : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Hallowed Grand Falchion");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {


            
            item.autoReuse = true;
            //item.prefixType=368;
            item.rare = ItemRarityID.Pink;
            item.damage = 52;
            item.width = 36;
            item.height = 46;
            item.knockBack = (float)5.5;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1;
            item.useAnimation = 22;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 230000;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(mod.GetItem("HallowedFalchion"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 5000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
