using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class FireSpiritTome2 : ModItem
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Detonation Tome");
            // Tooltip.SetDefault("Summons a hail of explosive fireballs");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 7;
            Item.useTime = 7;
            Item.maxStack = 1;
            Item.damage = 64;
            Item.knockBack = 8;
            Item.autoReuse = true;
            Item.scale = 1.3f;
            Item.UseSound = SoundID.Item20;
            Item.rare = ItemRarityID.Lime;
            Item.shootSpeed = 22;
            Item.mana = 5;
            Item.value = PriceByRarity.Lime_7;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.Fireball2>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<FireSpiritTome>(), 1);
            recipe.AddIngredient(ModContent.ItemType<ExplosionRune>(), 1);
            recipe.AddIngredient(ItemID.LunarTabletFragment, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
