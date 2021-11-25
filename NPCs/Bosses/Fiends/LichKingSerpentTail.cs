using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Fiends
{
    class LichKingSerpentTail : ModNPC
    {
        public override void SetDefaults()
        {
            animationType = 10;
            npc.netAlways = true;
            npc.npcSlots = 1;
            npc.width = 21;
            npc.height = 14;
            npc.aiStyle = 6;
            npc.timeLeft = 750;
            npc.damage = 90;
            npc.defense = 18;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.lavaImmune = true;
            npc.knockBackResist = 0;
            npc.lifeMax = 60000000;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.behindTiles = true;
            npc.value = 500;
            npc.buffImmune[BuffID.Confused] = true;

            bodyTypes = new int[43];
            int bodyID = ModContent.NPCType<LichKingSerpentBody>();
            for (int i = 0; i < 43; i++)
            {
                bodyTypes[i] = bodyID;
            }
        }
        int[] bodyTypes;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lich King Serpent");
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.defense = npc.defense += 12;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<LichKingSerpentHead>(), bodyTypes, ModContent.NPCType<LichKingSerpentTail>(), 45, .8f, 22, 0.25f, false, false, false, true, true); //30f was 10f
        }
    }
}