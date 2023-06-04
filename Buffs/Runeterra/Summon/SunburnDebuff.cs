using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class SunburnDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Sunburnt = true;

            if (Main.GameUpdateCount % 5 == 0)
            {
                Dust.NewDust(npc.Center, 20, 20, DustID.GoldFlame);
            }
        }
	}
}