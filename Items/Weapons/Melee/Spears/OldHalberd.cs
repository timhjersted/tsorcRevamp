using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
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
            Item.damage = 35;
            Item.width = 60;
            Item.height = 60;
            Item.knockBack = 6;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1f;
            Item.useAnimation = 32;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 32;
            Item.value = 7000;
            Item.shootSpeed = 2.7f;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {

                Item.useStyle = ItemUseStyleID.Swing;
                Item.shoot = ProjectileID.None;
                Item.noMelee = false;
                Item.noUseGraphic = false;
            }
            else
            {
                Item.noMelee = true;
                Item.noUseGraphic = true;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ModContent.ProjectileType<Projectiles.Spears.OldHalberd>();
            }
            return base.CanUseItem(player);
        }

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
