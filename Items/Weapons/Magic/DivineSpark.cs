using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Magic {
    class DivineSpark : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Freaking huge magical laser");
        }
        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 2;
            item.useTime = 1;
            item.maxStack = 1;
            item.damage = 170;
            item.autoReuse = true;
            item.rare = ItemRarityID.Blue;
            item.shootSpeed = 1;
            item.mana = 10;
            item.noMelee = true;
            item.value = 20000;
            item.magic = true;
            item.channel = true;
            item.shoot = ModContent.ProjectileType<Projectiles.MasterBuster>();
        }
    }
}
