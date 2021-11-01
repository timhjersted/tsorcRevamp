using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenKotetsu : ModItem {

        public override void SetDefaults() {
            item.autoReuse = true;
            item.useTurn = true;
            item.rare = ItemRarityID.Green;
            item.damage = 33;
            item.height = 40;
            item.knockBack = 3;
            item.melee = true;
            item.useAnimation = 18;
            item.useTime = 18;
            item.UseSound = SoundID.Item1;
            item.value = 200000;
            item.width = 39;
            item.useStyle = ItemUseStyleID.SwingThrow;
        }
    }
}
