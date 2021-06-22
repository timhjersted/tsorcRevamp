using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies {
    class VampireBat : ModNPC {
        public override void SetDefaults() {
            npc.width = 22;
            npc.height = 18;
            npc.aiStyle = 14;
            aiType = NPCID.CaveBat;
            npc.timeLeft = 750;
            npc.damage = 88;
            npc.defense = 70;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath4;
            npc.lifeMax = 2092;
            npc.scale = 1;
            npc.knockBackResist = 0.5f;
            npc.value = 650;
            animationType = NPCID.CaveBat;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            Player p = spawnInfo.player;
            if (tsorcRevampWorld.SuperHardMode) {
                if (p.ZoneDungeon) {
                    return 0.125f;
                }
                else if (Underworld(p)) {
                    return 0.0667f;
                }
            }
            return 0;
        }

        public override void AI() {
            base.AI();
        }

        public override void NPCLoot() {
            base.NPCLoot();
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            target.AddBuff(ModContent.BuffType<Buffs.SlowedLifeRegen>(), 3600);
            target.AddBuff(BuffID.Poisoned, 3600);
            target.AddBuff(BuffID.Bleeding, 3600);
        }
    }
}
