using System;
using Terraria;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// One logical move in an enemy's combo pool. Keys are local to that enemy; positive projectile types are
    /// convenient for AddAttack moves, while negative keys identify bespoke melee or timeline attacks.
    /// </summary>
    public readonly struct CombatComboMove
    {
        public int Key { get; }
        public float MinimumRange { get; }
        public float MaximumRange { get; }
        public bool CanRepeat { get; }
        public float Weight { get; }

        public CombatComboMove(int key, float minimumRange, float maximumRange, bool canRepeat = true, float weight = 1f)
        {
            Key = key;
            MinimumRange = Math.Max(0f, minimumRange);
            MaximumRange = Math.Max(MinimumRange, maximumRange);
            CanRepeat = canRepeat;
            Weight = Math.Max(0f, weight);
        }

        public bool IsInRange(NPC npc)
        {
            if (!npc.HasValidTarget)
            {
                return false;
            }

            float distance = npc.Distance(Main.player[npc.target].Center);
            return distance >= MinimumRange && distance <= MaximumRange;
        }

        public static CombatComboMove AnyRange(int key, bool canRepeat = true, float weight = 1f)
        {
            return new CombatComboMove(key, 0f, float.MaxValue, canRepeat, weight);
        }
    }

    /// <summary>Shared local keys used by the small humanoid melee vocabulary.</summary>
    public static class CombatComboMoveKey
    {
        public const int CloseHopMelee = -1;
        public const int LongHopMelee = -2;
    }
}
