using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class ManaBomb : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Creates a magic vortex at your location that deals high damage over time");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noUseGraphic = true;
            Item.damage = 250;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.scale = 1;
            Item.UseSound = SoundID.Item29;
            Item.value = 150000;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Green;
            Item.shoot = ModContent.ProjectileType<Projectiles.MagicalBall>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ManaCrystal, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 200);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
