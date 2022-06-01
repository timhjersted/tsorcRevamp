using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class ReforgedOldSabre : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldSabre";
        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.width = 34;
            Item.height = 38;
            Item.knockBack = 4;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1f;
            Item.useAnimation = 17;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = 6000;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("OldSabre").Type);
            recipe.AddTile(Mod.GetTile("SweatyCyclopsForge"));

            recipe.Register();
        }
    }
}
