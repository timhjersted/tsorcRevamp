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
            Item.damage = 16;
            Item.width = 40;
            Item.height = 40;
            Item.knockBack = 3;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1;
            Item.useAnimation = 12;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Thrust;
            Item.useTime = 15;
            Item.value = 4000;
        }

        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
