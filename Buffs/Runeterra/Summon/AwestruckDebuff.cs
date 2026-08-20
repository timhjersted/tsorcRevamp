using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class AwestruckDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            BuffID.Sets.IsATagBuff[Type] = true; //DoT part in GlobalNPC is restricted by a bool that checks whether the NPC is immune to regular debuffs
            tsorcFactory.NonWhipTagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Awestruck = true;

            if (Main.GameUpdateCount % 5 == 0 && !NPCID.Sets.ImmuneToRegularBuffs[npc.type])
            {
                Dust.NewDust(npc.Center, 20, 20, DustID.CosmicCarKeys);
            }
        }
    }
}