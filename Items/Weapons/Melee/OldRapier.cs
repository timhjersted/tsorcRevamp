using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class OldRapier : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Does random damage from 0 to 16" +
                                "\nMaximum damage is increased by damage modifiers");
        }

        public override void SetDefaults()
        {
            item.damage = 16;
            item.width = 40;
            item.height = 40;
            item.knockBack = 3;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1;
            item.useAnimation = 12;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.Stabbing;
            item.useTime = 15;
            item.value = 4000;
        }

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
