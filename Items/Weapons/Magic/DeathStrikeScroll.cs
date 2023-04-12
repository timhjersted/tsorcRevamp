using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class DeathStrikeScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("The scroll reads \"Exori mort.\"");
        }
        public override void SetDefaults()
        {
            Item.damage = 100;
            Item.height = 10;
            Item.knockBack = 1;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 4;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 200;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 30;
            Item.value = PriceByRarity.LightRed_4;
            Item.width = 34;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.DeathStrike>();
        }
    }
}
