using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    class Freezethrower : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Uses Gel as ammo." +
                                "\nHas a chance to freeze");
        }
        public override void SetDefaults()
        {
            Item.width = 54;
            Item.height = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 30;
            Item.useTime = 5;
            Item.damage = 66;
            Item.knockBack = 2;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item34;
            Item.rare = ItemRarityID.LightPurple;
            Item.shootSpeed = 9;
            Item.useAmmo = AmmoID.Gel;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightPurple_6;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<Projectiles.Freezethrower>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Flamethrower, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 30);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 40000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
