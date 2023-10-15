using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    public class IrradiatedDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = false;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Irradiated = true;

            if (Main.GameUpdateCount % 5 == 0)
            {
                Dust.NewDust(npc.Top, 10, 10, DustID.PoisonStaff);
            }
        }
    }
}