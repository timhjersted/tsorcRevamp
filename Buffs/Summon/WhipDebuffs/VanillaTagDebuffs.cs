using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Summon
{
    public class VanillaTagDebuffs : GlobalBuff
    {
        public override void Update(int type, NPC npc, ref int buffIndex)
        {
            if (type == BuffID.BlandWhipEnemyDebuff)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByLeatherWhip = true;
            }
            if (type == BuffID.ThornWhipNPCDebuff)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedBySnapthorn = true;
            }
            if (type == BuffID.BoneWhipNPCDebuff)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedBySpinalTap = true;
            }
            if (type == BuffID.FlameWhipEnemyDebuff)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByFirecracker = true;
            }
            if (type == BuffID.CoolWhipNPCDebuff)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByCoolWhip = true;
            }
            if (type == BuffID.SwordWhipNPCDebuff)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByDurendal = true;
            }
            if (type == BuffID.MaceWhipNPCDebuff)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByMorningStar = true;
            }
            if (type == BuffID.ScytheWhipEnemyDebuff)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByDarkHarvest = true;
            }
            if (type == BuffID.RainbowWhipNPCDebuff)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByKaleidoscope = true;
            }
        }
    }
}