using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class ScorchingDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Scorched = true;

            if (Main.GameUpdateCount % 5 == 0)
            {
                Dust.NewDust(npc.Center, 10, 10, DustID.GoldFlame);
            }
            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().SuperScorchDuration > 0)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().SuperScorchDuration -= 0.0167f;
            }
            if (npc.buffTime[buffIndex] == 1)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ScorchMarks = 0;
            }
        }
    }
}