using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class SilverSpear : ModItem
    {

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Item.damage = 9;
            Item.knockBack = 4f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 31;
            Item.useTime = 31;
            Item.shootSpeed = 3.7f;
            //item.shoot = ProjectileID.DarkLance;

            Item.height = 32;
            Item.width = 32;

            Item.melee = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.value = 1000;
            Item.rare = ItemRarityID.White;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.SilverSpear>();

        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.SilverBar, 10);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.Anvils);
            recipe.AddRecipe();
        }
    }
}
