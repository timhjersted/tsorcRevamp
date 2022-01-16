using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Magic {
    class DeathStrikeScroll : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The scroll reads \"Exori mort.\"");
        }
        public override void SetDefaults() {
            item.damage = 100;
            item.height = 10;
            item.knockBack = 1;
            item.rare = ItemRarityID.LightRed;
            item.shootSpeed = 4;
            item.magic = true;
            item.mana = 200;
            item.useAnimation = 30;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 30;
            item.value = PriceByRarity.LightRed_4;
            item.width = 34;
            item.noMelee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.DeathStrike>();
        }
    }
}
