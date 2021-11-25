using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode.SerpentOfTheAbyss {
    class SerpentOfTheAbyssTail : ModNPC {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Serpent of the Abyss");
        }

        public override void SetDefaults() {
            npc.netAlways = true;
            npc.npcSlots = 1;
            npc.width = 21;
            npc.height = 14;
            npc.aiStyle = 6;
            npc.timeLeft = 750;
            npc.damage = 120;
            npc.defense = 228;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.lavaImmune = true;
            npc.knockBackResist = 0;
            npc.lifeMax = 60000000;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.behindTiles = true;
            npc.value = 500; 
            
            bodyTypes = new int[33];
            int bodyID = ModContent.NPCType<SerpentOfTheAbyssBody>();
            for (int i = 0; i < 33; i++)
            {
                bodyTypes[i] = bodyID;
            }
        }
        int[] bodyTypes;

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override void AI() {
            tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<SerpentOfTheAbyssHead>(), bodyTypes, ModContent.NPCType<SerpentOfTheAbyssTail>(), 35, .8f, 17, 0.25f, false, false, false, true, true);
        }
    }
}
