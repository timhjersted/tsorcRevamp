using MonoMod.Cil;
using Mono.Cecil.Cil;
using Terraria;

using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace tsorcRevamp.NPCs {
    class Skeletron : GlobalNPC {
        public override bool PreAI(NPC npc) { //todo
            if (npc.type == NPCID.SkeletronHead || npc.type == NPCID.SkeletronHand) { //i am well aware that it says "Skeletron has awoken!" if Skeletron is 
                npc.GivenName = "Gravelord Nito"; //spawned via the old man / clothier. i am not familiar enough with IL editing to fix that. 
            }
            return true;
        }
    }
}
