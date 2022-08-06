
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class WandOfDarkness2 : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Magic/WandOfDarkness";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wand of Darkness II");
            Tooltip.SetDefault("Greater damage and higher knockback");
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
            Item.damage = 18;
            Item.knockBack = 2.5f;
            Item.mana = 5;
            Item.UseSound = SoundID.Item8;
            Item.shootSpeed = 7;
            Item.noMelee = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.ShadowBall>();
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("WandOfDarkness").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2700);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
