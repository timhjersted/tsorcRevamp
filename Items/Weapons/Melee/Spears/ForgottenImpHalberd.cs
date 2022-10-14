using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    class ForgottenImpHalberd : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A crescent shaped spear used by imps.");
        }

        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.knockBack = 6;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.rare = ItemRarityID.Pink;
            Item.shootSpeed = 7;
            Item.useAnimation = 16;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 8;
            Item.value = PriceByRarity.Pink_5;
            Item.height = 50;
            Item.width = 50;
            Item.shoot = ModContent.ProjectileType<Projectiles.Spears.ForgottenImpHalberd>();
        }
    }
}
