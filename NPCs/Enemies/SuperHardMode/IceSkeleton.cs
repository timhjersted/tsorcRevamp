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
            npc.knockBackResist = .1f;
            npc.value = 1630;
            npc.aiStyle = 3;
            npc.timeLeft = 750;
            npc.damage = 60;
            npc.defense = 73;
            npc.HitSound = SoundID.NPCHit2;
            npc.DeathSound = SoundID.NPCDeath2;
            npc.lifeMax = 1300;
            npc.scale = 1;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            Player p = spawnInfo.player;
            if (tsorcRevampWorld.SuperHardMode && p.position.X > Main.maxTilesX * 0.7f) {
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

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
            if (Main.rand.Next(2) == 0) {
                target.AddBuff(BuffID.Frozen, 180);
            }
        }
    }
}
