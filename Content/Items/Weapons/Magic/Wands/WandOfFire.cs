using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Wands
{
    class WandOfFire : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Wand of Fire");
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
            Item.damage = 20;
            Item.knockBack = 1;
            Item.mana = 6;
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
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<WoodenWand>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 1800);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
