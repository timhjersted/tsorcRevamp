using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // PLACEHOLDER sprite (copy of BlightBuildup.png) — replace with bespoke art
    /// <summary>
    /// Visible buildup marker for Miasma. Attacks should call <see cref="Apply"/> instead of
    /// adding this buff directly so the buildup value stays server-authoritative in multiplayer.
    /// </summary>
    public class MiasmaBuildup : ModBuff
    {
        public const int MaximumBuildup = 100;
        public const int DefaultBuildupPerHit = 15;
        public const int DecayIntervalTicks = 60;
        public const int MarkerDurationTicks = 120 * 60;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            int currentBuildup = Main.LocalPlayer.GetModPlayer<MiasmaPlayer>().MiasmaLevel;
            int drainSeconds = Miasma.DurationTicks / 60;
            tip = Description.WithFormatArgs(currentBuildup, MaximumBuildup, drainSeconds).Value;
        }

        /// <summary>
        /// Adds Miasma buildup from a hit. The victim client reports it in multiplayer; the server
        /// owns the threshold check and the resulting Miasma drain.
        /// </summary>
        public static void Apply(Player player, int buildup = DefaultBuildupPerHit)
        {
            if (player == null || !player.active || player.dead || buildup <= 0)
            {
                return;
            }

            player.GetModPlayer<MiasmaPlayer>().RequestBuildup(buildup);
        }
    }
}
