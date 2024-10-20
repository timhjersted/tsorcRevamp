﻿using MonoMod.Cil;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Core.ItemComponents;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee;

public sealed class ItemMeleeCooldownReplacement : ItemComponent
{

    private static readonly Dictionary<int, bool> isDebugCheckCache = new();
    public override void Load()
    {
        // Disable attackCD for melee whenever this component is present on the held item and enabled.
        IL_Player.ItemCheck_MeleeHitNPCs += context =>
        {
            var il = new ILCursor(context);
            Assembly assembly = typeof(ModLoader).Assembly;
            //bool debugAssembly = OverhaulMod.TMLAssembly.IsDebugAssembly();

            int hash = assembly.GetHashCode();
            bool debugAssembly = false;

            if (isDebugCheckCache.TryGetValue(hash, out bool result))
            {
                debugAssembly = true;
            }

            var attribute = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();

            isDebugCheckCache[hash] = result = attribute?.Configuration?.Contains("Debug") == true;

            debugAssembly = result;

            // Match:
            // for (int i = 0; i < 200; i++)
            // To get labels and 'i'.

            int npcIdLocalId = 0;
            ILLabel? continueLabel = null;

            il.GotoNext(
                i => i.MatchLdcI4(0),
                i => i.MatchStloc(out npcIdLocalId),
                i => i.MatchBr(out continueLabel)
            );

            // Match:
            // NPC npc = Main.npc[i];
            // To get the local.

            int npcLocalId = 0;

            il.GotoNext(
                i => i.MatchLdsfld(typeof(Main), nameof(Main.npc)),
                i => i.MatchLdloc(npcIdLocalId),
                i => i.MatchLdelemRef(),
                i => i.MatchStloc(out npcLocalId)
            );

            /*
			ILLabel? checkSkipLabel = null;
			ILLabel? tempLabel = null;

			il.HijackIncomingLabels();

			// Create local var
			int callResultLocalId = il.AddLocalVariable(typeof(bool?));

			// Load 'this' (player)
			il.Emit(OpCodes.Ldarg_0);
			// Load NPC
			//il.Emit(OpCodes.Ldsfld, context.Import(typeof(Main).GetField(nameof(Main.npc))));
			il.Emit(OpCodes.Ldloc, npcIdLocalId);
			//il.Emit(OpCodes.Ldelem_Ref);
			// Invoke delegate & store the result
			il.EmitDelegate(CanAttackNPC);
			il.Emit(OpCodes.Stloc, callResultLocalId);

			// If the result is true - Skip over original checks if true is returned
			il.Emit(OpCodes.Ldloc, callResultLocalId);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Beq, checkSkipLabel!);

			// If the result is false - 'continue;' in the loop.
			il.Emit(OpCodes.Ldloc, callResultLocalId);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Beq, continueLabel!);

			// On null - the original checks get to run.
			*/
        };
    }

    private static bool? CanAttackNPC(Player player, int npcId)
    {
        var npc = Main.npc[npcId];

        if (player.HeldItem?.IsAir == false && player.HeldItem.TryGetGlobalItem(out ItemMeleeCooldownReplacement replacement) && replacement.Enabled)
        {
            return true;
        }

        return false;
    }
}
