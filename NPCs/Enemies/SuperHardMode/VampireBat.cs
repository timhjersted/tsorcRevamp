using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode {
    class VampireBat : ModNPC {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Vampire Bat");
            Main.npcFrameCount[npc.type] = 8;
        }

        public override void SetDefaults() {
            npc.width = 48;
            npc.height = 36;
            npc.aiStyle = 14;
            aiType = NPCID.CaveBat;
            npc.timeLeft = 750;
            npc.damage = 44;
            npc.defense = 70;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath4;
            npc.lifeMax = 1050;
            npc.scale = 1;
            npc.knockBackResist = 0.5f;
            npc.value = 650;
            //animationType = NPCID.CaveBat;
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

        public int frame = 0;

        public override void FindFrame(int frameHeight)
        {
            npc.spriteDirection = npc.direction;

            if (++npc.frameCounter >= 4)
            {
                ++frame;
                npc.frame.Y = frame * frameHeight;
                npc.frameCounter = 0;

                if (frame >= 7)
                {
                    frame = 0;
                }
            }
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
