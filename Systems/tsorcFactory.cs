using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems;

[ReinitializeDuringResizeArrays]
public static class tsorcFactory
{
    /// <summary>
    /// Special bool for tag debuffs that are supposed to proc Condition Overload since they aren't inflicted by whips
    /// </summary>
    public static bool[] NonWhipTagBuff = BuffID.Sets.Factory.CreateBoolSet(false);
}