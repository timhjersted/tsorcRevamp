using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class PerfectDodge : ModBuff
    {
        public const int DurationTicks = 3 * 60;
        public const float StaminaRegenBonus = 50f;
        public const float CriticalStrikeChanceBonus = 15f;

        public override LocalizedText Description => base.Description.WithFormatArgs(
            StaminaRegenBonus,
            CriticalStrikeChanceBonus);

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += StaminaRegenBonus / 100f;
            player.GetCritChance(DamageClass.Generic) += CriticalStrikeChanceBonus;
        }
    }
}
