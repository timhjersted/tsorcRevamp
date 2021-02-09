using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using tsorcRevamp.Items;


namespace tsorcRevamp.NPCs {
    class tsorcRevampGlobalNPC : GlobalNPC {
        public override void NPCLoot(NPC npc) {
            
            if (npc.lifeMax >= 5 && npc.value >= 10f) { //stop zero-value souls from dropping
                int enemyValue;
                if (Main.expertMode) {
                    enemyValue = (int)npc.value / 25;
                }
                else {
                    enemyValue = (int)npc.value / 10;
                }
                Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), enemyValue);
            }
        }
    }
}
