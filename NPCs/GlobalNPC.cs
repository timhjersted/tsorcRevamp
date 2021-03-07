using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs {
    class tsorcRevampGlobalNPC : GlobalNPC {

        public override void SetDefaults(NPC npc)
        {
            if (npc.type == NPCID.TheHungryII)
            {
                npc.knockBackResist = 0f;
            }
        }

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

                // Consumable Soul drops ahead 
                // Jungle slime has enemyValue 50, therefore can drop any of the first 4 Consumable Souls, making them great for soul farming. ProudKnightSoul can be changed to (enemyValue > 50) if this is seen to be too OP.

                if ((enemyValue >= 1) && (enemyValue <= 50) && (Main.rand.NextFloat() < .015f)) // 1.5% chance of all enemies between enemyValue 1 and 50 dropping FadingSoul aka 1/75
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<FadingSoul>(), 1); // Zombies and eyes are 6 and 7 enemyValue, so will only drop FadingSoul
                }

                if ((enemyValue > 7) && (enemyValue <= 2000) && (Main.rand.NextFloat() < .02f)) // 2% chance of all enemies between enemyValue 8 and 2000 dropping LostUndeadSoul aka 1/50
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<LostUndeadSoul>(), 1); // Most pre-HM enemies fall into this category
                }

                if ((enemyValue > 20) && (enemyValue <= 5000) && (Main.rand.NextFloat() < .02f)) // 2% chance of all enemies between enemyValue 21 and 2000 dropping NamelessSoldierSoul aka 1/50
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<NamelessSoldierSoul>(), 1); // Most HM enemies fall into this category
                }

                if ((enemyValue >= 50) && (enemyValue <= 5000) && (Main.rand.NextFloat() < .015f)) // 1.5% chance of all enemies between enemyValue 50 and 5000 dropping ProudKnightSoul aka 1/75
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<ProudKnightSoul>(), 1);
                }
            }
        }
    }
}
