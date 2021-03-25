using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class OldHalberd : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Does random damage from 0 to 35" +
                                "\nMaximum damage is increased by damage modifiers.");
        }

        public override void SetDefaults()
        {
            item.damage = 35;
            item.width = 60;
            item.height = 60;
            item.knockBack = 6;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1f;
            item.useAnimation = 28;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 7000;
        }

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
