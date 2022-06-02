using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    class RTQ2Rifle : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("RTQ2 Rifle");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 9;
            Item.useTime = 9;
            Item.damage = 42;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item91;
            Item.rare = ItemRarityID.LightPurple;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 11;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightPurple_6;
            Item.useAmmo = AmmoID.Bullet;
            Item.DamageType = DamageClass.Ranged;
        }

        public override void AddRecipes()
        {
            Terraria.Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Megashark, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
