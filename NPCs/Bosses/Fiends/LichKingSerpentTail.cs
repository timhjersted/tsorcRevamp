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
            npc.lifeMax = 600;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.behindTiles = true;
            npc.value = 500;
            npc.buffImmune[BuffID.Confused] = true;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lich King Serpent");
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.damage = (int)(npc.damage * 1.3 / 2);
            npc.defense = npc.defense += 12;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override void AI()
        {
            if (!Main.npc[(int)npc.ai[1]].active)
            {
                npc.life = 0;
                npc.HitEffect(0, 10.0);
                npc.active = false;
            }
            //npc.AI(true);
        }
    }
}