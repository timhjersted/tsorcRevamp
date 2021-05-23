using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Magic {
    class GreatEnergyBeamScroll : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The scroll reads \"Exevo gran vix lux.\"");
        }
        public override void SetDefaults() {
            item.damage = 55;
            item.height = 10;
            item.knockBack = 1;
            item.rare = ItemRarityID.LightRed;
            item.autoReuse = true;
            item.shootSpeed = 4;
            item.magic = true;
            item.mana = 30;
            item.useAnimation = 30;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 30;
            item.value = 180000;
            item.width = 34;
            item.noMelee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.GreatEnergyBeamBall>();
        }
    }
}
