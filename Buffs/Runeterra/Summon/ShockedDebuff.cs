using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class ShockedDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Shocked = true; 

			if (Main.GameUpdateCount % 5 == 0)
            {
                Dust.NewDust(npc.Center, 10, 10, DustID.Electric);
            }
            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().SuperShockDuration > 0)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().SuperShockDuration -= 0.0167f;
            }
            if (npc.buffTime[buffIndex] == 1)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ShockMarks = 0;
            }
        }
	}
}