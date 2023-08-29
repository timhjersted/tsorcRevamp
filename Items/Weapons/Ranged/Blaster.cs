using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Weapons.Ranged;

public class Blaster : ModItem
{

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Blaster");
        Tooltip.SetDefault("Relatively short range but pierces once. Doesn't require ammo");
    }

    public override void SetDefaults()
    {
        Item.damage = 20;
        Item.DamageType = DamageClass.Ranged;
        Item.crit = 4;
        Item.width = 44;
        Item.height = 24;
        Item.useTime = 24;
        Item.useAnimation = 24;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 3f;
        Item.value = 20000;
        Item.scale = 0.7f;
        Item.rare = ItemRarityID.Blue;
        Item.shoot = ModContent.ProjectileType<BlasterShot>();
        Item.shootSpeed = 14f;
    }

    public override void AddRecipes()
    {


        Recipe recipe = CreateRecipe();
        //recipe.AddIngredient(null, "oddscrapmetal", 10);
        recipe.AddIngredient(ItemID.IronBar, 5);
        recipe.AddIngredient(ItemID.Diamond, 2);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1200);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();

        Recipe recipe2 = CreateRecipe();
        //recipe.AddIngredient(null, "oddscrapmetal", 10);
        recipe2.AddIngredient(ItemID.LeadBar, 5);
        recipe2.AddIngredient(ItemID.Diamond, 2);
        recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 1200);
        recipe2.AddTile(TileID.DemonAltar);

        recipe2.Register();
    }

    public override Vector2? HoldoutOffset()
    {
        return new Vector2(2, 2);
    }

    public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
    {

        Vector2 muzzleOffset = Vector2.Normalize(speed) * 20f;
        if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0))
        {
            position += muzzleOffset;
        }
        return true;


    }
}
