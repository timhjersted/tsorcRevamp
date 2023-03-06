using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    class OldLongbow : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Does random damage from 0 to 32" +
                                "\nMaximum damage is increased by damage modifiers."); */
        }

        public override void SetDefaults()
        {

            Item.damage = 32;
            Item.height = 66;
            Item.width = 16;
            Item.DamageType = DamageClass.Ranged;
            Item.autoReuse = true;
            Item.knockBack = 4f;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.rare = ItemRarityID.White;
            Item.scale = 0.9f;
            Item.shoot = AmmoID.Arrow;
            Item.shootSpeed = 7.5f;
            Item.useAmmo = AmmoID.Arrow;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = 50000;

        }
        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
