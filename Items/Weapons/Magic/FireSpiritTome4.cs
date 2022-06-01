using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class FireSpiritTome4 : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire Spirit Tome IV");
            Tooltip.SetDefault("Summons fire spirits with incredible speed and damage.");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.maxStack = 1;
            Item.damage = 133;
            Item.knockBack = 8;
            Item.autoReuse = true;
            Item.scale = 1.3f;
            Item.UseSound = SoundID.Item9;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = 14;
            Item.mana = 5;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.FireSpirit2>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("FireSpiritTome3").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<Items.BequeathedSoul>(), 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 215000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
