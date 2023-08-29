
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Spears;

public class Longinus : ModItem
{

    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Legendary spear fabled to hold sway over the world.\nIncreases attack damage by 30% when falling and has a chance to heal 3 HP on hit.");
    }


    public override void SetDefaults()
    {
        Item.damage = 175;
        Item.knockBack = 9f;

        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useAnimation = 11;
        Item.useTime = 1;
        Item.shootSpeed = 8;
        //item.shoot = ProjectileID.DarkLance;

        Item.height = 50;
        Item.width = 50;

        Item.DamageType = DamageClass.Melee;
        Item.noMelee = true;
        Item.noUseGraphic = true;

        Item.value = PriceByRarity.Cyan_9;
        Item.rare = ItemRarityID.Cyan;
        Item.maxStack = 1;
        Item.UseSound = SoundID.Item1;
        Item.shoot = ModContent.ProjectileType<Projectiles.Spears.LonginusProj>();

    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        if (player.gravDir == 1f && player.velocity.Y > 0 || player.gravDir == -1f && player.velocity.Y < 0)
        {
            damage += 1.3f;
        }
    }


    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.ChlorophytePartisan, 1);
        recipe.AddIngredient(ModContent.ItemType<GuardianSoul>());
        recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>(), 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 160000);

        recipe.AddTile(TileID.DemonAltar);
        recipe.Register();
    }
}
