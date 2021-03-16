using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.NPCs.Friendly {
    class Chicken : ModNPC {
        public override void SetStaticDefaults() {
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.PossessedArmor];
        }
        public override void SetDefaults() {
            npc.knockBackResist = 0;
            npc.aiStyle = 3;
            npc.height = 28;
            npc.width = 20;
            npc.lifeMax = 3;
            npc.damage = 0;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.value = 30;
            animationType = NPCID.PossessedArmor;
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            return SpawnCondition.TownGeneralCritter.Chance * 0.2f;
        }

        public override void NPCLoot() {
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DeadChicken>());
        }
    }
}
