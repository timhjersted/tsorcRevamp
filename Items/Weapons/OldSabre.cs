using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    class OldSabre : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Does random damage from 0 to 14" +
                                "\nMaximum damage is increased by damage modifiers.");
        }
        public override void SetDefaults()
        {
            item.damage = 14;
            item.width = 34;
            item.height = 38;
            item.knockBack = 4;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1f;
            item.useAnimation = 17;
            item.autoReuse = true;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 6000;
        }
        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
