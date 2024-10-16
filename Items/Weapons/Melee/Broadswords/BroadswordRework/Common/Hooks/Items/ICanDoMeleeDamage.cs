﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Hook = tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items.ICanDoMeleeDamage;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items;

public interface ICanDoMeleeDamage
{
    //public static readonly GlobalHookList<GlobalItem> Hook = ItemLoader.AddModHook(new GlobalHookList<GlobalItem>(typeof(Hook).GetMethod(nameof(CanDoMeleeDamage))));
    public static readonly GlobalHookList<GlobalItem> Hook = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((Hook)g).CanDoMeleeDamage));

    bool CanDoMeleeDamage(Item item, Player player);

    public static bool Invoke(Item item, Player player)
    {
        foreach (Hook g in Hook.Enumerate(item))
        {
            if (!g.CanDoMeleeDamage(item, player))
            {
                return false;
            }
        }

        return true;
    }
}
