﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Hook = tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items.IHoldItemWhileDead;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items;

public interface IHoldItemWhileDead
{
    //public static readonly GlobalHookList<GlobalItem> Hook = ItemLoader.AddModHook(new GlobalHookList<GlobalItem>(typeof(Hook).GetMethod(nameof(HoldItemWhileDead))));
    public static readonly GlobalHookList<GlobalItem> Hook = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((Hook)g).HoldItemWhileDead));

    void HoldItemWhileDead(Item item, Player player);

    public static void Invoke(Item item, Player player)
    {
        foreach (Hook g in Hook.Enumerate(item))
        {
            g.HoldItemWhileDead(item, player);
        }
    }
}
