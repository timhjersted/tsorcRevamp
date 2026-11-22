using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    // PLACEHOLDER sprite (copy of MadnessBuildup) — replace with bespoke art
    /// <summary>
    /// Mark of Madness (Acolyte of Grand Occultist): while it lasts the player's Madness meter stops decaying
    /// (MadnessPlayer.PostUpdateBuffs) and every acolyte Madness hit adds double (AcolyteMadness.Apply).
    /// Applied by the marked player's own client from the MarkGlyph projectile.
    /// </summary>
    public class MadnessMarked : ModBuff
    {
        public const int DurationTicks = 8 * 60;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
}
