using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class RTQ2Sword : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("RTQ2 Sword");
            Tooltip.SetDefault("Causes stars to rain from the sky" +
                                "\nMagnetic to stars.");
        }

        public override void SetDefaults() {
            Item.width = 14;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 25;
            Item.useTime = 5;
            Item.damage = 46;
            Item.knockBack = 5f;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item9;
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ProjectileID.Starfury;
            Item.shootSpeed = 12;
            Item.mana = 14;
            Item.value = 200000;
            Item.DamageType = DamageClass.Magic;
        }
    }
}
