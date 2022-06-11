using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class FireSpiritTome3 : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Incineration Tome");
            Tooltip.SetDefault("Summons a barrage of flames that combust into lingering explosions" +
                "\nShatters enemy defense");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 10;
            Item.useTime = 10;
            Item.damage = 50;
            Item.knockBack = 11;
            Item.autoReuse = true;
            Item.scale = 1.3f;
            Item.UseSound = SoundID.Item9;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = 20;
            Item.mana = 5;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.Fireball3>();
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("FireSpiritTome2").Type, 1);
            recipe.AddIngredient(ItemID.InfernoFork, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("RedTitanite").Type, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 45000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
