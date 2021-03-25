using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class RTQ2Sword : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Causes stars to rain from the sky" +
                                "\nMagnetic to stars.");
        }

        public override void SetDefaults() {
            item.width = 14;
            item.height = 28;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 25;
            item.useTime = 5;
            item.damage = 46;
            item.knockBack = 5f;
            item.autoReuse = true;
            item.UseSound = SoundID.Item9;
            item.rare = ItemRarityID.Blue;
            item.shoot = ProjectileID.Starfury;
            item.shootSpeed = 12;
            item.mana = 14;
            item.value = 200000;
            item.magic = true;
        }
    }
}
