using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class ShadowSickle : ModItem
    {
        
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A curiously simple but deadly weapon - its short reach seems to be its only weakness.");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Blue;
            Item.damage = 50;
            Item.width = 32;
            Item.height = 32;
            Item.knockBack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1f;
            Item.useAnimation = 40;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 40;
            Item.value = 13500;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DemoniteBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
