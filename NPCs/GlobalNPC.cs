using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using tsorcRevamp.Items;
using static tsorcRevamp.tsorcRevampPlayer;


namespace tsorcRevamp.NPCs {
    class tsorcRevampGlobalNPC : GlobalNPC {



        public override void NPCLoot(NPC npc) {
            
            if (npc.lifeMax > 5 && npc.value >= 10f) { //stop zero-value souls from dropping

                float enemyValue;

                if (Main.expertMode) { //npc.value is the amount of coins they drop
                    enemyValue = (int)npc.value / 25; //all enemies drop more money in expert mode, so the divisor is larger to compensate
                } 
                else {
                    enemyValue = (int)npc.value / 10;
                }

                if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing) {
                    enemyValue *= 1.25f;
                }
                Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), (int)(enemyValue));
            }
        }
    }
}
