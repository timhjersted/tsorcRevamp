using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Fiends
{
    class LichKingSerpentBody : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[npc.type] = 1;
            animationType = 10;
            npc.netAlways = true;
            npc.npcSlots = 1;
            npc.width = 21;
            npc.height = 14;
            npc.aiStyle = 6;
            npc.timeLeft = 750;
            npc.damage = 190;
            npc.defense = 28;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath5;
            npc.lavaImmune = true;
            npc.knockBackResist = 0;
            npc.lifeMax = 60000000;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.behindTiles = true;
            npc.value = 460; 
            
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

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.damage = (int)(npc.damage *  1.3 / tsorcRevampGlobalNPC.expertScale);
            npc.defense = npc.defense += 12;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        
        public override void AI()
        {

            tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<LichKingSerpentHead>(), bodyTypes, ModContent.NPCType<LichKingSerpentTail>(), 45, .8f, 22, 0.25f, false, false, false, true, true);
        }
        public override bool CheckActive()
        {
            return false;
        }

        public override void NPCLoot()
        {
            if (npc.life <= 0)
            {
                Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Lich King Serpent Body Gore"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Lich King Serpent Body Gore"), 1f);
            }
        }
    }
}