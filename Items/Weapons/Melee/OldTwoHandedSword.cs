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
            Item.damage = 30;
            Item.width = 50;
            Item.height = 50;
            Item.knockBack = 5;
            Item.maxStack = 1;
            Item.melee = true;
            Item.scale = 1f;
            Item.useAnimation = 30;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 30;
            Item.value = 15000;
        }

        public override void HoldItem(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
