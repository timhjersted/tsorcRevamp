using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenKotetsu : ModItem { // post skeletron, sold by solaire

        public override void SetDefaults() {
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Orange;
            Item.damage = 30;
            Item.height = 40;
            Item.knockBack = 3;
            Item.melee = true;
            Item.useAnimation = 16;
            Item.useTime = 16;
            Item.UseSound = SoundID.Item1;
            Item.value = PriceByRarity.Orange_3;
            Item.width = 39;
            Item.useStyle = ItemUseStyleID.Swing;
        }
    }
}
