using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    // The Cleric of Sorrow's dying curse (Last Rites). Applied to nearby enemies for 30s when the cleric
    // dies below 5% health; while it holds, the afflicted enemy's attacks inflict Cursed Inferno on the
    // player (see tsorcRevampGlobalNPC.SorrowfulCurse + OnHitPlayer). Mirrors PlaguesmithBuff.
    public class SorrowfulCurseBuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff_" + Terraria.ID.BuffID.CursedInferno;

        public override void SetStaticDefaults()
        {
            /*Main.debuff[Type] = false; these 3 are generally unnecessary to bother with on an enemy debuff, but leaving it here for clarification
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;*/
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<NPCs.tsorcRevampGlobalNPC>().SorrowfulCurse = true;
        }
    }
}
