using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // PLACEHOLDER sprite (copy of Blight.png) — replace with bespoke art
    /// <summary>
    /// Two-minute life drain from a full Miasma meter. The drain itself is applied by
    /// MiasmaPlayer.UpdateBadLifeRegen; this buff only marks the duration.
    /// </summary>
    public class Miasma : ModBuff
    {
        public const int DurationTicks = 120 * 60;
        public const int DrainPerSecondPreHardmode = 5;
        public const int DrainPerSecondHardmode = 10;
        public const int DrainPerSecondSuperHardmode = 15;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = Description.WithFormatArgs(CurrentDrainPerSecond()).Value;
        }

        // Drain scales with world progression. Both flags are world state, so every machine agrees.
        public static int CurrentDrainPerSecond()
        {
            if (tsorcRevampWorld.SuperHardMode)
            {
                return DrainPerSecondSuperHardmode;
            }

            if (Main.hardMode)
            {
                return DrainPerSecondHardmode;
            }

            return DrainPerSecondPreHardmode;
        }
    }
}
