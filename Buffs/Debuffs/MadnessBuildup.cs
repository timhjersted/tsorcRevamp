using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    /// <summary>
    /// Visible buildup marker for Madness. Attacks should call <see cref="Apply"/> instead of
    /// adding this buff directly so the buildup value remains server-authoritative in multiplayer.
    /// </summary>
    public class MadnessBuildup : ModBuff
    {
        public const int MaximumBuildup = 100;
        public const int DefaultBuildupPerHit = 25;
        public const int DefaultDurationTicks = 15 * 60;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            int buildup = Main.LocalPlayer.GetModPlayer<MadnessPlayer>().MadnessLevel;
            tip = base.Description.WithFormatArgs(MaximumBuildup, buildup).Value;
        }

        /// <summary>
        /// Adds Madness buildup from a hit. The victim client reports the hit in multiplayer;
        /// the server owns the threshold check, damage, stagger, and resulting Madness buff.
        /// </summary>
        public static void Apply(Player player, int buildup = DefaultBuildupPerHit,
            int durationTicks = DefaultDurationTicks)
        {
            if (player == null || !player.active || player.dead || buildup <= 0)
            {
                return;
            }

            player.GetModPlayer<MadnessPlayer>().RequestBuildup(buildup, durationTicks);
        }
    }
}
