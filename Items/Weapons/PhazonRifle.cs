using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class PhazonRifle : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Three round burst" +
                                "\nBeware; Phazon is EXTREMELY toxic.");
        }
        public override void SetDefaults() {
            item.width = 50;
            item.height = 18;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 12;
            item.useTime = 4;
            item.maxStack = 1;
            item.damage = 25;
            item.autoReuse = true;
            item.UseSound = SoundID.Item31;
            item.rare = ItemRarityID.LightRed;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 7;
            item.useAmmo = 14;
            item.noMelee = true;
            item.value = 300000;
            item.ranged = true;
            item.reuseDelay = 11;
            item.useAmmo = AmmoID.Bullet;
        }
    }
}
