using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    /// <summary>
    /// Visible buildup marker for poison. A full meter applies vanilla Poisoned for PoisonDurationTicks.
    /// Poison immunity (Bezoar etc.) blocks the buildup too. The meter is owned by the victim's client,
    /// so <see cref="Apply"/> must be called on that machine (hostile OnHitPlayer, local contact checks).
    /// </summary>
    public class PoisonBuildup : ModBuff
    {
        public const int MaximumBuildup = 100;
        public const int DecayIntervalTicks = 60;
        public const int MarkerDurationTicks = 120 * 60;
        public const int PoisonDurationTicks = 60 * 60;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            int currentBuildup = Main.LocalPlayer.GetModPlayer<PoisonPlayer>().PoisonLevel;
            int poisonSeconds = PoisonDurationTicks / 60;
            tip = Description.WithFormatArgs(currentBuildup, MaximumBuildup, poisonSeconds).Value;
        }

        /// <summary>Adds poison buildup. Ignored off the victim's own machine and while immune to Poisoned.</summary>
        public static void Apply(Player player, int buildup)
        {
            if (player == null || !player.active || player.dead || buildup <= 0)
            {
                return;
            }

            if (player.whoAmI != Main.myPlayer || player.buffImmune[BuffID.Poisoned])
            {
                return;
            }

            player.GetModPlayer<PoisonPlayer>().AddBuildupLocal(buildup);
        }
    }
}
