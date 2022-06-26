using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class ReforgedOldAxe : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldAxe";
        public override void SetDefaults()
        {
            Item.damage = 12;
            Item.width = 36;
            Item.height = 30;
            Item.knockBack = 6;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1;
            Item.useAnimation = 20;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.value = 9000;
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("OldAxe").Type);            
            recipe.AddTile(ModContent.TileType<Tiles.SweatyCyclopsForge>());

            recipe.Register();
        }
    }
}
