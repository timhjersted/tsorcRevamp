using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode.SerpentOfTheAbyss {
    class SerpentOfTheAbyssBody : ModNPC {

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
            npc.damage = 105;
            npc.defense = 158;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath5;
            npc.lavaImmune = true;
            npc.knockBackResist = 0;
            npc.lifeMax = 15000;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.behindTiles = true;
            npc.value = 460;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            return base.SpawnChance(spawnInfo);
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }
        public override void AI() {
            if (!Main.npc[(int)npc.ai[1]].active) {
                npc.life = 0;
                npc.HitEffect(0, 10.0);
                npc.active = false;
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            if (Main.rand.Next(2) == 0) {
                target.AddBuff(BuffID.CursedInferno, 180);
                target.AddBuff(ModContent.BuffType<Buffs.SlowedLifeRegen>(), 1800);
            }
        }
    }
}
