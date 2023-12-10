using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class ForgottenPearlSpear : ModItem
    {

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Item.damage = 52;
            Item.knockBack = 7f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 15;
            Item.useTime = 7;
            Item.shootSpeed = 7;
            //item.shoot = ProjectileID.DarkLance;

            Item.height = 50;
            Item.width = 50;

            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.value = PriceByRarity.Pink_5;
            Item.rare = ItemRarityID.Pink;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<ForgottenPearlSpearProj>();

        }

    }
}
