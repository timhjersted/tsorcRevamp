using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenMetalKnuckles : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Iron knuckles that hit with extra weight.");
        }

        public override void SetDefaults() {
            item.autoReuse = true;
            item.useTurn = true;
            item.rare = ItemRarityID.Pink;
            item.damage = 9;
            item.height = 12;
            item.knockBack = 5;
            item.melee = true;
            item.useAnimation = 8;
            item.useTime = 8;
            item.UseSound = SoundID.Item1;
            item.value = 15000;
            item.width = 14;
        }
    }
}
