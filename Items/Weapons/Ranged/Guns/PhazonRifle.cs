using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged.Guns;

class PhazonRifle : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Three round burst \nOnly the first shot consumes ammo\nPhazon rounds are extremely volatile");
    }
    public override void SetDefaults()
    {
        Item.width = 50;
        Item.height = 18;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useAnimation = 12;
        Item.useTime = 4;
        Item.maxStack = 1;
        Item.damage = 25;
        Item.knockBack = 0.01f;
        Item.autoReuse = true;
        Item.UseSound = SoundID.Item31;
        Item.rare = ItemRarityID.LightPurple;
        Item.shoot = ProjectileID.PurificationPowder;
        Item.shootSpeed = 7;
        Item.useAmmo = 14;
        Item.noMelee = true;
        Item.value = PriceByRarity.LightPurple_6;
        Item.DamageType = DamageClass.Ranged;
        Item.reuseDelay = 11;
        Item.useAmmo = AmmoID.Bullet;
    }

    public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
    {
        type = ModContent.ProjectileType<Projectiles.PhazonRound>();
    }

    public override bool CanConsumeAmmo(Item ammo, Player player)
    {
        return !(player.itemAnimation < Item.useAnimation - 2); //consume 1 ammo instead of 3
    }

    public override Vector2? HoldoutOffset()
    {
        return new Vector2(-8, 0);
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.ClockworkAssaultRifle);
        //recipe.AddIngredient(ItemID.MeteoriteBar, 30);
        recipe.AddIngredient(ItemID.MythrilBar, 3);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 16000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }
}
