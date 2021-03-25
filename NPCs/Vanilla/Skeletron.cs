using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs {
    class Skeletron : GlobalNPC {
        public override bool PreAI(NPC npc) { //todo
            if ((npc.type == NPCID.SkeletronHead || npc.type == NPCID.SkeletronHand) && ModContent.GetInstance<tsorcRevampConfig>().RenameSkeletron) {
                npc.GivenName = "Gravelord Nito"; //this will not replace the text written to chat. that is handled in tsorcRevamp.cs
            }
            return true;
        }
    }
}
