using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Magic {
    class GreatEnergyBeamScroll : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The scroll reads \"Exevo gran vix lux.\"");
        }
        public override void SetDefaults() {
            Item.damage = 55;
            Item.height = 10;
            Item.knockBack = 1;
            Item.rare = ItemRarityID.Pink;
            Item.autoReuse = true;
            Item.shootSpeed = 4;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 30;
            Item.value = PriceByRarity.Pink_5;
            Item.width = 34;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.GreatEnergyBeamBall>();
        }
    }
}
