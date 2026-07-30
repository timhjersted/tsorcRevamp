using System;

namespace tsorcRevamp
{
    /// <summary>
    /// Describes which defensive actions an attack can bypass. Shield bypass is live for the Red Knight B3 pilot;
    /// dodge-roll bypass remains reserved for a later yellow-telegraph system.
    /// </summary>
    [Flags]
    public enum AttackDefenseTraits : ushort
    {
        None = 0,
        BypassesActiveShield = 1 << 0,
        IgnoresDodgeRollIFrames = 1 << 1,
    }
}
