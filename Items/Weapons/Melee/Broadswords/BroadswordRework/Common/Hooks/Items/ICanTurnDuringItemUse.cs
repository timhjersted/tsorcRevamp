﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Hook = tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items.ICanTurnDuringItemUse;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items;

public interface ICanTurnDuringItemUse
{
    //public static readonly GlobalHookList<GlobalItem> Hook = ItemLoader.AddModHook(new GlobalHookList<GlobalItem>(typeof(Hook).GetMethod(nameof(CanTurnDuringItemUse))));

    public static readonly GlobalHookList<GlobalItem> Hook = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((Hook)g).CanTurnDuringItemUse));
    bool? CanTurnDuringItemUse(Item item, Player player);

    public static bool Invoke(Item item, Player player)
    {
        bool? globalResult = null;

        foreach (Hook g in Hook.Enumerate(item))
        {
            bool? result = g.CanTurnDuringItemUse(item, player);

            if (result.HasValue)
            {
                if (result.Value)
                {
                    globalResult = true;
                }
                else
                {
                    return false;
                }
            }
        }

        return globalResult ?? item.useTurn;
    }
}
