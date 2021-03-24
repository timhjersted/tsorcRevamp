using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class Jawblade : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A blade of bone and fangs");
        }
        public override void SetDefaults() {
            item.width = 68;
            item.height = 76;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 30;
            item.useTime = 30;
            item.damage = 36;
            item.knockBack = 7;
            item.scale = 1.3f;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Orange;
            item.value = 27000;
            item.melee = true;
        }
    }
}
