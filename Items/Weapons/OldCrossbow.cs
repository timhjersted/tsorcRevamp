using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    class OldCrossbow : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Does random damage from 0 to 38" +
                                "\nMaximum damage is increased by damage modifiers.");
        }

        public override void SetDefaults()
        {
            item.damage = 38;
            item.width = 28;
            item.height = 14;
            item.knockBack = 4;
            item.maxStack = 1;
            item.ranged = true;
            item.scale = 1;
            item.useAnimation = 45;
            item.useTime = 45;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item5;
            item.useAmmo = mod.ItemType("Bolt");
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 10;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.value = 9000;
            item.noMelee = true;
        }
        public override void HoldItem(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
