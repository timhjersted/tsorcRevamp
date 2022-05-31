using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Magic {
    class DivineSpark : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Freaking huge magical laser");
        }
        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 2;
            Item.useTime = 1;
            Item.maxStack = 1;
            Item.damage = 170;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = 1;
            Item.mana = 3;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.magic = true;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.MasterBuster>();
        }
    }
}
