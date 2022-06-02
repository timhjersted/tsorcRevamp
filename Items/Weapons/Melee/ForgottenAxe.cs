using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class ForgottenAxe : ModItem
    {

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Blue;
            Item.damage = 18;
            Item.height = 30;
            Item.knockBack = 6;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = 4500;
            Item.width = 30;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.StoneBlock, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 1200);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
