using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // PLACEHOLDER sprite: copy of Madness.png, for the user to replace.
    // One-time 200% max-health damage is resolved by BlightPlayer at the threshold.
    public class Blight : ModBuff
    {
        public const int DurationTicks = 270;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            Main.persistentBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
}
