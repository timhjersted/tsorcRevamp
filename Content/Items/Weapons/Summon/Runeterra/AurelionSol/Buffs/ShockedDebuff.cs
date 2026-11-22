using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Bases;
using tsorcRevamp.NPCs;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Buffs
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
            var globalNPC = npc.GetGlobalNPC<RuneterraGauntletNPC>();
            globalNPC.Shocked = true;

            if (Main.GameUpdateCount % 5 == 0 && !NPCID.Sets.ImmuneToRegularBuffs[npc.type])
            {
                Dust.NewDust(npc.Center, 10, 10, DustID.Electric);
            }
            if (globalNPC.SuperShockDuration > 0)
            {
                globalNPC.SuperShockDuration -= 0.0167f;
            }
            if (npc.buffTime[buffIndex] == 1)
            {
                globalNPC.ShockMarks = 0;
            }
        }
    }
}