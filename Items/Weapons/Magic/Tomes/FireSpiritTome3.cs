using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    class FireSpiritTome3 : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Incineration Tome");
            /* Tooltip.SetDefault("Summons a barrage of solar flares that combust into lingering explosions" +
                "\nShatters enemy defense"); */
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 10;
            Item.useTime = 10;
            Item.damage = 100;
            Item.knockBack = 11;
            Item.autoReuse = true;
            Item.scale = 1.3f;
            Item.UseSound = SoundID.Item20;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = 20;
            Item.mana = 5;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.Fireball3>();
            Item.noMelee = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<FireSpiritTome2>(), 1);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 45000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ModContent.ItemType<FireSpiritTome2>(), 1);
            recipe2.AddIngredient(ItemID.FragmentSolar, 10);
            recipe2.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();
        }
    }
}
