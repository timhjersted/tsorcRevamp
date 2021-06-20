using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using tsorcRevamp.Projectiles.Enemy;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies {
    class Parasprite : ModNPC {
        public override void SetDefaults() {
            npc.width = 12;
            npc.height = 12;
            npc.aiStyle = 22;
            npc.damage = 60;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.lifeMax = 10;
            npc.friendly = false;
            npc.noTileCollide = false;
            npc.noGravity = true;
            npc.knockBackResist = 0;
            npc.value = 50;
            Main.npcFrameCount[npc.type] = 7;
        }
        int spriteColor;
        int timer;
        bool init;


        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            bool underground = (spawnInfo.player.position.Y >= (Main.maxTilesY / 2.43309f) * 16); //magic number
            float chance = 0;

            if (spawnInfo.player.ZoneHoly && underground && (NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3)) { //it's spawning on the surface for some reason too
                chance = 0.1f;
            }
            if (Main.hardMode && Sky(spawnInfo.player)) {
                chance = 0.2f;
            }
            return chance;
        }

        public override void AI() { // some stuff has been shuffled around, since there's only 1 parasprite enemy instead of 4 (lol)
            npc.velocity.Y += Main.rand.Next(-10, 10) / 8;
            npc.velocity.X += Main.rand.Next(-10, 10) / 20;
            if (npc.ai[3] == 0) { // check if a sprite is allowed to spawn copies (see below)
                timer++;
                if (timer >= 300) {
                    int totalParasprites = 0;
                    for (int i = 0; i < 200; i++) {
                        if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Parasprite>()) {
                            totalParasprites++;
                        }
                    }
                    if (totalParasprites < 20) {
                        int spawner = Main.rand.Next(2); // decide if a sprite is allowed to spawn more copies. only 1 in 2 parasprites can spawn copies, for lag reduction...
                        int NewSprite = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<Parasprite>(), 0, 0, 0, 0, spawner);
                        Main.npc[NewSprite].velocity.X = -npc.velocity.X * 2;
                    }
                    timer = 0 - Main.rand.Next(200); // ...but they can spawn copies more frequently to make up for it
                } 
            }
        }

        public override bool PreAI() {
            if (!init) {
                spriteColor = (Main.rand.Next(3) * 2) + 1;
                init = true;
            }
            return true;
        }
        public override void FindFrame(int frameHeight) { // and this feels like magic
            npc.frameCounter++;
            if (npc.frameCounter == 2) {
                npc.frame.Y = spriteColor * frameHeight;
            }
            if (npc.frameCounter == 4) {
                npc.frame.Y = (spriteColor + 1) * frameHeight;
                npc.frameCounter = 0;
            }
            if (npc.velocity.X < 0) { npc.spriteDirection = -1; }
            if (npc.velocity.X > 0) { npc.spriteDirection = 1; }
        }
    }
}
