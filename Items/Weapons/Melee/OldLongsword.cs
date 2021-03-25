using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class OldLongsword : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Does random damage from 0 to 16" +
                                "\nMaximum damage is increased by damage modifiers.");
        }

        public override void SetDefaults() {
            item.damage = 16;
            item.width = 44;
            item.height = 44;
            item.knockBack = 4;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1f;
            item.useAnimation = 19;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 7000;
        }

        public override void HoldItem(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
