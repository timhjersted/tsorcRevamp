using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs
{
    [Autoload(false)] //no longer used
    public class UrumiDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByUrumi = true;
        }
    }
}