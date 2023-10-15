using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class ExplosionRune : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Projectile that explodes with 5 small fireballs at the point of impact.");
        }
        public override void SetDefaults()
        {
            Item.consumable = false;
            Item.damage = 30;
            Item.height = 28;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 16;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.LightRed_4;
            Item.width = 20;
            Item.mana = 16;
            Item.shoot = ModContent.ProjectileType<Projectiles.ExplosionBall>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            //recipe.AddIngredient(ItemID.Fireblossom, 3);
            //recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
