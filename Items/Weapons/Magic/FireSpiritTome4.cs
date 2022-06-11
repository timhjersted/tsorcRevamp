using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class FireSpiritTome4 : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("[WIP!!!] Tome of the Dying Star");
            Tooltip.SetDefault("Leave nothing but ash in your wake.");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.maxStack = 1;
            Item.damage = 133;
            Item.knockBack = 8;
            Item.autoReuse = true;
            Item.scale = 1.3f;
            Item.UseSound = SoundID.Item9;
            Item.rare = ItemRarityID.Purple;
            Item.shootSpeed = 54;
            Item.mana = 1;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.Fireball4>();
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("FireSpiritTome3").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<Items.BequeathedSoul>(), 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 115000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
