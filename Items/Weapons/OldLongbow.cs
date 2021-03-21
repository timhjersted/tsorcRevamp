using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    class OldLongbow : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Does random damage from 0 to 32" +
                                "\nMaximum damage is increased by damage modifiers.");
        }

        public override void SetDefaults()
        {

            item.damage = 32;
            item.height = 66;
            item.width = 16;
            item.ranged = true;
            item.knockBack = 4f;
            item.maxStack = 1;
            item.noMelee = true;
            item.rare = ItemRarityID.White;
            item.scale = 0.9f;
            item.shoot = AmmoID.Arrow;
            item.shootSpeed = 7.5f;
            item.useAmmo = AmmoID.Arrow;
            item.useAnimation = 25;
            item.useTime = 25;
            item.UseSound = SoundID.Item5;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.value = 50000;

        }
        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
