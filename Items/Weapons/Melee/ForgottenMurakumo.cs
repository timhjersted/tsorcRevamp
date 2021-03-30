using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenMurakumo : ModItem {

        public override void SetDefaults() {
            item.autoReuse = true;
            item.rare = ItemRarityID.Pink;
            item.damage = 45;
            item.height = 72;
            item.knockBack = 7;
            item.autoReuse = true;
            item.melee = true;
            item.useAnimation = 16;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 3000000;
            item.width = 48;
        }
    }
}
