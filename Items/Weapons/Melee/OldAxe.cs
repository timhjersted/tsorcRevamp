using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class OldAxe : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Does random damage from 0 to 14" +
                                "\nMaximum damage is increased by damage modifiers.");
        }

        public override void SetDefaults() {
            item.damage = 14;
            item.width = 36;
            item.height = 30;
            item.knockBack = 6;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1;
            item.useAnimation = 20;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 20;
            item.value = 9000;
        }

        public override void HoldItem(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
