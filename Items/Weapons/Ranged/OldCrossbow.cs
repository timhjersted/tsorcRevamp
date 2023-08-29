using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Ammo;

namespace tsorcRevamp.Items.Weapons.Ranged;

class OldCrossbow : ModItem
{
    public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/Crossbow";
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Does random damage from 0 to 38" +
                            "\nMaximum damage is increased by damage modifiers.");
    }

    public override void SetDefaults()
    {
        Item.damage = 38;
        Item.width = 28;
        Item.height = 14;
        Item.knockBack = 4;
        Item.maxStack = 1;
        Item.DamageType = DamageClass.Ranged;
        Item.scale = 1;
        Item.crit = 16;
        Item.useAnimation = 45;
        Item.useTime = 45;
        Item.rare = ItemRarityID.White;
        Item.UseSound = SoundID.Item5;
        Item.useAmmo = ModContent.ItemType<Bolt>();
        Item.shoot = ProjectileID.PurificationPowder;
        Item.shootSpeed = 10;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.value = 9000;
        Item.noMelee = true;
    }
    public override void HoldItem(Player player)
    {
        player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
    }
}
