using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class OldBroadsword : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Does random damage from 0 to 26" +
                                "\nMaximum damage is increased by damage modifiers.");
        }

        public override void SetDefaults()
        {
            Item.damage = 26;
            Item.width = 44;
            Item.height = 44;
            Item.knockBack = 4;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = .8f;
            Item.useAnimation = 17;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = 7000;
        }

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
