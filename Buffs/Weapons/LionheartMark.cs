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
            if (npc.buffTime[buffIndex] == 1)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks = 0;
            }
        }
    }
}