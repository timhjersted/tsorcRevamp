using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class OldTwoHandedSword : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Does random damage from 0 to 30" +
                                "\nMaximum damage is increased by damage modifiers.");
        }

        public override void SetDefaults() {
            item.damage = 30;
            item.width = 50;
            item.height = 50;
            item.knockBack = 5;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1f;
            item.useAnimation = 28;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 15000;
        }

        public override void HoldItem(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
