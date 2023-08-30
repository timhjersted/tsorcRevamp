using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Weapons
{
    public class LionheartMark : ModBuff
    {
        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks >= LionheartGunblade.MaxMarks)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarked = true;
            }
            if (npc.buffTime[buffIndex] == 1)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks = 0;
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarked = false;
            }
        }
    }
}