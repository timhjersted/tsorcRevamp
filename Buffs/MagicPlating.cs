using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class MagicPlatingLight : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Light Magic Plating");
            Description.SetDefault("Damage is reduced by 5%");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.endurance += 0.05f;
        }

    }
    class MagicPlatingMedium : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Magic Plating");
            Description.SetDefault("Damage is reduced by 10%");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.endurance += 0.1f;
            player.buffImmune[ModContent.BuffType<MagicPlatingLight>()] = true;
        }
    }
    class MagicPlatingHard : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hard Magic Plating");
            Description.SetDefault("Damage is reduced by 15%");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.endurance += 0.15f;
            player.buffImmune[ModContent.BuffType<MagicPlatingLight>()] = true;
            player.buffImmune[ModContent.BuffType<MagicPlatingMedium>()] = true;
        }
    }
}