using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class HolyWarElixir : ModItem
    {
        public static int Duration = 10;
        public static int Cooldown = 60;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Cooldown);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.height = 62;
            Item.consumable = true;
            Item.height = 34;
            Item.maxStack = 29;
            Item.rare = ItemRarityID.Pink;
            Item.useAnimation = 17;
            Item.UseSound = SoundID.Item3;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.value = 0;
            Item.width = 14;
            Item.buffType = ModContent.BuffType<Buffs.Invincible>();
            Item.buffTime = Duration * 60;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.ElixirCooldown>()))
            {
                return false;
            }
            return base.CanUseItem(player);
        }

        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.ElixirCooldown>(), (Cooldown + Duration) * 60);
            return base.UseItem(player);
        }
    }
}
