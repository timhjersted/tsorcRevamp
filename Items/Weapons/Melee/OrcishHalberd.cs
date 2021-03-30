using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class OrcishHalberd : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Does random damage from 0 to 32" +
                                "\nMaximum damage is increased by damage modifiers.");
        }

        public override void SetDefaults() {
            item.damage = 32;
            item.width = 48;
            item.height = 48;
            item.knockBack = 5;
            item.maxStack = 1;
            item.melee = true;
            item.useAnimation = 21;
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
