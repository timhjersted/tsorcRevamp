using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenKotetsu : ModItem { // post skeletron, sold by solaire

        public override void SetDefaults() {
            item.autoReuse = true;
            item.useTurn = true;
            item.rare = ItemRarityID.Orange;
            item.damage = 30;
            item.height = 40;
            item.knockBack = 3;
            item.melee = true;
            item.useAnimation = 16;
            item.useTime = 16;
            item.UseSound = SoundID.Item1;
            item.value = PriceByRarity.Orange_3;
            item.width = 39;
            item.useStyle = ItemUseStyleID.SwingThrow;
        }
    }
}
