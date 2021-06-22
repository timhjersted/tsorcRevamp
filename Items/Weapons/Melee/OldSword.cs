using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class OldSword : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Does random damage from 0 to 14" +
                                "\nMaximum damage is increased by damage modifiers.");
        }
        public override void SetDefaults() {
            item.damage = 14;
            item.width = 44;
            item.height = 48;
            item.knockBack = 4;
            item.melee = true;
            item.scale = 1f;
            item.useAnimation = 17;
            item.autoReuse = true;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 3000;
        }
        public override void HoldItem(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
