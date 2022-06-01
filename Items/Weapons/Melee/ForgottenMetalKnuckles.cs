using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenMetalKnuckles : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Iron knuckles that hit with extra weight.");
        }

        public override void SetDefaults() {
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Blue;
            Item.damage = 11;
            Item.height = 12;
            Item.knockBack = 5;
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 8;
            Item.useTime = 8;
            Item.UseSound = SoundID.Item1;
            Item.value = PriceByRarity.Blue_1;
            Item.width = 14;
        }
    }
}
