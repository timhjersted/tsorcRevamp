using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // PLACEHOLDER sprite: copy of Madness.png, for the user to replace.
    // Frozen and the one-time max-HP damage are resolved by FrostPlayer at the threshold.
    public class Frost : ModBuff
    {
        public const int FrozenDurationTicks = 2 * 60;
        public const int DurationTicks = 12 * 60;
        public const float StaminaRegenMultiplier = 0.9f;
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public override void Update(Player player, ref int buffIndex)
            => player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= StaminaRegenMultiplier;
    }
}
