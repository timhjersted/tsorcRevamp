using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class AwestruckDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Awestruck = true;

            if (Main.GameUpdateCount % 5 == 0)
            {
                Dust.NewDust(npc.Center, 20, 20, DustID.CosmicCarKeys);
            }
        }
    }
}