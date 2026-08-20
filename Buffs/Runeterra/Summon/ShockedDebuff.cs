using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class ShockedDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            BuffID.Sets.IsATagBuff[Type] = true; //DoT part in GlobalNPC is restricted by a bool that checks whether the NPC is immune to regular debuffs
            tsorcFactory.NonWhipTagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Shocked = true;

            if (Main.GameUpdateCount % 5 == 0 && !NPCID.Sets.ImmuneToRegularBuffs[npc.type])
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