using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class DivineSpark : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("This mysterious device shoots a huge, uncontrollable wave beam");
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 2;
            Item.useTime = 1;
            Item.damage = 170;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = 1;
            Item.mana = 3;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.MasterBuster>();
        }
    }
}
