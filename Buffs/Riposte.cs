using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    // Awarded by a perfect parry (tsorcRevampActiveShieldPlayer). Unlike PerfectDodge this grants nothing
    // passively — it is a one-shot charge spent by the next attack, so the whole effect lives in the hit
    // hooks on the shield player rather than in Update here.
    public class Riposte : ModBuff
    {
        /// <summary>How long the parry stays convertible. Short on purpose: a riposte is an immediate counter,
        /// so the reward is for stepping in rather than for having parried at some point.</summary>
        public const int DurationTicks = 60;

        /// <summary>Final-damage multiplier applied to the empowered attack.</summary>
        public const float DamageMultiplier = 1.5f;

        public override LocalizedText Description => base.Description.WithFormatArgs(
            (int)((DamageMultiplier - 1f) * 100f));

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
