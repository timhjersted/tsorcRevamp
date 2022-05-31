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
            NPC.width = 12;
            NPC.height = 12;
            NPC.aiStyle = 22;
            NPC.damage = 30;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 5;
            NPC.friendly = false;
            NPC.noTileCollide = false;
            NPC.noGravity = true;
            NPC.knockBackResist = 0;
            NPC.value = 50;
            Main.npcFrameCount[NPC.type] = 7;
            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.ParaspriteBanner>();
        }
        int spriteColor;
        int timer;
        bool init;


        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            bool underground = (spawnInfo.Player.position.Y >= (Main.maxTilesY / 2.43309f) * 16); //magic number
            float chance = 0;
            Player p = spawnInfo.Player;

            if (spawnInfo.Player.ZoneHallow && underground && NPC.downedBoss3) { //it's spawning on the surface for some reason too
                chance = 0.1f;
            }
            if (Main.hardMode && Sky(p) && NoSpecialBiome(p)) {
                chance = 0.2f;
            }
            return chance;
        }

        public override void AI() { // some stuff has been shuffled around, since there's only 1 parasprite enemy instead of 4 (lol)
            NPC.velocity.Y += Main.rand.Next(-10, 10) / 8;
            NPC.velocity.X += Main.rand.Next(-10, 10) / 20;
            if (NPC.ai[3] == 0) { // check if a sprite is allowed to spawn copies (see below)
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
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int NewSprite = NPC.NewNPC((int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Parasprite>(), 0, 0, 0, 0, spawner);
                            Main.npc[NewSprite].velocity.X = -NPC.velocity.X * 2;
                        }
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
            NPC.frameCounter++;
            if (NPC.frameCounter == 2) {
                NPC.frame.Y = spriteColor * frameHeight;
            }
            if (NPC.frameCounter == 4) {
                NPC.frame.Y = (spriteColor + 1) * frameHeight;
                NPC.frameCounter = 0;
            }
            if (NPC.velocity.X < 0) { NPC.spriteDirection = -1; }
            if (NPC.velocity.X > 0) { NPC.spriteDirection = 1; }
        }
    }
}
