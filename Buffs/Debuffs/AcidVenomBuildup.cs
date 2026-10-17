using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    /// <summary>
    /// Visible buildup marker for Acid Venom, mirroring <see cref="PoisonBuildup"/>. A full meter applies vanilla
    /// Venom for VenomDurationTicks. Venom immunity blocks the buildup too. The meter is owned by the victim's client,
    /// so <see cref="Apply"/> must be called on that machine (hostile OnHitPlayer, local contact checks).
    /// </summary>
    // PLACEHOLDER sprite (copy of PoisonBuildup) — replace with bespoke art
    public class AcidVenomBuildup : ModBuff
    {
        public const int MaximumBuildup = 100;
        public const int DecayIntervalTicks = 60;
        public const int MarkerDurationTicks = 120 * 60;
        // 17s of Venom (15 HP/s). Expert/Master double it to 34s on purpose: AcidVenomPlayer doesn't overwrite the timer.
        public const int VenomDurationTicks = 17 * 60;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            int currentBuildup = Main.LocalPlayer.GetModPlayer<AcidVenomPlayer>().VenomLevel;
            // Show what the player will actually get: vanilla stretches Venom by the mode's debuff multiplier
            // (x1 Normal, x2 Expert, x2.5 Master).
            float debuffTimeMultiplier = Main.GameModeInfo.DebuffTimeMultiplier;
            int venomSeconds = (int)(VenomDurationTicks * debuffTimeMultiplier) / 60;

            tip =Description.WithFormatArgs(currentBuildup, MaximumBuildup, venomSeconds).Value;
        }

        /// <summary>Adds acid venom buildup. Ignored off the victim's own machine and while immune to Venom.</summary>
        public static void Apply(Player player, int buildup)
        {
            if (player == null || !player.active || player.dead || buildup <= 0)
            {
                return;
            }

            if (player.whoAmI != Main.myPlayer || player.buffImmune[BuffID.Venom])
            {
                return;
            }

            player.GetModPlayer<AcidVenomPlayer>().AddBuildupLocal(buildup);
        }
    }
}
