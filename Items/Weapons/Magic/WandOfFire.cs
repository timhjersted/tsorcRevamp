using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class WandOfFire : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wand of Fire");
            Item.staff[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.width = 12;
            Item.height = 17;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.maxStack = 1;
            Item.damage = 20;
            Item.knockBack = 1;
            Item.mana = 7;
            Item.UseSound = SoundID.Item20;
            Item.shootSpeed = 12;
            Item.noMelee = true;
            Item.value = PriceByRarity.Blue_1;
            Item.DamageType = DamageClass.Magic;
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<Projectiles.FireBall>();
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("WoodenWand").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2300);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
