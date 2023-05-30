using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs
{
    public class BiohazardDrain : ModBuff
    {
        //Generic texture since this buff is enemy-only
        public override string Texture => "Terraria/Images/Buff";

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().BiohazardDrain = true;
        }
    }
}
