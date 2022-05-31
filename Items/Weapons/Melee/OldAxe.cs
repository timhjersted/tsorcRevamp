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
            Item.damage = 14;
            Item.width = 36;
            Item.height = 30;
            Item.knockBack = 6;
            Item.maxStack = 1;
            Item.melee = true;
            Item.scale = 1;
            Item.useAnimation = 20;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.value = 9000;
        }

        public override void HoldItem(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
