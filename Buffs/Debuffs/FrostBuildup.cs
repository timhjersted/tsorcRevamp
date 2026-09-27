using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // PLACEHOLDER sprite: copy of MadnessBuildup.png, for the user to replace.
    public class FrostBuildup : ModBuff
    {
        public const int MaximumBuildup = 100;
        public const int BuildupPerHit = 44;
        public const int DecayIntervalTicks = 60;
        public const int DecayPerInterval = 4;
        // Long enough for even99 buildup to drain naturally; the marker clears at zero.
        public const int MarkerDurationTicks = 120 * 60;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            FrostPlayer frost = Main.LocalPlayer.GetModPlayer<FrostPlayer>();
            tip = Description.WithFormatArgs(frost.FrostLevel, frost.MovementSlowFraction * 100f).Value;
        }
        public static void Apply(Player player, int buildup = BuildupPerHit)
        {
            if (player == null || !player.active || player.dead || buildup <= 0) return;
            player.GetModPlayer<FrostPlayer>().RequestBuildup(buildup);
        }
    }
}
