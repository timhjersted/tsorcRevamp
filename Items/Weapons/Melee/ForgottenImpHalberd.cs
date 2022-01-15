using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenImpHalberd : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A crescent shaped spear used by imps.");
        }

        public override void SetDefaults() {
            item.damage = 60;
            item.knockBack = 6;
            item.melee = true;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.rare = ItemRarityID.Pink;
            item.shootSpeed = 7;
            item.useAnimation = 16;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 8;
            item.value = PriceByRarity.Pink_5;
            item.height = 50;
            item.width = 50;
            item.shoot = ModContent.ProjectileType<Projectiles.ForgottenImpHalberd>();
        }
    }
}
