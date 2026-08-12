using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.Basilisk
{
    /// <summary>
    /// Shared natural-spawn safeguards for the basilisk family. Biome, depth, time, and
    /// progression rules remain in each basilisk's SpawnChance method so their individual
    /// weights stay easy to audit and can be extracted by the enemy-spawn DevData tool.
    /// </summary>
    internal static class BasiliskSpawnRules
    {
        private const int MaximumActiveOfEachType = 2;

        public static bool MeetsSharedRequirements(NPCSpawnInfo spawnInfo, int npcType)
        {
            return !spawnInfo.Water
                && spawnInfo.Player.townNPCs <= 0f
                && NPC.CountNPCS(npcType) < MaximumActiveOfEachType;
        }
    }
}
