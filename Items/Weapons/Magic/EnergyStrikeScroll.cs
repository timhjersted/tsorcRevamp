using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Magic;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class EnergyStrikeScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("The scroll reads \"Exori vis.\"");
        }
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 10;
            Item.damage = 30;
            Item.DamageType = DamageClass.MagicSummonHybrid;
            Item.mana = 100;
            Item.knockBack = 2f;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
            Item.shoot = ModContent.ProjectileType<EnergyStrikeScrollProjectile>();
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.noMelee = true;
        }
    }
}
