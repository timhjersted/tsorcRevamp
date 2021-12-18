using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenLongSword : ModItem {

        public override void SetDefaults() {
            item.autoReuse = true;
            item.useTurn = true;
            item.rare = ItemRarityID.Blue;
            item.damage = 24;
            item.height = 34;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.knockBack = 4;
            item.melee = true;
            item.useAnimation = 22;
            item.useTime = 22;
            item.UseSound = SoundID.Item1;
            item.value = 100000;
            item.width = 34;
        }
    }
}
