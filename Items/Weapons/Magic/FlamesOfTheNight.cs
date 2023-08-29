using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic;

class FlamesOfTheNight : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("A full sprectral array of green flame will light up the skies");
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 28;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useAnimation = 20;
        Item.useTime = 2;
        Item.damage = 40;
        Item.knockBack = 1;
        Item.autoReuse = true;
        Item.UseSound = SoundID.Item20;
        Item.rare = ItemRarityID.Pink;
        Item.shootSpeed = 21;
        Item.mana = 12;
        Item.value = PriceByRarity.Pink_5;
        Item.DamageType = DamageClass.Magic;
        Item.shoot = ModContent.ProjectileType<Projectiles.CursedFlames>();
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.SpellTome, 1);
        recipe.AddIngredient(ItemID.CursedFlame, 6);
        recipe.AddIngredient(ItemID.SoulofSight, 5);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 50000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
