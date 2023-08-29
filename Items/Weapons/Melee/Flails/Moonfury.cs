using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Melee.Flails;


public class Moonfury : ModItem
{

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
    }

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.channel = true;
        Item.useAnimation = 44;
        Item.useTime = 44;
        Item.damage = 88;
        Item.knockBack = 8;
        Item.UseSound = SoundID.Item1;
        Item.rare = ItemRarityID.LightRed;
        Item.shootSpeed = 13;
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.value = PriceByRarity.LightRed_4;
        Item.DamageType = DamageClass.Melee;
        Item.shoot = ModContent.ProjectileType<Projectiles.Flails.MoonfuryBall>();
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.BlueMoon, 1);
        recipe.AddIngredient(ItemID.CobaltBar, 3);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 11000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }


}
