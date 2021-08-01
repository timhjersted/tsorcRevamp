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
                                "\nMaximum damage is increased by damage modifiers." +
                                "\nLeft-click to stab like a spear, right-click to swing");
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
            item.useAnimation = 32;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 32;
            item.value = 7000;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {

                item.useStyle = ItemUseStyleID.SwingThrow;
                item.shoot = ProjectileID.None;
                item.noMelee = false;
                item.noUseGraphic = false;
            }
            else
            {
                item.noMelee = true;
                item.noUseGraphic = true;
                item.useStyle = ItemUseStyleID.HoldingOut;
                item.shoot = ModContent.ProjectileType<Projectiles.OldHalberd>();
                item.shootSpeed = 2.7f;
            }
            return base.CanUseItem(player);
        }

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
