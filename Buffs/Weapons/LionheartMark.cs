using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Buffs.Weapons
{
    public class LionheartMark : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            BuffID.Sets.IsATagBuff[Type] = true;
            tsorcFactory.NonWhipTagBuff[Type] = true;
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