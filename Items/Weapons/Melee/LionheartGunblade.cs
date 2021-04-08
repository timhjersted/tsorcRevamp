using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class LionheartGunblade : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("\"Of gunblades, the finest model is the Lionheart.\"");
        }
        public override void SetDefaults() {
            item.damage = 50;
            item.width = 66;
            item.height = 26;
            item.knockBack = 7;
            item.rare = ItemRarityID.Pink;
            item.scale = 1.1f;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 10;
            item.useAmmo = AmmoID.Bullet;
            item.ranged = true;
            item.melee = true;
            item.useAnimation = 15;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 45;
            item.value = 4600000;
        }
    }
}
