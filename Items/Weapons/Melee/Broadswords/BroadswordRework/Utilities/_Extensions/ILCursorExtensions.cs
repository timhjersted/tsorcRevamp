﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Linq;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities;

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
