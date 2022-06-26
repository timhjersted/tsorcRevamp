using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class CopperSpear : ModItem
    {

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Item.damage = 5;
            Item.knockBack = 4f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 31;
            Item.useTime = 31;
            Item.shootSpeed = 3.7f;
            //item.shoot = ProjectileID.DarkLance;

            Item.height = 32;
            Item.width = 32;

            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.value = PriceByRarity.White_0;
            Item.rare = ItemRarityID.White;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.CopperSpear>();

        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CopperBar, 10);

            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }
    }
}
