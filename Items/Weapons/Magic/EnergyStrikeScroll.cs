using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Magic {
    class EnergyStrikeScroll : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The scroll reads \"Exori vis.\"");
        }
        public override void SetDefaults() {
            item.damage = 40;
            item.height = 10;
            item.knockBack = 1;
            item.maxStack = 1;
            item.rare = ItemRarityID.LightRed;
            item.shootSpeed = 4;
            item.magic = true;
            item.mana = 10;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 10;
            item.useAnimation = 10;
            item.value = PriceByRarity.LightRed_4;
            item.width = 34;
            item.noMelee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.EnergyStrike>();
        }
    }
}
