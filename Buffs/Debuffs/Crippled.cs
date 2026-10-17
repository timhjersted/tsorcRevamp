using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class Crippled : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        /// <summary>
        /// Crippled is not a LongerExpertDebuff, so AddBuff never stretches it. Callers that want it to scale like
        /// Broken Armor / Bleeding pass their Normal-mode ticks through here: x1 Normal, x2 Expert, x2.5 Master
        /// (Main.GameModeInfo.DebuffTimeMultiplier, the same multiplier vanilla applies to its own debuffs).
        /// </summary>
        public static int ScaledTicks(int normalModeTicks)
        {
            float multiplier = Main.GameModeInfo.DebuffTimeMultiplier;
            return (int)(normalModeTicks * multiplier);
        }

        public override void Update(Player P, ref int buffIndex)
        {
            P.GetModPlayer<tsorcRevampPlayer>().Crippled = true;
        }
    }
}