using System.Text;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Opt-in for the DebugMode above-head attack readout drawn by
    /// <c>tsorcRevampSystems.DrawPuppetAttackDebug</c>. Implement this on any ModNPC with a bespoke
    /// attack state machine and its current move is displayed over its head during playtesting,
    /// along with the two moves before it, so you can always tell what just hit you.
    ///
    /// This exists so the HUD doesn't need a hand-written <c>else if (npc.ModNPC is X x)</c> branch
    /// per enemy - that chain had already reached four entries and adding the rest of the reworked
    /// roster would have pushed it past ten. PuppetNPC is still special-cased there because its
    /// label is composed from three separate sources (label / combo tag / phase name).
    /// </summary>
    internal interface IDebugAttackLabel
    {
        /// <summary>Friendly name of whatever the enemy is doing right now (never null/empty).</summary>
        string DebugAttackLabel { get; }
    }

    internal static class DebugLabels
    {
        /// <summary>
        /// Turns a PascalCase enum member into a readable label: <c>SunlightSlam</c> -> "Sunlight Slam",
        /// <c>ToxicGasNova</c> -> "Toxic Gas Nova". Lets an enemy's state enum drive the HUD directly
        /// instead of maintaining a parallel switch of display strings that drifts out of sync.
        /// </summary>
        internal static string Humanize(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            StringBuilder builder = new(name.Length + 8);
            for (int i = 0; i < name.Length; i++)
            {
                char character = name[i];
                // Only break on a capital that follows a non-capital, so runs like "AI" stay intact.
                if (i > 0 && char.IsUpper(character) && !char.IsUpper(name[i - 1]))
                {
                    builder.Append(' ');
                }
                builder.Append(character);
            }
            return builder.ToString();
        }
    }
}
