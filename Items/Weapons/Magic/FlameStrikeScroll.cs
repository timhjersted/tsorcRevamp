using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Magic {
    class FlameStrikeScroll : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The scroll reads \"Exori flam.\"");
        }
        public override void SetDefaults() {
            Item.damage = 40;
            Item.height = 10;
            Item.knockBack = 1;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 4;
            Item.magic = true;
            Item.mana = 10;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.value = PriceByRarity.LightRed_4;
            Item.width = 34;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.FlameStrike>();
        }
    }
}
