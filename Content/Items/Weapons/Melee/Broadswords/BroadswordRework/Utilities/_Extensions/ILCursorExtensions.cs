using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities._Extensions;

internal static class ILCursorExtensions
{
    public static ILCursor HijackIncomingLabels(this ILCursor cursor)
    {
        var incomingLabels = cursor.IncomingLabels.ToArray();

        cursor.Emit(OpCodes.Nop);

        foreach (var incomingLabel in incomingLabels)
        {
            incomingLabel.Target = cursor.Prev;
        }

        return cursor;
    }

}
