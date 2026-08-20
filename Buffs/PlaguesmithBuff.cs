using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class PlaguesmithBuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff";
        public override void SetStaticDefaults()
        {
            /*Main.debuff[Type] = false; these 3 are generally unnecessary to bother with on an enemy debuff, but leaving it here for clarification
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;*/
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<NPCs.tsorcRevampGlobalNPC>().PlaguesmithBuff = true;
        }
    }
}