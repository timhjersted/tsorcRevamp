using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Shortswords;

class YellowTail : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Shatters strong defenses with high penetration(multi - hit damage)");
    }

    public override void SetDefaults()
    {
        Item.width = 46;
        Item.height = 46;
        Item.useStyle = ItemUseStyleID.Rapier;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.useAnimation = 10;
        Item.autoReuse = true;
        Item.useTime = 10;
        Item.maxStack = 1;
        Item.damage = 17;
        Item.knockBack = 4;
        Item.scale = 1;
        Item.UseSound = SoundID.Item1;
        Item.rare = ItemRarityID.Blue;
        Item.value = PriceByRarity.Blue_1;
        Item.DamageType = DamageClass.Melee;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<Projectiles.Shortswords.YellowTailProjectile>(); // The projectile is what makes a shortsword work
        Item.shootSpeed = 2.1f; // This value bleeds into the behavior of the projectile as velocity, keep that in mind when tweaking values
    }
    public override bool MeleePrefix()
    {
        return true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.GoldShortsword, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1500);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
