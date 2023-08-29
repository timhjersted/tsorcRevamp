using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Weapons.Melee.Hammers;

class AncientWarhammer : ModItem
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Ancient Warhammer");
        Tooltip.SetDefault("An old choice for advancing druids");

    }

    public override void SetDefaults()
    {

        Item.rare = ItemRarityID.Green;
        Item.DamageType = DamageClass.Melee;
        Item.damage = 32;
        Item.width = 48;
        Item.height = 48;
        Item.knockBack = 9f;
        Item.maxStack = 1;
        Item.autoReuse = true;
        Item.useTurn = false;
        Item.useAnimation = 40;
        Item.useTime = 29;
        Item.UseSound = SoundID.Item1;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.value = PriceByRarity.Green_2;
        Item.shoot = ModContent.ProjectileType<Nothing>();
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.TheBreaker, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
