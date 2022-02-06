using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode {
    class IceSkeleton : ModNPC {
        public override void SetDefaults() {
            Main.npcFrameCount[npc.type] = 15;
            npc.npcSlots = 1;
            animationType = NPCID.AngryBones;
            npc.width = 18;
            npc.height = 40;
            npc.knockBackResist = .3f;
            npc.value = 1630;
            npc.timeLeft = 750;
            npc.damage = 120;
            npc.defense = 73;
            npc.HitSound = SoundID.NPCHit2;
            npc.DeathSound = SoundID.NPCDeath2;
            npc.lifeMax = 2000;
            npc.scale = 1;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.IceSkeletonBanner>();
        }

        int dashCounter = 0;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(npc, 5, 0.1f, enragePercent: 0.95f, enrageTopSpeed: 10);
            tsorcRevampAIs.LeapAtPlayer(npc, 5, 5, 4, 200);
            dashCounter++;

            if(dashCounter > 120)
            {
                UsefulFunctions.DustRing(npc.Center, 32, DustID.BlueCrystalShard, 12, 4);
                Lighting.AddLight(npc.Center, Color.Orange.ToVector3() * 5);
            }

            if(dashCounter > 150 && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
            {
                npc.velocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 15);
                dashCounter = 0;
            }
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            Player p = spawnInfo.player;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.spawnTileX > Main.maxTilesX * 0.7f) {
                if (p.ZoneDirtLayerHeight) {
                    if (!Main.dayTime) { return 0.2f; }
                    else return 0.067f;
                }
                else if (p.ZoneRockLayerHeight) { return 0.1f; }
            }
            return 0f;
        }

        public override void HitEffect(int hitDirection, double damage) {
            if (npc.life <= 0) {
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ice Skelly Head"), 1.1f);
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ice Skelly Vert"), 1.1f);
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ice Skelly Vert"), 1.1f);
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ice Skelly Piece"), 1.1f);
                Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ice Skelly Piece"), 1.1f);
            }
        }
    }
}
