using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.FriendlyPuppets;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    /// <summary>
    /// The Mimic Tear's lifetime. While the owner's mimic exists this refreshes itself; cancel it (right-click) and
    /// the server despawns the mimic (MimicTear.AI: "dismissed"); if the mimic dies it runs out two ticks later.
    /// Applied with MimicTear.SummonBuffGraceTicks, which it simply counts down until the mimic first appears.
    /// </summary>
    public class MimicTearBuff : ModBuff
    {
        // PLACEHOLDER sprite (copy of ArcherSpiritBuff) — replace with bespoke art.

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true; // Permanent like a vanilla minion - no countdown to show.
        }

        public override void Update(Player player, ref int buffIndex)
        {
            int mimicType = ModContent.NPCType<MimicTear>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                bool ownedMimic = npc.active && npc.type == mimicType && (int)npc.ai[0] == player.whoAmI;

                if (ownedMimic)
                {
                    // Held at 2 rather than refreshed to a long time, so it ends right after the mimic does.
                    player.buffTime[buffIndex] = 2;
                    return;
                }
            }
        }
    }
}
